using System;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	public class PostgreSQLDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "PostgresException"))
				return false;

			var message = exception.Message;

			// 42P01 = undefined table
			return message.Contains("42P01",          StringComparison.Ordinal)
				|| message.Contains("does not exist", StringComparison.Ordinal);
		}
	}
}
