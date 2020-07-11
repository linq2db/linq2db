using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.MySql
{
	using System.Threading;
	using System.Threading.Tasks;
	using Data;
	using LinqToDB.Common;

	class MySqlBulkCopy : BasicBulkCopy
	{
		private readonly MySqlDataProvider _provider;

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
					source,
					false,
					default).Result;
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
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options,
					source,
					true,
					cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T>           table,
			BulkCopyOptions     options,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				return ProviderSpecificCopyInternal(
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
		{
			if (table.DataContext is DataConnection dataConnection && _provider.Adapter.BulkCopy != null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				var transaction = dataConnection.Transaction;
				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(transaction, dataConnection.MappingSchema);

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

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternal<T>(
			ProviderConnections providerConnections,
			ITable<T>           table,
			BulkCopyOptions     options,
			IEnumerable<T>      source,
			bool                runAsync,
			CancellationToken   cancellationToken)
		{
			var dataConnection = providerConnections.DataConnection;
			var connection = providerConnections.ProviderConnection;
			var transaction = providerConnections.ProviderTransaction;
			var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
			var rc      = new BulkCopyRowsCopied();

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
			foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
			{
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch);

				await TraceActionAsync(
					dataConnection,
					() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
					async () => {
						if (runAsync)
						{
#if !NET45 && !NET46
							if (bc.CanWriteToServerAsync2)
								await bc.WriteToServerAsync2(rd, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
							else
#endif
								if (bc.CanWriteToServerAsync)
									await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
								else
									bc.WriteToServer(rd);
						}
						else
							bc.WriteToServer(rd); 
						return rd.Count; 
					}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				rc.RowsCopied += rd.Count;
			}

			if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				options.RowsCopiedCallback(rc);

			return rc;
		}

#if !NET45 && !NET46
		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternal<T>(
			ProviderConnections providerConnections,
			ITable<T>           table,
			BulkCopyOptions     options,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
		{
			var dataConnection = providerConnections.DataConnection;
			var connection = providerConnections.ProviderConnection;
			var transaction = providerConnections.ProviderTransaction;
			var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
			var rc      = new BulkCopyRowsCopied();

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
			await foreach (var batch in batches.WithCancellation(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				var rd = new BulkCopyReader<T>(dataConnection, columns, batch, cancellationToken);

				await TraceActionAsync(
					dataConnection,
					() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
					async () => {
						if (bc.CanWriteToServerAsync2)
							await bc.WriteToServerAsync2(rd, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
						else
							if (bc.CanWriteToServerAsync)
								await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
							else
								bc.WriteToServer(rd);
						return rd.Count; 
					}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
#endif
	}
}
