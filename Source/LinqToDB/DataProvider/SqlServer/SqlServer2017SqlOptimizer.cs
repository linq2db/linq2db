namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SqlServer2017SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2017SqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, SqlServerVersion.v2017, ast)
		{ }
	}
}
