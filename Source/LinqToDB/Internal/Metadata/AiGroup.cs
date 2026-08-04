using System;

namespace LinqToDB.Internal.Metadata
{
	[Flags]
	enum AiGroup
	{
		None               = 0,
		QueryDirectives    = 1 << 0,
		NavigationLoading  = 1 << 1,
		Hints              = 1 << 2,
		DML                = 1 << 3,
		Merge              = 1 << 4,
		Helpers            = 1 << 5,
		Configuration      = 1 << 6,
		Connection         = 1 << 7,
		RawSQL             = 1 << 8,
		Schema             = 1 << 9,
	}
}
