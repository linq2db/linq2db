using System;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			// SQL Server (System.Data.SqlClient / Microsoft.Data.SqlClient)
			if (!TypeOrMessageContains(exception, "SqlException"))
				return false;

			var message = exception.Message;

			// 3701 = cannot drop table / not found
			return message.Contains("3701",                  StringComparison.Ordinal)
				|| message.Contains("Cannot drop the table", StringComparison.Ordinal)
				|| message.Contains("Could not find object", StringComparison.Ordinal);
		}
	}
}
