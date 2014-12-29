using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.DB2
{
	using Data;
	using SqlProvider;

	class DB2BulkCopy : BasicBulkCopy
	{
		public DB2BulkCopy(Type connectionType)
		{
			_connectionType = connectionType;
		}

		readonly Type _connectionType;

		Func<IDbConnection,int,IDisposable> _bulkCopyCreator;
		Func<int,string,object>             _columnMappingCreator;
		Action<object,Action<object>>       _bulkCopySubscriber;

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection  dataConnection,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			if (dataConnection.Transaction == null)
			{
				var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder();
				var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
				var tableName  = GetTableName(sqlBuilder, descriptor);

				if (_bulkCopyCreator == null)
				{
					var bulkCopyType       = _connectionType.Assembly.GetType("IBM.Data.DB2.DB2BulkCopy",              false);
					var bulkCopyOptionType = _connectionType.Assembly.GetType("IBM.Data.DB2.DB2BulkCopyOptions",       false);
					var columnMappingType  = _connectionType.Assembly.GetType("IBM.Data.DB2.DB2BulkCopyColumnMapping", false);

					if (bulkCopyType != null)
					{
						_bulkCopyCreator      = CreateBulkCopyCreator(_connectionType, bulkCopyType, bulkCopyOptionType);
						_columnMappingCreator = CreateColumnMappingCreator(columnMappingType);
					}
				}

				if (_bulkCopyCreator != null)
				{
					var columns = descriptor.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var rd      = new BulkCopyReader(dataConnection.DataProvider, columns, source);
					var rc      = new BulkCopyRowsCopied();

					var bcOptions = 0; // Default

					if (options.KeepIdentity == true) bcOptions |= 1; // KeepIdentity = 1, TableLock = 2, Truncate = 4,
					if (options.TableLock    == true) bcOptions |= 2;

					using (var bc = _bulkCopyCreator(dataConnection.Connection, bcOptions))
					{
						dynamic dbc = bc;

						var notifyAfter = options.NotifyAfter == 0 && options.MaxBatchSize.HasValue ?
							options.MaxBatchSize.Value : options.NotifyAfter;

						if (notifyAfter != 0 && options.RowsCopiedCallback != null)
						{
							if (_bulkCopySubscriber == null)
								_bulkCopySubscriber = CreateBulkCopySubscriber(bc, "DB2RowsCopied");

							dbc.NotifyAfter = notifyAfter;

							_bulkCopySubscriber(bc, arg =>
							{
								dynamic darg = arg;
								rc.RowsCopied = darg.RowsCopied;
								options.RowsCopiedCallback(rc);
								if (rc.Abort)
									darg.Abort = true;
							});
						}

						if (options.BulkCopyTimeout.HasValue)
							dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						dbc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							dbc.ColumnMappings.Add((dynamic)_columnMappingCreator(i, columns[i].ColumnName));

						dbc.WriteToServer(rd);
					}

					rc.RowsCopied = rd.Count;

					return rc;
				}
			}

			return MultipleRowsCopy(dataConnection, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2(dataConnection, options, false, source, " FROM SYSIBM.SYSDUMMY1");

			return MultipleRowsCopy1(dataConnection, options, false, source);
		}
	}
}
