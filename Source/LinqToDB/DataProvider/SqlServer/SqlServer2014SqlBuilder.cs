using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServer2014SqlBuilder : SqlServer2012SqlBuilder
	{
		public SqlServer2014SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2014SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2014SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2014;
	}
}
