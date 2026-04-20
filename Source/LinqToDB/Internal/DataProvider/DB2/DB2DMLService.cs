using System;

namespace LinqToDB.Internal.DataProvider.DB2
{
	public class DB2DMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "DB2Exception"))
				return false;

			return exception.Message.Contains("-204", StringComparison.Ordinal); // object not found
		}
	}
}
