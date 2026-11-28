namespace LinqToDB.Internal.DataProvider.Sybase
{
	/// <summary>
	/// ASE allows 255 bytes for most user identifiers, counted in bytes rather than characters.
	/// </summary>
	public sealed class SybaseIdentifierService() : IdentifierServiceSimple(255, IdentifierLengthUnit.Utf8Bytes);
}
