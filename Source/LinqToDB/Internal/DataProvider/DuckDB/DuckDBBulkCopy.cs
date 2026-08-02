using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBBulkCopy : BasicBulkCopy
	{
		readonly DuckDBDataProvider _provider;

		public DuckDBBulkCopy(DuckDBDataProvider provider)
		{
			_provider = provider;
		}

		// there is no limits on parameters and 4Gb on SQL, just use sane defaults here
		protected override int MaxParameters => 2048;
		protected override int MaxSqlLength  => 1000000;

		protected override string? GetMultipleRowsSuffix(BulkCopyOptions options)
		{
			return options.ConflictAction switch
			{
				ConflictAction.Ignore => $"{Environment.NewLine}ON CONFLICT DO NOTHING",
				_                     => null,
			};
		}

		protected override Func<DataOptions, DbDataType, object?, bool>? MultipleRowsConvertToParameter => _convertToParameter;

		// sync with SqlOptimizer when edit
		private static readonly Func<DataOptions, DbDataType, object?, bool> _convertToParameter =
			static (o, t, v) =>
			{
				if (v is null)
					return false;

				if (!o.BulkCopyOptions.UseParameters)
					return false;

				if (t.DataType is DataType.BitArray)
					return false;

				if (t is { DataType: DataType.Time, Precision: > 6 })
					return false;

				if (v.GetType().UnwrapNullableType() == DuckDBProviderAdapter.Instance.DuckDBInterval)
					return false;

				return true;
			};

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

		#region ProviderSpecific (Appender)

		ProviderConnections? TryGetProviderConnections<T>(ITable<T> table)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());
				if (connection != null)
					return new ProviderConnections { DataConnection = dataConnection, ProviderConnection = connection };
			}

			return null;
		}

		async ValueTask<ProviderConnections?> TryGetProviderConnectionsAsync<T>(ITable<T> table, CancellationToken cancellationToken)
			where T : notnull
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection,
					await dataConnection.OpenDbConnectionAsync(cancellationToken).ConfigureAwait(false));
				if (connection != null)
					return new ProviderConnections { DataConnection = dataConnection, ProviderConnection = connection };
			}

			return null;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var connections = TryGetProviderConnections(table);
			if (connections.HasValue)
			{
				var result = ProviderSpecificCopyImpl(connections.Value, table, options, source);
				if (result != null)
				{
					CloseConnectionIfNecessary(table.DataContext);
					return result;
				}
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await TryGetProviderConnectionsAsync(table, cancellationToken).ConfigureAwait(false);
			if (connections.HasValue)
			{
				var result = ProviderSpecificCopyImpl(connections.Value, table, options, source);
				if (result != null)
				{
					await CloseConnectionIfNecessaryAsync(table.DataContext).ConfigureAwait(false);
					return result;
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var connections = await TryGetProviderConnectionsAsync(table, cancellationToken).ConfigureAwait(false);
			if (connections.HasValue)
			{
				// DuckDB Appender is synchronous; convert IAsyncEnumerable to IEnumerable
				var result = ProviderSpecificCopyImpl(
					connections.Value, table, options,
					EnumerableHelper.AsyncToSyncEnumerable(source.GetAsyncEnumerator(cancellationToken)));
				if (result != null)
				{
					await CloseConnectionIfNecessaryAsync(table.DataContext).ConfigureAwait(false);
					return result;
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Single implementation for both sync and async paths.
		/// DuckDB Appender is always synchronous (in-process database).
		/// Caller is responsible for CloseConnectionIfNecessary.
		/// </summary>
		BulkCopyRowsCopied? ProviderSpecificCopyImpl<T>(
			ProviderConnections connections,
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source)
			where T : notnull
		{
			var dataConnection  = connections.DataConnection;
			var ed              = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var copyOptions     = options.BulkCopyOptions;
			var rawDatabaseName = copyOptions.DatabaseName ?? table.DatabaseName;
			var rawTableName    = copyOptions.TableName    ?? table.TableName;
			var rawSchemaName   = copyOptions.SchemaName   ?? table.SchemaName;

			// DuckDB Appender requires values for ALL table columns in order.
			// Build a mapping from table columns to entity columns; unmapped columns get AppendDefault.
			var entityColumns  = ed.Columns.ToArray();
			var columnTypes    = ed.Columns.Select(c => c.GetDbDataType(true)).ToArray();

			string[]? tableColumns;
			var closeAfterUse = ((IDataContext)dataConnection).CloseAfterUse;
			try
			{
				((IDataContext)dataConnection).CloseAfterUse = false;
				tableColumns = DuckDBSchemaProvider.GetTableColumns(dataConnection, rawDatabaseName, rawSchemaName, rawTableName);
			}
			finally
			{
				((IDataContext)dataConnection).CloseAfterUse = closeAfterUse;
			}

			if (tableColumns == null)
				return null;

			// Build per-table-column mapping: index into entityColumns or -1 for default
			var columnMap = new int[tableColumns.Length];
			var entityColumnsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (var i = 0; i < entityColumns.Length; i++)
			{
				var col = entityColumns[i];
				if (col.SkipOnInsert && !(copyOptions.KeepIdentity == true && col.IsIdentity))
					continue;
				entityColumnsByName[col.ColumnName] = i;
			}

			var hasUnmappedColumns = false;
			for (var i = 0; i < tableColumns.Length; i++)
			{
				if (entityColumnsByName.TryGetValue(tableColumns[i], out var idx))
					columnMap[i] = idx;
				else
				{
					columnMap[i] = -1;
					hasUnmappedColumns = true;
				}
			}

			// DuckDB AppendDefault doesn't work with nextval() defaults (identity columns).
			// If there are unmapped columns, fall back to MultipleRows which uses INSERT with explicit column list.
			if (hasUnmappedColumns)
				return null;

			var adapter        = _provider.Adapter;
			var rc             = new BulkCopyRowsCopied();
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var sqlTableName   = GetTableName(sb, copyOptions, table);
			var connection     = connections.ProviderConnection;

			using var appender = adapter.CreateAppender(connection, rawDatabaseName, rawSchemaName, rawTableName);
			foreach (var item in source)
			{
				var row = appender.CreateRow();

				for (var i = 0; i < columnMap.Length; i++)
				{
					if (columnMap[i] >= 0)
						Append(adapter, row, ref columnTypes[columnMap[i]], entityColumns[columnMap[i]].GetProviderValue(item!));
					else
						row.AppendDefault();
				}

				row.EndRow();
				rc.RowsCopied++;

				if (copyOptions.NotifyAfter != 0 && copyOptions.RowsCopiedCallback != null
					&& rc.RowsCopied % copyOptions.NotifyAfter == 0)
				{
					copyOptions.RowsCopiedCallback(rc);
					if (rc.Abort)
						break;
				}
			}

			if (!rc.Abort)
			{
				TraceAction(
					dataConnection,
					() => $"INSERT BULK {sqlTableName}({string.Join(", ", entityColumns.Where((_, i) => entityColumnsByName.ContainsValue(i)).Select(c => c.ColumnName))}){Environment.NewLine}",
					() =>
					{
						return (int)rc.RowsCopied;
					});
			}

			if (copyOptions.NotifyAfter != 0 && copyOptions.RowsCopiedCallback != null)
				copyOptions.RowsCopiedCallback(rc);

			return rc;
		}

		void Append(
			DuckDBProviderAdapter adapter,
			DuckDBProviderAdapter.Wrappers.IDuckDBAppenderRow row,
			ref DbDataType type,
			object? value)
		{
			if (value.IsNullValue)
			{
				row.AppendNullValue();
				return;
			}

			if (value is TimeSpan ts)
			{
				if (type.DataType == DataType.TimeTZ)
				{
					value = DateTimeOffset.MinValue + ts;
				}
				else if (type.DataType == DataType.Int64)
				{
					value = ts.Ticks;
				}
#if NET8_0_OR_GREATER
				else if (type.DataType == DataType.Time)
				{
					value = TimeOnly.FromTimeSpan(ts);
				}
#endif
			}
			else if (value is DateTimeOffset dto && type.DataType == DataType.DateTime)
			{
				value = dto.DateTime;
			}
			else if (value is Binary b)
			{
				value = b.ToArray();
			}

			switch (value)
			{
				case bool           boolVal          : row.AppendValue(boolVal          ); return;
				case byte[]         byteArrVal       : row.AppendValue(byteArrVal       ); return;
				case string         strVal           : row.AppendValue(strVal           ); return;
				case char           chrVal           : row.AppendValue(chrVal.ToString()); return;
				case decimal        decVal           : row.AppendValue(decVal           ); return;
				case Guid           guidVal          : row.AppendValue(guidVal          ); return;
				case BigInteger     bigIntVal        : row.AppendValue(bigIntVal        ); return;
				case sbyte          sbyteVal         : row.AppendValue(sbyteVal         ); return;
				case short          shortVal         : row.AppendValue(shortVal         ); return;
				case int            intVal           : row.AppendValue(intVal           ); return;
				case long           longVal          : row.AppendValue(longVal          ); return;
				case byte           byteVal          : row.AppendValue(byteVal          ); return;
				case ushort         ushortVal        : row.AppendValue(ushortVal        ); return;
				case uint           uintVal          : row.AppendValue(uintVal          ); return;
				case ulong          ulongVal         : row.AppendValue(ulongVal         ); return;
				case float          floatVal         : row.AppendValue(floatVal         ); return;
				case double         doubleVal        : row.AppendValue(doubleVal        ); return;
				case DateTime       dateTimeVal      : row.AppendValue(dateTimeVal      ); return;
				case DateTimeOffset dateTimeOffsetVal: row.AppendValue(dateTimeOffsetVal); return;
				case TimeSpan       timeSpanValue    : row.AppendValue(timeSpanValue    ); return;
#if NET8_0_OR_GREATER
				case TimeOnly       timeOnlyValue    : row.AppendValue(timeOnlyValue    ); return;
				case DateOnly       dateOnlyValue    : row.AppendValue(dateOnlyValue    ); return;
#endif
			}

			var valueType = value.GetType();

			if (valueType == adapter.DuckDBDateOnly)
				row.AppendDuckDBDateOnly(value);
			else if (valueType == adapter.DuckDBTimeOnly)
				row.AppendDuckDBTimeOnly(value);
			else if (valueType == adapter.DuckDBInterval)
				row.AppendDuckDBInterval(value);
			else if (valueType == adapter.DuckDBTimestamp)
				row.AppendDuckDBTimestamp(value);
			else
				throw new LinqToDBException($"DuckDB Appender: unsupported value type '{valueType}'.");
		}

		#endregion
	}
}
