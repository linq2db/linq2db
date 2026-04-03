using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBBulkCopy : BasicBulkCopy
	{
		protected override int MaxParameters => 2048;
		protected override int MaxSqlLength  => 1000000;

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
	}
}
