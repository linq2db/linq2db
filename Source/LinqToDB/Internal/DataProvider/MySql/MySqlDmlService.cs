using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.MySql
{
	/// <summary>
	/// MySQL DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
	/// <c>SELECT LAST_INSERT_ID()</c>; everything else falls back to the legacy command-splitting path.
	/// </summary>
	public sealed class MySqlDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement.NeedsIdentity)
			{
				var idType   = factory.GetDbDataType(typeof(long));
				var idSelect = new SqlSelectStatement();

				idSelect.SelectQuery.Select.AddNew(factory.Function(idType, "LAST_INSERT_ID"));

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

			return null;
		}
	}
}
