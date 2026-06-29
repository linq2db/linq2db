using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	// PostgreSQL 12+ supports the CTE AS [NOT] MATERIALIZED hint. Selected by PostgreSQLDataProvider.CreateSqlBuilder
	// for v13+ providers (v13 is the lowest version enum entry >= 12). The support lives in the builder TYPE rather
	// than a runtime DataProvider.Version check so it stays consistent across LinqService remote mode — the
	// version-aware provider picks the builder type, independent of whether DataProvider is set at SQL-build time.
	sealed class PostgreSQL13SqlBuilder : PostgreSQLSqlBuilder
	{
		public PostgreSQL13SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		PostgreSQL13SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => new PostgreSQL13SqlBuilder(this);

		protected override bool SupportsMaterializedCteHint => true;
	}
}
