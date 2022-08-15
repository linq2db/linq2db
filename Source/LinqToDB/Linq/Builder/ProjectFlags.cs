using System;

namespace LinqToDB.Linq.Builder
{
	[Flags]
	enum ProjectFlags
	{
		SQL             = 0x01,
		Expression      = 0x02,
		Root            = 0x04,
		/// <summary>
		/// Forces expanding associations and GroupJoin into query expression
		/// </summary>
		Expand          = 0x08,		
		AggregationRoot = 0x10,		
		Keys            = 0x20,
		Test            = 0x40,
		AssociationRoot = 0x80,		
	}
}
