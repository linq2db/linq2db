using System;
using System.Linq;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Sybase
{
	using System.Text;
	using Data;
	using LinqToDB.SqlProvider;

	class SybaseBulkCopy : BasicBulkCopy
	{
		private readonly SybaseDataProvider _provider;

		public SybaseBulkCopy(SybaseDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T>       table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (table.DataContext is DataConnection dataConnection && _provider.Adapter.BulkCopy != null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				// for run in transaction see
				// https://stackoverflow.com/questions/57675379
				// provider will call sp_oledb_columns which creates temp table
				var transaction = dataConnection.Transaction;
				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(transaction, dataConnection.MappingSchema);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
					var rd      = new BulkCopyReader(dataConnection, columns, source);
					var sqlopt  = SybaseProviderAdapter.AseBulkCopyOptions.Default;
					var rc      = new BulkCopyRowsCopied();

					if (options.CheckConstraints       == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.CheckConstraints;
					if (options.KeepIdentity           == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.KeepIdentity;
					if (options.TableLock              == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.TableLock;
					if (options.KeepNulls              == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.KeepNulls;
					if (options.FireTriggers           == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.FireTriggers;
					if (options.UseInternalTransaction == true) sqlopt |= SybaseProviderAdapter.AseBulkCopyOptions.UseInternalTransaction;

					using (var bc = _provider.Adapter.BulkCopy.Create(connection, sqlopt, transaction))
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

						if (options.MaxBatchSize.HasValue)    bc.BatchSize       = options.MaxBatchSize.Value;
						if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						var tableName = GetTableName(sb, options, table);

						// do not convert table and column names to valid sql name, as sybase bulk copy implementation
						// doesn't understand escaped names (facepalm)
						// which means it will probably fail when escaping required anyways...
						bc.DestinationTableName = GetDestinationTableName(sb, options, table);

						for (var i = 0; i < columns.Count; i++)
							bc.ColumnMappings.Add(_provider.Adapter.BulkCopy.CreateColumnMapping(columns[i].ColumnName, columns[i].ColumnName));

						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
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
			}

			return MultipleRowsCopy(table, options, source);
		}

		private static string GetDestinationTableName<T>(ISqlBuilder sqlBuilder, BulkCopyOptions options, ITable<T> table)
		{
			var serverName   = options.ServerName   ?? table.ServerName;
			var databaseName = options.DatabaseName ?? table.DatabaseName;
			var schemaName   = options.SchemaName   ?? table.SchemaName;
			var tableName    = options.TableName    ?? table.TableName;

			// no escaping, as otherwise driver will be unable to load table schema and fail
			// basically, just another bug in driver, which makes bulk copy unusable for some table names
			return sqlBuilder.BuildTableName(
				new StringBuilder(),
				serverName,
				databaseName,
				schemaName,
				tableName)
			.ToString();
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy2(table, options, source, "");
		}
	}
}
