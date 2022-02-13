using System;

namespace LinqToDB.SqlProvider
{
	[Flags]
	public enum RowFeature
	{		
		IsNull        = 0b00_0001,  // (1, 2) IS NULL		
		Equality      = 0b00_0010,  // Operators = and <>
		Comparisons   = 0b00_0110,  // Equality as well as >, >=, <, <=
		In            = 0b00_1000,  // (1, 2) IN ((1, 2), (3, 4))		
		Update        = 0b01_0000, // UPDATE T SET (COL1, COL2) = (SELECT 1, 2)
		UpdateLiteral = 0b10_0000, // UPDATE T SET (COL1, COL2) = (1, 2)
	}
}
