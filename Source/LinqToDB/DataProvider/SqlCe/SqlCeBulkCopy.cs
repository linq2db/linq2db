using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;
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

		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = await MultipleRowsCopy2Async(helper, source, "")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}

#if !NET45 && !NET46
		protected override async Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(table, options);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " ON")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			var ret = await MultipleRowsCopy2Async(helper, source, "")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			if (options.KeepIdentity == true)
				await helper.DataConnection.ExecuteAsync("SET IDENTITY_INSERT " + helper.TableName + " OFF")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return ret;
		}
#endif
	}
}
