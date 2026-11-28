namespace LinqToDB.Internal.DataProvider.Informix
{
	/// <summary>
	/// Informix counts its 128 in bytes. Its default locale also cannot represent a non-ASCII identifier
	/// even delimited: the server drops those characters while parsing the name, so two aliases differing
	/// only in them collapse into one and it reports a duplicate table name.
	/// </summary>
	public sealed class InformixIdentifierService() : IdentifierServiceSimple(128, IdentifierLengthUnit.Utf8Bytes)
	{
		protected override bool IsIdentifierChar(char c)
			=> c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
	}
}
