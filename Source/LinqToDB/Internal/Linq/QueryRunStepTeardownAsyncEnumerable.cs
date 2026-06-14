using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Wraps an <see cref="IAsyncEnumerable{T}"/> so that disposal of each enumerator triggers
	/// the teardown closure. Used by execute paths that return <see cref="IAsyncEnumerable{T}"/>
	/// instead of <see cref="IAsyncEnumerator{T}"/> directly.
	/// </summary>
	sealed class QueryRunStepTeardownAsyncEnumerable<T> : IAsyncEnumerable<T>
	{
		readonly IAsyncEnumerable<T> _inner;
		readonly Func<ValueTask>     _teardown;

		public QueryRunStepTeardownAsyncEnumerable(IAsyncEnumerable<T> inner, Func<ValueTask> teardown)
		{
			_inner    = inner;
			_teardown = teardown;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			=> new QueryRunStepTeardownAsyncEnumerator<T>(_inner.GetAsyncEnumerator(cancellationToken), _teardown);
	}
}
