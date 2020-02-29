namespace LinqToDB.DataProvider.Oracle
{
	using LinqToDB.Mapping;
	using SqlProvider;
	using SqlQuery;

	partial class Oracle12SqlBuilder : Oracle11SqlBuilder
	{
		public Oracle12SqlBuilder(
			OracleDataProvider? provider,
			MappingSchema       mappingSchema,
			ISqlOptimizer       sqlOptimizer,
			SqlProviderFlags    sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		// remote context
		public Oracle12SqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Oracle12SqlBuilder(Provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override bool BuildWhere(SelectQuery selectQuery)
		{
			return selectQuery.Where.SearchCondition.Conditions.Count != 0;
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			return "FETCH NEXT {0} ROWS ONLY";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ROWS";
		}

		protected override bool OffsetFirst => true;
	}
}
