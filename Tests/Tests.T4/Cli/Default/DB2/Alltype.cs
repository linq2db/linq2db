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

namespace Cli.Default.DB2
{
	[Table("ALLTYPES")]
	public class Alltype
	{
		[Column("ID"               , IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int       Id                { get; set; } // INTEGER
		[Column("BIGINTDATATYPE"                                                                                     )] public long?     Bigintdatatype    { get; set; } // BIGINT
		[Column("INTDATATYPE"                                                                                        )] public int?      Intdatatype       { get; set; } // INTEGER
		[Column("SMALLINTDATATYPE"                                                                                   )] public short?    Smallintdatatype  { get; set; } // SMALLINT
		[Column("DECIMALDATATYPE"                                                                                    )] public decimal?  Decimaldatatype   { get; set; } // DECIMAL
		[Column("DECFLOATDATATYPE"                                                                                   )] public decimal?  Decfloatdatatype  { get; set; } // DECFLOAT(16)
		[Column("REALDATATYPE"                                                                                       )] public float?    Realdatatype      { get; set; } // REAL
		[Column("DOUBLEDATATYPE"                                                                                     )] public double?   Doubledatatype    { get; set; } // DOUBLE
		[Column("CHARDATATYPE"                                                                                       )] public char?     Chardatatype      { get; set; } // CHARACTER(1)
		[Column("CHAR20DATATYPE"                                                                                     )] public string?   Char20Datatype    { get; set; } // CHARACTER(20)
		[Column("VARCHARDATATYPE"                                                                                    )] public string?   Varchardatatype   { get; set; } // VARCHAR(20)
		[Column("CLOBDATATYPE"                                                                                       )] public string?   Clobdatatype      { get; set; } // CLOB(1048576)
		[Column("DBCLOBDATATYPE"                                                                                     )] public string?   Dbclobdatatype    { get; set; } // DBCLOB(100)
		[Column("BINARYDATATYPE"                                                                                     )] public byte[]?   Binarydatatype    { get; set; } // CHAR (5) FOR BIT DATA
		[Column("VARBINARYDATATYPE"                                                                                  )] public byte[]?   Varbinarydatatype { get; set; } // VARCHAR (5) FOR BIT DATA
		[Column("BLOBDATATYPE"                                                                                       )] public byte[]?   Blobdatatype      { get; set; } // BLOB(1048576)
		[Column("GRAPHICDATATYPE"                                                                                    )] public string?   Graphicdatatype   { get; set; } // GRAPHIC(10)
		[Column("DATEDATATYPE"                                                                                       )] public DateTime? Datedatatype      { get; set; } // DATE
		[Column("TIMEDATATYPE"                                                                                       )] public TimeSpan? Timedatatype      { get; set; } // TIME
		[Column("TIMESTAMPDATATYPE"                                                                                  )] public DateTime? Timestampdatatype { get; set; } // TIMESTAMP
		[Column("XMLDATATYPE"                                                                                        )] public string?   Xmldatatype       { get; set; } // XML
	}
}
