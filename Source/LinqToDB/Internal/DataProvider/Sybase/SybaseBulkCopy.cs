using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	// !table.TableOptions.HasIsTemporary() check:
	// native bulk copy produce following error for insert into temp table:
	// AseException : Incorrect syntax near ','.
	sealed class SybaseBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Setting is conservative based on https://maxdb.sap.com/doc/7_6/f6/069940ccd42a54e10000000a1550b0/content.htm
		/// Possible to be higher in other versions.
		/// </remarks>
		protected override int                MaxSqlLength  => 65536;
		/// <remarks>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// We subtract 1 based on possibility of provider using parameter for command.
		/// </remarks>
		protected override int                MaxParameters => 1999;
		private readonly   SybaseDataProvider _provider;

		public SybaseBulkCopy(SybaseDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var connections = GetProviderConnection(table);
			if (connections.HasValue && !table.TableOptions.HasIsTemporary())
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
			var connections = await GetProviderConnectionAsync(table, cancellationToken).ConfigureAwait(false);
			if (connections.HasValue && !table.TableOptions.HasIsTemporary())
			{
				// call the synchronous provider-specific implementation
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options.BulkCopyOptions,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source));
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await GetProviderConnectionAsync(table, cancellationToken).ConfigureAwait(false);
			if (connections.HasValue && !table.TableOptions.HasIsTemporary())
			{
				// call the synchronous provider-specific implementation
				return ProviderSpecificCopyInternal(
					connections.Value,
					table,
					options.BulkCopyOptions,
					(columns) => new BulkCopyReader<T>(connections.Value.DataConnection, columns, source, cancellationToken));
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		private ProviderConnections? GetProviderConnection<T>(ITable<T> table)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());

				// for run in transaction see
				// https://stackoverflow.com/questions/57675379
				// provider will call sp_oledb_columns which creates temp table
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

		private async Task<ProviderConnections?> GetProviderConnectionAsync<T>(ITable<T> table, CancellationToken cancellationToken)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, await dataConnection.OpenDbConnectionAsync(cancellationToken).ConfigureAwait(false));

				// for run in transaction see
				// https://stackoverflow.com/questions/57675379
				// provider will call sp_oledb_columns which creates temp table
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
			var sqlopt         = SybaseProviderAdapter.AseBulkCopyOptions.Default;
			var rc             = new BulkCopyRowsCopied();

			if (options.CheckConstraints       == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.CheckConstraints;
			if (options.KeepIdentity           == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.KeepIdentity;
			if (options.TableLock              == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.TableLock;
			if (options.KeepNulls              == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.KeepNulls;
			if (options.FireTriggers           == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.FireTriggers;
			if (options.UseInternalTransaction == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.UseInternalTransaction;

			using (var bc = _provider.Adapter.BulkCopy!.Create(connection, sqlopt, transaction))
			{
				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
				{
					bc.NotifyAfter = options.NotifyAfter;

					bc.AseRowsCopied += (sender, args) =>
					{
						rc.RowsCopied = args.RowCopied;
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

				var tableName = GetTableName(sb, options, table);

				// do not convert table and column names to valid sql name, as sybase bulk copy implementation
				// doesn't understand escaped names (facepalm)
				// which means it will probably fail when escaping required anyways...
				bc.DestinationTableName = GetTableName(sb, options, table, false);

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(_provider.Adapter.BulkCopy.CreateColumnMapping(columns[i].ColumnName, columns[i].ColumnName));

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

			if (table.DataContext.CloseAfterUse)
				table.DataContext.Close();

			return rc;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy2(table, options, source, "");
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy2Async(table, options, source, "", cancellationToken);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy2Async(table, options, source, "", cancellationToken);
		}
	}
}
