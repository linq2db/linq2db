using System;

namespace LinqToDB.DataProvider.Firebird
{
	public static class FirebirdConfiguration
	{
		[Obsolete("Use FirebirdSqlBuilder.IdentifierQuoteMode instead.")]
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

		/// <summary>
		/// Specifies that Firebird supports literals encoding. Availiable from version 2.5.
		/// </summary>
		public static bool SupportsLiteralEncoding = true;
	}
}
