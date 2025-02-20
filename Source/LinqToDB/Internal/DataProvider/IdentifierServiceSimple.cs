using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.DataProvider;

namespace LinqToDB.Internal.DataProvider
{
	public class IdentifierServiceSimple : IdentifierServiceBase
	{
		public int MaxLength { get; }

		public IdentifierServiceSimple(int maxLength)
		{
			MaxLength  = maxLength;

			if (maxLength <= 4)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "MaxLength should be at least 4");
		}

		public override bool IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement)
		{
			if (identifier.Length > MaxLength)
			{
				sizeDecrement = identifier.Length - MaxLength;
				return false;
			}

			sizeDecrement = null;
			return true;
		}
	}
}
