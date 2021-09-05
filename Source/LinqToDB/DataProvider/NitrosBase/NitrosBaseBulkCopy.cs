using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.NitrosBase
{
	class NitrosBulkCopy : BasicBulkCopy
	{
		// TODO: add implementation if base class implementation is not sufficient
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy2(table, options, source, null);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy2Async(table, options, source, null, cancellationToken);
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy2Async(table, options, source, null, cancellationToken);
		}
#endif
	}
}
