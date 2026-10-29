using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix
{
	/// <summary>
	/// Informix DML mechanics. An identity insert retrieves the generated key with a from-less scalar
	/// <c>SELECT DBINFO('sqlca.sqlerrd1')</c> — the builder appends Informix's single-row fake table (a scalar step); a
	/// truncate-with-identity-reset is the truncate plus one <c>ALTER TABLE … MODIFY … SERIAL(1)</c> per identity
	/// column (a <see cref="SqlFragmentStatement"/>). Everything else uses the base scenario.
	/// </summary>
	public sealed class InformixDmlService : DmlServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception) => false;

		public override SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			if (statement is SqlTruncateTableStatement { ResetIdentity: true } truncate && truncate.Table!.IdentityFields.Count > 0)
			{
				var table = truncate.Table!;

				return PerFieldResetScenario(truncate, field => new SqlFragmentStatement(factory.Fragment(
					"ALTER TABLE {0} MODIFY {1} SERIAL(1)",
					new SqlObjectNameExpression(table.TableName, ConvertType.NameToQueryTable),
					new SqlObjectNameExpression(new SqlObjectName(field.PhysicalName), ConvertType.NameToQueryField))));
			}

			if (statement is SqlInsertStatement { Insert.WithIdentity: true })
			{
				// Informix returns the generated identity via a separate scalar query (not a RETURNING clause):
				// SELECT DBINFO('sqlca.sqlerrd1'). Built as a real from-less select — the builder appends Informix's
				// single-row FakeTable.
				var idType = factory.GetDbDataType(typeof(long));

				return IdentitySelectScenario(statement, factory.Function(idType, "DBINFO", factory.Value("sqlca.sqlerrd1")));
			}

			return base.BuildCommandScenario(statement, flags, factory);
		}
	}
}
