namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SqlServer2022SqlOptimizer : SqlServer2019SqlOptimizer
	{
		public SqlServer2022SqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, SqlServerVersion.v2022, ast)
		{ }
	}
}
