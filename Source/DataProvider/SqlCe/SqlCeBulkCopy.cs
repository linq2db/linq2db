using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlCe
{
	using Data;

	class SqlCeBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy2(dataConnection, options, source, "");
		}
	}
}
