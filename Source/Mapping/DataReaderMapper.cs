using System;
using System.Data;
using System.Data.SqlTypes;

namespace LinqToDB.Mapping
{
	using Data;

	public class DataReaderMapper : IMapDataSource
	{
		public DataReaderMapper(MappingSchemaOld mappingSchema, IDataReader dataReader)
		{
			_mappingSchema = mappingSchema;
			_dataReader    = dataReader;
			_dataReaderEx  = dataReader as IDataReaderEx;
		}

		readonly IDataReaderEx _dataReaderEx;

		readonly IDataReader _dataReader;
		public   IDataReader  DataReader
		{
			get { return _dataReader; }
		}

		readonly MappingSchemaOld _mappingSchema;
		public   MappingSchemaOld  MappingSchema
		{
			get { return _mappingSchema; }
		}

		#region IMapDataSource Members

		public virtual int Count
		{
			get { return _dataReader.FieldCount; }
		}

		public virtual Type GetFieldType(int index)
		{
			return _dataReader.GetFieldType(index);
		}

		public virtual string GetName(int index)
		{
			return _dataReader.GetName(index);
		}

		public virtual int GetOrdinal(string name)
		{
			return _dataReader.GetOrdinal(name);
		}

		public virtual object GetValue(object o, int index)
		{
			var value = _dataReader.GetValue(index);
			return value is DBNull? null: value;
		}

		public virtual object GetValue(object o, string name)
		{
			var value = _dataReader[name];
			return value is DBNull? null: value;
		}

		public virtual bool     IsNull     (object o, int index) { return _dataReader.IsDBNull(index); }
		public virtual bool     SupportsTypedValues(int index)   { return true; }

		// Simple type getters.
		//
		[CLSCompliant(false)]
		public virtual SByte    GetSByte   (object o, int index) { return _mappingSchema.ConvertToSByte(GetValue(o, index)); }
		public virtual Int16    GetInt16   (object o, int index) { return _dataReader.GetInt16   (index); }
		public virtual Int32    GetInt32   (object o, int index) { return _dataReader.GetInt32   (index); }
		public virtual Int64    GetInt64   (object o, int index) { return _dataReader.GetInt64   (index); }

		public virtual Byte     GetByte    (object o, int index) { return _dataReader.GetByte    (index); }
		[CLSCompliant(false)]
		public virtual UInt16   GetUInt16  (object o, int index) { return _mappingSchema.ConvertToUInt16(GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt32   GetUInt32  (object o, int index) { return _mappingSchema.ConvertToUInt32(GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt64   GetUInt64  (object o, int index) { return _mappingSchema.ConvertToUInt64(GetValue(o, index)); }

		public virtual Boolean  GetBoolean (object o, int index) { return _dataReader.GetBoolean (index); }
		public virtual Char     GetChar    (object o, int index) { return _dataReader.GetChar    (index); }
		public virtual Single   GetSingle  (object o, int index) { return _dataReader.GetFloat   (index); }
		public virtual Double   GetDouble  (object o, int index) { return _dataReader.GetDouble  (index); }
		public virtual Decimal  GetDecimal (object o, int index) { return _dataReader.GetDecimal (index); }
		public virtual Guid     GetGuid    (object o, int index) { return _dataReader.GetGuid    (index); }
		public virtual DateTime GetDateTime(object o, int index) { return _dataReader.GetDateTime(index); }

		public virtual DateTimeOffset GetDateTimeOffset(object o, int index)
		{
			return _dataReaderEx != null?
				_dataReaderEx.GetDateTimeOffset(index):
				_mappingSchema.ConvertToDateTimeOffset(_dataReader.GetValue(index));
		}

		// Nullable type getters.
		//
		[CLSCompliant(false)]
		public virtual SByte?    GetNullableSByte   (object o, int index) { return _dataReader.IsDBNull(index)? null: _mappingSchema.ConvertToNullableSByte(GetValue(o, index)); }
		public virtual Int16?    GetNullableInt16   (object o, int index) { return _dataReader.IsDBNull(index)? null: (Int16?)_dataReader.GetInt16   (index); }
		public virtual Int32?    GetNullableInt32   (object o, int index) { return _dataReader.IsDBNull(index)? null: (Int32?)_dataReader.GetInt32   (index); }
		public virtual Int64?    GetNullableInt64   (object o, int index) { return _dataReader.IsDBNull(index)? null: (Int64?)_dataReader.GetInt64   (index); }

		public virtual Byte?     GetNullableByte    (object o, int index) { return _dataReader.IsDBNull(index)? null: (Byte?) _dataReader.GetByte    (index); }
		[CLSCompliant(false)]
		public virtual UInt16?   GetNullableUInt16  (object o, int index) { return _dataReader.IsDBNull(index)? null: _mappingSchema.ConvertToNullableUInt16(GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt32?   GetNullableUInt32  (object o, int index) { return _dataReader.IsDBNull(index)? null: _mappingSchema.ConvertToNullableUInt32(GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt64?   GetNullableUInt64  (object o, int index) { return _dataReader.IsDBNull(index)? null: _mappingSchema.ConvertToNullableUInt64(GetValue(o, index)); }

		public virtual Boolean?  GetNullableBoolean (object o, int index) { return _dataReader.IsDBNull(index)? null: (Boolean?) _dataReader.GetBoolean (index); }
		public virtual Char?     GetNullableChar    (object o, int index) { return _dataReader.IsDBNull(index)? null: (Char?)    _dataReader.GetChar    (index); }
		public virtual Single?   GetNullableSingle  (object o, int index) { return _dataReader.IsDBNull(index)? null: (Single?)  _dataReader.GetFloat   (index); }
		public virtual Double?   GetNullableDouble  (object o, int index) { return _dataReader.IsDBNull(index)? null: (Double?)  _dataReader.GetDouble  (index); }
		public virtual Decimal?  GetNullableDecimal (object o, int index) { return _dataReader.IsDBNull(index)? null: (Decimal?) _dataReader.GetDecimal (index); }
		public virtual Guid?     GetNullableGuid    (object o, int index) { return _dataReader.IsDBNull(index)? null: (Guid?)    _dataReader.GetGuid    (index); }
		public virtual DateTime? GetNullableDateTime(object o, int index) { return _dataReader.IsDBNull(index)? null: (DateTime?)_dataReader.GetDateTime(index); }
		public virtual DateTimeOffset? GetNullableDateTimeOffset(object o, int index)
		{
			return _dataReader.IsDBNull(index)? null:
				_dataReaderEx != null? _dataReaderEx.GetDateTimeOffset(index):
				_mappingSchema.ConvertToNullableDateTimeOffset(_dataReader.GetValue(index));
		}

#if !SILVERLIGHT

		// SQL type getters.
		//
		public virtual SqlByte     GetSqlByte    (object o, int index) { return _dataReader.IsDBNull(index)? SqlByte.    Null: _dataReader.GetByte    (index); }
		public virtual SqlInt16    GetSqlInt16   (object o, int index) { return _dataReader.IsDBNull(index)? SqlInt16.   Null: _dataReader.GetInt16   (index); }
		public virtual SqlInt32    GetSqlInt32   (object o, int index) { return _dataReader.IsDBNull(index)? SqlInt32.   Null: _dataReader.GetInt32   (index); }
		public virtual SqlInt64    GetSqlInt64   (object o, int index) { return _dataReader.IsDBNull(index)? SqlInt64.   Null: _dataReader.GetInt64   (index); }
		public virtual SqlSingle   GetSqlSingle  (object o, int index) { return _dataReader.IsDBNull(index)? SqlSingle.  Null: _dataReader.GetFloat   (index); }
		public virtual SqlBoolean  GetSqlBoolean (object o, int index) { return _dataReader.IsDBNull(index)? SqlBoolean. Null: _dataReader.GetBoolean (index); }
		public virtual SqlDouble   GetSqlDouble  (object o, int index) { return _dataReader.IsDBNull(index)? SqlDouble.  Null: _dataReader.GetDouble  (index); }
		public virtual SqlDateTime GetSqlDateTime(object o, int index) { return _dataReader.IsDBNull(index)? SqlDateTime.Null: _dataReader.GetDateTime(index); }
		public virtual SqlDecimal  GetSqlDecimal (object o, int index) { return _dataReader.IsDBNull(index)? SqlDecimal. Null: _dataReader.GetDecimal (index); }
		public virtual SqlMoney    GetSqlMoney   (object o, int index) { return _dataReader.IsDBNull(index)? SqlMoney.   Null: _dataReader.GetDecimal (index); }
		public virtual SqlGuid     GetSqlGuid    (object o, int index) { return _dataReader.IsDBNull(index)? SqlGuid.    Null: _dataReader.GetGuid    (index); }
		public virtual SqlString   GetSqlString  (object o, int index) { return _dataReader.IsDBNull(index)? SqlString.  Null: _dataReader.GetString  (index); }

#endif

		#endregion
	}
}
