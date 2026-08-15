namespace LinqToDB.Internal.DataProvider.SapHana
{
	/// <summary>
	/// 127 for every kind. A column alias may reach 255, but a table alias may not - HANA answers a
	/// longer one with "identifier is too long", and a table alias is what aliasing generates most.
	/// </summary>
	public sealed class SapHanaIdentifierService() : IdentifierServiceSimple(127);
}
