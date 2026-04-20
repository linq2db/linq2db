using System;

namespace LinqToDB.Internal.DataProvider.Informix
{
	public class InformixDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "IfxException"))
				return false;

			var message = exception.Message;

			return message.Contains("-206", StringComparison.Ordinal)
				|| message.Contains("-111", StringComparison.Ordinal);
		}
	}
}
