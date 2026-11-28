namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Limits assumed when a provider declares none: 128 characters, which every supported server meets
	/// or exceeds.
	/// </summary>
	public sealed class DefaultIdentifierService() : IdentifierServiceSimple(128);
}
