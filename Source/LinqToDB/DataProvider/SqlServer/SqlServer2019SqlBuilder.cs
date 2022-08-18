namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlProvider;

	class SqlServer2019SqlBuilder : SqlServer2017SqlBuilder
	{
		public SqlServer2019SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2019SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2019SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2019;
	}
}
