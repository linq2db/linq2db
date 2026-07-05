using System;

using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine
{
	internal static class QuerySafetyValidator
	{
		public static QuerySafetyResult Validate(IDataProvider provider, string sql)
		{
			return IsSqlServerProvider(provider.Name)
				? SqlServerQuerySafetyValidator.Validate(sql)
				: GenericQuerySafetyValidator.Validate(sql);
		}

		public static QuerySafetyResult ValidateSingleStatement(IDataProvider provider, string sql)
		{
			// Single-statement execution is a hard query command contract. SQL Server gets
			// AST-level validation; other providers use generic best-effort validation.
			return IsSqlServerProvider(provider.Name)
				? SqlServerQuerySafetyValidator.ValidateSingleStatement(sql)
				: GenericQuerySafetyValidator.ValidateSingleStatement(sql);
		}

		private static bool IsSqlServerProvider(string provider)
		{
			return provider.StartsWith(ProviderName.SqlServer, StringComparison.OrdinalIgnoreCase);
		}
	}
}
