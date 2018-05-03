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
			// firebird doesn't have built-in identity management, it must be implemented by used using generators and triggers
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2(dataConnection, options, source, " FROM rdb$database");
		}
	}
}
