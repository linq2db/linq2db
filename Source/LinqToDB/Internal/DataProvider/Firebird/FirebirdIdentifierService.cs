namespace LinqToDB.Internal.DataProvider.Firebird
{
	/// <summary>
	/// Firebird 4 stores metadata as UTF8 and caps it at 63 characters.
	/// </summary>
	public sealed class FirebirdIdentifierService() : IdentifierServiceSimple(63);
}