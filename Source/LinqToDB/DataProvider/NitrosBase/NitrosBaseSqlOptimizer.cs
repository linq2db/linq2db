namespace LinqToDB.DataProvider.NitrosBase
{
	using SqlProvider;

	class NitrosBaseSqlOptimizer : BasicSqlOptimizer
	{
		// this constructor is required for remote context and should always present
		public NitrosBaseSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		// TODO: add/override base implementation if needed
	}
}
