// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;
using System;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.PostgreSQL
{
	[Table("multitenant_table")]
	public class MultitenantTable
	{
		[Column("tenantid"   , DataType = DataType.Guid     , DbType = "uuid"                           , SkipOnUpdate = true                     )] public Guid     Tenantid    { get; set; } // uuid
		[Column("id"         , DataType = DataType.Guid     , DbType = "uuid"                           , SkipOnUpdate = true                     )] public Guid     Id          { get; set; } // uuid
		[Column("name"       , DataType = DataType.NVarChar , DbType = "character varying(100)"         , Length       = 100 , SkipOnUpdate = true)] public string?  Name        { get; set; } // character varying(100)
		[Column("description", DataType = DataType.Text     , DbType = "text"                           , SkipOnUpdate = true                     )] public string?  Description { get; set; } // text
		[Column("createdat"  , DataType = DataType.DateTime2, DbType = "timestamp (6) without time zone", Precision    = 6   , SkipOnUpdate = true)] public DateTime Createdat   { get; set; } // timestamp (6) without time zone
	}
}
