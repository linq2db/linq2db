using System.Collections.Generic;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	class SQLiteBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(dataConnection, options, options.KeepIdentity == true, source);
		}
	}
}
