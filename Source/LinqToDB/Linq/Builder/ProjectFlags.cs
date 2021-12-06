using System;

namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		SQL        = 0x1,
		Expression = 0x2,
		Root       = 0x4,
		Test       = 0x8,
	}
}
