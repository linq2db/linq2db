namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SqlServer2019SqlOptimizer : SqlServer2012SqlOptimizer
	{
		public SqlServer2019SqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, SqlServerVersion.v2019, ast)
		{ }

		protected SqlServer2019SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version, AstFactory ast)
			: base(sqlProviderFlags, version, ast)
		{ }
	}
}
