using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqToDB.Internal.Linq
{
	// Wraps a result enumerator so a resource acquired before enumeration (the read-consistency transaction opened by
	// ExpressionQuery.StartLoadTransaction) lives for the whole enumeration and is disposed together with the enumerator.
	// The sync counterpart of AsyncEnumeratorAsyncWrapper: without it, "using (StartLoadTransaction()) return enumerator;"
	// would dispose the transaction before the (lazy) enumerator runs any SQL, so the read would execute outside it.
	internal sealed class EnumeratorWrapper<T> : IEnumerator<T>
	{
		readonly IEnumerator<T>        _enumerator;
		readonly Query.LoadTransaction _transaction;

		bool _faulted;

		public EnumeratorWrapper(IEnumerator<T> enumerator, Query.LoadTransaction transaction)
		{
			_enumerator  = enumerator;
			_transaction = transaction;
		}

		public T Current => _enumerator.Current;

		object? IEnumerator.Current => _enumerator.Current;

		public bool MoveNext()
		{
			try
			{
				return _enumerator.MoveNext();
			}
			catch
			{
				_faulted = true;
				throw;
			}
		}

		public void Reset() => _enumerator.Reset();

		public void Dispose()
		{
			// The second resource is the read-consistency transaction this wrapper exists to hold open for the whole
			// enumeration, so its release must not be conditional on the inner enumerator disposing cleanly - that one
			// owns a DbDataReader and a DbCommand, either of which can throw on a broken connection.
			// A data-modifying query commits after the reader is closed, unless the enumeration failed.
			try
			{
				_enumerator.Dispose();

				if (!_faulted)
					_transaction.Complete();
			}
			finally
			{
				_transaction.Dispose();
			}
		}
	}
}
