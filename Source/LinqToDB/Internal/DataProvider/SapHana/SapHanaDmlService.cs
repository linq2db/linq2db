using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	/// <summary>
	/// SAP HANA DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
	/// <c>SELECT CURRENT_IDENTITY_VALUE()</c> (the builder appends <c>FROM DUMMY</c> for the from-less select);
	/// everything else falls back to the legacy command-splitting path.
	/// </summary>
	public sealed class SapHanaDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement.NeedsIdentity)
			{
				var idType   = factory.GetDbDataType(typeof(long));
				var idSelect = new SqlSelectStatement();

				idSelect.SelectQuery.Select.AddNew(factory.Function(idType, "CURRENT_IDENTITY_VALUE"));

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
	}
}
