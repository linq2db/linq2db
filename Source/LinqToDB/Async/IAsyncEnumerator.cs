#if !NATIVE_ASYNC
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Asynchronous version of the IEnumerator&lt;T&gt; interface, allowing elements to be retrieved asynchronously.
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	[PublicAPI]
	public interface IAsyncEnumerator<out T> : IAsyncDisposable
	{
		/// <summary>Gets the current element in the iteration.</summary>
		T Current { get; }

		/// <summary>
		/// Advances the enumerator to the next element in the sequence, returning the result asynchronously.
		/// </summary>
		/// <returns>
		/// Task containing the result of the operation: true if the enumerator was successfully advanced
		/// to the next element; false if the enumerator has passed the end of the sequence.
		/// </returns>
		Task<bool> MoveNextAsync();
	}

	internal class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T>    _enumerator;
		private readonly CancellationToken _cancellationToken;

		public AsyncEnumeratorImpl(IEnumerator<T> enumerator, CancellationToken cancellationToken)
		{
			_enumerator        = enumerator;
			_cancellationToken = cancellationToken;
		}

		T IAsyncEnumerator<T>.Current => _enumerator.Current;

		Task IAsyncDisposable.DisposeAsync()
		{
			_enumerator.Dispose();
			return TaskEx.CompletedTask;
		}

		Task<bool> IAsyncEnumerator<T>.MoveNextAsync()
		{
			_cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(_enumerator.MoveNext());
		}
	}
}
#endif
