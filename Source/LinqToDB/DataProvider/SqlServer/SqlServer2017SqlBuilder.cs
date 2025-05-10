using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServer2017SqlBuilder : SqlServer2016SqlBuilder
	{
		public SqlServer2017SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2017SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2017SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2017;
	}
}
