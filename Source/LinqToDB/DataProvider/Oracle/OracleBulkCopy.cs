using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using Data;
	using SqlProvider;

	class OracleBulkCopy : BasicBulkCopy
	{
		private readonly OracleDataProvider _provider;

		public OracleBulkCopy(OracleDataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table,
			BulkCopyOptions options,
			IEnumerable<T>  source)
		{
			if (table.DataContext is DataConnection dataConnection && dataConnection.Transaction == null && _provider.Adapter.BulkCopy != null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				if (connection != null)
				{
					var ed        = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns   = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb        = _provider.CreateSqlBuilder(dataConnection.MappingSchema);
					var rd        = new BulkCopyReader(dataConnection, columns, source);
					var sqlopt    = OracleProviderAdapter.OracleBulkCopyOptions.Default;
					var rc        = new BulkCopyRowsCopied();
					var tableName = GetTableName(sb, options, table);

					if (options.UseInternalTransaction == true) sqlopt |= OracleProviderAdapter.OracleBulkCopyOptions.UseInternalTransaction;

					using (var bc = _provider.Adapter.BulkCopy.Create(connection, sqlopt))
					{
						var notifyAfter = options.NotifyAfter == 0 && options.MaxBatchSize.HasValue
							? options.MaxBatchSize.Value
							: options.NotifyAfter;

						if (notifyAfter != 0 && options.RowsCopiedCallback != null)
						{
							bc.NotifyAfter = options.NotifyAfter;

							bc.OracleRowsCopied += (sender, args) =>
							{
								rc.RowsCopied = args.RowsCopied;
								options.RowsCopiedCallback(rc);
								if (rc.Abort)
									args.Abort = true;
							};
						}

						if (options.MaxBatchSize.HasValue)    bc.BatchSize       = options.MaxBatchSize.Value;
						if (options.BulkCopyTimeout.HasValue) bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;

						bc.DestinationTableName = tableName;

						for (var i = 0; i < columns.Count; i++)
							bc.ColumnMappings.Add(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

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
			}
			

			return MultipleRowsCopy(table, options, source);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			switch (OracleTools.UseAlternativeBulkCopy)
			{
				case AlternativeBulkCopy.InsertInto: return OracleMultipleRowsCopy2(new MultipleRowsHelper<T>(table, options), source);
				case AlternativeBulkCopy.InsertDual: return OracleMultipleRowsCopy3(new MultipleRowsHelper<T>(table, options), source);
				default                            : return OracleMultipleRowsCopy1(new MultipleRowsHelper<T>(table, options), source);
			}
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
		{
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

		static BulkCopyRowsCopied OracleMultipleRowsCopy2(MultipleRowsHelper helper, IEnumerable source)
		{
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

			var list = new List<object>(31);

			foreach (var item in source)
			{
				list.Add(item);

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize)
				{
					if (!Execute(helper, list))
						return helper.RowsCopied;

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				Execute(helper, list);
			}

			return helper.RowsCopied;
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy3(MultipleRowsHelper helper, IEnumerable source)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.AppendLine()
					.Append("\t")
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(",");

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.AppendLine(")")
				;

			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("\tSELECT ");
				helper.BuildColumns(item, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
				helper.StringBuilder.Append(" FROM DUAL ");
				helper.StringBuilder.Append(" UNION ALL");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.Length -= " UNION ALL".Length;
					helper.StringBuilder
						.AppendLine()
						;
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.Length -= " UNION ALL".Length;
				helper.StringBuilder
					.AppendLine()
					;
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		static bool Execute(MultipleRowsHelper helper, List<object> list)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? helper.DataConnection.MappingSchema.GetDataType(column.MemberType).DataType
					: column.DataType;

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), list.Select(o => column.GetValue(helper.DataConnection.MappingSchema, o)).ToArray(), dataType, column.DbType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			return helper.Execute();
		}
	}
}
