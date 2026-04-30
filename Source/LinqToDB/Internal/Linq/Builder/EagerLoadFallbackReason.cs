namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Reason a strategy attempt failed and the dispatcher fell back to the next strategy in the
	/// <c>CteUnion → KeyedQuery → Default</c> chain. Set by strategy entry points
	/// (<c>ProcessCteUnionBatch</c> / <c>ProcessEagerLoadingKeyedQuery</c>) before returning <c>null</c>.
	/// Diagnostic-only — the dispatcher does not branch on this value.
	/// </summary>
	enum EagerLoadFallbackReason
	{
		/// <summary>No fallback recorded (success path or the field was never set).</summary>
		None = 0,

		/// <summary>No <c>SqlEagerLoadExpression</c> nodes found — strategy not applicable to the input.</summary>
		NoEagerLoads,

		/// <summary>Nested CTE batch (called with non-empty <c>previousKeys</c>) — CteUnion only handles top-level batches.</summary>
		NestedBatchNotSupported,

		/// <summary>All candidate branches were filtered out before or during construction.</summary>
		NoBranches,

		/// <summary>Branches in the batch have non-matching key types — UNION ALL requires a uniform key shape.</summary>
		KeyTypeMismatch,

		/// <summary>Carrier <c>ValueTuple</c> width would exceed the provider's <c>MaxColumnCount</c>.</summary>
		MaxColumnCountExceeded,

		/// <summary>KeyedQuery: the child references a parent expression that can't be expressed as a <c>VALUES</c> key (e.g. <c>Contains</c> / <c>Any</c> on a parent collection, or a parent ref used only in a projection).</summary>
		ComplexParentReference,

		/// <summary>CteUnion: the batch succeeded for some eager loads but did not include this one (e.g. a branch with zero detail placeholders was skipped).</summary>
		BatchCacheMiss,
	}
}
