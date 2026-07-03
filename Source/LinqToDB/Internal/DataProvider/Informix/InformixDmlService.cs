using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix
{
	/// <summary>
	/// Informix DML mechanics. An identity insert retrieves the generated key with
	/// <c>SELECT DBINFO('sqlca.sqlerrd1') FROM systables WHERE tabid = 1</c> (a scalar step); a
	/// truncate-with-identity-reset is the truncate plus one <c>ALTER TABLE … MODIFY … SERIAL(1)</c> per identity
	/// column. Both auxiliary commands are <see cref="SqlFragmentStatement"/>s. Everything else uses the base scenario.
	/// </summary>
	public sealed class InformixDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

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
						"ALTER TABLE {0} MODIFY {1} SERIAL(1)",
						new SqlObjectNameExpression(truncate.Table.TableName, ConvertType.NameToQueryTable),
						new SqlObjectNameExpression(new SqlObjectName(fields[i].PhysicalName), ConvertType.NameToQueryField)));

					steps[i + 1] = new SqlCommandStep { Statement = reset, Kind = SqlStepKind.NonQuery };
				}

				return new SqlCommandScenario { Steps = steps, OutcomeSteps = [0] };
			}

			if (statement is SqlInsertStatement { Insert.WithIdentity: true })
			{
				// Informix returns the generated identity via a separate scalar query (not a RETURNING clause):
				// SELECT DBINFO('sqlca.sqlerrd1'). Built as a real from-less select — the builder appends Informix's
				// single-row FakeTable.
				var idType   = factory.GetDbDataType(typeof(long));
				var idSelect = new SqlSelectStatement();

				idSelect.SelectQuery.Select.AddNew(factory.Function(idType, "DBINFO", factory.Value("sqlca.sqlerrd1")));

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
