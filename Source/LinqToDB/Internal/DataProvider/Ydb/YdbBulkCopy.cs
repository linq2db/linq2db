using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Provider-specific implementation of BulkCopy for YDB.
	/// </summary>
	public class YdbBulkCopy : BasicBulkCopy
	{
		readonly YdbDataProvider _provider;

		public YdbBulkCopy(YdbDataProvider dataProvider)
		{
			_provider = dataProvider;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source)
			=> MultipleRowsCopy1(table, options, source);

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table,
			DataOptions options,
			IEnumerable<T> source,
			CancellationToken cancellationToken)
			=> MultipleRowsCopy1Async(table, options, source, cancellationToken);

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table,
			DataOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken)
			=> MultipleRowsCopy1Async(table, options, source, cancellationToken);

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());

				if (connection != null)
				{
					return SafeAwaiter.Run(() => ProviderSpecificCopyImplAsync(
						dataConnection,
						connection,
						table,
						options,
						columns => new BulkCopyReader<T>(dataConnection, columns, source),
						default));
				}
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, await dataConnection.OpenDbConnectionAsync(cancellationToken).ConfigureAwait(false));

				if (connection != null)
				{
					return await ProviderSpecificCopyImplAsync(
						dataConnection,
						connection,
						table,
						options,
						columns => new BulkCopyReader<T>(dataConnection, columns, source, cancellationToken),
						cancellationToken)
						.ConfigureAwait(false);
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, await dataConnection.OpenDbConnectionAsync(cancellationToken).ConfigureAwait(false));

				if (connection != null)
				{
					return await ProviderSpecificCopyImplAsync(
						dataConnection,
						connection,
						table,
						options,
						columns => new BulkCopyReader<T>(dataConnection, columns, source),
						cancellationToken)
						.ConfigureAwait(false);
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		async Task<BulkCopyRowsCopied> ProviderSpecificCopyImplAsync<T>(
			DataConnection                                  dataConnection,
			DbConnection                                    dbConnection,
			ITable<T>                                       table,
			DataOptions                                     options,
			Func<List<ColumnDescriptor>, BulkCopyReader<T>> createDataReader,
			CancellationToken                               cancellationToken)
			where T : notnull
		{
			var sqlBuilder  = (YdbSqlBuilder)_provider.CreateSqlBuilder(table.DataContext.MappingSchema, dataConnection.Options);
			var ed          = table.DataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
			var columns     = ed.Columns.Where(c => !c.SkipOnInsert || (options.BulkCopyOptions.KeepIdentity == true && c.IsIdentity)).ToList();
			var command     = dataConnection.GetOrCreateCommand();

			// table name shouldn't be escaped
			// TOD: test FQN
			var tableName   = table.TableName;
			var fields      = columns.Select(column => column.ColumnName).ToArray();

			var batchSize   = options.BulkCopyOptions.MaxBatchSize ?? 10_000;

			var rd = createDataReader(columns);
			await using var _ = rd.ConfigureAwait(false);

			// there is no any options available for this API
			var writer = _provider.Adapter.BeginBulkCopy(dbConnection, tableName, fields, cancellationToken);

			var rowsCopied   = new BulkCopyRowsCopied();
			var currentCount = 0;

			// for now we will rely on implementation details and reuse row object
			var row = new object?[columns.Count];
			while (await rd.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				// provider parameters used as they already have properly types values
				// because bulk api doesn't type parameters itself
				rd.GetAsParameters(command.CreateParameter, row);

				await writer.AddRowAsync(row).ConfigureAwait(false);
				currentCount++;
				rowsCopied.RowsCopied++;

				if (options.BulkCopyOptions.NotifyAfter != 0 &&
					options.BulkCopyOptions.RowsCopiedCallback != null &&
					rowsCopied.RowsCopied % options.BulkCopyOptions.NotifyAfter == 0)
				{
					options.BulkCopyOptions.RowsCopiedCallback(rowsCopied);

					if (rowsCopied.Abort)
					{
						break;
					}
				}

				if (currentCount >= batchSize)
				{
					await writer.FlushAsync().ConfigureAwait(false);
					currentCount = 0;
				}
			}

			if (!rowsCopied.Abort)
			{
				await TraceActionAsync(
					dataConnection,
					() => $"INSERT ASYNC BULK {tableName}({string.Join(", ", columns.Select(x => x.ColumnName))}){Environment.NewLine}",
					async () =>
					{
						if (currentCount > 0)
							await writer.FlushAsync().ConfigureAwait(false);

						return (int)rowsCopied.RowsCopied;
					}).ConfigureAwait(false);
			}

			if (currentCount > 0 && options.BulkCopyOptions.NotifyAfter != 0 && options.BulkCopyOptions.RowsCopiedCallback != null)
				options.BulkCopyOptions.RowsCopiedCallback(rowsCopied);

			if (table.DataContext.CloseAfterUse)
				await table.DataContext.CloseAsync().ConfigureAwait(false);

			return rowsCopied;
		}
	}
}
