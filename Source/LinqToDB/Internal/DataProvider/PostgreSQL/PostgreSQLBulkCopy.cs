using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Model;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// We subtract 1 based on possibility of provider using parameter for command.
		/// </remarks>
		protected override int                    MaxParameters => 32766;
		/// <summary>
		/// Setting based on https://stackoverflow.com/a/4937695/2937845
		/// Max is actually 2GiB, but we keep a lower number here to avoid the cost of huge statements.
		/// </summary>
		protected override int                    MaxSqlLength  => 327670;
		readonly           PostgreSQLDataProvider _provider;

		public PostgreSQLBulkCopy(PostgreSQLDataProvider dataProvider)
		{
			_provider = dataProvider;
		}

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

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			if (table.TryGetDataConnection(out var dataConnection))
				return ProviderSpecificCopyImpl(dataConnection, table, options, source);

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dataConnection))
				return ProviderSpecificCopyImplAsync(dataConnection, table, options, source, cancellationToken);

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dataConnection))
				return ProviderSpecificCopyImplAsync(dataConnection, table, options, source, cancellationToken);

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		BulkCopyRowsCopied ProviderSpecificCopyImpl<T>(
			DataConnection dataConnection, ITable<T> table, DataOptions options, IEnumerable<T> source)
			where T : notnull
		{
			var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

			if (connection == null)
				return MultipleRowsCopy(table, options, source);

			var sqlBuilder = (PostgreSQLSqlBuilder)_provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var ed         = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var tableName  = GetTableName(sqlBuilder, options.BulkCopyOptions, table);
			var columns    = ed.Columns.Where(c => !c.SkipOnInsert || options.BulkCopyOptions.KeepIdentity == true && c.IsIdentity).ToArray();

			var (npgsqlTypes, dbTypes, columnTypes) = BuildTypes(_provider.Adapter, sqlBuilder, columns);

			var fields      = string.Join(", ", columns.Select(column => sqlBuilder.ConvertInline(column.ColumnName, ConvertType.NameToQueryField)));
			var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

			// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
			var batchSize   = Math.Max(10, options.BulkCopyOptions.MaxBatchSize ?? 10000);

			var writer      = _provider.Adapter.BeginBinaryImport(connection, copyCommand);

			return ProviderSpecificCopySyncImpl(table.DataContext, dataConnection, options.BulkCopyOptions, source, connection, tableName, columns, columnTypes, npgsqlTypes, dbTypes, copyCommand, batchSize, writer);
		}

		private (NpgsqlProviderAdapter.NpgsqlDbType?[] npgsqlTypes, string?[] dbTypes, DbDataType[] columnTypes) BuildTypes(
			NpgsqlProviderAdapter adapter,
			PostgreSQLSqlBuilder  sqlBuilder,
			ColumnDescriptor[]    columns)
		{
			var npgsqlTypes = new NpgsqlProviderAdapter.NpgsqlDbType?[columns.Length];
			var dbTypes     = new string?[columns.Length];
			var columnTypes = new DbDataType[columns.Length];
			for (var i = 0; i < columns.Length; i++)
			{
				dbTypes[i]     = columns[i].DbType;
				columnTypes[i] = columns[i].GetDbDataType(true);
				var npgsqlType = _provider.GetNativeType(columns[i].DbType, true);
				if (npgsqlType == null)
				{
					var sb = new System.Text.StringBuilder();
					sqlBuilder.BuildTypeName(sb, columnTypes[i]);
					npgsqlType = _provider.GetNativeType(sb.ToString(), true);
				}

				npgsqlTypes[i] = npgsqlType;

				if (npgsqlType == null && dbTypes[i] == null)
					throw new LinqToDBException($"Cannot guess PostgreSQL type for column {columns[i].ColumnName}. Specify type explicitly in column mapping.");
			}

			return (npgsqlTypes, dbTypes, columnTypes);
		}

		private BulkCopyRowsCopied ProviderSpecificCopySyncImpl<T>(
			IDataContext                               originalContext,
			DataConnection                             dataConnection,
			BulkCopyOptions                            options,
			IEnumerable<T>                             source,
			DbConnection                               connection,
			string                                     tableName,
			ColumnDescriptor[]                         columns,
			DbDataType[]                               columnTypes,
			NpgsqlProviderAdapter.NpgsqlDbType?[]      npgsqlTypes,
			string?[]                                  dbTypes,
			string                                     copyCommand,
			int                                        batchSize,
			NpgsqlProviderAdapter.NpgsqlBinaryImporter writer)
		{
			var pgOptions    = dataConnection.Options.FindOrDefault(PostgreSQLOptions.Default);
			var currentCount = 0;
			var rowsCopied   = new BulkCopyRowsCopied();

			try
			{
				foreach (var item in source)
				{
					writer.StartRow();

					for (var i = 0; i < columns.Length; i++)
					{
						// DBNull.Value : https://github.com/npgsql/npgsql/issues/5330
						var value = _provider.NormalizeTimeStamp(pgOptions, columns[i].GetProviderValue(item!) ?? DBNull.Value, columnTypes[i], npgsqlTypes[i]);

						if (value is DateTimeOffset dto && npgsqlTypes[i] == NpgsqlProviderAdapter.NpgsqlDbType.TimeTZ)
						{
							// https://github.com/npgsql/npgsql/issues/5332
							// reset date
							value = new DateTimeOffset(1, 1, 1, 0, 0, 0, dto.Offset) + dto.TimeOfDay;
						}

						if (npgsqlTypes[i] != null)
							writer.Write(value, npgsqlTypes[i]!.Value);
						else
							writer.Write(value, dbTypes[i]!);
					}

					currentCount++;
					rowsCopied.RowsCopied++;

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
					{
						options.RowsCopiedCallback(rowsCopied);

						if (rowsCopied.Abort)
						{
							if (!writer.HasComplete && !writer.HasComplete5)
								writer.Cancel();
							break;
						}
					}

					if (currentCount >= batchSize)
					{
						if (writer.HasComplete)
							writer.Complete();
						else if (writer.HasComplete5)
							writer.Complete5();

						writer.Dispose();

						writer = _provider.Adapter.BeginBinaryImport(connection, copyCommand);
						currentCount = 0;
					}
				}

				if (!rowsCopied.Abort)
				{
					TraceAction(
						dataConnection,
						() => $"INSERT BULK {tableName}({string.Join(", ", columns.Select(x => x.ColumnName))}){Environment.NewLine}",
						() =>
						{
							if (writer.HasComplete)
								writer.Complete();
							else if (writer.HasComplete5)
								writer.Complete5();
							return (int)rowsCopied.RowsCopied;
						});
				}

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rowsCopied);
			}
			catch when (!writer.HasComplete && !writer.HasComplete5)
			{
				writer.Cancel();
				throw;
			}
			finally
			{
				writer.Dispose();
			}

			if (originalContext.CloseAfterUse)
				originalContext.Close();

			return rowsCopied;
		}

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyImplAsync<T>(
			DataConnection    dataConnection,
			ITable<T>         table,
			DataOptions       options,
			IEnumerable<T>    source,
			CancellationToken cancellationToken)
			where T : notnull
		{
			var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

			if (connection == null)
				return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);

			var sqlBuilder = (PostgreSQLSqlBuilder)_provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var ed         = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var tableName  = GetTableName(sqlBuilder, options.BulkCopyOptions, table);
			var columns    = ed.Columns.Where(c => !c.SkipOnInsert || options.BulkCopyOptions.KeepIdentity == true && c.IsIdentity).ToArray();

			var fields      = string.Join(", ", columns.Select(column => sqlBuilder.ConvertInline(column.ColumnName, ConvertType.NameToQueryField)));
			var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

			// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
			var batchSize    = Math.Max(10, options.BulkCopyOptions.MaxBatchSize ?? 10000);

			var (npgsqlTypes, dbTypes, columnTypes) = BuildTypes(_provider.Adapter, sqlBuilder, columns);

			var writer = _provider.Adapter.BeginBinaryImportAsync != null
				? await _provider.Adapter.BeginBinaryImportAsync(connection, copyCommand, cancellationToken).ConfigureAwait(false)
				: _provider.Adapter.BeginBinaryImport(connection, copyCommand);

			if (!writer.SupportsAsync)
			{
				// seems to be missing one of the required async methods; fallback to sync importer
				return ProviderSpecificCopySyncImpl(table.DataContext, dataConnection, options.BulkCopyOptions, source, connection, tableName, columns, columnTypes, npgsqlTypes, dbTypes, copyCommand, batchSize, writer);
			}

			var pgOptions    = dataConnection.Options.FindOrDefault(PostgreSQLOptions.Default);
			var rowsCopied   = new BulkCopyRowsCopied();
			var currentCount = 0;

			try
			{
				foreach (var item in source)
				{
					await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);

					for (var i = 0; i < columns.Length; i++)
					{
						// DBNull.Value : https://github.com/npgsql/npgsql/issues/5330
						var value = _provider.NormalizeTimeStamp(pgOptions, columns[i].GetProviderValue(item!) ?? DBNull.Value, columnTypes[i], npgsqlTypes[i]);
						
						if (value is DateTimeOffset dto && npgsqlTypes[i] == NpgsqlProviderAdapter.NpgsqlDbType.TimeTZ)
						{
							// https://github.com/npgsql/npgsql/issues/5332
							// reset date
							value = new DateTimeOffset(1, 1, 1, 0, 0, 0, dto.Offset) + dto.TimeOfDay;
						}

						if (npgsqlTypes[i] != null)
							await writer.WriteAsync(value, npgsqlTypes[i]!.Value, cancellationToken).ConfigureAwait(false);
						else
							await writer.WriteAsync(value, dbTypes[i]!, cancellationToken).ConfigureAwait(false);
					}

					currentCount++;
					rowsCopied.RowsCopied++;

					if (options.BulkCopyOptions.NotifyAfter        != 0 &&
					    options.BulkCopyOptions.RowsCopiedCallback != null &&
					    rowsCopied.RowsCopied % options.BulkCopyOptions.NotifyAfter == 0)
					{
						options.BulkCopyOptions.RowsCopiedCallback(rowsCopied);

						if (rowsCopied.Abort)
						{
							break;
						}
					}

					if (currentCount >= batchSize)
					{
						await writer.CompleteAsync(cancellationToken)
							.ConfigureAwait(false);

						await writer.DisposeAsync()
							.ConfigureAwait(false);

						writer = _provider.Adapter.BeginBinaryImportAsync != null
							? await _provider.Adapter.BeginBinaryImportAsync(connection, copyCommand, cancellationToken).ConfigureAwait(false)
							: _provider.Adapter.BeginBinaryImport(connection, copyCommand);
						currentCount = 0;
					}
				}

				if (!rowsCopied.Abort)
				{
					await TraceActionAsync(
						dataConnection,
						() => $"INSERT ASYNC BULK {tableName}({string.Join(", ", columns.Select(x => x.ColumnName))}){Environment.NewLine}",
						async () => {
							var ret = await writer.CompleteAsync(cancellationToken)
								.ConfigureAwait(false);
							return (int)rowsCopied.RowsCopied;
						}).ConfigureAwait(false);
				}

				if (options.BulkCopyOptions.NotifyAfter != 0 && options.BulkCopyOptions.RowsCopiedCallback != null)
					options.BulkCopyOptions.RowsCopiedCallback(rowsCopied);
			}
			finally
			{
				await writer.DisposeAsync()
					.ConfigureAwait(false);
			}

			if (table.DataContext.CloseAfterUse)
				await table.DataContext.CloseAsync().ConfigureAwait(false);

			return rowsCopied;
		}

		async Task<BulkCopyRowsCopied> ProviderSpecificCopyImplAsync<T>(
			DataConnection      dataConnection,
			ITable<T>           table,
			DataOptions         options,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
		where T: notnull
		{
			var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

			if (connection == null)
				return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);

			var sqlBuilder  = (PostgreSQLSqlBuilder)_provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var ed          = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var tableName   = GetTableName(sqlBuilder, options.BulkCopyOptions, table);
			var columns     = ed.Columns.Where(c => !c.SkipOnInsert || options.BulkCopyOptions.KeepIdentity == true && c.IsIdentity).ToArray();
			var fields      = string.Join(", ", columns.Select(column => sqlBuilder.ConvertInline(column.ColumnName, ConvertType.NameToQueryField)));
			var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

			// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
			var batchSize    = Math.Max(10, options.BulkCopyOptions.MaxBatchSize ?? 10000);

			var (npgsqlTypes, dbTypes, columnTypes) = BuildTypes(_provider.Adapter, sqlBuilder, columns);

			var writer = _provider.Adapter.BeginBinaryImportAsync != null
				? await _provider.Adapter.BeginBinaryImportAsync(connection, copyCommand, cancellationToken).ConfigureAwait(false)
				: _provider.Adapter.BeginBinaryImport(connection, copyCommand);

			if (!writer.SupportsAsync)
			{
				// seems to be missing one of the required async methods; fallback to sync importer
				var enumerator = source.GetAsyncEnumerator(cancellationToken);
				await using (enumerator.ConfigureAwait(false))
				{
					return ProviderSpecificCopySyncImpl(table.DataContext, dataConnection, options.BulkCopyOptions, EnumerableHelper.AsyncToSyncEnumerable(enumerator), connection, tableName, columns, columnTypes, npgsqlTypes, dbTypes, copyCommand, batchSize, writer);
				}
			}

			var pgOptions    = dataConnection.Options.FindOrDefault(PostgreSQLOptions.Default);
			var rowsCopied   = new BulkCopyRowsCopied();
			var currentCount = 0;

			try
			{
				await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
				{
					await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);
					for (var i = 0; i < columns.Length; i++)
					{
						// DBNull.Value : https://github.com/npgsql/npgsql/issues/5330
						var value = _provider.NormalizeTimeStamp(pgOptions, columns[i].GetProviderValue(item!) ?? DBNull.Value, columnTypes[i], npgsqlTypes[i]);

						if (value is DateTimeOffset dto && npgsqlTypes[i] == NpgsqlProviderAdapter.NpgsqlDbType.TimeTZ)
						{
							// https://github.com/npgsql/npgsql/issues/5332
							// reset date
							value = new DateTimeOffset(1, 1, 1, 0, 0, 0, dto.Offset) + dto.TimeOfDay;
						}

						if (npgsqlTypes[i] != null)
							await writer.WriteAsync(value, npgsqlTypes[i]!.Value, cancellationToken).ConfigureAwait(false);
						else
							await writer.WriteAsync(value, dbTypes[i]!, cancellationToken).ConfigureAwait(false);
					}

					currentCount++;
					rowsCopied.RowsCopied++;

					if (options.BulkCopyOptions.NotifyAfter        != 0 &&
					    options.BulkCopyOptions.RowsCopiedCallback != null &&
					    rowsCopied.RowsCopied % options.BulkCopyOptions.NotifyAfter == 0)
					{
						options.BulkCopyOptions.RowsCopiedCallback(rowsCopied);

						if (rowsCopied.Abort)
						{
							break;
						}
					}

					if (currentCount >= batchSize)
					{
						await writer.CompleteAsync(cancellationToken)
							.ConfigureAwait(false);

						await writer.DisposeAsync()
							.ConfigureAwait(false);

						writer = _provider.Adapter.BeginBinaryImportAsync != null
							? await _provider.Adapter.BeginBinaryImportAsync(connection, copyCommand, cancellationToken).ConfigureAwait(false)
							: _provider.Adapter.BeginBinaryImport(connection, copyCommand);
						currentCount = 0;
					}
				}

				if (!rowsCopied.Abort)
				{
					await TraceActionAsync(
						dataConnection,
						() => $"INSERT ASYNC BULK {tableName}({string.Join(", ", columns.Select(x => x.ColumnName))}){Environment.NewLine}",
						async () => {
							var ret = await writer.CompleteAsync(cancellationToken)
								.ConfigureAwait(false);
							return (int)rowsCopied.RowsCopied;
						}).ConfigureAwait(false);
				}

				if (options.BulkCopyOptions.NotifyAfter != 0 && options.BulkCopyOptions.RowsCopiedCallback != null)
					options.BulkCopyOptions.RowsCopiedCallback(rowsCopied);
			}
			finally
			{
				await writer.DisposeAsync()
					.ConfigureAwait(false);
			}

			if (table.DataContext.CloseAfterUse)
				await table.DataContext.CloseAsync().ConfigureAwait(false);

			return rowsCopied;
		}
	}
}
