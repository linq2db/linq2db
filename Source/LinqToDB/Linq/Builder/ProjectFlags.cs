using System;

namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		SQL     = 0x1,
		Column  = 0x2,
		NoError = 0x4
	}
}
