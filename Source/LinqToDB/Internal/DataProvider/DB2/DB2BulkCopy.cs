using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.DB2;

namespace LinqToDB.Internal.DataProvider.DB2
{
	sealed class DB2BulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Settings based on https://www.ibm.com/docs/en/i/7.3?topic=reference-sql-limits
		/// We subtract 1 here to be safe since some ADO providers use parameter for command itself.
		/// </remarks>
		protected override int             MaxParameters => 1999;
		/// <remarks>
		/// Setting based on https://www.ibm.com/docs/en/i/7.3?topic=reference-sql-limits
		/// Max is actually 2MIB, but we keep a lower number here to avoid the cost of huge statements.
		/// </remarks>
		protected override int             MaxSqlLength  => 327670;
		private readonly   DB2DataProvider _provider;

		public DB2BulkCopy(DB2DataProvider provider)
		{
			_provider = provider;
		}

		protected override BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

				if (connection != null)
					return DB2BulkCopyShared.ProviderSpecificCopyImpl(
						table,
						options.BulkCopyOptions,
						source,
						dataConnection,
						connection,
						_provider.Adapter.BulkCopy,
						TraceAction);
			}

			return MultipleRowsCopy(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);
				if (connection != null)
					// call the synchronous provider-specific implementation
					return Task.FromResult(DB2BulkCopyShared.ProviderSpecificCopyImpl(
						table,
						options.BulkCopyOptions,
						source,
						dataConnection,
						connection,
						_provider.Adapter.BulkCopy,
						TraceAction));
			}

			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		protected override async Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (table.TryGetDataConnection(out var dataConnection))
			{
				var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.Connection);

				if (connection != null)
				{
					var enumerator = source.GetAsyncEnumerator(cancellationToken);
					await using (enumerator.ConfigureAwait(false))
					{
						// call the synchronous provider-specific implementation
						return DB2BulkCopyShared.ProviderSpecificCopyImpl(
							table,
							options.BulkCopyOptions,
							EnumerableHelper.AsyncToSyncEnumerable(enumerator),
							dataConnection,
							connection,
							_provider.Adapter.BulkCopy,
							TraceAction);
					}
				}
			}

			return await MultipleRowsCopyAsync(table, options, source, cancellationToken).ConfigureAwait(false);
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var dataConnection = table.GetDataConnection();

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2(table, options, source, " FROM SYSIBM.SYSDUMMY1");

			return MultipleRowsCopy1(table, options, source);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var dataConnection = table.GetDataConnection();

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2Async(table, options, source, " FROM SYSIBM.SYSDUMMY1", cancellationToken);

			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var dataConnection = table.GetDataConnection();

			if (((DB2DataProvider)dataConnection.DataProvider).Version == DB2Version.zOS)
				return MultipleRowsCopy2Async(table, options, source, " FROM SYSIBM.SYSDUMMY1", cancellationToken);

			return MultipleRowsCopy1Async(table, options, source, cancellationToken);
		}
	}
}
