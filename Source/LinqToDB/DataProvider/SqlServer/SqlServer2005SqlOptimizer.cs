namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2005SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2005SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2005)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			//SQL Server 2005 supports ROW_NUMBER but not OFFSET/FETCH

			statement = SeparateDistinctFromPagination(statement);
			statement = ReplaceDistinctOrderByWithRowNumber(statement);
			if (statement.IsUpdate() || statement.IsDelete()) statement = WrapRootTakeSkipOrderBy(statement);
			statement = ReplaceSkipWithRowNumber(statement);
			if (statement.QueryType == QueryType.Select)
				statement = QueryHelper.OptimizeSubqueries(statement); // OptimizeSubqueries can break update queries

			return statement;
		}

	}
}
