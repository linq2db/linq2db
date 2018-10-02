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
		/// Specifies that Firebird supports literal encoding. Availiable from version 2.5.
		/// </summary>
		public static bool IsLiteralEncodingSupported = true;

		/// <summary>
		/// Default Firebird dialect. Default value: <see cref="FirebirdDialect.Dialect3"/>.
		/// Important: this setting must not conflict with dialect, set in connection string.
		/// </summary>
		public static FirebirdDialect Dialect = FirebirdDialect.Dialect3;
	}
}
