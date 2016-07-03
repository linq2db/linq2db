using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Oracle
{
	using Data;

	class OracleMerge : BasicMerge
	{
		protected override bool BuildUsing<T>(DataConnection dataConnection, IEnumerable<T> source)
		{
			return BuildUsing2(dataConnection, source, null, "FROM SYS.DUAL");
		}
	}
}
