using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.DB2
{
	using Data;
	using DB2BulkCopyOptions = DB2Wrappers.DB2BulkCopyOptions;

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
			if (!(table?.DataContext is DataConnection dataConnection))
				throw new ArgumentNullException(nameof(dataConnection));

			if (dataConnection.Transaction == null)
			{
				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
				var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
				var tableName  = GetTableName(sqlBuilder, options, table);

				DB2Wrappers.Initialize(dataConnection.MappingSchema);

				var connection = _provider.TryConvertConnection(DB2Wrappers.ConnectionType, dataConnection.Connection, dataConnection.MappingSchema);
				if (connection != null)
				{
					var columns = descriptor.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var rd      = new BulkCopyReader(dataConnection, columns, source);
					var rc      = new BulkCopyRowsCopied();

					var bcOptions = DB2BulkCopyOptions.Default;

					if (options.KeepIdentity == true) bcOptions |= DB2BulkCopyOptions.KeepIdentity;
					if (options.TableLock    == true) bcOptions |= DB2BulkCopyOptions.TableLock;

					using (var bc = DB2Wrappers.NewDB2BulkCopy(connection, bcOptions))
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

						bc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							bc.ColumnMappings.Add(DB2Wrappers.NewDB2BulkCopyColumnMapping(i, columns[i].ColumnName));

						TraceAction(
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
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			var dataConnection = (DataConnection)table.DataContext;

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2(table, options, source, " FROM SYSIBM.SYSDUMMY1");

			return MultipleRowsCopy1(table, options, source);
		}
	}
}
