using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new PostgreSQLSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			return statement.QueryType switch
			{
				QueryType.Delete => CorrectPostgreSqlDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema),
				_                => statement,
			};
		}

		SqlStatement CorrectPostgreSqlDelete(SqlDeleteStatement statement, DataOptions dataOptions)
		{
			statement = GetAlternativeDelete(statement, dataOptions);

			return statement;
		}

	}
}
