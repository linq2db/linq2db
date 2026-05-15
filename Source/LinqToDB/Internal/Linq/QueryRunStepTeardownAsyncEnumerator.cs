using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Async counterpart of <see cref="QueryRunStepTeardownEnumerator{T}"/>. Fires the teardown
	/// closure exactly once when the enumerator is asynchronously disposed.
	/// </summary>
	sealed class QueryRunStepTeardownAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		readonly IAsyncEnumerator<T> _inner;
		readonly Func<ValueTask>     _teardown;
		bool                         _torn;

		public QueryRunStepTeardownAsyncEnumerator(IAsyncEnumerator<T> inner, Func<ValueTask> teardown)
		{
			_inner    = inner;
			_teardown = teardown;
		}

		public T Current => _inner.Current;

		public ValueTask<bool> MoveNextAsync() => _inner.MoveNextAsync();

		public async ValueTask DisposeAsync()
		{
			try
			{
				await _inner.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if (!_torn)
				{
					_torn = true;
					await _teardown().ConfigureAwait(false);
				}
			}
		}
	}
}
