using System;

namespace LinqToDB.DataProvider.Firebird
{
	public static class FirebirdConfiguration
	{
		/// <summary>  Values that represent fb dialects. 
		/// <see href="https://www.firebirdsql.org/pdfmanual/html/isql-dialects.html">Firbird-Dialects</see></summary>
		public enum FbDialect
		{
         /// <summary>  There is no specific dialect defined. </summary>
			Undefined = 0,
         /// <summary> dialect 1. </summary>
			Dialect1 = 1,
         /// <summary>  dialect 2. </summary>
			Dialect2 = 2,
         /// <summary>  dialect 3. </summary>
			Dialect3 = 3
		}

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

      /// <summary>  The current used Firebird dialect. This is not a connection configuration. 
      ///            It is more a global information to handle dialect specific conversions (e.g. bigint in dialect 1).</summary>
		public static FbDialect UsedDialect = FbDialect.Undefined;
	}
}
