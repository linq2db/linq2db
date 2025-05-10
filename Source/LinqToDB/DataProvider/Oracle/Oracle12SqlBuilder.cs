using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	sealed class Oracle12SqlBuilder : OracleSqlBuilderBase
	{
		public Oracle12SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		Oracle12SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new Oracle12SqlBuilder(this) { HintBuilder = HintBuilder };
		}

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

		protected override bool ShouldBuildWhere(SelectQuery selectQuery, out SqlSearchCondition condition)
		{
			condition = PrepareSearchCondition(selectQuery.Where.SearchCondition);

			if (condition.IsTrue())
				return false;

			return true;
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
