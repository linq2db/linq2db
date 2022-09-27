using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;
	using System.Threading;
	using System.Threading.Tasks;

	class SqlCeBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			var ret = MultipleRowsCopy2(helper, source, "");

			if (options.KeepIdentity == true)
				helper.DataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

			return ret;
		}

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}

#if NATIVE_ASYNC
		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = await MultipleRowsCopy2Async(helper, source, "", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}
#endif
	}
}
