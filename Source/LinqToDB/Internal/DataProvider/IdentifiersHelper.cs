namespace LinqToDB.Internal.DataProvider
{
	public static class IdentifiersHelper
	{
		public static string TruncateIdentifier(IIdentifierService identifierService, IdentifierKind identifierKind, string identifier)
		{
			return TruncateIdentifier(identifierService, identifierKind, identifier, reserve: 4);
		}

		// reserve: units kept free below the limit. An alias keeps four for the "_<n>" suffix the uniquifier
		// appends; a generated name nothing uniquifies (a primary-key name) uses the full limit.
		internal static string TruncateIdentifier(IIdentifierService identifierService, IdentifierKind identifierKind, string identifier, int reserve)
		{
			if (!identifierService.IsFit(identifierKind, identifier, out var sizeDecrement))
			{
				var decrement = sizeDecrement.Value + reserve;

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
