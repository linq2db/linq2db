// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SqlServerNorthwind
{
	[Table("Quarterly Orders", IsView = true)]
	public class QuarterlyOrder
	{
		[Column("CustomerID" )] public string? CustomerId  { get; set; } // nchar(5)
		[Column("CompanyName")] public string? CompanyName { get; set; } // nvarchar(40)
		[Column("City"       )] public string? City        { get; set; } // nvarchar(15)
		[Column("Country"    )] public string? Country     { get; set; } // nvarchar(15)
	}
}
