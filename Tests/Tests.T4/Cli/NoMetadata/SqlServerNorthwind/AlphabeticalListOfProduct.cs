// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------


#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.NoMetadata.SqlServerNorthwind
{
	public class AlphabeticalListOfProduct
	{
		public int      ProductId       { get; set; } // int
		public string   ProductName     { get; set; } = null!; // nvarchar(40)
		public int?     SupplierId      { get; set; } // int
		public int?     CategoryId      { get; set; } // int
		public string?  QuantityPerUnit { get; set; } // nvarchar(20)
		public decimal? UnitPrice       { get; set; } // money
		public short?   UnitsInStock    { get; set; } // smallint
		public short?   UnitsOnOrder    { get; set; } // smallint
		public short?   ReorderLevel    { get; set; } // smallint
		public bool     Discontinued    { get; set; } // bit
		public string   CategoryName    { get; set; } = null!; // nvarchar(15)
	}
}
