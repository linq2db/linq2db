using System;

using LinqToDB.CommandLine.Options;
using LinqToDB.CommandLine;
using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	internal static class ReadOnlySqlGuard
	{
		public static SqlGuardResult Validate(IDataProvider provider, string sql)
		{
			return IsSqlServerProvider(provider.Name)
				? SqlServerReadOnlySqlGuard.Validate(sql)
				: GenericReadOnlySqlGuard.Validate(sql);
		}

		public static SqlGuardResult ValidateSingleStatement(IDataProvider provider, string sql)
		{
			// Single-statement execution is a hard query command contract. SQL Server gets
			// AST-level validation; other providers use generic best-effort validation.
			return IsSqlServerProvider(provider.Name)
				? SqlServerReadOnlySqlGuard.ValidateSingleStatement(sql)
				: GenericReadOnlySqlGuard.ValidateSingleStatement(sql);
		}

		private static bool IsSqlServerProvider(string provider)
		{
			return provider.StartsWith(ProviderName.SqlServer, StringComparison.OrdinalIgnoreCase);
		}
	}
}
