using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	public class OracleBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Settings based on https://www.jooq.org/doc/3.12/manual/sql-building/dsl-context/custom-settings/settings-inline-threshold/
		/// We subtract 1 based on possibility of provider using parameter for command.
		/// </remarks>
		protected override int                 MaxParameters => 32766;

		/// <remarks>
		/// Oracle publishes no fixed maximum statement length. "Logical Database Limits" only says the limit
		/// "depends on many factors, including database configuration, disk space, and memory":
		/// https://docs.oracle.com/en/database/oracle/oracle-database/19/refrn/logical-database-limits.html
		/// The previous 65535 came from Oracle 8 documentation, which no longer applies to any supported version.
		/// <para>
		/// 393,216 characters - 384 * 1024, counted in UTF-16 characters of the generated SQL rather than in bytes,
		/// the same unit as <see cref="BulkCopyOptions.MaxSqlLengthForBatch"/> - was chosen from measurement
		/// (2026-08, issue #5825): on Oracle 11 - the oldest supported version - and on Oracle 23, both
		/// <c>INSERT ALL</c> and <c>INSERT ... SELECT FROM DUAL UNION ALL</c> statements of inlined literals parsed
		/// and executed correctly up to 4M characters (8MB of bytes for a multi-byte payload under AL32UTF8). This
		/// value therefore keeps better than 10x headroom over what was verified, while staying in line with the
		/// other providers here and bounding the cost of a hard parse: the default Oracle path inlines
		/// literals, so every batch is a distinct statement that is parsed from scratch.
		/// </para>
		/// Users whose database and driver accept longer statements can raise it with
		/// <see cref="BulkCopyOptions.MaxSqlLengthForBatch"/>.
		/// </remarks>
		protected override int                 MaxSqlLength  => 384 * 1024;

		private readonly   OracleDataProvider  _provider;
		private readonly   AlternativeBulkCopy _useAlternativeBulkCopy;

		public OracleBulkCopy(OracleDataProvider provider, AlternativeBulkCopy useAlternativeBulkCopy)
		{
			_provider               = provider;
			_useAlternativeBulkCopy = useAlternativeBulkCopy;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var opts = options.BulkCopyOptions;

			// database name is not a part of table FQN in oracle
			var serverName = opts.ServerName ?? table.ServerName;

			if (table.TryGetDataConnection(out var dataConnection) && _provider.Adapter.BulkCopy != null && serverName == null)
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());

				if (connection != null)
				{
					var ed        = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
					var columns   = ed.Columns.Where(c => !c.SkipOnInsert || (opts.KeepIdentity == true && c.IsIdentity)).ToList();
					var sb        = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);

					// ODP.NET doesn't bulk copy doesn't work if columns that require escaping:
					// - if escaping applied, pre-flight validation fails as it performs uppercase comparison and quotes make it fail with
					//   InvalidOperationException: Column mapping is invalid
					// - if escaping not applied - if fails as expected on server, because it treats passed name as uppercased name
					//   and gives "ORA-00904: "STRINGVALUE": invalid identifier" error
					// That's quite common error in bulk copy implementation error by providers...
					var supported = true;

					foreach (var column in columns)
						if (!string.Equals(column.ColumnName, sb.ConvertInline(column.ColumnName, ConvertType.NameToQueryField), StringComparison.Ordinal))
						{
							// fallback to sql-based copy
							// TODO: we should add support for by-ordinal column mapping to workaround it
							supported = false;
							break;
						}

					if (supported)
					{
						using var rd   = new BulkCopyReader<T>(dataConnection, columns, source);
						var sqlopt     = OracleProviderAdapter.BulkCopyOptions.Default;
						var rc         = new BulkCopyRowsCopied();

						var tableName  = sb.ConvertInline(opts.TableName ?? table.TableName, ConvertType.NameToQueryTable);
						var schemaName = opts.SchemaName ?? table.SchemaName;

						if (schemaName != null)
							schemaName  = sb.ConvertInline(schemaName, ConvertType.NameToSchema);

						if (opts.UseInternalTransaction == true) sqlopt |= OracleProviderAdapter.BulkCopyOptions.UseInternalTransaction;
						if (opts.CheckConstraints       == true) sqlopt |= OracleProviderAdapter.BulkCopyOptions.KeepConstraints;
						if (opts.FireTriggers           != true) sqlopt |= OracleProviderAdapter.BulkCopyOptions.DisableTriggers;

						var notifyAfter = opts.NotifyAfter == 0 && opts.MaxBatchSize.HasValue
							? opts.MaxBatchSize.Value
							: opts.NotifyAfter;

						using (var bc = _provider.Adapter.BulkCopy.Create(
							connection,
							sqlopt,
							tableName,
							schemaName,
							notifyAfter != 0 && opts.RowsCopiedCallback != null ? notifyAfter : null,
							opts.RowsCopiedCallback,
							rc,
							opts.MaxBatchSize,
							opts.BulkCopyTimeout ?? (LinqToDB.Common.Configuration.Data.BulkCopyUseConnectionCommandTimeout ? dataConnection.CommandTimeout : null)))
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

							if (opts.NotifyAfter != 0 && opts.RowsCopiedCallback != null)
								opts.RowsCopiedCallback(rc);
						}

						CloseConnectionIfNecessary(table.DataContext);

						return rc;
					}
				}
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			// call the synchronous provider-specific implementation
			return Task.FromResult(ProviderSpecificCopy(table, options, source));
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			await using (enumerator.ConfigureAwait(false))
			{
				// call the synchronous provider-specific implementation
				return ProviderSpecificCopy(table, options, EnumerableHelper.AsyncToSyncEnumerable(enumerator));
			}
		}

		// INSERT ALL is a multitable insert, and Oracle evaluates a sequence only once per row returned by the
		// driving query. The driving query here is SELECT * FROM dual, which returns a single row, so every INTO
		// branch would receive the same value from the identity generator. Oracle 11 was unaffected because its
		// identity is emulated by a per-row BEFORE INSERT trigger, but from Oracle 12 the column is a native
		// identity, so fall back to the single-table INSERT ... SELECT ... UNION ALL form whenever the server
		// is the one generating the value. A SequenceName on the identity member keeps the sequence and trigger,
		// hence keeps INSERT ALL usable.
		AlternativeBulkCopy GetAlternativeBulkCopy<T>(ITable<T> table, DataOptions options)
			where T : notnull
		{
			if (_useAlternativeBulkCopy == AlternativeBulkCopy.InsertAll
				&& _provider.Version >= OracleVersion.v12
				&& options.BulkCopyOptions.KeepIdentity != true
				&& table.DataContext.MappingSchema
					.GetEntityDescriptor(typeof(T), options.ConnectionOptions.OnEntityDescriptorCreated)
					.Columns.Any(static c => c.IsIdentity && c.SequenceName == null))
			{
				return AlternativeBulkCopy.InsertDual;
			}

			return _useAlternativeBulkCopy;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			return GetAlternativeBulkCopy(table, options) switch
			{
				AlternativeBulkCopy.InsertInto => OracleMultipleRowsCopy2(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source),
				AlternativeBulkCopy.InsertDual => OracleMultipleRowsCopy3(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source),
				_                              => OracleMultipleRowsCopy1(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source),
			};
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return GetAlternativeBulkCopy(table, options) switch
			{
				AlternativeBulkCopy.InsertInto => OracleMultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source, cancellationToken),
				AlternativeBulkCopy.InsertDual => OracleMultipleRowsCopy3Async(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source, cancellationToken),
				_                              => OracleMultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source, cancellationToken),
			};
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return GetAlternativeBulkCopy(table, options) switch
			{
				AlternativeBulkCopy.InsertInto => OracleMultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source, cancellationToken),
				AlternativeBulkCopy.InsertDual => OracleMultipleRowsCopy3Async(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source, cancellationToken),
				_                              => OracleMultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options, MultipleRowsConvertToParameter), source, cancellationToken),
			};
		}

		static void OracleMultipleRowsCopy1Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder.AppendLine("INSERT ALL");
			helper.SetHeader();
		}

		protected override Func<DataOptions, DbDataType, object?, bool>? MultipleRowsConvertToParameter => _convertToParameter;

		private static readonly Func<DataOptions, DbDataType, object?, bool> _convertToParameter =
			static (o, t, v) => v != null
				&& (o.BulkCopyOptions.UseParameters
					|| t.DataType is DataType.Text or DataType.NText or DataType.Binary or DataType.VarBinary);

		static void OracleMultipleRowsCopy1Add(MultipleRowsHelper helper, object item, string? from)
		{
			helper.StringBuilder.Append(CultureInfo.InvariantCulture, $"\tINTO {helper.TableName} (");

			foreach (var column in helper.Columns)
			{
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(", ");
			}

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.Append(") VALUES (");
			helper.BuildColumns(item);
			helper.StringBuilder.AppendLine(")");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		static void OracleMultipleRowsCopy1Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.AppendLine("SELECT * FROM dual");
		}

		BulkCopyRowsCopied OracleMultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, MaxParameters, MaxSqlLength);

		Task<BulkCopyRowsCopied> OracleMultipleRowsCopy1Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, cancellationToken, MaxParameters, MaxSqlLength);

		Task<BulkCopyRowsCopied> OracleMultipleRowsCopy1Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy1Prep, OracleMultipleRowsCopy1Add, OracleMultipleRowsCopy1Finish, cancellationToken, MaxParameters, MaxSqlLength);

		static List<object> OracleMultipleRowsCopy2Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder.Append(CultureInfo.InvariantCulture, $"INSERT INTO {helper.TableName} (");

			foreach (var column in helper.Columns)
			{
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(", ");
			}

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.Append(") VALUES (");

			for (var i = 0; i < helper.Columns.Length; i++)
				helper.StringBuilder.Append(CultureInfo.InvariantCulture, $":p{i + 1}, ");

			helper.StringBuilder.Length -= 2;

			helper.StringBuilder.AppendLine(")");
			helper.SetHeader();

			return new List<object>(helper.BatchSize);
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
					{
						if (!helper.SuppressCloseAfterUse)
							CloseConnectionIfNecessary(helper.OriginalContext);

						return helper.RowsCopied;
					}

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
				Execute(helper, list);

			if (!helper.SuppressCloseAfterUse)
				CloseConnectionIfNecessary(helper.OriginalContext);

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
					if (!await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(false))
					{
						if (!helper.SuppressCloseAfterUse)
							await CloseConnectionIfNecessaryAsync(helper.OriginalContext).ConfigureAwait(false);

						return helper.RowsCopied;
					}

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(false);
			}

			if (!helper.SuppressCloseAfterUse)
				await CloseConnectionIfNecessaryAsync(helper.OriginalContext).ConfigureAwait(false);

			return helper.RowsCopied;
		}

		async Task<BulkCopyRowsCopied> OracleMultipleRowsCopy2Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var list = OracleMultipleRowsCopy2Prep(helper);

			await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				list.Add(item!);

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize)
				{
					if (!await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(false))
					{
						if (!helper.SuppressCloseAfterUse)
							await CloseConnectionIfNecessaryAsync(helper.OriginalContext).ConfigureAwait(false);

						return helper.RowsCopied;
					}

					list.Clear();
				}
			}

			if (helper.CurrentCount > 0)
			{
				await ExecuteAsync(helper, list, cancellationToken).ConfigureAwait(false);
			}

			if (!helper.SuppressCloseAfterUse)
				await CloseConnectionIfNecessaryAsync(helper.OriginalContext).ConfigureAwait(false);

			return helper.RowsCopied;
		}

		bool Execute(MultipleRowsHelper helper, List<object> list)
		{
			var valueConverter = new BulkCopyReader.Parameter();

			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column     = helper.Columns[i];
				var columnType = column.GetConvertedDbDataType();

				var value = new object?[list.Count];
				for (var j = 0; j < value.Length; j++)
				{
					helper.DataConnection.DataProvider.SetParameter(helper.DataConnection, valueConverter, string.Empty, columnType, column.GetProviderValue(list[j]));
					value[j] = valueConverter.Value;
				}

				helper.Parameters.Add(new DataParameter(string.Create(CultureInfo.InvariantCulture, $":p{i + 1}"), value, columnType.DataType, columnType.DbType)
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
						?? throw new LinqToDBException($"AlternativeBulkCopy.InsertInto BulkCopy mode cannot be used with {cmd.GetType()} type. Use OracleTools.UseAlternativeBulkCopy to change mode."),
					iters));
		}

		Task<bool> ExecuteAsync(MultipleRowsHelper helper, List<object> list, CancellationToken cancellationToken)
		{
			var valueConverter = new BulkCopyReader.Parameter();

			for (var i = 0; i < helper.Columns.Length; i++)
			{
				var column     = helper.Columns[i];
				var columnType = column.GetConvertedDbDataType();

				var value = new object?[list.Count];
				for (var j = 0; j < value.Length; j++)
				{
					helper.DataConnection.DataProvider.SetParameter(helper.DataConnection, valueConverter, string.Empty, columnType, column.GetProviderValue(list[j]));
					value[j] = valueConverter.Value;
				}

				helper.Parameters.Add(new DataParameter(string.Create(CultureInfo.InvariantCulture, $":p{i + 1}"), value, columnType.DataType, columnType.DbType)
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
				.AppendLine(CultureInfo.InvariantCulture, $"INSERT INTO {helper.TableName}")
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
			helper.BuildColumns(item);
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

		BulkCopyRowsCopied OracleMultipleRowsCopy3(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, MaxParameters, MaxSqlLength);

		Task<BulkCopyRowsCopied> OracleMultipleRowsCopy3Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, cancellationToken, MaxParameters, MaxSqlLength);

		Task<BulkCopyRowsCopied> OracleMultipleRowsCopy3Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, OracleMultipleRowsCopy3Prep, OracleMultipleRowsCopy3Add, OracleMultipleRowsCopy3Finish, cancellationToken, MaxParameters, MaxSqlLength);
	}
}
