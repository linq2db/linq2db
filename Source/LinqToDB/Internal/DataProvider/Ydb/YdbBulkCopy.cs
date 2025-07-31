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
	sealed class YdbBulkCopy : BasicBulkCopy
	{
		readonly YdbDataProvider _provider;

		public YdbBulkCopy(YdbDataProvider provider)
		{
			_provider = provider;
		}

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

		#region Provider-specific entry points

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source)
		{
			if (table.TryGetDataConnection(out var dc))
				return ProviderSpecificCopyImpl(dc, table, options, source);

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dc))
				return Task.Run(() => ProviderSpecificCopyImpl(dc, table, options, source), cancellationToken);

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			DataOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dc))
				return ProviderSpecificCopyImplAsync(dc, table, options, source, cancellationToken);

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		#endregion

		#region Synchronous implementation

		BulkCopyRowsCopied ProviderSpecificCopyImpl<T>(
			DataConnection dc,
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source)
			where T : notnull
		{
			// 1. Get native YDB connection
			var ydbConnection = _provider.TryGetProviderConnection(dc, dc.OpenDbConnection());

			if (ydbConnection == null || _provider.Adapter.BeginBinaryImport == null)
				return MultipleRowsCopy(table, options, source);

			// 2. SQL builder + table metadata
			var sqlBuilder = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dc.Options);
			var ed         = table.DataContext.MappingSchema
									 .GetEntityDescriptor(typeof(T), dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

			var columns = ed.Columns
							.Where(c => !c.SkipOnInsert ||
										options.BulkCopyOptions.KeepIdentity == true && c.IsIdentity)
							.ToArray();

			var tableName   = GetTableName(sqlBuilder, options.BulkCopyOptions, table);
			var fragments = columns
			.Select(c => Convert.ToString(
				sqlBuilder.ConvertInline(
					c.ColumnName,
					ConvertType.NameToQueryField),
				CultureInfo.InvariantCulture))
			.ToArray();

			// Then combine them into a single string via the non-generic overload
			var fieldList = string.Join(", ", fragments);

			var copyCommand = $"INSERT INTO {tableName} ({fieldList}) VALUES"; // YDB expects this exact header

			// 3. Start bulk session
			var writer = _provider.Adapter.BeginBinaryImport(ydbConnection, copyCommand);

			var rowsCopied = new BulkCopyRowsCopied();
			var batchSize  = Math.Max(10, options.BulkCopyOptions.MaxBatchSize ?? 10_000);
			var current    = 0;

			try
			{
				foreach (var item in source)
				{
					object[] values = new object[columns.Length];

					for (int i = 0; i < columns.Length; i++)
						values[i] = columns[i].GetProviderValue(item) ?? DBNull.Value;

					writer.WriteRow(values);

					rowsCopied.RowsCopied++;
					current++;

					// callback
					var opts = options.BulkCopyOptions;
					if (opts.NotifyAfter != 0 && opts.RowsCopiedCallback != null &&
						rowsCopied.RowsCopied % opts.NotifyAfter == 0)
					{
						opts.RowsCopiedCallback(rowsCopied);
						if (rowsCopied.Abort)
							break;
					}

					// Restart writer every batchSize rows,
					// because YDB closes the transaction on Dispose.
					if (current >= batchSize)
					{
						writer.Dispose();
						writer = _provider.Adapter.BeginBinaryImport(ydbConnection, copyCommand);
						current = 0;
					}
				}
			}
			finally
			{
				writer.Dispose();
				if (table.DataContext.CloseAfterUse)
					table.DataContext.Close();
			}

			return rowsCopied;
		}

		#endregion

		#region Asynchronous implementation (via synchronous API)

		async Task<BulkCopyRowsCopied> ProviderSpecificCopyImplAsync<T>(
			DataConnection dc,
			ITable<T> table,
			DataOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
			where T : notnull
		{
			// since the YDB SDK does not yet provide truly async bulk write,
			// wrap the synchronous logic in Task.Run,
			// after converting the IAsyncEnumerable to synchronous enumeration.
			var list = new List<T>();
			await foreach (var item in source.WithCancellation(cancellationToken))
				list.Add(item);

			return await Task.Run(() => ProviderSpecificCopyImpl(dc, table, options, list), cancellationToken).ConfigureAwait(false);
		}

		#endregion
	}
}
