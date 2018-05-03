using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	class PostgreSQLBulkCopy : BasicBulkCopy
	{
		private readonly PostgreSQLDataProvider _dataProvider;
		private readonly Type                   _connectionType;

		private static readonly ConcurrentDictionary<object, object> _rowWriterCache = new ConcurrentDictionary<object, object>();

		public PostgreSQLBulkCopy(PostgreSQLDataProvider dataProvider, Type connectionType)
		{
			_dataProvider   = dataProvider;
			_connectionType = connectionType;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(dataConnection, options, source);
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			if (dataConnection == null) throw new ArgumentNullException(nameof(dataConnection));

			var connection = dataConnection.Connection;

			if (connection == null)
				return MultipleRowsCopy(dataConnection, options, source);

			if (!(connection.GetType() == _connectionType || connection.GetType().IsSubclassOfEx(_connectionType)))
				return MultipleRowsCopy(dataConnection, options, source);

			var sqlBuilder   = _dataProvider.CreateSqlBuilder();
			var ed           = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName    = GetTableName(sqlBuilder, options, ed);
			var columns      = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToArray();
			var writerType   = _connectionType.AssemblyEx().GetType("Npgsql.NpgsqlBinaryImporter", true);

			var fields       = string.Join(", ", columns.Select(column => sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField)));
			var copyCommand  = $"COPY {tableName} ({fields}) FROM STDIN (FORMAT BINARY)";

			var rowsCopied   = new BulkCopyRowsCopied();
			// batch size numbers not based on any strong grounds as I didn't found any recommendations for it
			var batchSize    = Math.Max(10, options.MaxBatchSize ?? 10000);
			var currentCount = 0;

			var dc           = (dynamic)connection;

			var key = new { Type = typeof(T), options.KeepIdentity, ed };
			var rowWriter = (Action<MappingSchema, object, ColumnDescriptor[], T>)_rowWriterCache.GetOrAdd(
				key,
				_ => BuildRowWriter<T>(writerType, columns, dataConnection.MappingSchema));

			var writer       = dc.BeginBinaryImport(copyCommand);

			// https://github.com/npgsql/npgsql/issues/1646
			// npgsql 4.0 will revert logic by removing explicit Cancel() and add explicit Complete()
			var hasCancel = writer.GetType().GetMethod("Cancel") != null;
			var hasComplete = writer.GetType().GetMethod("Complete") != null;
			try
			{
				foreach (var item in source)
				{
					rowWriter(dataConnection.MappingSchema, writer, columns, item);

					currentCount++;
					rowsCopied.RowsCopied++;

					if (currentCount >= batchSize)
					{
						if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
						{
							options.RowsCopiedCallback(rowsCopied);

							if (rowsCopied.Abort)
							{
								if (hasCancel)
									writer.Cancel();
								break;
							}
						}

						if (hasComplete)
							writer.Complete();

						writer.Dispose();

						writer = dc.BeginBinaryImport(copyCommand);
						currentCount = 0;
					}
				}

				if (!rowsCopied.Abort && hasComplete)
					writer.Complete();
			}
			catch when (hasCancel)
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

		private Action<MappingSchema, object, ColumnDescriptor[], TEntity> BuildRowWriter<TEntity>(
			Type writerType,
			ColumnDescriptor[] columns,
			MappingSchema mappingSchema)
		{
			var pMapping    = Expression.Parameter(typeof(MappingSchema));
			var pWriter     = Expression.Parameter(typeof(object));
			var pColumns    = Expression.Parameter(typeof(ColumnDescriptor[]));
			var pEntity     = Expression.Parameter(typeof(TEntity));

			var writerVar   = Expression.Variable(writerType);
			var exprs       = new List<Expression>();

			exprs.Add(Expression.Assign(writerVar, Expression.Convert(pWriter, writerType)));
			exprs.Add(Expression.Call(writerVar, "StartRow", Array<Type>.Empty));

			var builder = (BasicSqlBuilder)_dataProvider.CreateSqlBuilder();

			for (var i = 0; i < columns.Length; i++)
			{
				var npgsqlType = _dataProvider.GetNativeType(columns[i].DbType);
				if (npgsqlType == null)
				{
					var columnType = columns[i].DataType != DataType.Undefined ? new SqlDataType(columns[i].DataType) : null;

					if (columnType == null || columnType.DataType == DataType.Undefined)
						columnType = mappingSchema.GetDataType(columns[i].StorageType);

					var sb = new StringBuilder();
					builder.BuildTypeName(sb, columnType);
					npgsqlType = _dataProvider.GetNativeType(sb.ToString());
				}

				if (npgsqlType == null)
					throw new LinqToDBException($"Cannot guess PostgreSQL type for column {columns[i].ColumnName}. Specify type explicitly in column mapping.");

				// don't use WriteNull because Write already handle both null and DBNull values properly
				exprs.Add(Expression.Call(
					writerVar,
					"Write",
					new[] { typeof(object) },
					Expression.Call(
						Expression.ArrayIndex(pColumns, Expression.Constant(i)),
						MemberHelper.MethodOf((ColumnDescriptor cd) => cd.GetValue(default, default)),
						pMapping,
						pEntity),
					Expression.Constant(npgsqlType)));
			}

			var ex = Expression.Lambda<Action<MappingSchema, object, ColumnDescriptor[], TEntity>>(
					Expression.Block(new[] { writerVar }, exprs),
					pMapping, pWriter, pColumns, pEntity);

			return ex.Compile();
		}
	}
}
