using System;

using IBM.Data.DB2Types;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class DB2MappingSchema : MappingSchema
	{
		public DB2MappingSchema() : base(ProviderName.DB2)
		{
			AddScalarType(typeof(DB2Int64),        DB2Int64.       Null, DataType.Int64);
			AddScalarType(typeof(DB2Int32),        DB2Int32.       Null, DataType.Int32);
			AddScalarType(typeof(DB2Int16),        DB2Int16.       Null, DataType.Int16);
			AddScalarType(typeof(DB2Decimal),      DB2Decimal.     Null, DataType.Decimal);
			AddScalarType(typeof(DB2DecimalFloat), DB2DecimalFloat.Null, DataType.Decimal);
			AddScalarType(typeof(DB2Real),         DB2Real.        Null, DataType.Single);
			AddScalarType(typeof(DB2Real370),      DB2Real370.     Null, DataType.Single);
			AddScalarType(typeof(DB2Double),       DB2Double.      Null, DataType.Double);
			AddScalarType(typeof(DB2String),       DB2String.      Null, DataType.NVarChar);
			AddScalarType(typeof(DB2Clob),         DB2Clob.        Null, DataType.NText);
			AddScalarType(typeof(DB2Binary),       DB2Binary.      Null, DataType.VarBinary);
			AddScalarType(typeof(DB2Blob),         DB2Blob.        Null, DataType.VarBinary);
			AddScalarType(typeof(DB2Date),         DB2Date.        Null, DataType.Date);
			AddScalarType(typeof(DB2Time),         DB2Time.        Null, DataType.Time);
			AddScalarType(typeof(DB2TimeStamp),    DB2TimeStamp.   Null, DataType.DateTime2);
			AddScalarType(typeof(DB2Xml),          DB2Xml.         Null, DataType.Xml);
			AddScalarType(typeof(DB2RowId),        DB2RowId.       Null, DataType.VarBinary);
		}
	}
}
