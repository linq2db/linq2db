namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.Mapping;
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SqlServer2016SqlBuilder : SqlServer2012SqlBuilder
	{
		public SqlServer2016SqlBuilder(SqlServerDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public SqlServer2016SqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2016SqlBuilder(Provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		public override string Name => ProviderName.SqlServer2016;
	}
}
