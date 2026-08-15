namespace LinqToDB.Internal.DataProvider.Firebird
{
	/// <summary>
	/// Firebird 3 and earlier store metadata as UNICODE_FSS and cap it at 31 bytes.
	/// </summary>
	public sealed class Firebird3IdentifierService() : IdentifierServiceSimple(31, IdentifierLengthUnit.Utf8Bytes);
}