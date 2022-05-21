namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlProvider;

	class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(DB2DataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		DB2zOSSqlBuilder(DB2zOSSqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<DB2DataProvider> CreateSqlBuilder()
			=> new DB2zOSSqlBuilder(this);

		protected override DB2Version Version => DB2Version.zOS;
	}
}
