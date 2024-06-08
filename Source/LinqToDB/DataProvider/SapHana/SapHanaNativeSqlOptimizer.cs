namespace LinqToDB.DataProvider.SapHana
{
	using SqlProvider;

	sealed class SapHanaNativeSqlOptimizer : SapHanaSqlOptimizer
	{
		public SapHanaNativeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}
	}

}
