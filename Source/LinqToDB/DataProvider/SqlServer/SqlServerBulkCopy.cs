using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using LinqToDB.Configuration;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;
	using SqlProvider;

	class SqlServerBulkCopy : BasicBulkCopy
	{
		public SqlServerBulkCopy(SqlServerDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
		}

		readonly SqlServerDataProvider _dataProvider;

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			if (dataConnection.Connection is DbConnection)
			{
				var connection = Proxy.GetUnderlyingObject((DbConnection)dataConnection.Connection) as SqlConnection;

				if (connection != null)
				{
					var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb      = _dataProvider.CreateSqlBuilder();
					var rd      = new BulkCopyReader(_dataProvider, dataConnection.MappingSchema, columns, source);
					var sqlopt  = SqlBulkCopyOptions.Default;
					var rc      = new BulkCopyRowsCopied();

					if (options.CheckConstraints       == true) sqlopt |= SqlBulkCopyOptions.CheckConstraints;
					if (options.KeepIdentity           == true) sqlopt |= SqlBulkCopyOptions.KeepIdentity;
					if (options.TableLock              == true) sqlopt |= SqlBulkCopyOptions.TableLock;
					if (options.KeepNulls              == true) sqlopt |= SqlBulkCopyOptions.KeepNulls;
					if (options.FireTriggers           == true) sqlopt |= SqlBulkCopyOptions.FireTriggers;
					if (options.UseInternalTransaction == true) sqlopt |= SqlBulkCopyOptions.UseInternalTransaction;

					using (var bc = new SqlBulkCopy(connection, sqlopt, (SqlTransaction)dataConnection.Transaction))
					{
						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						{
							bc.NotifyAfter = options.NotifyAfter;

							bc.SqlRowsCopied += (sender,e) =>
							{
								rc.RowsCopied = e.RowsCopied;
								options.RowsCopiedCallback(rc);
								if (rc.Abort)
									e.Abort = true;
							};
						}

						if (options.MaxBatchSize.   HasValue) bc.BatchSize       = options.MaxBatchSize.   Value;
						if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						var sqlBuilder = _dataProvider.CreateSqlBuilder();
						var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
						var tableName  = GetTableName(sqlBuilder, options, descriptor);

						bc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(
								i,
								sb.Convert(columns[i].ColumnName, ConvertType.NameToQueryField).ToString()));

						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + "("+ string.Join(", ", bc.ColumnMappings.Cast<SqlBulkCopyColumnMapping>().Select(x=>x.DestinationColumn)) + Environment.NewLine,
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

			return MultipleRowsCopy(dataConnection, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			BulkCopyRowsCopied ret;

			var helper = new MultipleRowsHelper<T>(dataConnection, options);

			if (options.KeepIdentity == true)
				dataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			switch (((SqlServerDataProvider)dataConnection.DataProvider).Version)
			{
				case SqlServerVersion.v2000 :
				case SqlServerVersion.v2005 : ret = MultipleRowsCopy2(helper, dataConnection, options, source, ""); break;
				default                     : ret = MultipleRowsCopy1(helper, dataConnection, options, source);     break;
			}

			if (options.KeepIdentity == true)
				dataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

			return ret;
		}
	}
}
