using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;
	using LinqToDB.Linq;
	using SqlProvider;
	using System.Data;

	class SqlServerBulkCopy : BasicBulkCopy
	{
		private readonly SqlServerDataProvider _provider;

		public SqlServerBulkCopy(SqlServerDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source)
		{
			var connections = GetProviderConnection(table);
			if (connections != null)
			{
				return ProviderSpecificCopyInternal(
					connections.Item1,
					connections.Item2,
					connections.Item3,
					table,
					options,
					(columns) => new BulkCopyReader<T>(connections.Item1, columns, source),
					false).Result;
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source)
		{
			var connections = GetProviderConnection(table);
			if (connections != null)
			{
				return ProviderSpecificCopyInternal(
					connections.Item1,
					connections.Item2,
					connections.Item3,
					table,
					options,
					(columns) => new BulkCopyReader<T>(connections.Item1, columns, source),
					true);
			}

			return MultipleRowsCopyAsync(table, options, source);
		}

#if !NET45 && !NET46
		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IAsyncEnumerable<T> source)
		{
			var connections = GetProviderConnection(table);
			if (connections != null)
			{
				return ProviderSpecificCopyInternal(
					connections.Item1,
					connections.Item2,
					connections.Item3,
					table,
					options,
					(columns) => new BulkCopyReader<T>(connections.Item1, columns, source),
					true);
			}

			return MultipleRowsCopyAsync(table, options, source);
		}
#endif

		private Tuple<DataConnection, IDbConnection, IDbTransaction?>? GetProviderConnection<T>(ITable<T> table)
		{
			if (table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				var transaction = dataConnection.Transaction;
				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(transaction, dataConnection.MappingSchema);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					return new Tuple<DataConnection, IDbConnection, IDbTransaction?>(dataConnection, connection, transaction);
				}
			}
			return null;
		}

		private async Task<BulkCopyRowsCopied> ProviderSpecificCopyInternal<T>(
			DataConnection                                          dataConnection,
			IDbConnection                                           connection,
			IDbTransaction?                                         transaction,
			ITable<T>                                               table,
			BulkCopyOptions	                                        options,
			Func<List<Mapping.ColumnDescriptor>, BulkCopyReader<T>> createDataReader,
			bool                                                    runAsync)
		{
					var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
					var rd      = createDataReader(columns);
					var sqlopt  = SqlServerProviderAdapter.SqlBulkCopyOptions.Default;
					var rc      = new BulkCopyRowsCopied();

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

						if (options.MaxBatchSize.HasValue)    bc.BatchSize       = options.MaxBatchSize.Value;
						if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						var tableName = GetTableName(sb, options, table);

						bc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, sb.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

						await TraceActionAsync(
							dataConnection,
							() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
							async () => { 
								if (runAsync)
									await bc.WriteToServerAsync(rd).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
								else
									bc.WriteToServer(rd); 
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
	}
}
