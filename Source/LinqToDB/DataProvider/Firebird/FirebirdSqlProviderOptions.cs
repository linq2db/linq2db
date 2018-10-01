using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Firebird
{
	internal static class FirebirdSqlProviderOptions
	{
		private static readonly FirebirdDialect DefaultDialect = FirebirdDialect.Dialect3;

		private const string DialectOptionName = "dialect";

		public static FirebirdDialect GetDialect(this SqlProviderFlags flags)
		{
			return flags.GetProviderOption(DialectOptionName, DefaultDialect);
		}

		public static void SetDialect(this SqlProviderFlags flags, FirebirdDialect value)
		{
			flags.ProviderOptions[DialectOptionName] = value;
		}
	}
}
