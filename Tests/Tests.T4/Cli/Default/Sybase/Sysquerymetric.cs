// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.Sybase
{
	[Table("sysquerymetrics", IsView = true)]
	public class Sysquerymetric
	{
		[Column("uid"      )] public int     Uid      { get; set; } // int
		[Column("gid"      )] public int     Gid      { get; set; } // int
		[Column("hashkey"  )] public int     Hashkey  { get; set; } // int
		[Column("id"       )] public int     Id       { get; set; } // int
		[Column("sequence" )] public short   Sequence { get; set; } // smallint
		[Column("exec_min" )] public ulong?  ExecMin  { get; set; } // ubigint
		[Column("exec_max" )] public ulong?  ExecMax  { get; set; } // ubigint
		[Column("exec_avg" )] public ulong?  ExecAvg  { get; set; } // ubigint
		[Column("elap_min" )] public ulong?  ElapMin  { get; set; } // ubigint
		[Column("elap_max" )] public ulong?  ElapMax  { get; set; } // ubigint
		[Column("elap_avg" )] public ulong?  ElapAvg  { get; set; } // ubigint
		[Column("lio_min"  )] public ulong?  LioMin   { get; set; } // ubigint
		[Column("lio_max"  )] public ulong?  LioMax   { get; set; } // ubigint
		[Column("lio_avg"  )] public ulong?  LioAvg   { get; set; } // ubigint
		[Column("pio_min"  )] public ulong?  PioMin   { get; set; } // ubigint
		[Column("pio_max"  )] public ulong?  PioMax   { get; set; } // ubigint
		[Column("pio_avg"  )] public ulong?  PioAvg   { get; set; } // ubigint
		[Column("cnt"      )] public ulong?  Cnt      { get; set; } // ubigint
		[Column("abort_cnt")] public ulong?  AbortCnt { get; set; } // ubigint
		[Column("qtext"    )] public string? Qtext    { get; set; } // varchar(510)
	}
}
