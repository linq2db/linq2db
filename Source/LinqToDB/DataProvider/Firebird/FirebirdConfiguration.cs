using System;

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
		[Obsolete("Use FirebirdOptions.Default.IdentifierQuoteMode instead.")]
		public static FirebirdIdentifierQuoteMode IdentifierQuoteMode
		{
			get => FirebirdOptions.Default.IdentifierQuoteMode;
			set => FirebirdOptions.Default = FirebirdOptions.Default with { IdentifierQuoteMode = value };
		}

		/// <summary>
		/// Specifies that Firebird supports literal encoding. Available from version 2.5 and could be used for Dialect 1 databases.
		/// </summary>
		[Obsolete("Use FirebirdOptions.Default.IsLiteralEncodingSupported instead.")]
		public static bool IsLiteralEncodingSupported
		{
			get => FirebirdOptions.Default.IsLiteralEncodingSupported;
			set => FirebirdOptions.Default = FirebirdOptions.Default with { IsLiteralEncodingSupported = value };
		}
	}
}
