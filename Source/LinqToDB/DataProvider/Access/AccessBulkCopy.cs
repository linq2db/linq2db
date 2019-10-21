using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Access
{
	using Data;

	class AccessBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return base.MultipleRowsCopy(table, options, source);
			//return MultipleRowsCopy2(dataConnection, options, source, "");
		}
	}
}
