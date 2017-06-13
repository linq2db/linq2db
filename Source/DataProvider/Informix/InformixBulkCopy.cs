using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Informix
{
	using Data;

	class InformixBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return base.MultipleRowsCopy(dataConnection, options, source);
			//return MultipleRowsCopy3(dataConnection, options, source, " FROM table(set{1})");
		}
	}
}
