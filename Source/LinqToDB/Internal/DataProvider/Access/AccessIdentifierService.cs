namespace LinqToDB.Internal.DataProvider.Access
{
	/// <summary>
	/// Jet and ACE both cap a table or field name at 64 characters.
	/// </summary>
	public sealed class AccessIdentifierService() : IdentifierServiceSimple(64);
}
