using System;

namespace LinqToDB.Linq
{
	[Flags]
	internal enum QueryFlags
	{
		None                 = 0,
		/// <summary>
		/// Bit set, when inline parameters enabled for connection.
		/// </summary>
		InlineParameters     = 0x02,

		/// <summary>
		/// Indicates that query contains expression, which have been expanded
		/// </summary>
		ExpandedQuery = 0x04
	}
}
