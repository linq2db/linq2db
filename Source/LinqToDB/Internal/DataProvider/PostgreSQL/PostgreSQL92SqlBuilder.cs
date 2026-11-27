using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	// PostgreSQL 9.2-9.4 operator precedence. Selected by PostgreSQLDataProvider.CreateSqlBuilder for pre-9.5 providers,
	// in the builder type for the same reason as PostgreSQL12SqlBuilder.
	sealed class PostgreSQL92SqlBuilder : PostgreSQLSqlBuilder
	{
		public PostgreSQL92SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		PostgreSQL92SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => new PostgreSQL92SqlBuilder(this);

		protected override SqlPrecedenceTable PrecedenceTable => PrecedenceTable94;

		// keeps the dialect name in traced SQL as it was before this builder type existed
		public override string Name => ProviderName.PostgreSQL;
	}
}
