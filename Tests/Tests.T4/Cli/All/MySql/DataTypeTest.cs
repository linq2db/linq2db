// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using System;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.All.MySql
{
	[Table("DataTypeTest")]
	public class DataTypeTest : IEquatable<DataTypeTest>
	{
		[Column("DataTypeID", DataType = DataType.Int32    , DbType = "int"          , Precision = 10  , Scale = 0, IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int       DataTypeId { get; set; } // int
		[Column("Binary_"   , DataType = DataType.Binary   , DbType = "binary(50)"   , Length    = 50                                                                                               )] public byte[]?   Binary     { get; set; } // binary(50)
		[Column("Boolean_"  , DataType = DataType.BitArray , DbType = "bit(1)"       , Precision = 1                                                                                                )] public bool      Boolean    { get; set; } // bit(1)
		[Column("Byte_"     , DataType = DataType.SByte    , DbType = "tinyint"      , Precision = 3   , Scale = 0                                                                                  )] public sbyte?    Byte       { get; set; } // tinyint
		[Column("Bytes_"    , DataType = DataType.VarBinary, DbType = "varbinary(50)", Length    = 50                                                                                               )] public byte[]?   Bytes      { get; set; } // varbinary(50)
		[Column("Char_"     , DataType = DataType.Char     , DbType = "char(1)"      , Length    = 1                                                                                                )] public char?     Char       { get; set; } // char(1)
		[Column("DateTime_" , DataType = DataType.DateTime , DbType = "datetime"                                                                                                                    )] public DateTime? DateTime   { get; set; } // datetime
		[Column("Decimal_"  , DataType = DataType.Decimal  , DbType = "decimal(20,2)", Precision = 20  , Scale = 2                                                                                  )] public decimal?  Decimal    { get; set; } // decimal(20,2)
		[Column("Double_"   , DataType = DataType.Single   , DbType = "float"        , Precision = 12                                                                                               )] public float?    Double     { get; set; } // float
		[Column("Guid_"     , DataType = DataType.VarBinary, DbType = "varbinary(50)", Length    = 50                                                                                               )] public byte[]?   Guid       { get; set; } // varbinary(50)
		[Column("Int16_"    , DataType = DataType.Int16    , DbType = "smallint"     , Precision = 5   , Scale = 0                                                                                  )] public short?    Int16      { get; set; } // smallint
		[Column("Int32_"    , DataType = DataType.Int32    , DbType = "int"          , Precision = 10  , Scale = 0                                                                                  )] public int?      Int32      { get; set; } // int
		[Column("Int64_"    , DataType = DataType.Int64    , DbType = "bigint"       , Precision = 19  , Scale = 0                                                                                  )] public long?     Int64      { get; set; } // bigint
		[Column("Money_"    , DataType = DataType.Decimal  , DbType = "decimal(20,4)", Precision = 20  , Scale = 4                                                                                  )] public decimal?  Money      { get; set; } // decimal(20,4)
		[Column("SByte_"    , DataType = DataType.SByte    , DbType = "tinyint"      , Precision = 3   , Scale = 0                                                                                  )] public sbyte?    SByte      { get; set; } // tinyint
		[Column("Single_"   , DataType = DataType.Double   , DbType = "double"       , Precision = 22                                                                                               )] public double?   Single     { get; set; } // double
		[Column("Stream_"   , DataType = DataType.VarBinary, DbType = "varbinary(50)", Length    = 50                                                                                               )] public byte[]?   Stream     { get; set; } // varbinary(50)
		[Column("String_"   , DataType = DataType.VarChar  , DbType = "varchar(50)"  , Length    = 50                                                                                               )] public string?   String     { get; set; } // varchar(50)
		[Column("UInt16_"   , DataType = DataType.Int16    , DbType = "smallint"     , Precision = 5   , Scale = 0                                                                                  )] public short?    UInt16     { get; set; } // smallint
		[Column("UInt32_"   , DataType = DataType.Int32    , DbType = "int"          , Precision = 10  , Scale = 0                                                                                  )] public int?      UInt32     { get; set; } // int
		[Column("UInt64_"   , DataType = DataType.Int64    , DbType = "bigint"       , Precision = 19  , Scale = 0                                                                                  )] public long?     UInt64     { get; set; } // bigint
		[Column("Xml_"      , DataType = DataType.VarChar  , DbType = "varchar(1000)", Length    = 1000                                                                                             )] public string?   Xml        { get; set; } // varchar(1000)

		#region IEquatable<T> support
		private static readonly IEqualityComparer<DataTypeTest> _equalityComparer = ComparerBuilder.GetEqualityComparer<DataTypeTest>(c => c.DataTypeId);

		public bool Equals(DataTypeTest? other)
		{
			return _equalityComparer.Equals(this, other!);
		}

		public override int GetHashCode()
		{
			return _equalityComparer.GetHashCode(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as DataTypeTest);
		}
		#endregion
	}
}
