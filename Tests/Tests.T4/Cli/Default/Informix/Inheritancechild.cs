// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.Informix
{
	[Table("inheritancechild")]
	public class Inheritancechild
	{
		[Column("inheritancechildid" , IsPrimaryKey = true)] public int     Inheritancechildid  { get; set; } // INTEGER
		[Column("inheritanceparentid"                     )] public int     Inheritanceparentid { get; set; } // INTEGER
		[Column("typediscriminator"                       )] public int?    Typediscriminator   { get; set; } // INTEGER
		[Column("name"                                    )] public string? Name                { get; set; } // NVARCHAR(50)
	}
}
