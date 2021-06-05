﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;

	class MySqlBulkCopy : BasicBulkCopy
	{
		/// <summary>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// MySQL supports more but realistically this might be too much already for practical cases. 
		/// </summary>
		protected override int               MaxParameters => 32767;
		/// <summary>
		/// MySQL can support much larger sizes, based on
		/// https://dev.mysql.com/doc/refman/8.0/en/server-system-variables.html#sysvar_max_allowed_packet
		/// But we keep a smaller number here to avoid choking the network.
		/// </summary>
		protected override int               MaxSqlLength  => 327670;
		private readonly   MySqlDataProvider _provider;

		public MySqlBulkCopy(MySqlDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T>       table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options,
					source);
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T>         table,
			BulkCopyOptions   options,
			IEnumerable<T>    source,
			CancellationToken cancellationToken)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternalAsync(
					connections.Value,
					table,
					options,
					source,
					cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T>           table,
			BulkCopyOptions     options,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternalAsync(
					connections.Value,
					table,
					options,
					source,
					cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif

		private ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, table.DataContext.MappingSchema);

				var transaction = dataConnection.Transaction;
				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(transaction, table.DataContext.MappingSchema);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					return new ProviderConnections
					{
						DataConnection      = dataConnection,
						ProviderConnection  = connection,
						ProviderTransaction = transaction
					};
				}
			}
			return null;
		}

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternalAsync<T>(
			ProviderConnections providerConnections,
			ITable<T>           table,
			BulkCopyOptions     options,
			IEnumerable<T>      source,
			CancellationToken   cancellationToken)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var rc             = new BulkCopyRowsCopied();

			var bc = _provider.Adapter.BulkCopy!.Create(connection, transaction);
			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
			{
				bc.NotifyAfter = options.NotifyAfter;

				bc.MySqlRowsCopied += (sender, args) =>
				{
					rc.RowsCopied += args.RowsCopied;
					options.RowsCopiedCallback(rc);
					if (rc.Abort)
						args.Abort = true;
				};
			}

			if (options.BulkCopyTimeout.HasValue)
				bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;
			else if (Configuration.Data.BulkCopyUseConnectionCommandTimeout)
				bc.BulkCopyTimeout = connection.ConnectionTimeout;

			var tableName = GetTableName(sb, options, table);

			bc.DestinationTableName = GetTableName(sb, options, table);

			for (var i = 0; i < columns.Count; i++)
				bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

			// emulate missing BatchSize property
			// this is needed, because MySql fails on big batches, so users should be able to limit batch size
			foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
			{
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch);

				await TraceActionAsync(
					dataConnection,
					() =>
					((
#if !NETFRAMEWORK
							bc.CanWriteToServerAsync2 ||
#endif
							bc.CanWriteToServerAsync)
					? "INSERT ASYNC BULK " : "INSERT BULK ")
					+ tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
					async () => {
#if !NETFRAMEWORK
						if (bc.CanWriteToServerAsync2)
							await bc.WriteToServerAsync2(rd, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
						else
#endif
							if (bc.CanWriteToServerAsync)
								await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
							else
								bc.WriteToServer(rd);
						return rd.Count;
					}).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			return rc;
		}

		private BulkCopyRowsCopied ProviderSpecificCopyInternal<T>(
			ProviderConnections providerConnections,
			ITable<T>           table,
			BulkCopyOptions     options,
			IEnumerable<T>      source)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var rc             = new BulkCopyRowsCopied();

			var bc = _provider.Adapter.BulkCopy!.Create(connection, transaction);
			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
			{
				bc.NotifyAfter = options.NotifyAfter;

				bc.MySqlRowsCopied += (sender, args) =>
				{
					rc.RowsCopied += args.RowsCopied;
					options.RowsCopiedCallback(rc);
					if (rc.Abort)
						args.Abort = true;
				};
			}

			if (options.BulkCopyTimeout.HasValue)
				bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;
			else if (Configuration.Data.BulkCopyUseConnectionCommandTimeout)
				bc.BulkCopyTimeout = connection.ConnectionTimeout;

			var tableName = GetTableName(sb, options, table);

			bc.DestinationTableName = GetTableName(sb, options, table);

			for (var i = 0; i < columns.Count; i++)
				bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

			// emulate missing BatchSize property
			// this is needed, because MySql fails on big batches, so users should be able to limit batch size
			foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
			{
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch);

				TraceAction(
					dataConnection,
					() =>
					"INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
					() => {
						bc.WriteToServer(rd);
						return rd.Count;
					});

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			return rc;
		}

#if NATIVE_ASYNC
		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternalAsync<T>(
			ProviderConnections providerConnections,
			ITable<T>           table,
			BulkCopyOptions     options,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
			where T: notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);
			var rc             = new BulkCopyRowsCopied();

			var bc = _provider.Adapter.BulkCopy!.Create(connection, transaction);
			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
			{
				bc.NotifyAfter = options.NotifyAfter;

				bc.MySqlRowsCopied += (sender, args) =>
				{
					rc.RowsCopied += args.RowsCopied;
					options.RowsCopiedCallback(rc);
					if (rc.Abort)
						args.Abort = true;
				};
			}

			if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

			var tableName = GetTableName(sb, options, table);

			bc.DestinationTableName = GetTableName(sb, options, table);

			for (var i = 0; i < columns.Count; i++)
				bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

			// emulate missing BatchSize property
			// this is needed, because MySql fails on big batches, so users should be able to limit batch size
			var batches = EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue);
			await foreach (var batch in batches.WithCancellation(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
			{
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch, cancellationToken);

				await TraceActionAsync(
					dataConnection,
					() => (bc.CanWriteToServerAsync2 || bc.CanWriteToServerAsync ? "INSERT ASYNC BULK " : "INSERT BULK ") + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
					async () => {
						if (bc.CanWriteToServerAsync2)
							await bc.WriteToServerAsync2(rd, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
						else
							if (bc.CanWriteToServerAsync)
								await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
							else
								bc.WriteToServer(rd);
						return rd.Count;
					}).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			return rc;
		}
#endif

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
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
#endif
	}
}
