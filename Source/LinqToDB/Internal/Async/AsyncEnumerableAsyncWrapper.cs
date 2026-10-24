using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Async
{
	// Transfers ownership of a resource acquired before enumeration (the read-consistency transaction opened by
	// ExpressionQuery.StartLoadTransactionAsync) to the returned sequence, so it lives for the whole enumeration
	// instead of being released when the method that built the sequence returns. The IAsyncEnumerable counterpart of
	// EnumeratorWrapper: without it, "await using var tr = ...; return enumerable;" disposes the transaction before
	// the caller enumerates anything, leaving the main query to stream outside it.
	//
	// The resource is released when the FIRST enumerator is disposed - the transaction was opened for this one result,
	// so re-enumerating the sequence afterwards runs outside it.
	internal sealed class AsyncEnumerableAsyncWrapper<T> : IAsyncEnumerable<T>
	{
		readonly IAsyncEnumerable<T>   _enumerable;
		readonly Query.LoadTransaction _transaction;

		public AsyncEnumerableAsyncWrapper(IAsyncEnumerable<T> enumerable, Query.LoadTransaction transaction)
		{
			_enumerable  = enumerable;
			_transaction = transaction;
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			return new AsyncEnumeratorAsyncWrapper<T>(
				() => Task.FromResult(Tuple.Create(_enumerable.GetAsyncEnumerator(cancellationToken), (Query.LoadTransaction?)_transaction)));
		}
	}
}
