using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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

			var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder();
			var descriptor = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
			var tableName  = GetTableName(sqlBuilder, options, descriptor);

			if (dataConnection.Transaction == null)
			{
				if (_bulkCopyCreator == null)
				{
					var clientNamespace    = ((OracleDataProvider)dataConnection.DataProvider).AssemblyName + ".Client.";
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

						TraceAction(
							dataConnection,
							"INSERT BULK " + tableName + Environment.NewLine,
							() => { dbc.WriteToServer(rd); return rd.Count; });
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

			options.BulkCopyType = BulkCopyType.MultipleRows;

			return MultipleRowsCopy(dataConnection, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return OracleTools.UseAlternativeBulkCopy
				? MultipleRowsCopy2(dataConnection, options, source)
				: MultipleRowsCopy1(dataConnection, options, source);
		}

		BulkCopyRowsCopied MultipleRowsCopy1<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(dataConnection, options, false);

			helper.StringBuilder.AppendLine("INSERT ALL");
			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder.AppendFormat("\tINTO {0} (", helper.TableName);

				foreach (var column in helper.Columns)
					helper.StringBuilder
						.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
						.Append(", ");

				helper.StringBuilder.Length -= 2;

				helper.StringBuilder.Append(") VALUES (");
				helper.BuildColumns(item, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
				helper.StringBuilder.AppendLine(")");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.AppendLine("SELECT * FROM dual");
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.AppendLine("SELECT * FROM dual");
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		BulkCopyRowsCopied MultipleRowsCopy2<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(dataConnection, options, false);

			helper.StringBuilder.AppendFormat("INSERT INTO {0} (", helper.TableName);

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(", ");

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.Append(") VALUES (");

			for (var i = 0; i < helper.Columns.Length; i++)
				helper.StringBuilder.Append(":p" + ( i + 1)).Append(", ");

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.AppendLine(")");
			helper.SetHeader();

			var list = new List<T>(31);

			foreach (var item in source)
			{
				list.Add(item);

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize)
				{
					if (!Execute(dataConnection, helper, list))
						return helper.RowsCopied;

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				Execute(dataConnection, helper, list);
			}

			return helper.RowsCopied;
		}

		bool Execute<T>(DataConnection dataConnection, MultipleRowsHelper<T> helper, List<T> list)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? dataConnection.MappingSchema.GetDataType(column.MemberType).DataType
					: column.DataType;
				//var type     = dataConnection.DataProvider.ConvertParameterType(column.MemberType, dataType);

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), list.Select(o => column.GetValue(o)).ToArray(), dataType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			return helper.Execute();
		}
	}
}
