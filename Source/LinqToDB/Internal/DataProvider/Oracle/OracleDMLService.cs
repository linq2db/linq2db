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
			// Accept ORA-00942 only when it is the sole ORA- code in the message; compound
			// errors (ORA-06512/ORA-06550/ORA-14452/ORA-24344/…) embed the same code inside
			// an unrelated failure that must not be swallowed.
			if (!message.Contains("ORA-00942", StringComparison.Ordinal))
				return false;

			var firstOra = message.IndexOf("ORA-", StringComparison.Ordinal);
			var nextOra  = firstOra < 0 ? -1 : message.IndexOf("ORA-", firstOra + 4, StringComparison.Ordinal);

			return nextOra < 0;
		}
	}
}
