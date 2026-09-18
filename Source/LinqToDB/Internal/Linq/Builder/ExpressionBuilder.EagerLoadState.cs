using System.Collections.Generic;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		/// <summary>
		/// Scoped state for a single eager-loading strategy attempt. A fresh instance is created at
		/// the top of each iteration of the strategy-fallback loop in <see cref="CompleteEagerLoadingExpressions"/>,
		/// and on success it is committed to <see cref="_eagerLoadState"/>. On failure the instance is
		/// discarded — no rollback plumbing required, because nothing outside of <c>CompleteEagerLoadingExpressions</c>
		/// can observe it until commit.
		/// </summary>
		sealed class EagerLoadState
		{
			/// <summary>Set by ProcessEagerLoadingKeyedQuery to signal BuildQuery that buffer materialization is needed.</summary>
			public bool HasKeyedQueryPreambles;

			/// <summary>True when <see cref="CteUnionInfo"/> holds a single-query CteUnion setup.</summary>
			public bool HasCteUnionQuery;

			/// <summary>Phase-2 info populated by ProcessCteUnionBatch for the single-query CteUnion mode.</summary>
			public CteUnionPhase2Info? CteUnionInfo;

			/// <summary>
			/// True when a strategy requested that <c>_query.IsFinalized</c> be set on commit.
			/// Populated mid-attempt (e.g. by CteUnion batching); only applied to the real query once
			/// <see cref="CompleteEagerLoadingExpressions"/> decides the attempt succeeded.
			/// </summary>
			public bool QueryFinalizedRequested;

			/// <summary>
			/// Reason this attempt failed (if it failed). Set by strategy entry points before they
			/// return <c>null</c>; left at <see cref="EagerLoadFallbackReason.None"/> on success.
			/// Recorded into <see cref="LastEagerLoadFallbackChain"/> by the dispatcher when an attempt
			/// is discarded — diagnostic-only, the dispatcher does not branch on this value.
			/// </summary>
			public EagerLoadFallbackReason FallbackReason;
		}

		/// <summary>
		/// Strategy attempts that failed (and fell back) before the most recent successful
		/// <see cref="CompleteEagerLoadingExpressions"/> call. Empty when the first attempted strategy
		/// succeeded. Diagnostic-only — exposed for tests / traces. Populated in dispatcher order
		/// (first failed attempt first).
		/// </summary>
		internal List<(EagerLoadingStrategy Strategy, EagerLoadFallbackReason Reason)>? LastEagerLoadFallbackChain { get; private set; }

		/// <summary>
		/// Committed eager-load state from the most recent successful <see cref="CompleteEagerLoadingExpressions"/>
		/// call. Consumed (nulled out) by <see cref="BuildQuery{T}"/> when it wires up buffer materialization
		/// or the CteUnion streaming path.
		/// </summary>
		EagerLoadState? _eagerLoadState;
	}
}
