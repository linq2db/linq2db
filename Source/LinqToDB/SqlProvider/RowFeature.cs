using System;

namespace LinqToDB.SqlProvider;

// changing this enum incorrectly could break remote context serialization
// e.g. WCF require flags to be sequential
// https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/enumeration-types-in-data-contracts
[Flags]
public enum RowFeature
{
	IsNull          = 0b0_0000_0001, // (1, 2) IS NULL
	Equality        = 0b0_0000_0010, // Operators = and <>
	Comparisons     = 0b0_0000_0100, // >, >=, <, <=
	Overlaps        = 0b0_0000_1000, // OVERLAPS
	Between         = 0b0_0001_0000, // BETWEEN
	CompareToSelect = 0b0_0010_0000, // Compare to (SELECT 1, 2)
	In              = 0b0_0100_0000, // (1, 2) IN ((1, 2), (3, 4))
	Update          = 0b0_1000_0000, // UPDATE T SET (COL1, COL2) = (SELECT 1, 2)
	UpdateLiteral   = 0b1_0000_0000, // UPDATE T SET (COL1, COL2) = (1, 2)
}
