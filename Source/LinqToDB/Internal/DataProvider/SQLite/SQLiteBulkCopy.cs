using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	sealed class SQLiteBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// We subtract 1 based on possibility of ADO Provider using parameter for command.
		/// </remarks>
		protected override int MaxParameters => 998;
		/// <remarks>
		/// Based on https://www.sqlite.org/limits.html.
		/// Since SQLite is parsed locally by the lib, we aren't worried about network congestion and keep the max.
		/// </remarks>
		protected override int MaxSqlLength => 1000000;

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
