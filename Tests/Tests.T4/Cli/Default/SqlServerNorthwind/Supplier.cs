// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SqlServerNorthwind
{
	[Table("Suppliers")]
	public class Supplier
	{
		[Column("SupplierID"  , IsPrimaryKey = true , IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int     SupplierId   { get; set; } // int
		[Column("CompanyName" , CanBeNull    = false                                                             )] public string  CompanyName  { get; set; } = null!; // nvarchar(40)
		[Column("ContactName"                                                                                    )] public string? ContactName  { get; set; } // nvarchar(30)
		[Column("ContactTitle"                                                                                   )] public string? ContactTitle { get; set; } // nvarchar(30)
		[Column("Address"                                                                                        )] public string? Address      { get; set; } // nvarchar(60)
		[Column("City"                                                                                           )] public string? City         { get; set; } // nvarchar(15)
		[Column("Region"                                                                                         )] public string? Region       { get; set; } // nvarchar(15)
		[Column("PostalCode"                                                                                     )] public string? PostalCode   { get; set; } // nvarchar(10)
		[Column("Country"                                                                                        )] public string? Country      { get; set; } // nvarchar(15)
		[Column("Phone"                                                                                          )] public string? Phone        { get; set; } // nvarchar(24)
		[Column("Fax"                                                                                            )] public string? Fax          { get; set; } // nvarchar(24)
		[Column("HomePage"                                                                                       )] public string? HomePage     { get; set; } // ntext

		#region Associations
		/// <summary>
		/// FK_Products_Suppliers backreference
		/// </summary>
		[Association(ThisKey = nameof(SupplierId), OtherKey = nameof(Product.SupplierId))]
		public IEnumerable<Product> Products { get; set; } = null!;
		#endregion
	}
}
