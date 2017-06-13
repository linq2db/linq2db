using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Informix
{
	using Data;
	using System.Globalization;
	using System.Threading;

	class InformixBulkCopy : BasicBulkCopy
	{
		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			return base.MultipleRowsCopy(dataConnection, options, source);
			//return MultipleRowsCopy3(dataConnection, options, source, " FROM table(set{1})");
		}
	}
}
