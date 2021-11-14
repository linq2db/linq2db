using System;

namespace LinqToDB.SqlProvider
{
	[Flags]
	public enum RowFeature
	{
		// (1, 2) IS NULL
		IsNull      = 1,
		// (1, 2) >= (1, 3)
		Comparisons = 2,
		// (1, 2) IN ((1, 2), (3, 4))
		In          = 4,
		// UPDATE T SET (COL1, COL2) = (1, 2)
		Update      = 8,
	}
}
