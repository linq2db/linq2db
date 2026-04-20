using System;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessDMLService : DMLServiceBase
	{
		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			var message = exception.Message;

			// Access via OleDb
			if (TypeOrMessageContains(exception, "OleDbException"))
			{
				return message.Contains("could not find table",  StringComparison.OrdinalIgnoreCase)
					|| message.Contains("cannot find the table", StringComparison.OrdinalIgnoreCase)
					|| message.Contains("does not exist",        StringComparison.OrdinalIgnoreCase);
			}

			// Access via ODBC - SQLSTATE 42S02 = base table or view not found
			if (TypeOrMessageContains(exception, "OdbcException"))
			{
				return message.Contains("42S02",          StringComparison.OrdinalIgnoreCase)
					|| message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
					|| message.Contains("could not find", StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}
	}
}
