using System;

namespace LinqToDB.Internal.SqlProvider
{
	// changing this enum incorrectly could break remote context serialization
	// e.g. WCF require flags to be sequential
	// https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/enumeration-types-in-data-contracts
	/// <summary>
	/// ROW constructor (tuple) feature support flags.
	/// </summary>
	[Flags]
	public enum RowFeature
	{
		None            = 0,
		/// <summary>
		/// Provider supports for IS NULL operator: <c>(1, 2) IS NULL</c>.
		/// </summary>
		IsNull          = 0b0_0000_0001,
		/// <summary>
		/// Provider supports equality (=, &lt;&gt;) operators with tuples.
		/// </summary>
		Equality        = 0b0_0000_0010,
		/// <summary>
		/// Provider supports comparison operators for tuples: &gt;, &gt;=, &lt;&lt;=.
		/// </summary>
		Comparisons     = 0b0_0000_0100,
		/// <summary>
		/// Provider supports OVERLAPS operator.
		/// </summary>
		Overlaps        = 0b0_0000_1000,
		/// <summary>
		/// Provider supports BETWEEN operator for tuples.
		/// </summary>
		Between         = 0b0_0001_0000,
		/// <summary>
		/// Provider supports subqueries in tuple constructor: <c>(SELECT 1, 2)</c>.
		/// </summary>
		CompareToSelect = 0b0_0010_0000, // Compare to (SELECT 1, 2)
		/// <summary>
		/// Provider supports tuples with IN operator: <c>(1, 2) IN ((1, 2), (3, 4))</c>.
		/// </summary>
		In              = 0b0_0100_0000,
		/// <summary>
		/// Provider supports tuples in SET clause with non-literal rvalue: <c>UPDATE T SET (COL1, COL2) = (SELECT 1, 2)</c>.
		/// </summary>
		Update          = 0b0_1000_0000,
		/// <summary>
		/// Provider supports tuples in SET clause with rvalue literal: <c>UPDATE T SET (COL1, COL2) = (1, 2)</c>.
		/// </summary>
		UpdateLiteral   = 0b1_0000_0000,
	}
}
