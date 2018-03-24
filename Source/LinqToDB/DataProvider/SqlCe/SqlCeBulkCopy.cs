using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;

	class SqlCeBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			var helper = new MultipleRowsHelper<T>(dataConnection, options, options.KeepIdentity == true);

			if (options.KeepIdentity == true)
				dataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " ON");

			var ret = MultipleRowsCopy2(helper, dataConnection, options, source, "");

			if (options.KeepIdentity == true)
				dataConnection.Execute("SET IDENTITY_INSERT " + helper.TableName + " OFF");

			return ret;
		}
	}
}
