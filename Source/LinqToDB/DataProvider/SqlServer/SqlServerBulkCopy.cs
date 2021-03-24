using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Data;
	using System.Threading;
	using Data;
	using SqlProvider;

	class SqlServerBulkCopy : BasicBulkCopy
	{
		private readonly SqlServerDataProvider _provider;

		public SqlServerBulkCopy(SqlServerDataProvider provider)
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
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source));
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
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source),
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
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source, cancellationToken),
					cancellationToken);
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif

		private ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
			where T : notnull
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
			return null;
		}

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternalAsync<T>(
			ProviderConnections                                     providerConnections,
			ITable<T>                                               table,
			BulkCopyOptions                                         options,
			Func<List<Mapping.ColumnDescriptor>, BulkCopyReader<T>> createDataReader,
			CancellationToken                                       cancellationToken)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
			var rd             = createDataReader(columns);
			var sqlopt         = SqlServerProviderAdapter.SqlBulkCopyOptions.Default;
			var rc             = new BulkCopyRowsCopied();

			if (options.CheckConstraints       == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.CheckConstraints;
			if (options.KeepIdentity           == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.KeepIdentity;
			if (options.TableLock              == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.TableLock;
			if (options.KeepNulls              == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.KeepNulls;
			if (options.FireTriggers           == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.FireTriggers;
			if (options.UseInternalTransaction == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.UseInternalTransaction;

			using (var bc = _provider.Adapter.CreateBulkCopy(connection, sqlopt, transaction))
			{
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = options.NotifyAfter;

					bc.SqlRowsCopied += (sender, args) =>
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
				else if (Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = connection.ConnectionTimeout;

				var tableName = GetTableName(sb, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, sb.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

				await TraceActionAsync(
					dataConnection,
					() => "INSERT ASYNC BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					async () => {
						await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
						return rd.Count;
					}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			if (rc.RowsCopied != rd.Count)
			{
				rc.RowsCopied = rd.Count;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);
			}

			return rc;
		}

		private BulkCopyRowsCopied ProviderSpecificCopyInternal<T>(
			ProviderConnections                                     providerConnections,
			ITable<T>                                               table,
			BulkCopyOptions                                         options,
			Func<List<Mapping.ColumnDescriptor>, BulkCopyReader<T>> createDataReader)
			where T : notnull
		{
			var dataConnection = providerConnections.DataConnection;
			var connection     = providerConnections.ProviderConnection;
			var transaction    = providerConnections.ProviderTransaction;
			var ed             = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns        = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb             = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
			var rd             = createDataReader(columns);
			var sqlopt         = SqlServerProviderAdapter.SqlBulkCopyOptions.Default;
			var rc             = new BulkCopyRowsCopied();

			if (options.CheckConstraints       == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.CheckConstraints;
			if (options.KeepIdentity           == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.KeepIdentity;
			if (options.TableLock              == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.TableLock;
			if (options.KeepNulls              == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.KeepNulls;
			if (options.FireTriggers           == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.FireTriggers;
			if (options.UseInternalTransaction == true) sqlopt |= SqlServerProviderAdapter.SqlBulkCopyOptions.UseInternalTransaction;

			using (var bc = _provider.Adapter.CreateBulkCopy(connection, sqlopt, transaction))
			{
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = options.NotifyAfter;

					bc.SqlRowsCopied += (sender, args) =>
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
				else if (Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = connection.ConnectionTimeout;

				var tableName = GetTableName(sb, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, sb.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

				TraceAction(
					dataConnection,
					() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					() => {
						bc.WriteToServer(rd);
						return rd.Count;
					});
			}

			if (rc.RowsCopied != rd.Count)
			{
				rc.RowsCopied = rd.Count;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);
			}

			return rc;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			BulkCopyRowsCopied ret;

			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			switch (((SqlServerDataProvider)helper.DataConnection.DataProvider).Version)
			{
				case SqlServerVersion.v2000 :
				case SqlServerVersion.v2005 : ret = MultipleRowsCopy2(helper, source, ""); break;
				default                     : ret = MultipleRowsCopy1(helper, source);     break;
			}

			if (options.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			BulkCopyRowsCopied ret;

			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			switch (((SqlServerDataProvider)helper.DataConnection.DataProvider).Version)
			{
				case SqlServerVersion.v2000:
				case SqlServerVersion.v2005:
					ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					break;
				default:
					ret = await MultipleRowsCopy1Async(helper, source, cancellationToken)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					break;
			}

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}

#if NATIVE_ASYNC
		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			BulkCopyRowsCopied ret;

			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			switch (((SqlServerDataProvider)helper.DataConnection.DataProvider).Version)
			{
				case SqlServerVersion.v2000:
				case SqlServerVersion.v2005:
					ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					break;
				default:
					ret = await MultipleRowsCopy1Async(helper, source, cancellationToken)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					break;
			}

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}
#endif
	}
}
