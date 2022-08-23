using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;
	using SqlProvider;

	class OracleBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// We subtract 1 based on possibility of provider using parameter for command.
		/// </remarks>
		private const      int                _maxParameters = 32766;
		/// <summary>
		/// Setting is conservative, based on https://docs.oracle.com/cd/A58617_01/server.804/a58242/ch5.htm
		/// Max is actually more arbitrary in later versions than Oracle 8.
		/// </summary>
		private const      int                _maxSqlLength  = 65535;
		protected override int                MaxParameters => _maxParameters;
		protected override int                MaxSqlLength  => _maxSqlLength;
		private readonly   OracleDataProvider _provider;

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

			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null
				&& serverName == null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

				if (connection != null)
				{
					var ed        = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T));
					var columns   = ed.Columns.Where(c => !c.SkipOnInsert || options.KeepIdentity == true && c.IsIdentity).ToList();
					var sb        = _provider.CreateSqlBuilder(table.DataContext.MappingSchema);

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
						var sqlopt     = OracleProviderAdapter.BulkCopyOptions.Default;
						var rc         = new BulkCopyRowsCopied();

						var tableName   = sb.ConvertInline(options.TableName ?? table.TableName, ConvertType.NameToQueryTable);
						var schemaName  = options.SchemaName ?? table.SchemaName;
						if (schemaName != null)
							schemaName  = sb.ConvertInline(schemaName, ConvertType.NameToSchema);

						if (options.UseInternalTransaction == true) sqlopt |= OracleProviderAdapter.BulkCopyOptions.UseInternalTransaction;
						if (options.CheckConstraints       == true) sqlopt |= OracleProviderAdapter.BulkCopyOptions.KeepConstraints;
						if (options.FireTriggers           != true) sqlopt |= OracleProviderAdapter.BulkCopyOptions.DisableTriggers;

						var notifyAfter = options.NotifyAfter == 0 && options.MaxBatchSize.HasValue
							? options.MaxBatchSize.Value
							: options.NotifyAfter;

						using (var bc = _provider.Adapter.BulkCopy.Create(
							connection,
							sqlopt,
							tableName,
							schemaName,
							notifyAfter != 0 && options.RowsCopiedCallback != null ? notifyAfter : null,
							options.RowsCopiedCallback,
							rc,
							options.MaxBatchSize,
							options.BulkCopyTimeout ?? (Configuration.Data.BulkCopyUseConnectionCommandTimeout ? connection.ConnectionTimeout : null)))
						{
							for (var i = 0; i < columns.Count; i++)
								bc.AddColumn(i, columns[i]);
								//

							TraceAction(
								dataConnection,
								() => "INSERT BULK " + (schemaName == null ? tableName : schemaName + "." + tableName) + "(" + string.Join(", ", columns.Select(x => x.ColumnName)) + ")" + Environment.NewLine,
								() => { bc.Execute(rd); return rd.Count; });
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
			helper.BuildColumns(item, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
			helper.StringBuilder.AppendLine(")");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		static void OracleMultipleRowsCopy1Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.AppendLine("SELECT * FROM dual");
		}

		static BulkCopyRowsCopied OracleMultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, _maxParameters,_maxSqlLength);

		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy1Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, cancellationToken, _maxParameters,_maxSqlLength);

#if NATIVE_ASYNC
		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy1Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, cancellationToken, _maxParameters,_maxSqlLength);
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

		BulkCopyRowsCopied OracleMultipleRowsCopy2(MultipleRowsHelper helper, IEnumerable source)
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
				Execute(helper, list);

			return helper.RowsCopied;
		}

		async Task<BulkCopyRowsCopied> OracleMultipleRowsCopy2Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
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
		async Task<BulkCopyRowsCopied> OracleMultipleRowsCopy2Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
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

		bool Execute(MultipleRowsHelper helper, List<object> list)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? helper.MappingSchema.GetDataType(column.MemberType).Type.DataType
					: column.DataType;

				var value = new object?[list.Count];
				for (var j = 0; j < value.Length; j++)
					value[j] = column.GetProviderValue(list[j]);

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), value, dataType, column.DbType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			if (_provider.Adapter.ExecuteArray != null)
				return helper.ExecuteCustom((cn, sql, ps) => ExecuteArray(cn, sql, ps, list.Count));

			return helper.Execute();
		}

		int ExecuteArray(DataConnection connection, string sql, DataParameter[] parameters, int iters)
		{
			return new CommandInfo(connection, sql, parameters)
				.ExecuteCustom(cmd => _provider.Adapter.ExecuteArray!(
					_provider.TryGetProviderCommand(connection, cmd)
						?? ThrowHelper.ThrowLinqToDBException<DbCommand>($"AlternativeBulkCopy.InsertInto BulkCopy mode cannot be used with {cmd.GetType()} type. Use OracleTools.UseAlternativeBulkCopy to change mode."),
					iters));
		}

		Task<bool> ExecuteAsync(MultipleRowsHelper helper, List<object> list, CancellationToken cancellationToken)
		{
			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column   = helper.Columns[i];
				var dataType = column.DataType == DataType.Undefined
					? helper.MappingSchema.GetDataType(column.MemberType).Type.DataType
					: column.DataType;

				var value = new object?[list.Count];
				for (var j = 0; j < value.Length; j++)
					value[j] = column.GetProviderValue(list[j]);

				helper.Parameters.Add(new DataParameter(":p" + (i + 1), value, dataType, column.DbType)
				{
					Direction = ParameterDirection.Input,
					IsArray   = true,
				});
			}

			if (_provider.Adapter.ExecuteArray != null)
				return Task.FromResult(helper.ExecuteCustom((cn, sql, ps) => ExecuteArray(cn, sql, ps, list.Count)));

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
			helper.BuildColumns(item, _ => _.DataType == DataType.Text || _.DataType == DataType.NText);
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
			=> MultipleRowsCopyHelper(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, _maxParameters, _maxSqlLength);

		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy3Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, cancellationToken, _maxParameters, _maxSqlLength);

#if NATIVE_ASYNC
		static Task<BulkCopyRowsCopied> OracleMultipleRowsCopy3Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, cancellationToken, _maxParameters, _maxSqlLength);
#endif
	}
}
