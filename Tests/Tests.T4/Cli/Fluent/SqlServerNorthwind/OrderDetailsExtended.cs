// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------


#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Fluent.SqlServerNorthwind
{
	public class OrderDetailsExtended
	{
		public int      OrderId       { get; set; } // int
		public int      ProductId     { get; set; } // int
		public string   ProductName   { get; set; } = null!; // nvarchar(40)
		public decimal  UnitPrice     { get; set; } // money
		public short    Quantity      { get; set; } // smallint
		public float    Discount      { get; set; } // real
		public decimal? ExtendedPrice { get; set; } // money
	}
}
