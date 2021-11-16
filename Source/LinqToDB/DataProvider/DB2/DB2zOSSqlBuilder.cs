namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlProvider;

	class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(
			DB2DataProvider? provider,
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		// remote context
		public DB2zOSSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2zOSSqlBuilder(Provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override DB2Version Version => DB2Version.zOS;
	}
}
