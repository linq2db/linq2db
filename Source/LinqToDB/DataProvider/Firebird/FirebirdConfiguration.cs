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
		/// Indicates optimizator to convert INNER joins to LEFT JOIN with filter.
		/// Workaround for known Firebird issue http://tracker.firebirdsql.org/browse/CORE-3707
		/// </summary>
		public static bool ConvertInnerJoinsToLeftJoins = false;
	}
}
