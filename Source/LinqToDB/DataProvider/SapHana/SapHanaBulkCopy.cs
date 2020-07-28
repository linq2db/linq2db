using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;
	using System.Threading;
	using System.Threading.Tasks;

	class SapHanaBulkCopy : BasicBulkCopy
	{
		private readonly SapHanaDataProvider _provider;

		public SapHanaBulkCopy(SapHanaDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source),
					false,
					default).GetAwaiter().GetResult();
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
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source),
					true,
					cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NETFRAMEWORK
		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source, cancellationToken),
					true,
					cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif

		private ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
		{
			if (table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				var transaction = dataConnection.Transaction;
				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(transaction, dataConnection.MappingSchema);

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

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternal<T>(
			ProviderConnections                                     providerConnections,
			ITable<T>                                               table,
			BulkCopyOptions                                         options,
			Func<List<Mapping.ColumnDescriptor>, BulkCopyReader<T>> createDataReader,
			bool                                                    runAsync,
			CancellationToken                                       cancellationToken)
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var rc             = new BulkCopyRowsCopied();


			var hanaOptions = SapHanaProviderAdapter.HanaBulkCopyOptions.Default;

			if (options.KeepIdentity == true) hanaOptions |= SapHanaProviderAdapter.HanaBulkCopyOptions.KeepIdentity;

			using (var bc = _provider.Adapter.CreateBulkCopy(connection, hanaOptions, transaction))
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

				if (options.MaxBatchSize.HasValue)    bc.BatchSize       = options.MaxBatchSize.Value;
				if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
				var tableName  = GetTableName(sqlBuilder, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, columns[i].ColumnName));

				var rd = createDataReader(columns);

				await TraceActionAsync(
					dataConnection,
					() => (runAsync && bc.CanWriteToServerAsync ? "INSERT ASYNC BULK " : "INSERT BULK ") + tableName + Environment.NewLine,
					async () => {
						if (runAsync && bc.CanWriteToServerAsync)
							await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
						else
							bc.WriteToServer(rd);
						return rd.Count;
					}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				if (rc.RowsCopied != rd.Count)
				{
					rc.RowsCopied = rd.Count;

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rc);
				}

				return rc;
			}
		}
	}
}
