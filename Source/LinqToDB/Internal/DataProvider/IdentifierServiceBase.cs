using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.DataProvider
{
	public abstract class IdentifierServiceBase :  IIdentifierService
	{
		public abstract bool   IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement);

		/// <summary>
		/// Characters <see cref="CorrectAlias"/> keeps; everything else is dropped. C# allows any
		/// Unicode letter or digit in an identifier and the SQL builders quote an alias that needs it,
		/// so those are kept by default - stripping to ASCII turned a name like <c>顧客</c> into an
		/// empty string and the alias silently became <c>t1</c>. Punctuation, whitespace, control and
		/// surrogate characters always go. A provider whose dialect cannot carry non-ASCII identifiers
		/// overrides this to restrict aliases to ASCII.
		/// </summary>
		protected virtual bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

		public virtual string CorrectAlias(string alias)
		{
			alias = alias.TrimStart('_');

			var cs      = alias.ToCharArray();
			var replace = false;

			for (var i = 0; i < cs.Length; i++)
			{
				var c = cs[i];

				if (IsIdentifierChar(c))
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
