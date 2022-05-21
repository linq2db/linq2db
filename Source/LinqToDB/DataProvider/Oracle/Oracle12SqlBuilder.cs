namespace LinqToDB.DataProvider.Oracle
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	class Oracle12SqlBuilder : OracleSqlBuilderBase
	{
		public Oracle12SqlBuilder(OracleDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		Oracle12SqlBuilder(Oracle12SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<OracleDataProvider> CreateSqlBuilder()
			=> new Oracle12SqlBuilder(this) { HintBuilder = HintBuilder };

		protected override bool CanSkipRootAliases(SqlStatement statement)
		{
			if (statement.SelectQuery != null)
			{
				// https://github.com/linq2db/linq2db/issues/2785
				// https://stackoverflow.com/questions/57787579/
				return statement.SelectQuery.Select.TakeValue == null && statement.SelectQuery.Select.SkipValue == null;
			}

			return true;
		}

		protected override bool BuildWhere(SelectQuery selectQuery)
		{
			var condition = ConvertElement(selectQuery.Where.SearchCondition);
			return condition.Conditions.Count != 0;
		}

		protected override string? LimitFormat(SelectQuery selectQuery) => "FETCH NEXT {0} ROWS ONLY";

		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ROWS";

		protected override bool OffsetFirst => true;
	}
}
