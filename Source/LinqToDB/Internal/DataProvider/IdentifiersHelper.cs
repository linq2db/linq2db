namespace LinqToDB.Internal.DataProvider
{
	public static class IdentifiersHelper
	{
		public static string TruncateIdentifier(IIdentifierService identifierService, IdentifierKind identifierKind, string identifier)
		{
			if (!identifierService.IsFit(identifierKind, identifier, out var sizeDecrement))
			{
				// TODO: ???
				//TODO: It is quick solution
				var decrement = sizeDecrement.Value + 4;

				var size = identifier.Length - decrement;
				if (size <= 0)
					size = 1;

				// Never cut between the halves of a surrogate pair: the leftover lone surrogate is not
				// a valid identifier character and encodes as a replacement character downstream.
				if (size > 1 && size < identifier.Length && char.IsHighSurrogate(identifier[size - 1]))
					size--;

				identifier = identifier.Substring(0, size);
			}

			return identifier;
		}
	}
}
