// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using FirebirdSql.Data.Types;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using System;
using System.Collections.Generic;
using System.Numerics;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.Firebird
{
	[Table("AllTypes")]
	public class AllType : IEquatable<AllType>
	{
		[Column("ID"                 , DataType = DataType.Int32         , DbType = "integer"                 , IsPrimaryKey = true           )] public int              Id                  { get; set; } // integer
		[Column("bigintDataType"     , DataType = DataType.Int64         , DbType = "bigint"                                                  )] public long?            BigintDataType      { get; set; } // bigint
		[Column("smallintDataType"   , DataType = DataType.Int16         , DbType = "smallint"                                                )] public short?           SmallintDataType    { get; set; } // smallint
		[Column("decimalDataType"    , DataType = DataType.Decimal       , DbType = "decimal(18,0)"           , Precision    = 18  , Scale = 0)] public decimal?         DecimalDataType     { get; set; } // decimal(18,0)
		[Column("intDataType"        , DataType = DataType.Int32         , DbType = "integer"                                                 )] public int?             IntDataType         { get; set; } // integer
		[Column("floatDataType"      , DataType = DataType.Single        , DbType = "float"                                                   )] public float?           FloatDataType       { get; set; } // float
		[Column("realDataType"       , DataType = DataType.Single        , DbType = "float"                                                   )] public float?           RealDataType        { get; set; } // float
		[Column("doubleDataType"     , DataType = DataType.Double        , DbType = "double precision"                                        )] public double?          DoubleDataType      { get; set; } // double precision
		[Column("timestampDataType"  , DataType = DataType.DateTime      , DbType = "timestamp"                                               )] public DateTime?        TimestampDataType   { get; set; } // timestamp
		[Column("charDataType"       , DataType = DataType.NChar         , DbType = "char(1)"                 , Length       = 1              )] public char?            CharDataType        { get; set; } // char(1)
		[Column("char20DataType"     , DataType = DataType.NChar         , DbType = "char(20)"                , Length       = 20             )] public string?          Char20DataType      { get; set; } // char(20)
		[Column("varcharDataType"    , DataType = DataType.NVarChar      , DbType = "varchar(20)"             , Length       = 20             )] public string?          VarcharDataType     { get; set; } // varchar(20)
		[Column("textDataType"       , DataType = DataType.Text          , DbType = "blob sub_type 1"                                         )] public string?          TextDataType        { get; set; } // blob sub_type 1
		[Column("ncharDataType"      , DataType = DataType.NChar         , DbType = "char(20)"                , Length       = 20             )] public string?          NcharDataType       { get; set; } // char(20)
		[Column("nvarcharDataType"   , DataType = DataType.NVarChar      , DbType = "varchar(20)"             , Length       = 20             )] public string?          NvarcharDataType    { get; set; } // varchar(20)
		[Column("timestampTZDataType", DataType = DataType.DateTimeOffset, DbType = "timestamp with time zone"                                )] public FbZonedDateTime? TimestampTzDataType { get; set; } // timestamp with time zone
		[Column("timeTZDataType"     , DataType = DataType.TimeTZ        , DbType = "time with time zone"                                     )] public FbZonedTime?     TimeTzDataType      { get; set; } // time with time zone
		[Column("decfloat16DataType" , DataType = DataType.DecFloat      , DbType = "decfloat"                , Precision    = 16             )] public FbDecFloat?      Decfloat16DataType  { get; set; } // decfloat
		[Column("decfloat34DataType" , DataType = DataType.DecFloat      , DbType = "decfloat"                , Precision    = 34             )] public FbDecFloat?      Decfloat34DataType  { get; set; } // decfloat
		[Column("int128DataType"     , DataType = DataType.Int128        , DbType = "int128"                                                  )] public BigInteger?      Int128DataType      { get; set; } // int128
		[Column("blobDataType"       , DataType = DataType.Blob          , DbType = "blob"                                                    )] public byte[]?          BlobDataType        { get; set; } // blob

		#region IEquatable<T> support
		private static readonly IEqualityComparer<AllType> _equalityComparer = ComparerBuilder.GetEqualityComparer<AllType>(c => c.Id);

		public bool Equals(AllType? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as AllType);
		}
		#endregion
	}
}
