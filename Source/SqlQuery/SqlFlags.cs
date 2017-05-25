using System;

namespace LinqToDB.SqlQuery
{
	[Flags]
	public enum SqlFlags
	{
		None      = 0x0,
		Aggregate = 0x1,
	}
}