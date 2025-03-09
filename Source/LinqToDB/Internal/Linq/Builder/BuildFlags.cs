using System;

namespace LinqToDB.Internal.Linq.Builder
{
	[Flags]
	public enum BuildFlags
	{
		None                = 0,
		ForceParameter      = 1 << 0,
		ForceDefaultIfEmpty = 1 << 1,
		ForSetProjection    = 1 << 2,
		ForKeys             = 1 << 3,
		ForceOuter          = 1 << 4,
		ForExtension        = 1 << 5,
		ForExpanding        = 1 << 6,
		ForMemberRoot       = 1 << 7,
		FormatAsExpression  = 1 << 8,
		// forces clearing flags
		ResetPrevious       = 1 << 9,
	}
}
