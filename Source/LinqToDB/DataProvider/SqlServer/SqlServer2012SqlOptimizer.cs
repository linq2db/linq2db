using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

	class SqlServer2012SqlOptimizer : SqlServer2008SqlOptimizer
	{
		public SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags) : this(sqlProviderFlags, SqlServerVersion.v2012)
		{
		}

		protected SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			if (statement.IsUpdate())
				statement = ReplaceTakeSkipWithRowNumber(statement, false);
			else
			{
				statement = ReplaceTakeSkipWithRowNumber(statement, true);
				CorrectRootSkip(statement.SelectQuery);
			}

			return statement;
		}
	}
}
