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
		public TranslationProviderFlags(NullsDefaultOrdering defaultNullsOrdering, bool isNullsOrderingSupported)
			: this(defaultNullsOrdering, isNullsOrderingSupported, false, false)
		{
		}

		/// <summary>Initializes a new <see cref="TranslationProviderFlags"/>.</summary>
		/// <param name="defaultNullsOrdering">The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</param>
		/// <param name="isNullsOrderingSupported">Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</param>
		/// <param name="canLowerIntervalDifference">Whether an elapsed date difference can be lowered to a value.</param>
		/// <param name="canLowerIntervalPart">Whether a member of an elapsed date difference can be lowered.</param>
		/// <param name="intervalResolution">The finest unit the provider can resolve when measuring elapsed time.</param>
		public TranslationProviderFlags(
			NullsDefaultOrdering defaultNullsOrdering,
			bool                 isNullsOrderingSupported,
			bool                 canLowerIntervalDifference,
			bool                 canLowerIntervalPart,
			SqlIntervalUnit      intervalResolution = SqlIntervalUnit.Tick)
		{
			DefaultNullsOrdering       = defaultNullsOrdering;
			IsNullsOrderingSupported   = isNullsOrderingSupported;
			CanLowerIntervalDifference = canLowerIntervalDifference;
			CanLowerIntervalPart       = canLowerIntervalPart;
			IntervalResolution         = intervalResolution;
		}

		/// <summary>The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</summary>
		public NullsDefaultOrdering DefaultNullsOrdering { get; }

		/// <summary>Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</summary>
		public bool IsNullsOrderingSupported { get; }

		/// <summary>
		/// Whether an elapsed date difference can be lowered to a value. Answered by the provider's
		/// <c>SqlExpressionConvertVisitor</c>, which owns the lowering and documents the contract.
		/// </summary>
		public bool CanLowerIntervalDifference { get; }

		/// <summary>
		/// Whether a member of an elapsed date difference can be lowered. Separate from
		/// <see cref="CanLowerIntervalDifference"/> because a provider may have only this half.
		/// </summary>
		public bool CanLowerIntervalPart { get; }

		/// <summary>
		/// The finest unit the provider can resolve when measuring elapsed time. A <em>component</em> asked for in
		/// a finer unit is identically zero rather than merely imprecise, so the translator declines to build it
		/// and leaves the member to .NET.
		/// </summary>
		public SqlIntervalUnit IntervalResolution { get; }
	}
}
