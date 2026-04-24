using System;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessDMLService : DMLServiceBase
	{
		// DB_E_NOTABLE — "The specified table does not exist."
		const int DB_E_NOTABLE = unchecked((int)0x80040E37);

		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			// Access via OleDb — HResult is populated correctly.
			if (TypeOrMessageContains(exception, "OleDbException"))
			{
				return HResultMatches(exception, DB_E_NOTABLE)
					|| exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
			}

			// Access via ODBC — SQLSTATE 42S02 = "Base table or view not found".
			if (TypeOrMessageContains(exception, "OdbcException"))
			{
				return exception.Message.Contains("42S02",          StringComparison.Ordinal)
					|| exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}
	}
}
