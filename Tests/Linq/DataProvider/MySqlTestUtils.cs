extern alias MySqlData;
extern alias MySqlConnector;
using LinqToDB.Data;

namespace Tests.DataProvider
{
	internal static class MySqlTestUtils
	{
		public static void EnableNativeBulk(DataConnection db, string context)
		{
			if (context.IsAnyOf(TestProvName.AllMySqlConnector))
				db.Execute("SET GLOBAL local_infile=ON");
		}
	}
}
