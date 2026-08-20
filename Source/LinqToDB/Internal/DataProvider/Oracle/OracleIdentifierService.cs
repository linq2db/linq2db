namespace LinqToDB.Internal.DataProvider.Oracle
{
	/// <summary>
	/// Oracle 12.2 and later allow 128 bytes; the limit is counted in bytes, so a multibyte name reaches
	/// it sooner than its character count suggests.
	/// </summary>
	public sealed class OracleIdentifierService() : IdentifierServiceSimple(128, IdentifierLengthUnit.Utf8Bytes);
}