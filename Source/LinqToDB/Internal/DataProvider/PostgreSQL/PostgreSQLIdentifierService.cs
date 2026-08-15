namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	/// <summary>
	/// PostgreSQL truncates rather than rejects, so a name past the limit is silently shortened and two
	/// that differ only past it collapse into one. 63 is NAMEDATALEN - 1 for a standard build.
	/// </summary>
	public sealed class PostgreSQLIdentifierService() : IdentifierServiceSimple(63, IdentifierLengthUnit.Utf8Bytes);
}
