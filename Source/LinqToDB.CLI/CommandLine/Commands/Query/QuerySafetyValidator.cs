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

		private static bool IsSqlServerProvider(string provider)
		{
			return provider.StartsWith(ProviderName.SqlServer, StringComparison.OrdinalIgnoreCase);
		}
	}
}
