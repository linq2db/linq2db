using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Internal materialization adapter: wraps an already-ordered <see cref="IEnumerable{T}"/> and
	/// exposes it as <see cref="IOrderedEnumerable{T}"/> without performing any further ordering.
	/// <para>
	/// Constructed only by <c>ExpressionBuilder</c>'s <c>AdjustType</c>, when an eager-loaded child
	/// collection must satisfy the <see cref="IOrderedEnumerable{T}"/> shape (so a child sub-query that
	/// ends in <c>OrderBy</c> type-checks). The wrapped sequence is already in its final order — the SQL
	/// layer drove the ordering — so an in-memory re-sort would be redundant.
	/// </para>
	/// <para>
	/// <see cref="CreateOrderedEnumerable{TKey}"/> (the entry point for a chained
	/// <c>ThenBy</c>/<c>ThenByDescending</c>) deliberately returns the sequence unchanged: the data is
	/// already materialized in its final order, so a tie-break key cannot meaningfully reorder a
	/// concrete, fully-ordered sequence. Code that needs a different in-memory ordering of the
	/// materialized result should re-sort it explicitly.
	/// </para>
	/// </summary>
	internal sealed class PassThroughOrderedCollection<T> : IOrderedEnumerable<T>, IEnumerable<T>, IEnumerable
	{
		readonly IEnumerable<T> _source;

		public PassThroughOrderedCollection(IEnumerable<T> source)
		{
			_source = source;
		}

		public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
		{
			// Subsequent ThenBy / ThenByDescending is a deliberate no-op: the wrapped sequence is already
			// materialized in its final SQL-driven order, so there is no tie left to break. See the
			// type's <summary> for why this adapter exists and why re-sorting here would be redundant.
			return this;
		}

		public IEnumerator<T> GetEnumerator() => _source.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
