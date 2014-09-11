using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Oracle
{
	using Data;
	using SqlProvider;

	class OracleBulkCopy : BasicBulkCopy
	{
		public OracleBulkCopy(OracleDataProvider dataProvider, Type connectionType)
		{
			_dataProvider   = dataProvider;
			_connectionType = connectionType;
		}

		readonly OracleDataProvider _dataProvider;
		readonly Type               _connectionType;

		Func<IDbConnection,int,IDisposable> _bulkCopyCreator;
		Func<int,string,object>             _columnMappingCreator;
		Action<object,Action<object>>       _bulkCopySubscriber;

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (dataConnection == null) throw new ArgumentNullException("dataConnection");

			var sqlBuilder = _dataProvider.CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = GetTableName(sqlBuilder, descriptor);

			if (dataConnection.Transaction == null)
			{
				if (_bulkCopyCreator == null)
				{
					var clientNamespace    = OracleTools.AssemblyName + ".Client.";
					var bulkCopyType       = _connectionType.Assembly.GetType(clientNamespace + "OracleBulkCopy",              false);
					var bulkCopyOptionType = _connectionType.Assembly.GetType(clientNamespace + "OracleBulkCopyOptions",       false);
					var columnMappingType  = _connectionType.Assembly.GetType(clientNamespace + "OracleBulkCopyColumnMapping", false);

					if (bulkCopyType != null)
					{
						_bulkCopyCreator      = CreateBulkCopyCreator(_connectionType, bulkCopyType, bulkCopyOptionType);
						_columnMappingCreator = CreateColumnMappingCreator(columnMappingType);
					}
				}

				if (_bulkCopyCreator != null)
				{
					var columns = descriptor.Columns.Where(c => !c.SkipOnInsert).ToList();
					var rd      = new BulkCopyReader(_dataProvider, columns, source);
					var rc      = new BulkCopyRowsCopied();

					var bcOptions = 0; // Default

					if (options.UseInternalTransaction == true) bcOptions |= 1; // UseInternalTransaction = 1,

					using (var bc = _bulkCopyCreator(dataConnection.Connection, bcOptions))
					{
						dynamic dbc = bc;

						var notifyAfter = options.NotifyAfter == 0 && options.MaxBatchSize.HasValue ?
							options.MaxBatchSize.Value : options.NotifyAfter;

						if (notifyAfter != 0 && options.RowsCopiedCallback != null)
						{
							if (_bulkCopySubscriber == null)
								_bulkCopySubscriber = CreateBulkCopySubscriber(bc, "OracleRowsCopied");

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

						if (options.MaxBatchSize.   HasValue) dbc.BatchSize       = options.MaxBatchSize.   Value;
						if (options.BulkCopyTimeout.HasValue) dbc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

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

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			var sqlBuilder = _dataProvider.CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = GetTableName(sqlBuilder, descriptor);
			var rowsCopied = new BulkCopyRowsCopied();
			var sb         = new StringBuilder();
			var buildValue = BasicSqlBuilder.GetBuildValue(sqlBuilder, sb);
			var columns    = descriptor.Columns.Where(c => !c.SkipOnInsert).ToArray();
			var pname      = sqlBuilder.Convert("p", ConvertType.NameToQueryParameter).ToString();

			sb.AppendLine("INSERT ALL");

			var headerLen    = sb.Length;
			var currentCount = 0;
			var batchSize    = options.MaxBatchSize ?? 1000;

			if (batchSize <= 0)
				batchSize = 1000;

			var parms = new List<DataParameter>();
			var pidx = 0;

			foreach (var item in source)
			{
				sb.AppendFormat("\tINTO {0} (", tableName);

				foreach (var column in columns)
					sb
						.Append(sqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
						.Append(", ");

				sb.Length -= 2;

				sb.Append(") VALUES (");

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
									case DataType.NVarChar  :
									case DataType.Char      :
									case DataType.VarChar   :
									case DataType.NChar     :
									case DataType.Undefined :
										isString = true;
										break;
								}

								if (isString) goto case TypeCode.Int32;
								goto default;

							case TypeCode.Boolean  :
							case TypeCode.Char     :
							case TypeCode.SByte    :
							case TypeCode.Byte     :
							case TypeCode.Int16    :
							case TypeCode.UInt16   :
							case TypeCode.Int32    :
							case TypeCode.UInt32   :
							case TypeCode.Int64    :
							case TypeCode.UInt64   :
							case TypeCode.Single   :
							case TypeCode.Double   :
							case TypeCode.Decimal  :
							case TypeCode.DateTime :
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
				sb.AppendLine(")");

				rowsCopied.RowsCopied++;
				currentCount++;

				if (currentCount >= batchSize || parms.Count > 100000 || sb.Length > 100000)
				{
					sb.AppendLine("SELECT * FROM dual");

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
				sb.AppendLine("SELECT * FROM dual");

				dataConnection.Execute(sb.ToString(), parms.ToArray());
				sb.Length = headerLen;

				if (options.RowsCopiedCallback != null)
					options.RowsCopiedCallback(rowsCopied);
			}

			return rowsCopied;
		}
	}
}
