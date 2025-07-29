using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	public class Oracle12SqlBuilder : OracleSqlBuilderBase
	{
		public Oracle12SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		Oracle12SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
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
