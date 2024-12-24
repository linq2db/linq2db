using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Internals.SqlProvider;
using LinqToDB.Internals.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.Internals.DataProviders.PostgreSQL
{
	public class PostgreSQLSql15Builder : PostgreSQLSqlBuilder
	{
		public PostgreSQLSql15Builder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
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
