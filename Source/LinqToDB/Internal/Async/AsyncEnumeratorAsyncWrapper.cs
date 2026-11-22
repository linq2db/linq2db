using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Async
{
	internal sealed class AsyncEnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>
	{
		private IAsyncEnumerator<T>? _enumerator;
		private readonly Func<Task<IAsyncEnumerator<T>>> _init;
		private bool _disposed;

		public AsyncEnumeratorAsyncWrapper(Func<Task<IAsyncEnumerator<T>>> init)
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

			if (_enumerator != null)
				await _enumerator.DisposeAsync().ConfigureAwait(false);
		}

		async ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
		{
			// without this the disposed instance re-runs _init, and the enumerator it opens is unreachable from
			// DisposeAsync, which returns at the flag
			if (_disposed)
				return false;

			_enumerator ??= await _init().ConfigureAwait(false);

			return await _enumerator.MoveNextAsync().ConfigureAwait(false);
		}
	}
}
