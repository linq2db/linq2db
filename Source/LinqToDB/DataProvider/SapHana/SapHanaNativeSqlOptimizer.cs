namespace LinqToDB.DataProvider.SapHana
{
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SapHanaNativeSqlOptimizer : SapHanaSqlOptimizer
	{
		public SapHanaNativeSqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, ast)
		{ }
	}
}
