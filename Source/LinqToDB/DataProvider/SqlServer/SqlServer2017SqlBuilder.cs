namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.Mapping;
	using SqlProvider;

	class SqlServer2017SqlBuilder : SqlServer2012SqlBuilder
	{
		public SqlServer2017SqlBuilder(SqlServerDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public SqlServer2017SqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2017SqlBuilder(Provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		public override string Name => ProviderName.SqlServer2017;
	}
}
