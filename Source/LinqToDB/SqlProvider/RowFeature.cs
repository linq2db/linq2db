using System;

namespace LinqToDB.SqlProvider
{
	[Flags]
	public enum RowFeature
	{		
		IsNull        = 1,  // (1, 2) IS NULL		
		Comparisons   = 2,  // (1, 2) >= (1, 3)		
		In            = 4,  // (1, 2) IN ((1, 2), (3, 4))		
		Update        = 8,  // UPDATE T SET (COL1, COL2) = (SELECT 1, 2)
		UpdateLiteral = 16, // UPDATE T SET (COL1, COL2) = (1, 2)
	}
}
