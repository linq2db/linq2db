using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;

	class SQLiteBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(table, options, source);
		}
	}
}
