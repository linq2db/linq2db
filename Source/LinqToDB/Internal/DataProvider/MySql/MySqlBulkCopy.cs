using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public class MySqlBulkCopy : BasicBulkCopy
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
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options.BulkCopyOptions,
					source);
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await TryGetProviderConnectionsAsync(table, cancellationToken).ConfigureAwait(false);
			if (connections.HasValue)
			{
				return await ProviderSpecificCopyInternalAsync(
					connections.Value,
					table,
					options.BulkCopyOptions,
					source,
					cancellationToken).ConfigureAwait(false);
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await TryGetProviderConnectionsAsync(table, cancellationToken).ConfigureAwait(false);
			if (connections.HasValue)
			{
				return await ProviderSpecificCopyInternalAsync(
					connections.Value,
					table,
					options.BulkCopyOptions,
					source,
					cancellationToken).ConfigureAwait(false);
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		private ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null)
			{
				var connection  = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());
				var transaction = dataConnection.Transaction;

				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(dataConnection, transaction);

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

		private async Task<ProviderConnections?> TryGetProviderConnectionsAsync<T>(ITable<T> table, CancellationToken cancellationToken)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null)
			{
				var connection  = _provider.TryGetProviderConnection(dataConnection, await dataConnection.OpenDbConnectionAsync(cancellationToken).ConfigureAwait(false));
				var transaction = dataConnection.Transaction;

				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(dataConnection, transaction);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					return new ProviderConnections
					{
						DataConnection = dataConnection,
						ProviderConnection = connection,
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
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
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

			if (options.BulkCopyTimeout.HasValue || LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
				bc.BulkCopyTimeout = options.BulkCopyTimeout ?? dataConnection.CommandTimeout;

			var tableName = GetTableName(sb, options, table);

			bc.DestinationTableName = GetTableName(sb, options, table);

			for (var i = 0; i < columns.Count; i++)
				bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

			// emulate missing BatchSize property
			// this is needed, because MySql fails on big batches, so users should be able to limit batch size
			foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch);
#pragma warning restore CA2000 // Dispose objects before losing scope
				await using var _ = rd.ConfigureAwait(false);

				await TraceActionAsync(
					dataConnection,
					() =>
					(bc.HasWriteToServerAsync ? "INSERT ASYNC BULK " : "INSERT BULK ")
					+ tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					async () => {
						if (bc.HasWriteToServerAsync)
							await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(false);
						else
							bc.WriteToServer(rd);
						return rd.Count;
					}).ConfigureAwait(false);

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			if (table.DataContext.CloseAfterUse)
				await table.DataContext.CloseAsync().ConfigureAwait(false);

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
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
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

			if (options.BulkCopyTimeout.HasValue || LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
				bc.BulkCopyTimeout = options.BulkCopyTimeout ?? dataConnection.CommandTimeout;

			var tableName = GetTableName(sb, options, table);

			bc.DestinationTableName = GetTableName(sb, options, table);

			for (var i = 0; i < columns.Count; i++)
				bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

			// emulate missing BatchSize property
			// this is needed, because MySql fails on big batches, so users should be able to limit batch size
			foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
			{
				using var rd = new BulkCopyReader<T>(dataConnection, columns, batch);

				TraceAction(
					dataConnection,
					() =>
					"INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					() => {
						bc.WriteToServer(rd);
						return rd.Count;
					});

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			if (table.DataContext.CloseAfterUse)
				table.DataContext.Close();

			return rc;
		}

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
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
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

			if (options.BulkCopyTimeout.HasValue || LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
				bc.BulkCopyTimeout = options.BulkCopyTimeout ?? dataConnection.CommandTimeout;

			var tableName = GetTableName(sb, options, table);

			bc.DestinationTableName = GetTableName(sb, options, table);

			for (var i = 0; i < columns.Count; i++)
				bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

			// emulate missing BatchSize property
			// this is needed, because MySql fails on big batches, so users should be able to limit batch size
			var batches = EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue);
			await foreach (var batch in batches.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch, cancellationToken);
#pragma warning restore CA2000 // Dispose objects before losing scope
				await using var _ = rd.ConfigureAwait(false);

				await TraceActionAsync(
					dataConnection,
					() => (bc.HasWriteToServerAsync ? "INSERT ASYNC BULK " : "INSERT BULK ") + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					async () => {
						if (bc.HasWriteToServerAsync)
							await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(false);
						else
							bc.WriteToServer(rd);
						return rd.Count;
					}).ConfigureAwait(false);

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			if (table.DataContext.CloseAfterUse)
				await table.DataContext.CloseAsync().ConfigureAwait(false);

			return rc;
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
	}
}
