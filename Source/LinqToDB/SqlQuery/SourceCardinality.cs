using System;

namespace LinqToDB.SqlQuery
{
	[Flags]
	public enum SourceCardinality
	{
		Unknown = 0,

		Zero = 0x1,
		One  = 0x2,
		Many = 0x4,

		ZeroOrOne  = Zero | One,
		ZeroOrMany = Zero | Many,
		OneOrMany  = One | Many,

	}

}
