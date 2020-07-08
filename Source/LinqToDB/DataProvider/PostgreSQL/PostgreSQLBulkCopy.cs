using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.PostgreSQL
{
	class PostgreSQLBulkCopy : BasicBulkCopy
	{
		readonly PostgreSQLDataProvider _provider;

		// TODO: permanent cache is bad
		static readonly ConcurrentDictionary<object, object> _rowWriterCache = new ConcurrentDictionary<object, object>();

		public PostgreSQLBulkCopy(PostgreSQLDataProvider dataProvider)
		{
			_provider = dataProvider;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
#endif

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (table.DataContext is DataConnection dataConnection)
				return ProviderSpecificCopyImpl(dataConnection, table, options, source);

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.DataContext is DataConnection dataConnection)
#if !NET45 && !NET46
				return ProviderSpecificCopyImplAsync(dataConnection, table, options, EnumerableHelper.SyncToAsyncEnumerable(source), cancellationToken);
#else
				// call the synchronous provider-specific implementation,
				//   as the asynchronous implementation is only available with .Net Standard
				return Task.FromResult(ProviderSpecificCopyImpl(dataConnection, table, options, source));
#endif
			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.DataContext is DataConnection dataConnection)
				return ProviderSpecificCopyImplAsync(dataConnection, table, options, source, cancellationToken);

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif

		private BulkCopyRowsCopied ProviderSpecificCopyImpl<T>(DataConnection dataConnection, ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				if (connection == null)
					return MultipleRowsCopy(table, options, source);

				var sqlBuilder = (BasicSqlBuilder)_provider.CreateSqlBuilder(dataConnection.MappingSchema);
				var ed         = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
				var tableName  = GetTableName(sqlBuilder, options, table);
				var columns    = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToArray();

				var fields      = string.Join(", ", columns.Select(column => sqlBuilder.ConvertInline(column.ColumnName, ConvertType.NameToQueryField)));
				var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

				var rowsCopied = new BulkCopyRowsCopied();
				// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
				var batchSize = Math.Max(10, options.MaxBatchSize ?? 10000);
				var currentCount = 0;

				var key = new { Type = typeof(T), options.KeepIdentity, ed, dataConnection.MappingSchema.ConfigurationID };
				var rowWriter = (Action<NpgsqlProviderAdapter.NpgsqlBinaryImporter, ColumnDescriptor[], T>)_rowWriterCache.GetOrAdd(
					key,
					_ => _provider.Adapter.CreateBinaryImportRowWriter<T>(_provider, sqlBuilder, columns));

				var useComplete = _provider.Adapter.BinaryImporterHasComplete;
				var writer      = _provider.Adapter.BeginBinaryImport(connection, copyCommand);

				try
				{
					foreach (var item in source)
					{
						rowWriter(writer, columns, item);

						currentCount++;
						rowsCopied.RowsCopied++;

						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
						{
							options.RowsCopiedCallback(rowsCopied);

							if (rowsCopied.Abort)
							{
								if (!useComplete)
									writer.Cancel();
								break;
							}
						}

						if (currentCount >= batchSize)
						{
							if (useComplete)
								writer.Complete();

							writer.Dispose();

							writer       = _provider.Adapter.BeginBinaryImport(connection, copyCommand);
							currentCount = 0;
						}
					}

					if (!rowsCopied.Abort)
					{
						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
							() => { 
								if (useComplete)
									writer.Complete();
								return (int)rowsCopied.RowsCopied; });
					}

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rowsCopied);
				}
				catch when (!useComplete)
				{
					writer.Cancel();
					throw;
				}
				finally
				{
					writer.Dispose();
				}

				return rowsCopied;
		}

#if !NET45 && !NET46
		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyImplAsync<T>(DataConnection dataConnection, ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			// verify that async methods are available
			if (_provider.Adapter.BinaryImporterWriteAsyncFunction == null)
			{
				// run the synchronous provider-specific implementation
				var enumerator = source.GetAsyncEnumerator(cancellationToken);
				await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				{
					return ProviderSpecificCopyImpl(dataConnection, table, options, EnumerableHelper.AsyncToSyncEnumerable(enumerator));
				}
			}

			var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

			if (connection == null)
				return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var sqlBuilder = (BasicSqlBuilder)_provider.CreateSqlBuilder(dataConnection.MappingSchema);
			var ed = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName = GetTableName(sqlBuilder, options, table);
			var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToArray();

			var fields = string.Join(", ", columns.Select(column => sqlBuilder.ConvertInline(column.ColumnName, ConvertType.NameToQueryField)));
			var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

			var rowsCopied = new BulkCopyRowsCopied();
			// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
			var batchSize = Math.Max(10, options.MaxBatchSize ?? 10000);
			var currentCount = 0;

			var npgsqlTypes = new NpgsqlProviderAdapter.NpgsqlDbType[columns.Length];
			for (var i = 0; i < columns.Length; i++)
			{
				var npgsqlType = _provider.GetNativeType(columns[i].DbType, true);
				if (npgsqlType == null)
				{
					var columnType = columns[i].DataType != DataType.Undefined ? new SqlQuery.SqlDataType(columns[i]) : null;

					if (columnType == null || columnType.Type.DataType == DataType.Undefined)
						columnType = columns[i].MappingSchema.GetDataType(columns[i].StorageType);

					var sb = new System.Text.StringBuilder();
					sqlBuilder.BuildTypeName(sb, columnType);
					npgsqlType = _provider.GetNativeType(sb.ToString(), true);
				}

				if (npgsqlType == null)
					throw new LinqToDBException($"Cannot guess PostgreSQL type for column {columns[i].ColumnName}. Specify type explicitly in column mapping.");

				npgsqlTypes[i] = npgsqlType.Value;
			}

			var writeAsync = _provider.Adapter.BinaryImporterWriteAsyncFunction;
			var writer = _provider.Adapter.BeginBinaryImport(connection, copyCommand);
			if (!writer.SupportsAsync) // double check that all the async methods are available
			{
				// seems to be missing one of the required async methods; fallback to sync importer
				writer.Dispose();
				var enumerator = source.GetAsyncEnumerator(cancellationToken);
				await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				{
					return ProviderSpecificCopyImpl(dataConnection, table, options, EnumerableHelper.AsyncToSyncEnumerable(enumerator));
				}
			}
			try
			{
				await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				{
					await writer.StartRowAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					for (var i = 0; i < columns.Length; i++)
					{
						await writeAsync(writer, columns[i].GetValue(item!), npgsqlTypes[i], cancellationToken)
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					}

					currentCount++;
					rowsCopied.RowsCopied++;

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
					{
						options.RowsCopiedCallback(rowsCopied);

						if (rowsCopied.Abort)
						{
							break;
						}
					}

					if (currentCount >= batchSize)
					{
						await writer.CompleteAsync(cancellationToken)
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

						await writer.DisposeAsync()
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

						writer = _provider.Adapter.BeginBinaryImport(connection, copyCommand);
						currentCount = 0;
					}
				}

				if (!rowsCopied.Abort)
				{
					await TraceActionAsync(
						dataConnection,
						() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
						async () => {
							var ret = await writer.CompleteAsync(cancellationToken)
								.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
							return (int)rowsCopied.RowsCopied;
						}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rowsCopied);
			}
			finally
			{
				await writer.DisposeAsync()
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return rowsCopied;
		}
#endif
	}
}
