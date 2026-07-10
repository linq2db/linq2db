using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	// PostgreSQL 12+ supports the CTE AS [NOT] MATERIALIZED hint. Selected by PostgreSQLDataProvider.CreateSqlBuilder
	// for v12+ providers. The support lives in the builder TYPE rather than a runtime DataProvider.Version check so it
	// stays consistent across LinqService remote mode — the version-aware provider picks the builder type, independent
	// of whether DataProvider is set at SQL-build time.
	sealed class PostgreSQL12SqlBuilder : PostgreSQLSqlBuilder
	{
		public PostgreSQL12SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		PostgreSQL12SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => new PostgreSQL12SqlBuilder(this);

		protected override bool SupportsMaterializedCteHint => true;
	}
}
