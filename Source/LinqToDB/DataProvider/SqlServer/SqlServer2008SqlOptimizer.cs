using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServer2008SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2008SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags, SqlServerVersion.v2008)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			//SQL Server 2008 supports ROW_NUMBER but not OFFSET/FETCH

			statement = SeparateDistinctFromPagination(statement, q => q.Select.TakeValue != null || q.Select.SkipValue != null);
			if (statement.IsUpdate() || statement.IsDelete()) statement = WrapRootTakeSkipOrderBy(statement);
			statement = ReplaceSkipWithRowNumber(statement);

			return statement;
		}


		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}

	}
}
