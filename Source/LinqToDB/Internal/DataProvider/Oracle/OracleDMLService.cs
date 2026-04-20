using System;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	public class OracleDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "OracleException"))
				return false;

			var message = exception.Message;

			// Only ORA-00942 alone — NOT a compound error that includes other codes.
			return  message.Contains("ORA-00942", StringComparison.Ordinal)
				&& !message.Contains("ORA-14452", StringComparison.Ordinal)
				&& !message.Contains("ORA-06512", StringComparison.Ordinal);
		}
	}
}
