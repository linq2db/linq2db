using System;

using IBM.Data.DB2Types;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class DB2MappingSchema : MappingSchema
	{
		public DB2MappingSchema() : this(ProviderName.DB2)
		{
		}

		protected DB2MappingSchema(string configuration) : base(configuration)
		{
			AddScalarType(typeof(DB2Int64),        DB2Int64.       Null, true, DataType.Int64);
			AddScalarType(typeof(DB2Int32),        DB2Int32.       Null, true, DataType.Int32);
			AddScalarType(typeof(DB2Int16),        DB2Int16.       Null, true, DataType.Int16);
			AddScalarType(typeof(DB2Decimal),      DB2Decimal.     Null, true, DataType.Decimal);
			AddScalarType(typeof(DB2DecimalFloat), DB2DecimalFloat.Null, true, DataType.Decimal);
			AddScalarType(typeof(DB2Real),         DB2Real.        Null, true, DataType.Single);
			AddScalarType(typeof(DB2Real370),      DB2Real370.     Null, true, DataType.Single);
			AddScalarType(typeof(DB2Double),       DB2Double.      Null, true, DataType.Double);
			AddScalarType(typeof(DB2String),       DB2String.      Null, true, DataType.NVarChar);
			AddScalarType(typeof(DB2Clob),         DB2Clob.        Null, true, DataType.NText);
			AddScalarType(typeof(DB2Binary),       DB2Binary.      Null, true, DataType.VarBinary);
			AddScalarType(typeof(DB2Blob),         DB2Blob.        Null, true, DataType.VarBinary);
			AddScalarType(typeof(DB2Date),         DB2Date.        Null, true, DataType.Date);
			AddScalarType(typeof(DB2Time),         DB2Time.        Null, true, DataType.Time);
			AddScalarType(typeof(DB2TimeStamp),    DB2TimeStamp.   Null, true, DataType.DateTime2);
			AddScalarType(typeof(DB2Xml),          DB2Xml.         Null, true, DataType.Xml);
			AddScalarType(typeof(DB2RowId),        DB2RowId.       Null, true, DataType.VarBinary);
		}
	}
}
