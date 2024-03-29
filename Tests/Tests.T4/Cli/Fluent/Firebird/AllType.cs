// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using FirebirdSql.Data.Types;
using System;
using System.Numerics;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Fluent.Firebird
{
	public class AllType
	{
		public int              Id                  { get; set; } // integer
		public long?            BigintDataType      { get; set; } // bigint
		public short?           SmallintDataType    { get; set; } // smallint
		public decimal?         DecimalDataType     { get; set; } // decimal(18,0)
		public int?             IntDataType         { get; set; } // integer
		public float?           FloatDataType       { get; set; } // float
		public float?           RealDataType        { get; set; } // float
		public double?          DoubleDataType      { get; set; } // double precision
		public DateTime?        TimestampDataType   { get; set; } // timestamp
		public char?            CharDataType        { get; set; } // char(1)
		public string?          Char20DataType      { get; set; } // char(20)
		public string?          VarcharDataType     { get; set; } // varchar(20)
		public string?          TextDataType        { get; set; } // blob sub_type 1
		public string?          NcharDataType       { get; set; } // char(20)
		public string?          NvarcharDataType    { get; set; } // varchar(20)
		public FbZonedDateTime? TimestampTzDataType { get; set; } // timestamp with time zone
		public FbZonedTime?     TimeTzDataType      { get; set; } // time with time zone
		public FbDecFloat?      Decfloat16DataType  { get; set; } // decfloat
		public FbDecFloat?      Decfloat34DataType  { get; set; } // decfloat
		public BigInteger?      Int128DataType      { get; set; } // int128
		public byte[]?          BlobDataType        { get; set; } // blob
	}
}
