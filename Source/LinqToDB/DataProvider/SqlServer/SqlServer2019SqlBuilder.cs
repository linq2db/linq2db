namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlProvider;

	class SqlServer2019SqlBuilder : SqlServer2017SqlBuilder
	{
		public SqlServer2019SqlBuilder(SqlServerDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		SqlServer2019SqlBuilder(SqlServer2019SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<SqlServerDataProvider> CreateSqlBuilder()
			=> new SqlServer2019SqlBuilder(this);

		public override string Name => ProviderName.SqlServer2019;
	}
}
