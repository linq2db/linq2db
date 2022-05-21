namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;

	partial class SqlServer2008SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2008SqlBuilder(SqlServerDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		SqlServer2008SqlBuilder(SqlServer2008SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<SqlServerDataProvider> CreateSqlBuilder()
			=> new SqlServer2008SqlBuilder(this);

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
			StringBuilder.AppendLine(";");
		}

		public override string Name => ProviderName.SqlServer2008;
	}
}
