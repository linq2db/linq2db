using LinqToDB.Data;

namespace Tests.Model
{
	public static class Extensions
	{
		public static DataConnectionTransaction? BeginTransaction(this ITestDataContext context)
		{
			return context is DataConnection dc
				? dc.BeginTransaction()
				: null;
		}
	}
}
