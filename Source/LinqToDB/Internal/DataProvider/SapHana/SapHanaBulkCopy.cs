using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Extensions;
using LinqToDB.Model;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	sealed class SapHanaBulkCopy : BasicBulkCopy
	{
		private readonly SapHanaDataProvider _provider;

		public SapHanaBulkCopy(SapHanaDataProvider provider)
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
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source));
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await TryGetProviderConnectionsAsync(table,cancellationToken).ConfigureAwait(false);
			if (connections.HasValue)
			{
				return await ProviderSpecificCopyInternalAsync(
					connections.Value,
					table,
					options.BulkCopyOptions,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source),
					cancellationToken).ConfigureAwait(false);
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await TryGetProviderConnectionsAsync(table,cancellationToken).ConfigureAwait(false);
			if (connections.HasValue)
			{
				return await ProviderSpecificCopyInternalAsync(
					connections.Value,
					table,
					options.BulkCopyOptions,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source, cancellationToken),
					cancellationToken).ConfigureAwait(false);
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		private ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection  = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());
				var transaction = dataConnection.Transaction;

				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(dataConnection, transaction);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					return new ProviderConnections()
					{
						DataConnection      = dataConnection,
						ProviderConnection  = connection,
						ProviderTransaction = transaction
					};
				}
			}

			return default;
		}

		private async ValueTask<ProviderConnections?> TryGetProviderConnectionsAsync<T>(ITable<T> table, CancellationToken cancellationToken)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection  = _provider.TryGetProviderConnection(dataConnection, await dataConnection.OpenDbConnectionAsync(cancellationToken).ConfigureAwait(false));
				var transaction = dataConnection.Transaction;

				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(dataConnection, transaction);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					return new ProviderConnections()
					{
						DataConnection = dataConnection,
						ProviderConnection = connection,
						ProviderTransaction = transaction
					};
				}
			}

			return default;
		}

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternalAsync<T>(
			ProviderConnections                             providerConnections,
			ITable<T>                                       table,
			BulkCopyOptions                                 options,
			Func<List<ColumnDescriptor>, BulkCopyReader<T>> createDataReader,
			CancellationToken                               cancellationToken)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var rc             = new BulkCopyRowsCopied();

			var hanaOptions = SapHanaProviderAdapter.HanaBulkCopyOptions.Default;

			if (options.KeepIdentity == true) hanaOptions |= SapHanaProviderAdapter.HanaBulkCopyOptions.KeepIdentity;

			using (var bc = _provider.Adapter.CreateBulkCopy!(connection, hanaOptions, transaction))
			{
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = options.NotifyAfter;

					bc.HanaRowsCopied += (sender, args) =>
					{
						rc.RowsCopied = args.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							args.Abort = true;
					};
				}

				if (options.MaxBatchSize.HasValue)
					bc.BatchSize = options.MaxBatchSize.Value;

				if (options.BulkCopyTimeout.HasValue)
					bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;
				else if (LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = connection.ConnectionTimeout;

				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
				var tableName  = GetTableName(sqlBuilder, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping!(i, columns[i].ColumnName));

				var rd = createDataReader(columns);

				await TraceActionAsync(
					dataConnection,
					() => (bc.CanWriteToServerAsync ? "INSERT ASYNC BULK " : "INSERT BULK ") + tableName + Environment.NewLine,
					async () => {
						if (bc.CanWriteToServerAsync)
							await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(false);
						else
							bc.WriteToServer(rd);
						return rd.Count;
					}).ConfigureAwait(false);

				if (rc.RowsCopied != rd.Count)
				{
					rc.RowsCopied = rd.Count;

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rc);
				}

				if (table.DataContext.CloseAfterUse)
					await table.DataContext.CloseAsync().ConfigureAwait(false);

				return rc;
			}
		}

		private BulkCopyRowsCopied ProviderSpecificCopyInternal<T>(
			ProviderConnections                             providerConnections,
			ITable<T>                                       table,
			BulkCopyOptions                                 options,
			Func<List<ColumnDescriptor>, BulkCopyReader<T>> createDataReader)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var rc             = new BulkCopyRowsCopied();

			var hanaOptions = SapHanaProviderAdapter.HanaBulkCopyOptions.Default;

			if (options.KeepIdentity == true) hanaOptions |= SapHanaProviderAdapter.HanaBulkCopyOptions.KeepIdentity;

			using (var bc = _provider.Adapter.CreateBulkCopy!(connection, hanaOptions, transaction))
			{
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = options.NotifyAfter;

					bc.HanaRowsCopied += (sender, args) =>
					{
						rc.RowsCopied = args.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							args.Abort = true;
					};
				}

				if (options.MaxBatchSize.HasValue)
					bc.BatchSize = options.MaxBatchSize.Value;

				if (options.BulkCopyTimeout.HasValue)
					bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;
				else if (LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = connection.ConnectionTimeout;

				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
				var tableName  = GetTableName(sqlBuilder, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping!(i, columns[i].ColumnName));

				var rd = createDataReader(columns);

				TraceAction(
					dataConnection,
					() => "INSERT BULK " + tableName + Environment.NewLine,
					() => {
						bc.WriteToServer(rd);
						return rd.Count;
					});

				if (rc.RowsCopied != rd.Count)
				{
					rc.RowsCopied = rd.Count;

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rc);
				}

				if (table.DataContext.CloseAfterUse)
					table.DataContext.Close();

				return rc;
			}
		}
	}
}
