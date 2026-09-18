using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	/// <summary>
	/// Firebird DML mechanics. An identity insert runs as a single non-query whose generated key is returned through an
	/// OUT parameter (the insert renders a trailing <c>RETURNING &lt;id&gt;</c>); a truncate-with-identity-reset is the
	/// truncate plus a <see cref="SqlFragmentStatement"/>
	/// (<c>SET GENERATOR GIDENTITY_&lt;table&gt; TO 0</c>). Everything else uses the base scenario.
	/// </summary>
	public sealed class FirebirdDmlService : DmlServiceBase
	{
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
						new SqlCommandStep { Statement = new SqlFragmentStatement(factory.Fragment("SET GENERATOR {0} TO 0", new SqlObjectNameExpression(new SqlObjectName("GIDENTITY_" + truncate.Table.TableName.Name), ConvertType.NameToQueryTable))), Kind = SqlStepKind.NonQuery },
					],
					OutcomeSteps = [0],
				};
			}

			if (statement is SqlInsertStatement { Insert.WithIdentity: true })
			{
				// Firebird returns the generated identity through an OUT parameter: the insert renders a trailing
				// RETURNING <id>, and the interpreter adds the "IDENTITY_PARAMETER" output parameter and reads it back
				// after ExecuteNonQuery (the Firebird provider cannot return values through a reader).
				return new SqlCommandScenario
				{
					Steps        = [new SqlCommandStep { Statement = statement, Kind = SqlStepKind.NonQuery, OutParameterName = "IDENTITY_PARAMETER" }],
					OutcomeSteps = [0],
				};
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}
	}
}
