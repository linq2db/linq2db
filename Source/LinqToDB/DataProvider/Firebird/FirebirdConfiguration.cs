namespace LinqToDB.DataProvider.Firebird
{
	public static class FirebirdConfiguration
	{
		/// <summary>
		/// Specifies how identifiers like table and field names should be quoted.
		/// </summary>
		/// <remarks>
		/// Default value: <see cref="FirebirdIdentifierQuoteMode.Auto"/>.
		/// </remarks>
		public static FirebirdIdentifierQuoteMode IdentifierQuoteMode { get; set; } = FirebirdIdentifierQuoteMode.Auto;

		/// <summary>
		/// Specifies that Firebird supports literal encoding. Availiable from version 2.5.
		/// </summary>
		public static bool IsLiteralEncodingSupported = true;
	}
}
