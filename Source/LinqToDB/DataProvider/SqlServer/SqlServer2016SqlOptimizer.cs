namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SqlServer2016SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2016SqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, SqlServerVersion.v2016, ast)
		{ }
	}
}
