using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Provider-specific implementation of BulkCopy for YDB.
	/// </summary>
	public class YdbBulkCopy : BasicBulkCopy
	{
		// YDB limits on query length/number of parameters are not documented yet, so we use safe values.
		protected override int MaxParameters => 16_000;
		protected override int MaxSqlLength => 256_000;

		#region Generic fallbacks (MultipleRowsCopyX)

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

		#endregion
	}
}
