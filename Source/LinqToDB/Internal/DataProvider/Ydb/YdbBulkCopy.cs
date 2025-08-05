using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Provider-specific implementation of BulkCopy for YDB.
	/// </summary>
	public class YdbBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source)
			=> MultipleRowsCopy1(table, options, source);

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source,
			CancellationToken cancellationToken)
			=> MultipleRowsCopy1Async(table, options, source, cancellationToken);

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table,
			DataOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
			=> MultipleRowsCopy1Async(table, options, source, cancellationToken);
	}
}
