using System;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeDmlService : DmlServiceBase
	{
		/// <summary>
		/// SQL CE DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
		/// <c>SELECT @@IDENTITY</c>; a truncate-with-reset becomes the truncate plus one
		/// <see cref="SqlCommandFragment.IdentityReseed"/> fragment per identity column
		/// (<c>ALTER TABLE … ALTER COLUMN … IDENTITY(1, 1)</c>). Everything else uses the base scenario.
		/// </summary>
		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				var fields = truncate.Table.IdentityFields;
				var steps  = new SqlCommandStep[fields.Count + 1];

				steps[0] = new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery };

				for (var i = 0; i < fields.Count; i++)
					steps[i + 1] = new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery, Fragment = SqlCommandFragment.IdentityReseed, FragmentFieldIndex = i };

				return new SqlCommandScenario { Steps = steps, OutcomeSteps = [0] };
			}

			if (statement.NeedsIdentity)
			{
				var idType   = factory.GetDbDataType(typeof(long));
				var idSelect = new SqlSelectStatement();

				idSelect.SelectQuery.Select.AddNew(new SqlExpression(idType, "@@IDENTITY"));

				return new SqlCommandScenario
				{
					Steps =
					[
						new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery },
						new SqlCommandStep { Statement = idSelect,  Kind = SqlStepKind.Scalar   },
					],
					OutcomeSteps = [1],
				};
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}

		// DB_E_NOTABLE. SqlCeException stores this code but exposes it incorrectly, so the
		// HResult check below is best-effort and will usually miss.
		const int DB_E_NOTABLE = unchecked((int)0x80040E37);

		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "SqlCeException"))
				return false;

			return HResultMatches(exception, DB_E_NOTABLE)
				|| exception.Message.Contains("specified table does not exist", StringComparison.OrdinalIgnoreCase);
		}
	}
}
