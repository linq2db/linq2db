using System;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	public class ClickHouseDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "ClickHouse"))
				return false;

			var message = exception.Message;

			return message.Contains("Code: 60",      StringComparison.OrdinalIgnoreCase)
				|| message.Contains("UNKNOWN_TABLE", StringComparison.OrdinalIgnoreCase);
		}
	}
}
