using System;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public class SapHanaDMLService : DMLServiceBase
	{
        protected override bool IsTableNotFoundExceptionCore(Exception exception)
        {
            var message = exception.Message;

            // SAP HANA via native driver (Sap.Data.Hana)
            if (TypeOrMessageContains(exception, "HanaException"))
            {
                return message.Contains("invalid table name",   StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Could not find table", StringComparison.OrdinalIgnoreCase);
            }

            // SAP HANA via ODBC - SQLSTATE 42S02 = base table or view not found
            if (TypeOrMessageContains(exception, "OdbcException"))
            {
                return message.Contains("42S02",              StringComparison.OrdinalIgnoreCase)
                    || message.Contains("invalid table name",  StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Could not find table", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
	}
}

