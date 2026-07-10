using System;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Small provider-name to SQL dialect catalog for agent-facing metadata.
	/// </summary>
	internal static class ProviderDialectCatalog
	{
		public static string GetDialect(string providerName)
		{
			if (IsProvider(providerName, "SqlServer"))  return "SQL Server T-SQL";
			if (IsProvider(providerName, "SQLite"))     return "SQLite";
			if (IsProvider(providerName, "PostgreSQL")) return "PostgreSQL";
			if (IsProvider(providerName, "MySql") || IsProvider(providerName, "MySqlConnector")) return "MySQL";
			if (IsProvider(providerName, "MariaDB"))    return "MariaDB";
			if (IsProvider(providerName, "Oracle"))     return "Oracle SQL";
			if (IsProvider(providerName, "Firebird"))   return "Firebird SQL";
			if (IsProvider(providerName, "DB2"))        return "IBM DB2 SQL";
			if (IsProvider(providerName, "Informix"))   return "Informix SQL";
			if (IsProvider(providerName, "ClickHouse")) return "ClickHouse SQL";
			if (IsProvider(providerName, "DuckDB"))     return "DuckDB SQL";
			if (IsProvider(providerName, "Sybase") || IsProvider(providerName, "ASE")) return "Sybase ASE T-SQL";
			if (IsProvider(providerName, "Access"))     return "Microsoft Access SQL";
			if (IsProvider(providerName, "ODBC")  || IsProvider(providerName, "Odbc"))  return "ODBC provider-specific SQL";
			if (IsProvider(providerName, "OLEDB") || IsProvider(providerName, "OleDb")) return "OLE DB provider-specific SQL";
			if (IsProvider(providerName, "YDB"))        return "YDB SQL";

			return "provider-specific SQL";
		}

		static bool IsProvider(string providerName, string value)
		{
			return string.Equals(providerName, value, StringComparison.OrdinalIgnoreCase)
				|| providerName.StartsWith(value + ".", StringComparison.OrdinalIgnoreCase);
		}
	}
}
