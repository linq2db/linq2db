using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
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

		protected override int MaxParameters => 2048;
		protected override int MaxSqlLength  => 1000000;

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
			if (_provider.Adapter.SupportsAppender
				&& table.TryGetDataConnection(out var dataConnection))
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
			if (_provider.Adapter.SupportsAppender
				&& table.TryGetDataConnection(out var dataConnection))
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
			ITable<T>           table,
			DataOptions         options,
			IEnumerable<T>      source)
			where T : notnull
		{
			var dataConnection = connections.DataConnection;
			var connection     = connections.ProviderConnection;
			var adapter        = _provider.Adapter;
			var sb             = _provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var ed             = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var copyOptions    = options.BulkCopyOptions;
			// DuckDB Appender expects values for ALL table columns in order.
			// If there are skipped columns (identity), fall back — AppendDefault doesn't work with nextval() defaults.
			var columns        = ed.Columns.ToArray();
			var hasSkipped     = columns.Any(c => c.SkipOnInsert && !(copyOptions.KeepIdentity == true && c.IsIdentity));
			if (hasSkipped)
				return null;
			var rc             = new BulkCopyRowsCopied();

			var rawTableName   = copyOptions.TableName  ?? table.TableName;
			var rawSchemaName  = copyOptions.SchemaName ?? table.SchemaName;
			var sqlTableName   = GetTableName(sb, copyOptions, table);

			var appender = adapter.CreateAppender(connection, rawSchemaName, rawTableName);
			try
			{
				foreach (var item in source)
				{
					var row = adapter.CreateAppenderRow(appender);

					for (var i = 0; i < columns.Length; i++)
						AppendValue(adapter, row, columns[i].GetProviderValue(item!));

					adapter.EndRow(row);
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
						() => $"INSERT BULK {sqlTableName}({string.Join(", ", columns.Select(c => c.ColumnName))}){Environment.NewLine}",
						() =>
						{
							adapter.CloseAppender(appender);
							return (int)rc.RowsCopied;
						});
				}
			}
			finally
			{
				appender.Dispose();
			}

			if (copyOptions.NotifyAfter != 0 && copyOptions.RowsCopiedCallback != null)
				copyOptions.RowsCopiedCallback(rc);

			return rc;
		}

		static void AppendValue(DuckDBProviderAdapter adapter, object row, object? value)
		{
			if (value is null or DBNull)
			{
				adapter.AppendNull(row);
				return;
			}

			// DuckDB has no char type; convert to string
			if (value is char ch)
				value = ch.ToString();

			var type = value.GetType();

			// Convert enums to underlying type
			if (type.IsEnum)
			{
				value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), System.Globalization.CultureInfo.InvariantCulture);
				type  = value.GetType();
			}

			if (adapter.TryGetAppendValue(type, out var action))
			{
				action(row, value);
				return;
			}

#if NET6_0_OR_GREATER
			// Fallback: DateOnly → DateTime for DATE columns
			if (value is DateOnly dateOnly)
			{
				if (adapter.TryGetAppendValue(typeof(DateTime), out var dtAction))
				{
					dtAction(row, dateOnly.ToDateTime(default));
					return;
				}
			}
#endif

			throw new LinqToDBException($"DuckDB Appender: unsupported value type '{type}'.");
		}

		#endregion
	}
}
