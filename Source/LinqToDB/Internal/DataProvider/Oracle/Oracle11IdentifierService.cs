namespace LinqToDB.Internal.DataProvider.Oracle
{
	/// <summary>
	/// Oracle 11 and earlier allow 30 bytes.
	/// </summary>
	public sealed class Oracle11IdentifierService() : IdentifierServiceSimple(30, IdentifierLengthUnit.Utf8Bytes);
}