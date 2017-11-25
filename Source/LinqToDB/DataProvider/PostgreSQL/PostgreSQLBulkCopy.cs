using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Extensions;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;

	class PostgreSQLBulkCopy : BasicBulkCopy
	{
		private readonly PostgreSQLDataProvider _dataProvider;
		private readonly Type _connectionType;

		public PostgreSQLBulkCopy(PostgreSQLDataProvider dataProvider, Type connectionType)
		{
			_dataProvider = dataProvider;
			_connectionType = connectionType;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(dataConnection, options, false, source);
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			var connection = dataConnection.Connection;

			if (connection == null)
				return MultipleRowsCopy(dataConnection, options, source);

			if (!(connection.GetType() == _connectionType || connection.GetType().IsSubclassOfEx(_connectionType)))
				return MultipleRowsCopy(dataConnection, options, source);

			var sqlBuilder = _dataProvider.CreateSqlBuilder();
			var ed = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName = GetTableName(sqlBuilder, options, ed);
			var columns = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();

			var fields = string.Join(", ", columns.Select(column => sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField)));
			var copyCommand = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

			var rowsCopied = new BulkCopyRowsCopied();
			var batchSize = Math.Max(10, options.MaxBatchSize ?? 10000);
			var currentCount = 0;

			var dc = (dynamic)connection;
			var writer = dc.BeginBinaryImport(copyCommand);

			foreach (var item in source)
			{
				writer.StartRow();

				for (var i = 0; i < columns.Count; i++)
				{
					var column = columns[i];
					var value = column.GetValue(dataConnection.MappingSchema, item);

					if (value == null)
					{
						writer.WriteNull();
					}
					else
					{
						writer.Write(value);
					}
				}

				currentCount++;
				rowsCopied.RowsCopied++;

				if (currentCount >= batchSize)
				{
					writer.Dispose();

					if (options.RowsCopiedCallback != null)
					{
						options.RowsCopiedCallback(rowsCopied);

						if (rowsCopied.Abort) break;
					}

					writer = dc.BeginBinaryImport(copyCommand);
					currentCount = 0;
				}
			}

			writer.Dispose();

			return rowsCopied;
		}
	}
}
