using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	sealed class SqlCeBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(table, options);
			helper.SuppressCloseAfterUse = options.BulkCopyOptions.KeepIdentity == true;

			if (options.BulkCopyOptions.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			var ret = MultipleRowsCopy2(helper, source, "");

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

				if (helper.OriginalContext.CloseAfterUse)
					helper.OriginalContext.Close();
			}

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = new MultipleRowsHelper<T>(table, options);
			helper.SuppressCloseAfterUse = options.BulkCopyOptions.KeepIdentity == true;

			if (options.BulkCopyOptions.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(false);

			var ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
					.ConfigureAwait(false);

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(false);

				if (helper.OriginalContext.CloseAfterUse)
					await helper.OriginalContext.CloseAsync().ConfigureAwait(false);
			}

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = new MultipleRowsHelper<T>(table, options);
			helper.SuppressCloseAfterUse = options.BulkCopyOptions.KeepIdentity == true;

			if (options.BulkCopyOptions.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(false);

			var ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
					.ConfigureAwait(false);

			if (options.BulkCopyOptions.KeepIdentity == true)
			{
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(false);

				if (helper.OriginalContext.CloseAfterUse)
					await helper.OriginalContext.CloseAsync().ConfigureAwait(false);
			}

			return ret;
		}
	}
}
