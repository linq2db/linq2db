using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using System.Threading;
	using System.Threading.Tasks;
	using Data;
	using LinqToDB.Common;
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
			// database name is not a part of table FQN in oracle
			var serverName   = options.ServerName ?? table.ServerName;

			if (table.DataContext is DataConnection dataConnection && _provider.Adapter.BulkCopy != null
				&& serverName == null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);

				if (connection != null)
				{
					var ed        = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns   = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb        = _provider.CreateSqlBuilder(dataConnection.MappingSchema);

					// ODP.NET doesn't bulk copy doesn't work if columns that require escaping:
					// - if escaping applied, pre-flight validation fails as it performs uppercase comparison and quotes make it fail with
					//   InvalidOperationException: Column mapping is invalid
					// - if escaping not applied - if fails as expected on server, because it treats passed name as uppercased name
					//   and gives "ORA-00904: "STRINGVALUE": invalid identifier" error
					// That's quite common error in bulk copy implementation error by providers...
					var supported = true;
					foreach (var column in columns)
						if (column.ColumnName != sb.ConvertInline(column.ColumnName, ConvertType.NameToQueryField))
						{
							// fallback to sql-based copy
							// TODO: we should add support for by-ordinal column mapping to workaround it
							supported = false;
							break;
						}

					if (supported)
					{
						var rd         = new BulkCopyReader<T>(dataConnection, columns, source);
						var sqlopt     = OracleProviderAdapter.OracleBulkCopyOptions.Default;
						var rc         = new BulkCopyRowsCopied();

						var tableName   = sb.ConvertInline(options.TableName  ?? table.TableName , ConvertType.NameToQueryTable);
						var schemaName  = options.SchemaName ?? table.SchemaName;
						if (schemaName != null)
							schemaName  = sb.ConvertInline(schemaName, ConvertType.NameToSchema);

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

							if (options.MaxBatchSize.HasValue)
								bc.BatchSize = options.MaxBatchSize.Value;

							if (options.BulkCopyTimeout.HasValue)
								bc.BulkCopyTimeout = options.BulkCopyTimeout.Value;
							else if (Configuration.Data.BulkCopyUseConnectionCommandTimeout)
								bc.BulkCopyTimeout = connection.ConnectionTimeout;

							bc.DestinationTableName  = tableName;
							bc.DestinationSchemaName = schemaName;

							for (var i = 0; i < columns.Count; i++)
								bc.ColumnMappings.Add(_provider.Adapter.BulkCopy.CreateColumnMapping(i, columns[i].ColumnName));

							TraceAction(
								dataConnection,
								() => "INSERT BULK " + (schemaName == null ? tableName : schemaName + "." + tableName) + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
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
			}
			

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			// call the synchronous provider-specific implementation
			return Task.FromResult(ProviderSpecificCopy(table, options, source));
		}

#if NATIVE_ASYNC
		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(Configuration.ContinueOnCapturedContext))
			{
				// call the synchronous provider-specific implementation
				return ProviderSpecificCopy(table, options, EnumerableHelper.AsyncToSyncEnumerable(enumerator));
			}
		}
#endif

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return OracleTools.UseAlternativeBulkCopy switch
			{
				AlternativeBulkCopy.InsertInto => OracleMultipleRowsCopy2(new MultipleRowsHelper<T>(table, options), source),
				AlternativeBulkCopy.InsertDual => OracleMultipleRowsCopy3(new MultipleRowsHelper<T>(table, options), source),
				_                              => OracleMultipleRowsCopy1(new MultipleRowsHelper<T>(table, options), source),
			};
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			switch (OracleTools.UseAlternativeBulkCopy)
			{
				case AlternativeBulkCopy.InsertInto: return OracleMultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
				case AlternativeBulkCopy.InsertDual: return OracleMultipleRowsCopy3Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
				default                            : return OracleMultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
			}
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			switch (OracleTools.UseAlternativeBulkCopy)
			{
				case AlternativeBulkCopy.InsertInto: return OracleMultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
				case AlternativeBulkCopy.InsertDual: return OracleMultipleRowsCopy3Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
				default                            : return OracleMultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
			}
		}
#endif

		static void OracleMultipleRowsCopy1Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder.AppendLine("INSERT ALL");
			helper.SetHeader();
		}

		static void OracleMultipleRowsCopy1Add(MultipleRowsHelper helper, object item, string? from)
		{
			helper.StringBuilder.AppendFormat("\tINTO {0} (", helper.TableName);

			foreach (var column in helper.Columns)
			{
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(", ");
			}

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.Append(") VALUES (");
			helper.BuildColumns(item!, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
			helper.StringBuilder.AppendLine(")");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		static void OracleMultipleRowsCopy1Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.AppendLine("SELECT * FROM dual");
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish);

		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy1Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, cancellationToken);

#if NATIVE_ASYNC
		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy1Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, cancellationToken);
#endif

		static List<object> OracleMultipleRowsCopy2Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder.AppendFormat("INSERT INTO {0} (", helper.TableName);

			foreach (var column in helper.Columns)
			{
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(", ");
			}

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.Append(") VALUES (");

			for (var i = 0; i < helper.Columns.Length; i++)
				helper.StringBuilder.Append(":p" + (i + 1)).Append(", ");

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.AppendLine(")");
			helper.SetHeader();

			return new List<object>(31);
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy2(MultipleRowsHelper helper, IEnumerable source)
		{
			var list = OracleMultipleRowsCopy2Prep(helper);

			foreach (var item in source)
			{
				list.Add(item!);

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

		static async Task<BulkCopyRowsCopied> OracleMultipleRowsCopy2Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
		{
			var list = OracleMultipleRowsCopy2Prep(helper);

			foreach (var item in source)
			{
				list.Add(item!);

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize)
				{
					if (!await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}

#if NATIVE_ASYNC
		static async Task<BulkCopyRowsCopied> OracleMultipleRowsCopy2Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var list = OracleMultipleRowsCopy2Prep(helper);

			await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
			{
				list.Add(item!);

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize)
				{
					if (!await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}
#endif

		static bool Execute(MultipleRowsHelper helper, List<object> list)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? helper.DataConnection.MappingSchema.GetDataType(column.MemberType).Type.DataType
					: column.DataType;

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), list.Select(o => column.GetValue(o)).ToArray(), dataType, column.DbType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			return helper.Execute();
		}

		static Task<bool> ExecuteAsync(MultipleRowsHelper helper, List<object> list, CancellationToken cancellationToken)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? helper.DataConnection.MappingSchema.GetDataType(column.MemberType).Type.DataType
					: column.DataType;

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), list.Select(o => column.GetValue(o)).ToArray(), dataType, column.DbType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			return helper.ExecuteAsync(cancellationToken);
		}

		static void OracleMultipleRowsCopy3Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append('(');

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append('\t');
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(',');
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.AppendLine(")")
				;

			helper.SetHeader();
		}

		static void OracleMultipleRowsCopy3Add(MultipleRowsHelper helper, object item, string? from)
		{
			helper.StringBuilder
				.AppendLine()
				.Append("\tSELECT ");
			helper.BuildColumns(item!, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
			helper.StringBuilder.Append(" FROM DUAL ");
			helper.StringBuilder.Append(" UNION ALL");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		static void OracleMultipleRowsCopy3Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.Length -= " UNION ALL".Length;
			helper.StringBuilder.AppendLine();
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy3(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish);

		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy3Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, cancellationToken);

#if NATIVE_ASYNC
		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy3Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, cancellationToken);
#endif
	}
}
