using System;

namespace LinqToDB.SqlProvider
{
	// changing this enum incorrectly could break remote context serialization
	// e.g. WCF require flags to be sequential
	// https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/enumeration-types-in-data-contracts
	[Flags]
	public enum RowFeature
	{
		IsNull          = 0b000_0001, // (1, 2) IS NULL
		Equality        = 0b000_0010, // Operators = and <>
		Comparisons     = 0b000_0100, // >, >=, <, <=
		CompareToSelect = 0b000_1000, // Compare to (SELECT 1, 2)
		In              = 0b001_0000, // (1, 2) IN ((1, 2), (3, 4))
		Update          = 0b010_0000, // UPDATE T SET (COL1, COL2) = (SELECT 1, 2)
		UpdateLiteral   = 0b100_0000, // UPDATE T SET (COL1, COL2) = (1, 2)
	}
}
