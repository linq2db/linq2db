using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Informix
{
	using Data;

	class InformixBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			using (new InvariantCultureRegion())
				return base.MultipleRowsCopy(table, options, source);
		}
	}
}
