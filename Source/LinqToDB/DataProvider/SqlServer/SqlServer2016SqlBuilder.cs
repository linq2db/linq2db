namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	class SqlServer2016SqlBuilder : SqlServer2014SqlBuilder
	{
		public SqlServer2016SqlBuilder(SqlServerDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		protected SqlServer2016SqlBuilder(SqlServer2016SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<SqlServerDataProvider> CreateSqlBuilder()
			=> new SqlServer2016SqlBuilder(this);

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
			=> BuildDropTableStatementIfExists(dropTable);

		public override string Name => ProviderName.SqlServer2016;
	}
}
