using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.DB2
{
	using System.Threading;
	using System.Threading.Tasks;
	using Data;
	using LinqToDB.Common;
	using DB2BulkCopyOptions = DB2ProviderAdapter.DB2BulkCopyOptions;

	class DB2BulkCopy : BasicBulkCopy
	{
		private readonly DB2DataProvider _provider;

		public DB2BulkCopy(DB2DataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T>       table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);
				if (connection != null)
					return ProviderSpecificCopyImpl(
						table,
						options,
						source,
						dataConnection,
						connection,
						_provider.Adapter.BulkCopy,
						TraceAction);
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);
				if (connection != null)
					// call the synchronous provider-specific implementation
					return Task.FromResult(ProviderSpecificCopyImpl(
						table,
						options,
						source,
						dataConnection,
						connection,
						_provider.Adapter.BulkCopy,
						TraceAction));
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NET45 && !NET46
		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);
				if (connection != null)
				{
					var enumerator = source.GetAsyncEnumerator(cancellationToken);
					await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					{
						// call the synchronous provider-specific implementation
						return ProviderSpecificCopyImpl(
							table,
							options,
							EnumerableHelper.AsyncToSyncEnumerable(enumerator),
							dataConnection,
							connection,
							_provider.Adapter.BulkCopy,
							TraceAction);
					}
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
#endif

		internal static BulkCopyRowsCopied ProviderSpecificCopyImpl<T>(
			ITable<T>                                       table,
			BulkCopyOptions                                 options,
			IEnumerable<T>                                  source,
			DataConnection                                  dataConnection,
			IDbConnection                                   connection,
			DB2ProviderAdapter.BulkCopyAdapter              bulkCopy,
			Action<DataConnection, Func<string>, Func<int>> traceAction)
		{
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns    = descriptor.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var rd         = new BulkCopyReader<T>(dataConnection, columns, source);
			var rc         = new BulkCopyRowsCopied();
			var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
			var tableName  = GetTableName(sqlBuilder, options, table);

			var bcOptions = DB2BulkCopyOptions.Default;

			if (options.KeepIdentity == true) bcOptions |= DB2BulkCopyOptions.KeepIdentity;
			if (options.TableLock    == true) bcOptions |= DB2BulkCopyOptions.TableLock;

			using (var bc = bulkCopy.Create(connection, bcOptions))
			{
				var notifyAfter = options.NotifyAfter == 0 && options.MaxBatchSize.HasValue ?
					options.MaxBatchSize.Value : options.NotifyAfter;

				if (notifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = notifyAfter;

					bc.DB2RowsCopied += (sender, args) =>
					{
						rc.RowsCopied = args.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							args.Abort = true;
					};
				}

				if (options.BulkCopyTimeout.HasValue)
					bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;
				else if (Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = connection.ConnectionTimeout;

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(bulkCopy.CreateColumnMapping(i, sqlBuilder.ConvertInline(columns[i].ColumnName, SqlProvider.ConvertType.NameToQueryField)));

				traceAction(
					dataConnection,
					() => "INSERT BULK " + tableName + Environment.NewLine,
					() => { bc.WriteToServer(rd); return rd.Count; });
			}

			if (rc.RowsCopied != rd.Count)
			{
				rc.RowsCopied = rd.Count;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);
			}

			return rc;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			var dataConnection = (DataConnection)table.DataContext;

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2(table, options, source, " FROM SYSIBM.SYSDUMMY1");

			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var dataConnection = (DataConnection)table.DataContext;

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2Async(table, options, source, " FROM SYSIBM.SYSDUMMY1", cancellationToken);

			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var dataConnection = (DataConnection)table.DataContext;

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2Async(table, options, source, " FROM SYSIBM.SYSDUMMY1", cancellationToken);

			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
#endif
	}
}
