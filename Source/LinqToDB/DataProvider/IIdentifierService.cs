using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.DataProvider
{
	public interface IIdentifierService
	{
		bool   IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement);
		string CorrectAlias(string  alias);

		// Maybe in the future
		// void QuoteIdentifier(IdentifierKind identifierKind, string identifier, out string quotedIdentifier);
	}
}
