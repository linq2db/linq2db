// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.Oracle
{
	[Table("GrandChild")]
	public class GrandChild
	{
		[Column("ParentID"    )] public decimal? ParentId     { get; set; } // NUMBER
		[Column("ChildID"     )] public decimal? ChildId      { get; set; } // NUMBER
		[Column("GrandChildID")] public decimal? GrandChildId { get; set; } // NUMBER
	}
}
