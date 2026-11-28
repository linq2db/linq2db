namespace LinqToDB.Internal.DataProvider.Oracle
{
	/// <summary>
	/// 128 bytes, as Oracle 12.2 and later allow; the 12c dialect assumes 12.2 or later (12.1 still caps
	/// names at 30 bytes). The limit is counted in bytes, so a multibyte name reaches it sooner than its
	/// character count suggests.
	/// </summary>
	public sealed class OracleIdentifierService() : IdentifierServiceSimple(128, IdentifierLengthUnit.Utf8Bytes);
}