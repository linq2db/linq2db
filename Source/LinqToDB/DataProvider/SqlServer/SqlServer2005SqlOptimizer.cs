namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2005SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2005SqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, SqlServerVersion.v2005, ast)
		{ }

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			//SQL Server 2005 supports ROW_NUMBER but not OFFSET/FETCH

			statement = SeparateDistinctFromPagination(statement, q => q.Select.TakeValue != null || q.Select.SkipValue != null);
			statement = ReplaceDistinctOrderByWithRowNumber(statement, q => true);
			if (statement.IsUpdate() || statement.IsDelete()) statement = WrapRootTakeSkipOrderBy(statement);
			statement = ReplaceSkipWithRowNumber(statement);

			return statement;
		}

		protected override ISqlExpression ConvertFunction(ISqlExpression expr)
		{
			if (expr is not SqlFunction func) return expr;
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}
	}
}
