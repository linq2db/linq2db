using System.Collections.Generic;

namespace LinqToDB.DataProvider.Informix
{
	using System;
	using System.Data;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Data;
	using LinqToDB.Common;
	using LinqToDB.SqlProvider;

	class InformixBulkCopy : BasicBulkCopy
	{
		private readonly InformixDataProvider _provider;

		public InformixBulkCopy(InformixDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T>       table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if ((_provider.Adapter.InformixBulkCopy != null || _provider.Adapter.DB2BulkCopy != null) && table.DataContext is DataConnection dataConnection && dataConnection.Transaction == null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				if (connection != null)
				{
					if (_provider.Adapter.InformixBulkCopy != null)
						return IDSProviderSpecificCopy(
							table,
							options,
							source,
							dataConnection,
							connection,
							_provider.Adapter.InformixBulkCopy);
					else
						return DB2.DB2BulkCopy.ProviderSpecificCopyImpl(
							table,
							options,
							source,
							dataConnection,
							connection,
							_provider.Adapter.DB2BulkCopy!,
							TraceAction);
				}
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T>         table,
			BulkCopyOptions   options,
			IEnumerable<T>    source,
			CancellationToken cancellationToken)
		{
			if ((_provider.Adapter.InformixBulkCopy != null || _provider.Adapter.DB2BulkCopy != null) && table.DataContext is DataConnection dataConnection && dataConnection.Transaction == null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				if (connection != null)
				{
					// call the synchronous provider-specific implementation
					if (_provider.Adapter.InformixBulkCopy != null)
						return Task.FromResult(IDSProviderSpecificCopy(
							table,
							options,
							source,
							dataConnection,
							connection,
							_provider.Adapter.InformixBulkCopy));
					else
						return Task.FromResult(DB2.DB2BulkCopy.ProviderSpecificCopyImpl(
							table,
							options,
							source,
							dataConnection,
							connection,
							_provider.Adapter.DB2BulkCopy!,
							TraceAction));
				}
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NETFRAMEWORK
		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T>           table,
			BulkCopyOptions     options,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
		{
			if ((_provider.Adapter.InformixBulkCopy != null || _provider.Adapter.DB2BulkCopy != null) && table.DataContext is DataConnection dataConnection && dataConnection.Transaction == null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				if (connection != null)
				{
					var enumerator = source.GetAsyncEnumerator();
					await using (enumerator.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					{
						// call the synchronous provider-specific implementation
						var syncSource = EnumerableHelper.AsyncToSyncEnumerable(enumerator);
						if (_provider.Adapter.InformixBulkCopy != null)
							return IDSProviderSpecificCopy(
								table,
								options,
								syncSource,
								dataConnection,
								connection,
								_provider.Adapter.InformixBulkCopy);
						else
							return DB2.DB2BulkCopy.ProviderSpecificCopyImpl(
								table,
								options,
								syncSource,
								dataConnection,
								connection,
								_provider.Adapter.DB2BulkCopy!,
								TraceAction);
					}
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken)
				.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
#endif

		protected BulkCopyRowsCopied IDSProviderSpecificCopy<T>(
			ITable<T>                               table,
			BulkCopyOptions                         options,
			IEnumerable<T>                          source,
			DataConnection                          dataConnection,
			IDbConnection                           connection,
			InformixProviderAdapter.BulkCopyAdapter bulkCopy)
		{
			var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
			var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
			var rd      = new BulkCopyReader<T>(dataConnection, columns, source);
			var sqlopt  = InformixProviderAdapter.IfxBulkCopyOptions.Default;
			var rc      = new BulkCopyRowsCopied();

			if (options.KeepIdentity == true) sqlopt |= InformixProviderAdapter.IfxBulkCopyOptions.KeepIdentity;
			if (options.TableLock    == true) sqlopt |= InformixProviderAdapter.IfxBulkCopyOptions.TableLock;

			using (var bc = bulkCopy.Create(connection, sqlopt))
			{
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = options.NotifyAfter;

					bc.IfxRowsCopied += (sender, args) =>
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

				var tableName = GetTableName(sb, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(bulkCopy.CreateColumnMapping(i, sb.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

				TraceAction(
					dataConnection,
					() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
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

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			using (new InvariantCultureRegion(null))
				return base.MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			using (new InvariantCultureRegion(null))
				return base.MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NETFRAMEWORK
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			using (new InvariantCultureRegion(null))
				return base.MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif
	}
}
