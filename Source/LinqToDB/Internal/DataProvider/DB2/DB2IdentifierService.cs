namespace LinqToDB.Internal.DataProvider.DB2
{
	/// <summary>
	/// Db2 counts its 128 in bytes: SQL0107N rejects a name that fits in characters but not in bytes.
	/// </summary>
	public sealed class DB2IdentifierService() : IdentifierServiceSimple(128, IdentifierLengthUnit.Utf8Bytes);
}
