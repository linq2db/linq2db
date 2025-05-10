using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Firebird
{
	public class Firebird3SqlBuilder : FirebirdSqlBuilder
	{
		public Firebird3SqlBuilder(IDataProvider provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected Firebird3SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new Firebird3SqlBuilder(this);
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			// From doc:
			// The choice between ROW or ROWS, or FIRST or NEXT in the clauses is just for aesthetic purposes
			return "FETCH NEXT {0} ROWS ONLY";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ROWS";
		}

		protected override void BuildSkipFirst(SelectQuery selectQuery)
		{
		}

		protected override bool OffsetFirst => true;
	}
}
