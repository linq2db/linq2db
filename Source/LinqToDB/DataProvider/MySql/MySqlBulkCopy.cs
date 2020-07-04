using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;

namespace LinqToDB.DataProvider.MySql
{
	using Data;
	using LinqToDB.Common;
	using LinqToDB.SqlProvider;

	class MySqlBulkCopy : BasicBulkCopy
	{
		private readonly MySqlDataProvider _provider;

		public MySqlBulkCopy(MySqlDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T>       table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (_provider.Adapter.BulkCopy != null && table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				var transaction = dataConnection.Transaction;
				if (connection != null && transaction != null)
					transaction = _provider.TryGetProviderTransaction(transaction, dataConnection.MappingSchema);

				if (connection != null && (dataConnection.Transaction == null || transaction != null))
				{
					var ed      = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb      = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
					var rc      = new BulkCopyRowsCopied();

					var bc = _provider.Adapter.BulkCopy.Create(connection, transaction);
					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
					{
						bc.NotifyAfter = options.NotifyAfter;

						bc.MySqlRowsCopied += (sender, args) =>
						{
							rc.RowsCopied += args.RowsCopied;
							options.RowsCopiedCallback(rc);
							if (rc.Abort)
								args.Abort = true;
						};
					}

					if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

					var tableName = GetTableName(sb, options, table);

					bc.DestinationTableName = GetTableName(sb, options, table);

					for (var i = 0; i < columns.Count; i++)
						bc.AddColumnMapping(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

					// emulate missing BatchSize property
					// this is needed, because MySql fails on big batches, so users should be able to limit batch size
					foreach (var batch in EnumerableHelper.Batch(source, options.MaxBatchSize ?? int.MaxValue))
					{
						var rd = new BulkCopyReader<T>(dataConnection, columns, batch);

						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
							() => { bc.WriteToServer(rd); return rd.Count; });

						rc.RowsCopied += rd.Count;
					}

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rc);

					return rc;
				}
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}
	}
}
