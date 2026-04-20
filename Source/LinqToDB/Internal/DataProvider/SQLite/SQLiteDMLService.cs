using System;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "SqliteException"))
				return false;

			return exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase);
		}
	}
}
