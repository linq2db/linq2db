namespace LinqToDB.DataProvider.SapHana
{
	using LinqToDB.Internal.SqlProvider;

	sealed class SapHanaNativeSqlOptimizer : SapHanaSqlOptimizer
	{
		public SapHanaNativeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}
	}

}
