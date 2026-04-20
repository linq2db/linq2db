using System;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "SqlCeException"))
				return false;

			var message = exception.Message;

			return message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
				|| message.Contains("not exist",      StringComparison.OrdinalIgnoreCase);
		}
	}
}
