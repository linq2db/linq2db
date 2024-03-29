// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using System;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SQLite
{
	[Table("LinqDataTypes")]
	public class LinqDataType
	{
		[Column("ID"            )] public int?      Id             { get; set; } // int
		[Column("MoneyValue"    )] public decimal?  MoneyValue     { get; set; } // decimal
		[Column("DateTimeValue" )] public DateTime? DateTimeValue  { get; set; } // datetime
		[Column("DateTimeValue2")] public DateTime? DateTimeValue2 { get; set; } // datetime2
		[Column("BoolValue"     )] public bool?     BoolValue      { get; set; } // boolean
		[Column("GuidValue"     )] public Guid?     GuidValue      { get; set; } // uniqueidentifier
		[Column("BinaryValue"   )] public byte[]?   BinaryValue    { get; set; } // binary
		[Column("SmallIntValue" )] public short?    SmallIntValue  { get; set; } // smallint
		[Column("IntValue"      )] public int?      IntValue       { get; set; } // int
		[Column("BigIntValue"   )] public long?     BigIntValue    { get; set; } // bigint
		[Column("StringValue"   )] public string?   StringValue    { get; set; } // nvarchar(50)
	}
}
