using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.CodeGen.CodeModel
{
	public interface ILanguageServices
	{
		string? GetAlias(CodeIdentifier[]? @namespace, string name);
		string NormalizeIdentifier(string name);
		ISet<string> GetUniqueNameCollection();
		IEqualityComparer<string> GetNameComparer();
		string MakeUnique(ISet<string> knownNames, string name);
		bool IsValidIdentifierFirstChar(string character, UnicodeCategory category);
		bool IsValidIdentifierNonFirstChar(string character, UnicodeCategory category);
	}
}
