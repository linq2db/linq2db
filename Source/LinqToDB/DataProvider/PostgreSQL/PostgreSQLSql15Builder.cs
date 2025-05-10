using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.PostgreSQL
{
	public class PostgreSQLSql15Builder : PostgreSQLSqlBuilder
	{
		public PostgreSQLSql15Builder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		PostgreSQLSql15Builder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSql15Builder(this);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
		}
	}
}
