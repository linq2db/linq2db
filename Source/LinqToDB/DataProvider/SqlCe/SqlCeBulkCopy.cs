using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;

	sealed class SqlCeBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.BulkCopyOptions.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			var ret = MultipleRowsCopy2(helper, source, "");

			if (options.BulkCopyOptions.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.BulkCopyOptions.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (options.BulkCopyOptions.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}

#if NATIVE_ASYNC
		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.BulkCopyOptions.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (options.BulkCopyOptions.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}
#endif
	}
}
