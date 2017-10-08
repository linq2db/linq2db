using System;

namespace LinqToDB.DataProvider.Firebird
{
	[Obsolete("Use FirebirdSqlBuilder.IdentifierQuoteMode instead.")]
	public static class FirebirdConfiguration
	{
		public static bool QuoteIdentifiers
		{
			get
			{
				return FirebirdSqlBuilder.IdentifierQuoteMode != FirebirdIdentifierQuoteMode.None;
			}

			set
			{
				FirebirdSqlBuilder.IdentifierQuoteMode = value ? FirebirdIdentifierQuoteMode.Quote : FirebirdIdentifierQuoteMode.None;
			}
		}
	}
}
