// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.SQLite
{
	[Table("GrandChild")]
	public class GrandChild
	{
		[Column("ParentID"    , DataType = DataType.Int32, DbType = "int", Length = 4, Precision = 10, Scale = 0)] public int? ParentId     { get; set; } // int
		[Column("ChildID"     , DataType = DataType.Int32, DbType = "int", Length = 4, Precision = 10, Scale = 0)] public int? ChildId      { get; set; } // int
		[Column("GrandChildID", DataType = DataType.Int32, DbType = "int", Length = 4, Precision = 10, Scale = 0)] public int? GrandChildId { get; set; } // int
	}
}
