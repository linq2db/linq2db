using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;

	class FirebirdBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy2(dataConnection, options, false, source, " FROM rdb$database");
		}
	}
}
