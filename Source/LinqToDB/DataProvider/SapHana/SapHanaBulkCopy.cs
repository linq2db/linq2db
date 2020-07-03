using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	using Data;

	class SapHanaBulkCopy : BasicBulkCopy
	{
		private readonly SapHanaDataProvider _provider;

		public SapHanaBulkCopy(SapHanaDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T> source)
		{
			if (!(table?.DataContext is DataConnection dataConnection))
				throw new ArgumentNullException(nameof(dataConnection));

			var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

			var transaction = dataConnection.Transaction;
			if (connection != null && transaction != null)
				transaction = _provider.TryGetProviderTransaction(transaction, dataConnection.MappingSchema);

			if (connection != null && (dataConnection.Transaction == null || transaction != null))
			{
				var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
				var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
				var rc      = new BulkCopyRowsCopied();


				var hanaOptions = SapHanaProviderAdapter.HanaBulkCopyOptions.Default;

				if (options.KeepIdentity == true) hanaOptions |= SapHanaProviderAdapter.HanaBulkCopyOptions.KeepIdentity;


				using (var bc = _provider.Adapter.CreateBulkCopy(connection, hanaOptions, transaction))
				{
					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					{
						bc.NotifyAfter = options.NotifyAfter;

						bc.HanaRowsCopied += (sender, args) =>
						{
							rc.RowsCopied = args.RowsCopied;
							options.RowsCopiedCallback(rc);
							if (rc.Abort)
								args.Abort = true;
						};
					}

					if (options.MaxBatchSize.HasValue)    bc.BatchSize       = options.MaxBatchSize.Value;
					if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

					var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema);
					var tableName  = GetTableName(sqlBuilder, options, table);

					bc.DestinationTableName = tableName;

					for (var i = 0; i < columns.Count; i++)
						bc.ColumnMappings.Add(_provider.Adapter.CreateBulkCopyColumnMapping(i, columns[i].ColumnName));

					var rd = new BulkCopyReader<T>(dataConnection, columns, source);

					TraceAction(
						dataConnection,
						() => "INSERT BULK " + tableName + Environment.NewLine,
						() => { bc.WriteToServer(rd); return rd.Count; });

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
	}
}
