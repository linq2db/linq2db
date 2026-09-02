using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	internal sealed class AsyncEnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>
	{
		private IAsyncEnumerator<T>? _enumerator;
		private readonly Func<Task<Tuple<IAsyncEnumerator<T>, IAsyncDisposable?>>> _init;
		private IAsyncDisposable? _disposable;

		public AsyncEnumeratorAsyncWrapper(Func<Task<Tuple<IAsyncEnumerator<T>, IAsyncDisposable?>>> init)
		{
			_init = init;
		}

		T IAsyncEnumerator<T>.Current => _enumerator!.Current;

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			// The second resource is the read-consistency transaction this wrapper exists to hold open for the whole
			// enumeration, so its release must not be conditional on the inner enumerator disposing cleanly - that one
			// owns a DbDataReader and a DbCommand, either of which can throw on a broken connection.
			try
			{
				await _enumerator!.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if (_disposable != null)
					await _disposable.DisposeAsync().ConfigureAwait(false);
			}
		}

		async ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
		{
			if (_enumerator == null)
			{
				var tuple   = await _init().ConfigureAwait(false);
				_enumerator = tuple.Item1;
				_disposable = tuple.Item2;
			}

			return await _enumerator.MoveNextAsync().ConfigureAwait(false);
		}
	}
}
