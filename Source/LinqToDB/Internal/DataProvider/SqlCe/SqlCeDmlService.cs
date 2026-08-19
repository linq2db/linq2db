using System;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeDmlService : DmlServiceBase
	{
		// DB_E_NOTABLE. SqlCeException stores this code but exposes it incorrectly, so the
		// HResult check below is best-effort and will usually miss.
		const int DB_E_NOTABLE = unchecked((int)0x80040E37);

		protected override bool IsTableNotFoundExceptionCore(Exception exception)
		{
			if (!TypeOrMessageContains(exception, "SqlCeException"))
				return false;

			return HResultMatches(exception, DB_E_NOTABLE)
				|| exception.Message.Contains("specified table does not exist", StringComparison.OrdinalIgnoreCase);
		}
	}
}
