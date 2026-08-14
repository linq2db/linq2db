using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LinqToDB.Internal.DataProvider
{
	public sealed class IdentifierServiceSimple : IdentifierServiceBase
	{
		public int                  MaxLength { get; }
		public IdentifierLengthUnit Unit      { get; }

		public IdentifierServiceSimple(int maxLength)
			: this(maxLength, IdentifierLengthUnit.Characters)
		{
		}

		public IdentifierServiceSimple(int maxLength, IdentifierLengthUnit unit)
		{
			MaxLength  = maxLength;
			Unit       = unit;

			if (maxLength <= 4)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "MaxLength should be at least 4");
		}

		public override bool IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement)
		{
			if (Unit == IdentifierLengthUnit.Characters)
			{
				if (identifier.Length > MaxLength)
				{
					sizeDecrement = identifier.Length - MaxLength;
					return false;
				}

				sizeDecrement = null;
				return true;
			}

			if (Encoding.UTF8.GetByteCount(identifier) <= MaxLength)
			{
				sizeDecrement = null;
				return true;
			}

			// sizeDecrement is consumed as a count of trailing characters to drop, so the byte overflow
			// has to be converted back into characters - one character weighs one to four UTF-8 bytes.
			// Encoder.Convert fills a MaxLength-sized buffer and reports how many characters got in,
			// never splitting a surrogate pair; flush: false also holds back a dangling high surrogate.
			// MaxLength is always greater than the four bytes of the widest character, so at least one
			// character fits and Convert has something to write.
			var encoder = Encoding.UTF8.GetEncoder();
			int charsUsed;

#if SUPPORTS_SPAN
			// allocate memory on the stack if possible - every real provider's limit is far below this
			Span<byte> buffer = MaxLength < 500 ? stackalloc byte[MaxLength] : new byte[MaxLength];
			encoder.Convert(identifier.AsSpan(), buffer, false, out charsUsed, out _, out _);
#else
			var buffer = new byte[MaxLength];
			encoder.Convert(identifier.ToCharArray(), 0, identifier.Length, buffer, 0, MaxLength, false, out charsUsed, out _, out _);
#endif

			sizeDecrement = identifier.Length - charsUsed;
			return false;
		}
	}
}
