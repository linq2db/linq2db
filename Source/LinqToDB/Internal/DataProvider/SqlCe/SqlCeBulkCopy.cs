using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeBulkCopy : BasicBulkCopy
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

#pragma warning disable CS0618 // Type or member is obsolete
				if (helper.OriginalContext.CloseAfterUse)
					helper.OriginalContext.Close();
#pragma warning restore CS0618 // Type or member is obsolete
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

#pragma warning disable CS0618 // Type or member is obsolete
				if (helper.OriginalContext.CloseAfterUse)
					await helper.OriginalContext.CloseAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
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

#pragma warning disable CS0618 // Type or member is obsolete
				if (helper.OriginalContext.CloseAfterUse)
					await helper.OriginalContext.CloseAsync().ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
			}

			return ret;
		}
	}
}
