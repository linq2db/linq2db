using LinqToDB.Internal.SqlProvider;
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

		protected SqlServer2014SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2014SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2014;
	}
}
