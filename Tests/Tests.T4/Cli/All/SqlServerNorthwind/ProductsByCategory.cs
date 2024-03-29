// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;
using System.Data.SqlTypes;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.SqlServerNorthwind
{
	[Table("Products by Category", IsView = true)]
	public class ProductsByCategory
	{
		[Column("CategoryName"   , DataType = DataType.NVarChar, DbType = "nvarchar(15)", Length = 15)] public SqlString  CategoryName    { get; set; } // nvarchar(15)
		[Column("ProductName"    , DataType = DataType.NVarChar, DbType = "nvarchar(40)", Length = 40)] public SqlString  ProductName     { get; set; } // nvarchar(40)
		[Column("QuantityPerUnit", DataType = DataType.NVarChar, DbType = "nvarchar(20)", Length = 20)] public SqlString? QuantityPerUnit { get; set; } // nvarchar(20)
		[Column("UnitsInStock"   , DataType = DataType.Int16   , DbType = "smallint"                 )] public SqlInt16?  UnitsInStock    { get; set; } // smallint
		[Column("Discontinued"   , DataType = DataType.Boolean , DbType = "bit"                      )] public SqlBoolean Discontinued    { get; set; } // bit
	}
}
