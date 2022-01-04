using System;

namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		SQL            = 0x1,
		Expression     = 0x2,
		Root           = 0x4,
		AggregtionRoot = 0x10,		
		Keys           = 0x20,
		Test           = 0x40,
	}
}
