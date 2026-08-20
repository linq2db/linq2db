using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.DataProvider
{
	public abstract class IdentifierServiceBase :  IIdentifierService
	{
		public abstract bool   IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement);

		/// <summary>
		/// Characters <see cref="CorrectAlias"/> keeps; everything else is dropped. Unicode letters and
		/// digits are kept because the provider's builder delimits a name that needs it, so the decision
		/// of whether such a name can be emitted belongs to the provider rather than to this filter -
		/// stripping to ASCII turned a name like <c>顧客</c> into an empty string and the alias silently
		/// became <c>t1</c>. Punctuation, whitespace, control and surrogate characters always go, and a
		/// provider whose dialect cannot carry non-ASCII identifiers at all overrides this to restrict
		/// aliases to ASCII, as Informix does.
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
