using System;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public class MySqlDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "MySqlException"))
				return false;

			var message = exception.Message;

			// 1051 = unknown table
			return message.Contains("1051",          StringComparison.Ordinal)
				|| message.Contains("Unknown table", StringComparison.Ordinal)
				|| message.Contains("doesn't exist", StringComparison.Ordinal);
		}
	}
}
