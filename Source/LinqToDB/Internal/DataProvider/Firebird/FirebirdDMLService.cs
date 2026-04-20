using System;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	public class FirebirdDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "FbException"))
				return false;

			var message = exception.Message;

			return message.Contains("335544580",     StringComparison.Ordinal)
				|| message.Contains("table unknown", StringComparison.Ordinal);
		}
	}
}
