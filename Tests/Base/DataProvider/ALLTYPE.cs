using System;

using LinqToDB.Mapping;

namespace Tests.DataProvider
{
	[Table(Name="ALLTYPES")]
	public class ALLTYPE
	{
		[PrimaryKey, Identity] public int       ID                { get; set; } // INTEGER
		[Column,     Nullable] public long?     BIGINTDATATYPE    { get; set; } // BIGINT
		[Column,     Nullable] public int?      INTDATATYPE       { get; set; } // INTEGER
		[Column,     Nullable] public short?    SMALLINTDATATYPE  { get; set; } // SMALLINT
		[Column,     Nullable] public decimal?  DECIMALDATATYPE   { get; set; } // DECIMAL
		[Column,     Nullable] public decimal?  DECFLOATDATATYPE  { get; set; } // DECFLOAT
		[Column,     Nullable] public float?    REALDATATYPE      { get; set; } // REAL
		[Column,     Nullable] public double?   DOUBLEDATATYPE    { get; set; } // DOUBLE
		[Column,     Nullable] public char      CHARDATATYPE      { get; set; } // CHARACTER
		[Column,     Nullable] public string    VARCHARDATATYPE   { get; set; } // VARCHAR(20)
		[Column,     Nullable] public string    CLOBDATATYPE      { get; set; } // CLOB(1048576)
		[Column,     Nullable] public string    DBCLOBDATATYPE    { get; set; } // DBCLOB(100)
		[Column,     Nullable] public object    BINARYDATATYPE    { get; set; } // CHARACTER
		[Column,     Nullable] public string    VARBINARYDATATYPE { get; set; } // VARCHAR(5)
		[Column,     Nullable] public byte[]    BLOBDATATYPE      { get; set; } // BLOB(10)
		[Column,     Nullable] public string    GRAPHICDATATYPE   { get; set; } // GRAPHIC(10)
		[Column,     Nullable] public DateTime? DATEDATATYPE      { get; set; } // DATE
		[Column,     Nullable] public TimeSpan? TIMEDATATYPE      { get; set; } // TIME
		[Column,     Nullable] public DateTime? TIMESTAMPDATATYPE { get; set; } // TIMESTAMP
		[Column,     Nullable] public string    XMLDATATYPE       { get; set; } // XML
	}
}
