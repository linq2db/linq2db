namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;

	partial class SqlServer2012SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2012SqlBuilder(SqlServerDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		protected SqlServer2012SqlBuilder(SqlServer2012SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<SqlServerDataProvider> CreateSqlBuilder()
			=> new SqlServer2012SqlBuilder(this);

		protected override string? LimitFormat(SelectQuery selectQuery)
			=> selectQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null;

		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ROWS";

		protected override bool OffsetFirst => true;

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
			StringBuilder.AppendLine(";");
		}

		public override string Name => ProviderName.SqlServer2012;
	}
}
