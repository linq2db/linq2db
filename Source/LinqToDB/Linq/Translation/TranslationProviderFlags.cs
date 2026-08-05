using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	/// <summary>
	/// Read-only, translation-relevant subset of the provider's <c>SqlProviderFlags</c>, exposed to member
	/// translators via <see cref="ITranslationContext.ProviderFlags"/>. Only the flags member translators need
	/// are surfaced here, rather than the whole <c>SqlProviderFlags</c>.
	/// </summary>
	public sealed class TranslationProviderFlags
	{
		/// <summary>Initializes a new <see cref="TranslationProviderFlags"/>.</summary>
		/// <param name="defaultNullsOrdering">The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</param>
		/// <param name="isNullsOrderingSupported">Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</param>
		/// <param name="isIntervalDifferenceSupported">Whether the provider can lower an elapsed difference between two date/time values to SQL.</param>
		public TranslationProviderFlags(NullsDefaultOrdering defaultNullsOrdering, bool isNullsOrderingSupported, bool isIntervalDifferenceSupported)
		{
			DefaultNullsOrdering          = defaultNullsOrdering;
			IsNullsOrderingSupported      = isNullsOrderingSupported;
			IsIntervalDifferenceSupported = isIntervalDifferenceSupported;
		}

		/// <summary>The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</summary>
		public NullsDefaultOrdering DefaultNullsOrdering { get; }

		/// <summary>Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</summary>
		public bool IsNullsOrderingSupported { get; }

		/// <summary>
		/// Whether the provider can lower an elapsed difference between two date/time values to SQL. Checked
		/// before an interval node is created, since translation is the last point at which the expression can
		/// still fall back to client-side evaluation.
		/// </summary>
		public bool IsIntervalDifferenceSupported { get; }
	}
}
