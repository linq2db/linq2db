namespace LinqToDB.DataProvider.PostgreSQL
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public class PostgreSQLSql15Builder : PostgreSQLSqlBuilder
	{
		public PostgreSQLSql15Builder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		PostgreSQLSql15Builder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSql15Builder(this);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
		}
	}
}
