using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Length-only identifier rules, and the base each provider's own service derives from - the same
	/// shape the provider-specific mapping schemas and member translators use. A provider whose rules go
	/// beyond a single length overrides <see cref="GetMaxLength"/> or
	/// <see cref="IdentifierServiceBase.IsIdentifierChar"/> rather than carrying extra state here, which
	/// keeps the type transportable to a remote client by name alone.
	/// </summary>
	public class IdentifierServiceSimple : IdentifierServiceBase
	{
		/// <summary>
		/// Longest identifier the server accepts, counted in <see cref="Unit"/>.
		/// </summary>
		public int                  MaxLength { get; }

		/// <summary>
		/// Unit <see cref="MaxLength"/> is counted in.
		/// </summary>
		public IdentifierLengthUnit Unit      { get; }

		/// <summary>
		/// Creates a service whose limit is counted in characters.
		/// </summary>
		/// <param name="maxLength">Longest identifier the server accepts; must be greater than four,
		/// since truncation reserves four units for the suffix that makes a truncated name unique.</param>
		public IdentifierServiceSimple(int maxLength)
			: this(maxLength, IdentifierLengthUnit.Characters)
		{
		}

		/// <summary>
		/// Creates a service whose limit is counted in the given unit.
		/// </summary>
		/// <param name="maxLength">Longest identifier the server accepts; must be greater than four,
		/// since truncation reserves four units for the suffix that makes a truncated name unique.</param>
		/// <param name="unit">Whether the server counts characters or UTF-8 bytes.</param>
		public IdentifierServiceSimple(int maxLength, IdentifierLengthUnit unit)
		{
			MaxLength  = maxLength;
			Unit       = unit;

			if (maxLength <= 4)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "MaxLength should be greater than 4");
		}

		/// <summary>
		/// Limit for a particular kind of name. Providers whose cap differs by kind override this -
		/// MySQL, for instance, caps an object name at 64 but an alias at 256, and applying the smaller
		/// one truncates aliases the server would have accepted.
		/// </summary>
		protected virtual int GetMaxLength(IdentifierKind identifierKind) => MaxLength;

		public override bool IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement)
		{
			var maxLength = GetMaxLength(identifierKind);

			if (Unit == IdentifierLengthUnit.Characters)
			{
				if (identifier.Length > maxLength)
				{
					sizeDecrement = identifier.Length - maxLength;
					return false;
				}

				sizeDecrement = null;
				return true;
			}

			if (Encoding.UTF8.GetByteCount(identifier) <= maxLength)
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
			Span<byte> buffer = maxLength < 500 ? stackalloc byte[maxLength] : new byte[maxLength];
			encoder.Convert(identifier.AsSpan(), buffer, false, out charsUsed, out _, out _);
#else
			var buffer = new byte[maxLength];
			encoder.Convert(identifier.ToCharArray(), 0, identifier.Length, buffer, 0, maxLength, false, out charsUsed, out _, out _);
#endif

			sizeDecrement = identifier.Length - charsUsed;
			return false;
		}
	}
}
