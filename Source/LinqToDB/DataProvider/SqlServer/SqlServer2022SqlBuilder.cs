using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	sealed class SqlServer2022SqlBuilder : SqlServer2019SqlBuilder
	{
		public SqlServer2022SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SqlServer2022SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2022SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2022;

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			BuildExpression(GetPrecedence(expr), expr.Expr1);
			StringBuilder.Append(expr.IsNot ? " IS NOT DISTINCT FROM " : " IS DISTINCT FROM ");
			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}
	}
}
