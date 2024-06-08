namespace LinqToDB.DataProvider
{
	public abstract class IdentifierServiceBase :  IIdentifierService
	{
		public abstract bool   IsFit(IdentifierKind             identifierKind, string identifier, out int? sizeDecrement);

		public virtual string CorrectAlias(string alias)
		{
			alias = alias.TrimStart('_');

			var cs      = alias.ToCharArray();
			var replace = false;

			for (var i = 0; i < cs.Length; i++)
			{
				var c = cs[i];

				if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')
					continue;

				cs[i]   = ' ';
				replace = true;
			}

			if (replace)
				alias = new string(cs).Replace(" ", "");

			return alias;
		}
	}
}
