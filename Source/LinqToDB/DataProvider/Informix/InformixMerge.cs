using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Informix
{
	using Data;

	class InformixMerge : BasicMerge
	{
		protected override bool BuildUsing<T>(DataConnection dataConnection, IEnumerable<T> source)
		{
			return BuildUsing2(dataConnection, source, "FIRST 1 ", "FROM SYSTABLES");
		}
	}
}
