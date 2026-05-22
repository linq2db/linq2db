using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Internal.SqlProvider;

using DB2BulkCopyOptions = LinqToDB.Internal.DataProvider.DB2.DB2ProviderAdapter.DB2BulkCopyOptions;

namespace LinqToDB.Internal.DataProvider.DB2
{
	// must be public to allow reuse by iSeries provider
	// https://github.com/LinqToDB4iSeries/Linq2DB4iSeries/issues/69
	public static class DB2BulkCopyShared
	{
		public static BulkCopyRowsCopied ProviderSpecificCopyImpl<T>(
			ITable<T>                                       table,
			BulkCopyOptions                                 options,
			IEnumerable<T>                                  source,
			DataConnection                                  dataConnection,
			DbConnection                                    connection,
			DB2ProviderAdapter.BulkCopyAdapter              bulkCopy,
			Action<DataConnection, Func<string>, Func<int>> traceAction)
			where T : notnull
		{
			var descriptor = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns    = descriptor.Columns.Where(c => !c.SkipOnInsert || (options.KeepIdentity == true && c.IsIdentity)).ToList();
			using var rd   = new BulkCopyReader<T>(dataConnection, columns, source);
			var rc         = new BulkCopyRowsCopied();
			var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var tableName  = BasicBulkCopy.GetTableName(sqlBuilder, options, table);

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

					bc.DB2RowsCopied += (_, args) =>
					{
						rc.RowsCopied = args.RowsCopied;
						options.RowsCopiedCallback(rc);
						if (rc.Abort)
							args.Abort = true;
					};
				}

				if (options.BulkCopyTimeout.HasValue || LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout)
					bc.BulkCopyTimeout = options.BulkCopyTimeout ?? dataConnection.CommandTimeout;

				bc.DestinationTableName = tableName;

				for (var i = 0; i < columns.Count; i++)
					bc.ColumnMappings.Add(bulkCopy.CreateColumnMapping(i, sqlBuilder.ConvertInline(columns[i].ColumnName, ConvertType.NameToQueryField)));

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

			BasicBulkCopy.CloseConnectionIfNecessary(table.DataContext);

			return rc;
		}
	}
}
