using System.Collections.Generic;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;

	class SybaseBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy2(dataConnection, options, options.KeepIdentity == true, source, "");
		}
	}
}
