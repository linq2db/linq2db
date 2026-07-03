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
		readonly IEnumerator<T> _enumerator;
		readonly IDisposable    _disposable;

		public EnumeratorWrapper(IEnumerator<T> enumerator, IDisposable disposable)
		{
			_enumerator = enumerator;
			_disposable = disposable;
		}

		public T Current => _enumerator.Current;

		object? IEnumerator.Current => _enumerator.Current;

		public bool MoveNext() => _enumerator.MoveNext();

		public void Reset() => _enumerator.Reset();

		public void Dispose()
		{
			_enumerator.Dispose();
			_disposable.Dispose();
		}
	}
}