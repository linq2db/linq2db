using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Async
{
	internal sealed class AsyncEnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>
	{
		private IAsyncEnumerator<T>? _enumerator;
		private readonly Func<Task<Tuple<IAsyncEnumerator<T>, Query.LoadTransaction?>>> _init;
		private Query.LoadTransaction? _transaction;
		private bool _disposed;
		private bool _faulted;

		public AsyncEnumeratorAsyncWrapper(Func<Task<Tuple<IAsyncEnumerator<T>, Query.LoadTransaction?>>> init)
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
			// The transaction commits after the reader is closed, unless the load failed.
			try
			{
				if (_enumerator != null)
					await _enumerator.DisposeAsync().ConfigureAwait(false);

				if (!_faulted && _transaction != null)
					await _transaction.CompleteAsync(default).ConfigureAwait(false);
			}
			finally
			{
				if (_transaction != null)
					await _transaction.DisposeAsync().ConfigureAwait(false);
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
				var tuple    = await _init().ConfigureAwait(false);
				_enumerator  = tuple.Item1;
				_transaction = tuple.Item2;
			}

			try
			{
				return await _enumerator.MoveNextAsync().ConfigureAwait(false);
			}
			catch
			{
				_faulted = true;
				throw;
			}
		}
	}
}
