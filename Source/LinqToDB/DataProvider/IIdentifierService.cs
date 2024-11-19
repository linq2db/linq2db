namespace LinqToDB.DataProvider
{
	public interface IIdentifierService
	{
		bool   IsFit(IdentifierKind identifierKind, string identifier, out int? sizeDecrement);
		string CorrectAlias(string  alias);

		// Maybe in the future
		// void QuoteIdentifier(IdentifierKind identifierKind, string identifier, out string quotedIdentifier);
	}
}
