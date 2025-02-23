using System;

namespace LinqToDB.Internal.SqlQuery
{
	[Flags]
	public enum SqlFlags
	{
		None             = 0,
		IsAggregate      = 0x1,
		IsPure           = 0x4,
		IsPredicate      = 0x8,
		IsWindowFunction = 0x10,
	}
}
