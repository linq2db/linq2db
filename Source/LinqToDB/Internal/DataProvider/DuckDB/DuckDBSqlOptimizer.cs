using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlOptimizer : BasicSqlOptimizer
	{
		public DuckDBSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new DuckDBSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					break;
				}
				case QueryType.Update:
				{
					statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					break;
				}
			}

			return statement;
		}
	}
}
