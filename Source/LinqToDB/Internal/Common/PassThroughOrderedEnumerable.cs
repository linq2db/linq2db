using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Wraps an arbitrary <see cref="IEnumerable{T}"/> and exposes it as
	/// <see cref="IOrderedEnumerable{T}"/> without performing any actual ordering. Used by
	/// <c>ExpressionBuilder.AdjustType</c> when a result needs to satisfy the
	/// <see cref="IOrderedEnumerable{T}"/> shape (so subsequent <c>ThenBy</c>-style calls
	/// still type-check) but the source is already in the desired order — typically when
	/// the SQL layer has driven the ordering and an in-memory re-sort would be redundant.
	/// </summary>
	internal sealed class PassThroughOrderedEnumerable<T> : IOrderedEnumerable<T>, IEnumerable<T>, IEnumerable
	{
		readonly IEnumerable<T> _source;

		public PassThroughOrderedEnumerable(IEnumerable<T> source)
		{
			_source = source;
		}

		public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(System.Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
		{
			// Subsequent ThenBy / ThenByDescending: ignore additional ordering — the data is
			// already in the SQL-driven order. We're a type-shape adapter, not a sorter.
			return this;
		}

		public IEnumerator<T> GetEnumerator() => _source.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
