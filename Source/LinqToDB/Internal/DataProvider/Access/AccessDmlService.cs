using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessDmlService : DmlServiceBase
	{
		/// <summary>
		/// Access DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
		/// <c>SELECT @@IDENTITY</c>; a truncate-with-reset becomes the truncate plus one
		/// <see cref="SqlFragmentStatement"/> per identity column
		/// (<c>ALTER TABLE … ALTER COLUMN … COUNTER(1, 1)</c>). Everything else uses the base scenario.
		/// </summary>
		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				var fields = truncate.Table.IdentityFields;
				var steps  = new SqlCommandStep[fields.Count + 1];

				steps[0] = new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery };

				for (var i = 0; i < fields.Count; i++)
				{
					var reset = new SqlFragmentStatement(factory.Fragment(
						"ALTER TABLE {0} ALTER COLUMN {1} COUNTER(1, 1)",
						new SqlObjectNameExpression(truncate.Table.TableName, ConvertType.NameToQueryTable, truncate.Table.TableOptions),
						new SqlObjectNameExpression(new SqlObjectName(fields[i].PhysicalName), ConvertType.NameToQueryField)));

					steps[i + 1] = new SqlCommandStep { Statement = reset, Kind = SqlStepKind.NonQuery };
				}

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

		// DB_E_NOTABLE — "The specified table does not exist."
		const int DB_E_NOTABLE = unchecked((int)0x80040E37);

		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			// Access via OleDb — HResult is populated correctly.
			if (TypeOrMessageContains(exception, "OleDbException"))
			{
				return HResultMatches(exception, DB_E_NOTABLE)
					|| exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
			}

			// Access via ODBC — SQLSTATE 42S02 = "Base table or view not found".
			if (TypeOrMessageContains(exception, "OdbcException"))
			{
				return exception.Message.Contains("42S02",          StringComparison.Ordinal)
					|| exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}
	}
}
