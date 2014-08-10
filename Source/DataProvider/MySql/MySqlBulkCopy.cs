using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToDB.Data;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.MySql
{
	class MySqlBulkCopy : BasicBulkCopy
	{
		public MySqlBulkCopy(MySqlDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
		}

		readonly MySqlDataProvider _dataProvider;

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			var sqlBuilder = _dataProvider.CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = GetTableName(sqlBuilder, descriptor);
			var sb         = new StringBuilder();
			var buildValue = BasicSqlBuilder.GetBuildValue(sqlBuilder, sb);
			var columns    = descriptor.Columns.Where(c => !c.SkipOnInsert).ToArray();
			var pname      = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();
			var rowsCopied = new BulkCopyRowsCopied();

			sb
				.AppendFormat("INSERT INTO {0}", tableName).AppendLine()
				.Append("(");

			foreach (var column in columns)
				sb
					.AppendLine()
					.Append("\t")
					.Append(sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(",");

			sb.Length--;
			sb
				.AppendLine()
				.Append(")");

			sb
				.AppendLine()
				.Append("VALUES");

			var headerLen    = sb.Length;
			var currentCount = 0;
			var batchSize    = options.MaxBatchSize ?? 1000;

			if (batchSize <= 0)
				batchSize = 1000;

			var parms = new List<DataParameter>();
			var pidx = 0;

			foreach (var item in source)
			{
				sb
					.AppendLine()
					.Append("(");

				foreach (var column in columns)
				{
					var value = column.GetValue(item);

					if (value == null)
					{
						sb.Append("NULL");
					}
					else
						switch (Type.GetTypeCode(value.GetType()))
						{
							case TypeCode.DBNull:
								sb.Append("NULL");
								break;

							case TypeCode.String:
								var isString = false;

								switch (column.DataType)
								{
									case DataType.NVarChar:
									case DataType.Char:
									case DataType.VarChar:
									case DataType.NChar:
									case DataType.Undefined:
										isString = true;
										break;
								}

								if (isString) goto case TypeCode.Int32;
								goto default;

							case TypeCode.Boolean:
							case TypeCode.Char:
							case TypeCode.SByte:
							case TypeCode.Byte:
							case TypeCode.Int16:
							case TypeCode.UInt16:
							case TypeCode.Int32:
							case TypeCode.UInt32:
							case TypeCode.Int64:
							case TypeCode.UInt64:
							case TypeCode.Single:
							case TypeCode.Double:
							case TypeCode.Decimal:
							case TypeCode.DateTime:
								//SetParameter(dataParam, "", column.DataType, value);

								buildValue(value);
								break;

							default:
								var name = pname + ++pidx;

								sb.Append(name);
								parms.Add(new DataParameter("p" + pidx, value, column.DataType));

								break;
						}

					sb.Append(",");
				}

				sb.Length--;
				sb.Append("),");

				rowsCopied.RowsCopied++;
				currentCount++;

				if (currentCount >= batchSize || parms.Count > 100000 || sb.Length > 100000)
				{
					sb.Length--;

					dataConnection.Execute(sb.AppendLine().ToString(), parms.ToArray());

					if (options.RowsCopiedCallback != null)
					{
						options.RowsCopiedCallback(rowsCopied);

						if (rowsCopied.Abort)
							return rowsCopied;
					}

					parms.Clear();
					pidx         = 0;
					currentCount = 0;
					sb.Length    = headerLen;
				}
			}

			if (currentCount > 0)
			{
				sb.Length--;

				dataConnection.Execute(sb.ToString(), parms.ToArray());
				sb.Length = headerLen;

				if (options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rowsCopied);
			}

			return rowsCopied;
		}
	}
}
