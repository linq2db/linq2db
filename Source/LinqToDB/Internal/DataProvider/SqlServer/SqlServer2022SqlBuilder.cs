using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServer2022SqlBuilder : SqlServer2019SqlBuilder
	{
		public SqlServer2022SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SqlServer2022SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
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
