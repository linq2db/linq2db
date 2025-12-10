using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// We subtract 1 since SQL Server ADO Provider uses one parameter for command.
		/// </remarks>
		protected override int                   MaxParameters => 2099;
		/// <remarks>
		/// Based on https://docs.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?redirectedfrom=MSDN&amp;view=sql-server-ver15
		/// Default Max is actually (4096*65536) = 256MIB, but we keep a lower number here to avoid the cost of huge statements.
		/// </remarks>
		protected override int                   MaxSqlLength => 327670;
		private readonly   SqlServerDataProvider _provider;

		public SqlServerBulkCopy(SqlServerDataProvider provider)
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
			var connections = await TryGetProviderConnectionsAsync(table, cancellationToken).ConfigureAwait(false);
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
			var connections = await TryGetProviderConnectionsAsync(table, cancellationToken).ConfigureAwait(false);
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

			return null;
		}

		private async Task<ProviderConnections?> TryGetProviderConnectionsAsync<T>(ITable<T> table, CancellationToken cancellationToken)
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

			return null;
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
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
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

					bc.SqlRowsCopied += (_, args) =>
					{
						rc.RowsCopied = args.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							args.Abort = true;
					};
				}

				if (options.MaxBatchSize.HasValue)
					bc.BatchSize = options.MaxBatchSize.Value;

				if (options.BulkCopyTimeout.HasValue || LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = options.BulkCopyTimeout ?? dataConnection.CommandTimeout;

				var tableName = GetTableName(sb, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, sb.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

				await TraceActionAsync(
					dataConnection,
					() => "INSERT ASYNC BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					async () =>
					{
						await bc.WriteToServerAsync(rd, cancellationToken).ConfigureAwait(false);
						return rd.Count;
					}).ConfigureAwait(false);
			}

			if (rc.RowsCopied != rd.Count)
			{
				rc.RowsCopied = rd.Count;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rc);
			}

			await CloseConnectionIfNecessaryAsync(table.DataContext).ConfigureAwait(false);

			return rc;
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
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
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

					bc.SqlRowsCopied += (_, args) =>
					{
						rc.RowsCopied = args.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							args.Abort = true;
					};
				}

				if (options.MaxBatchSize.HasValue)
					bc.BatchSize = options.MaxBatchSize.Value;

				if (options.BulkCopyTimeout.HasValue || LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = options.BulkCopyTimeout ?? dataConnection.CommandTimeout;

				var tableName = GetTableName(sb, options, table);

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, sb.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

				TraceAction(
					dataConnection,
					() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
					() =>
					{
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

			CloseConnectionIfNecessary(table.DataContext);

			return rc;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var helper = CreateRowsHelper(table, options);

			if (options.BulkCopyOptions.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			var ret = ((SqlServerDataProvider)helper.DataConnection.DataProvider).Version switch
			{
				SqlServerVersion.v2005 => MultipleRowsCopy2(helper, source, ""),
				_                      => MultipleRowsCopy1(helper, source),
			};

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

				CloseConnectionIfNecessary(helper.OriginalContext);
			}

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = CreateRowsHelper(table, options);

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(false);
			}

			var ret = ((SqlServerDataProvider)helper.DataConnection.DataProvider).Version switch
			{
				SqlServerVersion.v2005 => await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
										.ConfigureAwait(false),
				_ => await MultipleRowsCopy1Async(helper, source, cancellationToken)
										.ConfigureAwait(false),
			};

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(false);

				await CloseConnectionIfNecessaryAsync(helper.OriginalContext).ConfigureAwait(false);
			}

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = CreateRowsHelper(table, options);

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(false);
			}

			var ret = ((SqlServerDataProvider)helper.DataConnection.DataProvider).Version switch
			{
				SqlServerVersion.v2005 =>
					await MultipleRowsCopy2Async(helper, source, "", cancellationToken).ConfigureAwait(false),

				_ =>
					await MultipleRowsCopy1Async(helper, source, cancellationToken).ConfigureAwait(false),
			};

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(false);

				await CloseConnectionIfNecessaryAsync(helper.OriginalContext).ConfigureAwait(false);
			}

			return ret;
		}

		private static readonly Func<DataOptions, ColumnDescriptor, object?, bool> _convertToParameter =
			static (options, cd, v) => options.BulkCopyOptions.UseParameters && cd.StorageType != typeof(float[])
#if NET8_0_OR_GREATER
				&& cd.StorageType != typeof(Half[])
#endif
				;

		private MultipleRowsHelper<T> CreateRowsHelper<T>(ITable<T> table, DataOptions options) where T : notnull
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (_provider.Provider == SqlServerProvider.SystemDataSqlClient)
				helper.ConvertToParameter = _convertToParameter;

			helper.SuppressCloseAfterUse = options.BulkCopyOptions.KeepIdentity == true;

			return helper;
	}
}
}
