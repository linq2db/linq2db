﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Async;
	using Common;
	using Data;
	using LinqToDB.Extensions;
	using SqlProvider;

	sealed class ClickHouseBulkCopy : BasicBulkCopy
	{
		private readonly ClickHouseDataProvider _provider;

		public ClickHouseBulkCopy(ClickHouseDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
#endif

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source)
		{
			var connections = TryGetProviderConnections(table);

			if (connections.HasValue)
			{
				if (_provider.Adapter.OctonicaCreateWriter != null)
					return ProviderSpecificOctonicaBulkCopy(connections.Value, table, options, source);
#if NATIVE_ASYNC
				if (_provider.Adapter.OctonicaCreateWriterAsync != null)
					return SafeAwaiter.Run(() => ProviderSpecificOctonicaBulkCopyAsync(connections.Value, table, options, source, default));

				if (_provider.Adapter.ClientBulkCopyCreator != null)
					return SafeAwaiter.Run(() => ProviderSpecificClientBulkCopyAsync(connections.Value, table, options, columns => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source), default));
#endif
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				if (_provider.Adapter.OctonicaCreateWriterAsync != null)
					return ProviderSpecificOctonicaBulkCopyAsync(connections.Value, table, options, source, cancellationToken);

				if (_provider.Adapter.OctonicaCreateWriter != null)
					return Task.FromResult(ProviderSpecificOctonicaBulkCopy(connections.Value, table, options, source));

				if (_provider.Adapter.ClientBulkCopyCreator != null)
					return ProviderSpecificClientBulkCopyAsync(connections.Value, table, options, (columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source), cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				if (_provider.Adapter.OctonicaCreateWriterAsync != null)
					return ProviderSpecificOctonicaBulkCopyAsync(connections.Value, table, options, source, cancellationToken);

				if (_provider.Adapter.OctonicaCreateWriter != null)
					return Task.FromResult(ProviderSpecificOctonicaBulkCopy(connections.Value, table, options, EnumerableHelper.AsyncToSyncEnumerable(source.GetAsyncEnumerator(cancellationToken))));

				if (_provider.Adapter.ClientBulkCopyCreator != null)
					return ProviderSpecificClientBulkCopyAsync(connections.Value, table, options, (columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source, cancellationToken), cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif

		private ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.SupportsBulkCopy)
			{
				var connection  = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

				if (connection != null)
				{
					return new ProviderConnections()
					{
						DataConnection = dataConnection,
						ProviderConnection = connection,
					};
				}
			}

			return null;
		}

		#region Octonica

		private BulkCopyRowsCopied ProviderSpecificOctonicaBulkCopy<T>(
			ProviderConnections providerConnections,
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert).ToList();
			var rc             = new BulkCopyRowsCopied();
			var data           = new List<List<object?>>(columns.Count);
			var cmd            = new StringBuilder();
			var columnTypes    = columns.Select(_ => _.GetConvertedDbDataType()).ToArray();
			var valueConverter = new BulkCopyReader.Parameter();

			cmd
				.Append("INSERT INTO ")
				.Append(GetTableName(sb, options, table))
				.Append('(');

			for (var i = 0; i < columns.Count; i++)
			{
				data.Add(new List<object?>());

				if (i > 0)
					cmd.Append(", ");

				sb.Convert(cmd, columns[i].ColumnName, ConvertType.NameToQueryField);
			}

			cmd.AppendLine(") VALUES");

			var sql = cmd.ToString();

			using var bc = _provider.Adapter.OctonicaCreateWriter!(connection, sql);

			for (var i = 0; i < columnTypes.Length; i++)
			{
				// configure String/FixedString non-default mappings
				// other types handled in provider's SetParameter method
				if (columnTypes[i].DataType == DataType.VarBinary && columns[i].MemberType == typeof(byte[]))
					bc.ConfigureColumn(i, _provider.Adapter.OctonicaColumnSettings!(typeof(byte[])));
				else if (columnTypes[i].DataType is DataType.Char or DataType.NChar && (columns[i].MemberType == typeof(string) || columns[i].MemberType.ToNullableUnderlying().IsEnum))
					bc.ConfigureColumn(i, _provider.Adapter.OctonicaColumnSettings!(typeof(string)));
			}

			var clear = false;

			// as alternative to EnumerableHelper.Batch we can use MaxBlockSize, but it will not make difference
			foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
			{
				int rows = 0;

				if (clear)
					foreach (var list in data)
						list.Clear();

				foreach (var record in batch)
				{
					for (var i = 0; i < columns.Count; i++)
					{
						dataConnection.DataProvider.SetParameter(dataConnection, valueConverter, string.Empty, columnTypes[i], columns[i].GetProviderValue(record));
						data[i].Add(valueConverter.Value);
					}

					rows++;
				}

				// source depleted
				if (rows == 0)
					break;

				TraceAction(
					dataConnection,
					() => sql,
					() =>
					{
						bc.WriteTable(data, rows);
						return rows;
					});

				// there is no notifications from provider, so we ignore NotifyAfter value
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					rc.RowsCopied += rows;
					options.RowsCopiedCallback(rc);
					if (rc.Abort)
						return rc;
				}

				clear = true;
			}

			bc.EndWrite();

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			return rc;
		}

		private async Task<BulkCopyRowsCopied> ProviderSpecificOctonicaBulkCopyAsync<T>(
			ProviderConnections providerConnections,
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source,
			CancellationToken cancellationToken)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert).ToList();
			var rc             = new BulkCopyRowsCopied();
			var data           = new List<List<object?>>(columns.Count);
			var cmd            = new StringBuilder();
			var columnTypes    = columns.Select(_ => _.GetConvertedDbDataType()).ToArray();
			var valueConverter = new BulkCopyReader.Parameter();

			cmd
				.Append("INSERT INTO ")
				.Append(GetTableName(sb, options, table))
				.Append('(');

			for (var i = 0; i < columns.Count; i++)
			{
				data.Add(new List<object?>());

				if (i > 0)
					cmd.Append(", ");

				sb.Convert(cmd, columns[i].ColumnName, ConvertType.NameToQueryField);
			}

			cmd.AppendLine(") VALUES");

			var sql = cmd.ToString();

			var bc = await _provider.Adapter.OctonicaCreateWriterAsync!(connection, sql, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			await using (bc)
			{
				for (var i = 0; i < columnTypes.Length; i++)
				{
					// configure String/FixedString non-default mappings
					// other types handled in provider's SetParameter method
					if (columnTypes[i].DataType == DataType.VarBinary && columns[i].MemberType == typeof(byte[]))
						bc.ConfigureColumn(i, _provider.Adapter.OctonicaColumnSettings!(typeof(byte[])));
					else if (columnTypes[i].DataType is DataType.Char or DataType.NChar && (columns[i].MemberType == typeof(string) || columns[i].MemberType.ToNullableUnderlying().IsEnum))
						bc.ConfigureColumn(i, _provider.Adapter.OctonicaColumnSettings!(typeof(string)));
				}

				var clear = false;

				// as alternative to EnumerableHelper.Batch we can use MaxBlockSize, but it will not make difference
				foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
				{
					int rows = 0;

					if (clear)
						foreach (var list in data)
							list.Clear();

					foreach (var record in batch)
					{
						for (var i = 0; i < columns.Count; i++)
						{
							dataConnection.DataProvider.SetParameter(dataConnection, valueConverter, string.Empty, columnTypes[i], columns[i].GetProviderValue(record));
							data[i].Add(valueConverter.Value);
						}

						rows++;
					}

					// source depleted
					if (rows == 0)
						break;

					await TraceActionAsync(
						dataConnection,
						() => sql,
						async () =>
						{
							await bc.WriteTableAsync(data, rows, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
							return rows;
						}).ConfigureAwait(Configuration.ContinueOnCapturedContext);

					// there is no notifications from provider, so we ignore NotifyAfter value
					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					{
						rc.RowsCopied += rows;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							return rc;
					}

					clear = true;
				}

				await bc.EndWriteAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);

				return rc;
			}
		}

#if NATIVE_ASYNC
		private async Task<BulkCopyRowsCopied> ProviderSpecificOctonicaBulkCopyAsync<T>(
			ProviderConnections providerConnections,
			ITable<T> table,
			BulkCopyOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert).ToList();
			var rc             = new BulkCopyRowsCopied();
			var data           = new List<List<object?>>(columns.Count);
			var cmd            = new StringBuilder();
			var columnTypes    = columns.Select(_ => _.GetConvertedDbDataType()).ToArray();
			var valueConverter = new BulkCopyReader.Parameter();

			cmd
				.Append("INSERT INTO ")
				.Append(GetTableName(sb, options, table))
				.Append('(');

			for (var i = 0; i < columns.Count; i++)
			{
				data.Add(new List<object?>());

				if (i > 0)
					cmd.Append(", ");

				sb.Convert(cmd, columns[i].ColumnName, ConvertType.NameToQueryField);
			}

			cmd.AppendLine(") VALUES");

			var sql = cmd.ToString();

			// thanks C#! (sarcasm)
			var bc = await _provider.Adapter.OctonicaCreateWriterAsync!(connection, sql, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			await using (bc.ConfigureAwait(Configuration.ContinueOnCapturedContext))
			{
				for (var i = 0; i < columnTypes.Length; i++)
				{
					// configure String/FixedString non-default mappings
					// other types handled in provider's SetParameter method
					if (columnTypes[i].DataType == DataType.VarBinary && columns[i].MemberType == typeof(byte[]))
						bc.ConfigureColumn(i, _provider.Adapter.OctonicaColumnSettings!(typeof(byte[])));
					else if (columnTypes[i].DataType is DataType.Char or DataType.NChar && (columns[i].MemberType == typeof(string) || columns[i].MemberType.ToNullableUnderlying().IsEnum))
						bc.ConfigureColumn(i, _provider.Adapter.OctonicaColumnSettings!(typeof(string)));
				}

				var clear = false;

				// as alternative to EnumerableHelper.Batch we can use MaxBlockSize, but it will not make difference
				var batches = EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue);

				await foreach (var batch in batches.WithCancellation(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
				{
					int rows = 0;

					if (clear)
						foreach (var list in data)
							list.Clear();

					await foreach (var record in batch.WithCancellation(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{
						for (var i = 0; i < columns.Count; i++)
						{
							dataConnection.DataProvider.SetParameter(dataConnection, valueConverter, string.Empty, columnTypes[i], columns[i].GetProviderValue(record));
							data[i].Add(valueConverter.Value);
						}

						rows++;
					}

					// source depleted
					if (rows == 0)
						break;

					await TraceActionAsync(
						dataConnection,
						() => sql,
						async () =>
						{
							await bc.WriteTableAsync(data, rows, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
							return rows;
						}).ConfigureAwait(Configuration.ContinueOnCapturedContext);

					// there is no notifications from provider, so we ignore NotifyAfter value
					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					{
						rc.RowsCopied += rows;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							return rc;
					}

					clear = true;
				}

				await bc.EndWriteAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);

				return rc;
			}
		}
#endif

		#endregion

		#region Client

		private async Task<BulkCopyRowsCopied> ProviderSpecificClientBulkCopyAsync<T>(
			ProviderConnections                                     providerConnections,
			ITable<T>                                               table,
			BulkCopyOptions                                         options,
			Func<List<Mapping.ColumnDescriptor>, BulkCopyReader<T>> createDataReader,
			CancellationToken                                       cancellationToken)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert).ToList();
			var rc             = new BulkCopyRowsCopied();

			var disposeConnection = false;

			if (options.WithoutSession)
			{
				var cnBuilder         = _provider.Adapter.CreateClientConnectionStringBuilder!(connection.ConnectionString);

				if (cnBuilder.UseSession)
				{
					disposeConnection    = true;
					cnBuilder.UseSession = false;
					connection           = _provider.Adapter.CreateConnection!(cnBuilder.ToString());
				}
			}

			try
			{
				using var bc = _provider.Adapter.ClientBulkCopyCreator!(connection);

				if (options.MaxBatchSize.HasValue)
					bc.BatchSize = options.MaxBatchSize.Value;

				var tableName = GetTableName(sb, options, table);

				bc.DestinationTableName = tableName;

				if (options.MaxDegreeOfParallelism != null)
					bc.MaxDegreeOfParallelism = options.MaxDegreeOfParallelism.Value;

				var rd = createDataReader(columns);

				await TraceActionAsync(
					dataConnection,
					() => "INSERT ASYNC BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					async () =>
					{
						await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
						return rd.Count;
					}).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				rc.RowsCopied = bc.RowsWritten;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);
			}
			finally
			{
				// actually currently DisposeAsync is not implemented in Client provider and we can call Dispose with same effect
				if (disposeConnection)
				{
#if NETSTANDARD2_1PLUS
					await connection.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
#else
					connection.Dispose();
#endif
				}
			}

			return rc;
		}

#endregion
	}
}
