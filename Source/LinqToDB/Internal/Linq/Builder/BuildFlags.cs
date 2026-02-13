using System;

namespace LinqToDB.Internal.Linq.Builder
{
	[Flags]
	enum BuildFlags
	{
		None                = 0,
		ForceParameter      = 1 << 0,
		ForceDefaultIfEmpty = 1 << 1,
		ForSetProjection    = 1 << 2,
		ForKeys             = 1 << 3,
		ForceOuter          = 1 << 4,
		ForExtension        = 1 << 5,
		ForMemberRoot       = 1 << 6,
		FormatAsExpression  = 1 << 7,
		// forces clearing flags
		ResetPrevious       = 1 << 8,
	}
}
