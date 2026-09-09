using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Async
{
	internal sealed class AsyncEnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>
	{
		private IAsyncEnumerator<T>? _enumerator;
		private readonly Func<Task<Tuple<IAsyncEnumerator<T>, IAsyncDisposable?>>> _init;
		private IAsyncDisposable? _disposable;
		private bool _disposed;

		public AsyncEnumeratorAsyncWrapper(Func<Task<Tuple<IAsyncEnumerator<T>, IAsyncDisposable?>>> init)
		{
			_init = init;
		}

		T IAsyncEnumerator<T>.Current
		{
			get
			{
				if (_enumerator == null)
					throw new InvalidOperationException(ErrorHelper.Error_EnumerationNotStarted);

				return _enumerator.Current;
			}
		}

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			if (_disposed)
				return;

			_disposed = true;

			// The second resource is the read-consistency transaction this wrapper exists to hold open for the whole
			// enumeration, so its release must not be conditional on the inner enumerator disposing cleanly - that one
			// owns a DbDataReader and a DbCommand, either of which can throw on a broken connection.
			try
			{
				if (_enumerator != null)
					await _enumerator.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if (_disposable != null)
					await _disposable.DisposeAsync().ConfigureAwait(false);
			}
		}

		async ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
		{
			// without this the disposed instance re-runs _init, and the enumerator and load
			// transaction it opens are unreachable from DisposeAsync, which returns at the flag
			if (_disposed)
				return false;

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
