using System.Collections.Generic;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;
	using System.Threading.Tasks;

	class SQLiteBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1Async(table, options, source);
		}

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			return MultipleRowsCopy1Async(table, options, source);
		}
#endif
	}
}
