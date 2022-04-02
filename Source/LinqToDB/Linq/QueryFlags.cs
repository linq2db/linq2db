using System;

namespace LinqToDB.Linq
{
	[Flags]
	internal enum QueryFlags
	{
		None                = 0,
		/// <summary>
		/// Bit set, when group by guard set for connection.
		/// </summary>
		GroupByGuard        = 0x1,
		/// <summary>
		/// Bit set, when inline parameters enabled for connection.
		/// </summary>
		InlineParameters    = 0x2,
		/// <summary>
		/// Bit set, when inline Take/Skip parameterization is enabled for query.
		/// </summary>
		ParameterizeTakeSkip = 0x4,
		/// <summary>
		/// Bit set, when PreferApply is enabled for query.
		/// </summary>
		PreferApply = 0x8,
	}
}
