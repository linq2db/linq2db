using LinqToDB.Data;

namespace Tests.Model
{
	public static class Extensions
	{
		public static DataConnectionTransaction? BeginTransaction(this ITestDataContext context)
		{
			if (context is DataConnection)
				return ((DataConnection)context).BeginTransaction();
			//else if (context is ServiceModelDataContext)
			//	((ServiceModelDataContext)context).BeginBatch();
			return null;
		}
	}
}
