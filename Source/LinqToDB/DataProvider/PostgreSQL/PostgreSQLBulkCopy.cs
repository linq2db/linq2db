using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.PostgreSQL
{
	class PostgreSQLBulkCopy : BasicBulkCopy
	{
		readonly PostgreSQLDataProvider _provider;

		// TODO: permanent cache is bad
		static readonly ConcurrentDictionary<object, object> _rowWriterCache = new ConcurrentDictionary<object, object>();

		public PostgreSQLBulkCopy(PostgreSQLDataProvider dataProvider)
		{
			_provider = dataProvider;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (table.DataContext is DataConnection dataConnection)
			{
				var connection = _provider.TryConvertConnection(_provider.Wrapper.Value.ConnectionType, dataConnection.Connection, dataConnection.MappingSchema);

				if (connection == null)
					return MultipleRowsCopy(table, options, source);

				var sqlBuilder = (BasicSqlBuilder)_provider.CreateSqlBuilder(dataConnection.MappingSchema);
				var ed         = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
				var tableName  = GetTableName(sqlBuilder, options, table);
				var columns    = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToArray();

				var fields      = string.Join(", ", columns.Select(column => sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField)));
				var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

				var rowsCopied = new BulkCopyRowsCopied();
				// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
				var batchSize = Math.Max(10, options.MaxBatchSize ?? 10000);
				var currentCount = 0;

				var key = new { Type = typeof(T), options.KeepIdentity, ed };
				var rowWriter = (Action<MappingSchema, PostgreSQLWrappers.NpgsqlBinaryImporter, ColumnDescriptor[], T>)_rowWriterCache.GetOrAdd(
					key,
					_ => _provider.Wrapper.Value.GetBinaryImportRowWriter<T>(_provider, sqlBuilder, columns, dataConnection.MappingSchema));

				var useComplete = _provider.Wrapper.Value.BinaryImporterHasComplete;
				var writer      = _provider.Wrapper.Value.BeginBinaryImport(connection, copyCommand);

				try
				{
					foreach (var item in source)
					{
						rowWriter(dataConnection.MappingSchema, writer, columns, item);

						currentCount++;
						rowsCopied.RowsCopied++;

						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
						{
							options.RowsCopiedCallback(rowsCopied);

							if (rowsCopied.Abort)
							{
								if (!useComplete)
									writer.Cancel();
								break;
							}
						}

						if (currentCount >= batchSize)
						{
							if (useComplete)
								writer.Complete();

							writer.Dispose();

							writer       = _provider.Wrapper.Value.BeginBinaryImport(connection, copyCommand);
							currentCount = 0;
						}
					}

					if (!rowsCopied.Abort)
					{
						TraceAction(
							dataConnection,
							() => "INSERT BULK " + tableName + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + Environment.NewLine,
							() => { 
								if (useComplete)
									writer.Complete();
								return (int)rowsCopied.RowsCopied; });
					}

					if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null)
						options.RowsCopiedCallback(rowsCopied);
				}
				catch when (!useComplete)
				{
					writer.Cancel();
					throw;
				}
				finally
				{
					writer.Dispose();
				}

				return rowsCopied;
			}

			return MultipleRowsCopy(table, options, source);
		}
	}
}
