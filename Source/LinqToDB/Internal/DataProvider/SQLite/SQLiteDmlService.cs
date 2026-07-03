using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	/// <summary>
	/// SQLite DML mechanics. An identity insert is split into the insert (non-query) plus a scalar
	/// <c>SELECT last_insert_rowid()</c>; everything else falls back to the legacy command-splitting path.
	/// </summary>
	public sealed class SQLiteDmlService : DmlServiceBase
	{
		// SQLite's DROP TABLE IF EXISTS handles "not exists" in SQL, so no exception-based detection is needed.
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				return new SqlCommandScenario
				{
					Steps =
					[
						new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery },
						new SqlCommandStep { Statement = new SqlFragmentStatement(factory.Fragment("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME={0}", factory.Value(truncate.Table.TableName.Name))), Kind = SqlStepKind.NonQuery },
					],
					OutcomeSteps = [0],
				};
			}

			if (statement.NeedsIdentity)
			{
				var idType   = factory.GetDbDataType(typeof(long));
				var idSelect = new SqlSelectStatement();

				idSelect.SelectQuery.Select.AddNew(factory.Function(idType, "last_insert_rowid"));

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
