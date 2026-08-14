using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.DataProvider
{
	public abstract class IdentifierServiceBase :  IIdentifierService
	{
		public abstract bool   IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement);

		public virtual string CorrectAlias(string alias)
		{
			alias = alias.TrimStart('_');

			var cs      = alias.ToCharArray();
			var replace = false;

			for (var i = 0; i < cs.Length; i++)
			{
				var c = cs[i];

				// C# allows any Unicode letter or digit in an identifier, and the SQL builders quote an
				// alias that needs it, so keep those instead of dropping them - stripping to ASCII turned
				// a name like "顧客" into an empty string and the alias silently became t1. Everything
				// else (punctuation, whitespace, control and surrogate characters) still goes.
				if (char.IsLetterOrDigit(c) || c == '_')
					continue;

				cs[i]   = ' ';
				replace = true;
			}

			if (replace)
				alias = new string(cs).Replace(" ", "", System.StringComparison.Ordinal);

			return alias;
		}
	}
}
