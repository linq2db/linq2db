using System;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public class SapHanaDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "HanaException"))
				return false;

			return exception.Message.Contains("259", StringComparison.Ordinal);
		}
	}
}
