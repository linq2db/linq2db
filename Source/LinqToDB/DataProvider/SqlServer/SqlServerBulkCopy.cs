using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Data;
	using Data;
	using SqlProvider;

	class SqlServerBulkCopy : BasicBulkCopy
	{
		private readonly SqlServerDataProvider _provider;

		public SqlServerBulkCopy(SqlServerDataProvider provider)
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

			var connection = _provider.TryConvertConnection(_provider.Wrapper.Value.ConnectionType, dataConnection.Connection, dataConnection.MappingSchema);

			var transaction = dataConnection.Transaction;
			if (connection != null && transaction != null)
				transaction = _provider.TryConvertTransaction(_provider.Wrapper.Value.TransactionType, transaction, dataConnection.MappingSchema);

			if (connection != null && (dataConnection.Transaction == null || transaction != null))
			{
				var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
				var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
				var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
				var rd      = new BulkCopyReader(dataConnection, columns, source);
				var sqlopt  = SqlServerWrappers.SqlBulkCopyOptions.Default;
				var rc      = new BulkCopyRowsCopied();

				if (options.CheckConstraints       == true) sqlopt |= SqlServerWrappers.SqlBulkCopyOptions.CheckConstraints;
				if (options.KeepIdentity           == true) sqlopt |= SqlServerWrappers.SqlBulkCopyOptions.KeepIdentity;
				if (options.TableLock              == true) sqlopt |= SqlServerWrappers.SqlBulkCopyOptions.TableLock;
				if (options.KeepNulls              == true) sqlopt |= SqlServerWrappers.SqlBulkCopyOptions.KeepNulls;
				if (options.FireTriggers           == true) sqlopt |= SqlServerWrappers.SqlBulkCopyOptions.FireTriggers;
				if (options.UseInternalTransaction == true) sqlopt |= SqlServerWrappers.SqlBulkCopyOptions.UseInternalTransaction;

				using (var bc = _provider.Wrapper.Value.CreateBulkCopy(connection, sqlopt, transaction))
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

					if (options.MaxBatchSize.HasValue) bc.BatchSize = options.MaxBatchSize.Value;
					if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

					var tableName = GetTableName(sb, options, table);

					bc.DestinationTableName = tableName;

					for (var i = 0; i < columns.Count; i++)
						bc.ColumnMappings.Add(_provider.Wrapper.Value.CreateBulkCopyColumnMapping(i, sb.Convert(columns[i].ColumnName, ConvertType.NameToQueryField).ToString()));

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

			return MultipleRowsCopy(table, options, source);
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
