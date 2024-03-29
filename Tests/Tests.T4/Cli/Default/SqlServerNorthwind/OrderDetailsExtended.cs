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
	[Table("Order Details Extended", IsView = true)]
	public class OrderDetailsExtended
	{
		[Column("OrderID"                         )] public int      OrderId       { get; set; } // int
		[Column("ProductID"                       )] public int      ProductId     { get; set; } // int
		[Column("ProductName"  , CanBeNull = false)] public string   ProductName   { get; set; } = null!; // nvarchar(40)
		[Column("UnitPrice"                       )] public decimal  UnitPrice     { get; set; } // money
		[Column("Quantity"                        )] public short    Quantity      { get; set; } // smallint
		[Column("Discount"                        )] public float    Discount      { get; set; } // real
		[Column("ExtendedPrice"                   )] public decimal? ExtendedPrice { get; set; } // money
	}
}
