using System;
using System.Data;

using LinqToDB.Common;
using System.Data.SqlTypes;

namespace LinqToDB.Mapping
{
	public class ScalarDataReaderMapper : DataReaderMapper
	{
		public ScalarDataReaderMapper(
			MappingSchema         mappingSchema,
			IDataReader           dataReader,
			NameOrIndexParameter  nameOrIndex)
			: base(mappingSchema, dataReader)
		{
			_index = nameOrIndex.ByName? dataReader.GetOrdinal(nameOrIndex.Name): nameOrIndex.Index;
		}

		private readonly int _index;
		public           int  Index
		{
			get { return _index; }
		}

		#region IMapDataSource Members

		public override int Count
		{
			get { return 1; }
		}

		public override Type GetFieldType(int index)
		{
			return DataReader.GetFieldType(_index);
		}

		public override string GetName(int index)
		{
			return DataReader.GetName(_index);
		}

		public override object GetValue(object o, int index)
		{
			return base.GetValue(o, _index);
		}

		public override object GetValue(object o, string name)
		{
			return base.GetValue(o, _index);
		}

		public override bool     IsNull     (object o, int index) { return DataReader.IsDBNull(_index); }

		// Simple type getters.
		//
		[CLSCompliant(false)]
		public override SByte    GetSByte   (object o, int index) { return base.GetSByte(o, _index); }
		public override Int16    GetInt16   (object o, int index) { return DataReader.GetInt16   (_index); }
		public override Int32    GetInt32   (object o, int index) { return DataReader.GetInt32   (_index); }
		public override Int64    GetInt64   (object o, int index) { return DataReader.GetInt64   (_index); }

		public override Byte     GetByte    (object o, int index) { return DataReader.GetByte    (_index); }
		[CLSCompliant(false)]
		public override UInt16   GetUInt16  (object o, int index) { return base.GetUInt16(o, _index); }
		[CLSCompliant(false)]
		public override UInt32   GetUInt32  (object o, int index) { return base.GetUInt32(o, _index); }
		[CLSCompliant(false)]
		public override UInt64   GetUInt64  (object o, int index) { return base.GetUInt64(o, _index); }

		public override Boolean  GetBoolean (object o, int index) { return DataReader.GetBoolean (_index); }
		public override Char     GetChar    (object o, int index) { return DataReader.GetChar    (_index); }
		public override Single   GetSingle  (object o, int index) { return DataReader.GetFloat   (_index); }
		public override Double   GetDouble  (object o, int index) { return DataReader.GetDouble  (_index); }
		public override Decimal  GetDecimal (object o, int index) { return DataReader.GetDecimal (_index); }
		public override Guid     GetGuid    (object o, int index) { return DataReader.GetGuid    (_index); }
		public override DateTime GetDateTime(object o, int index) { return DataReader.GetDateTime(_index); }
		public override DateTimeOffset GetDateTimeOffset(object o, int index) { return DataReader.GetDateTime(_index); }

		// Nullable type getters.
		//
		[CLSCompliant(false)]
		public override SByte?    GetNullableSByte   (object o, int index) { return base.GetNullableSByte(o, _index); }
		public override Int16?    GetNullableInt16   (object o, int index) { return DataReader.IsDBNull(_index)? null: (Int16?)DataReader.GetInt16   (_index); }
		public override Int32?    GetNullableInt32   (object o, int index) { return DataReader.IsDBNull(_index)? null: (Int32?)DataReader.GetInt32   (_index); }
		public override Int64?    GetNullableInt64   (object o, int index) { return DataReader.IsDBNull(_index)? null: (Int64?)DataReader.GetInt64   (_index); }

		public override Byte?     GetNullableByte    (object o, int index) { return DataReader.IsDBNull(_index)? null: (Byte?) DataReader.GetByte    (_index); }
		[CLSCompliant(false)]
		public override UInt16?   GetNullableUInt16  (object o, int index) { return base.GetNullableUInt16(o, _index); }
		[CLSCompliant(false)]
		public override UInt32?   GetNullableUInt32  (object o, int index) { return base.GetNullableUInt32(o, _index); }
		[CLSCompliant(false)]
		public override UInt64?   GetNullableUInt64  (object o, int index) { return base.GetNullableUInt64(o, _index); }

		public override Boolean?  GetNullableBoolean (object o, int index) { return DataReader.IsDBNull(_index)? null: (Boolean?) DataReader.GetBoolean (_index); }
		public override Char?     GetNullableChar    (object o, int index) { return DataReader.IsDBNull(_index)? null: (Char?)    DataReader.GetChar    (_index); }
		public override Single?   GetNullableSingle  (object o, int index) { return DataReader.IsDBNull(_index)? null: (Single?)  DataReader.GetFloat   (_index); }
		public override Double?   GetNullableDouble  (object o, int index) { return DataReader.IsDBNull(_index)? null: (Double?)  DataReader.GetDouble  (_index); }
		public override Decimal?  GetNullableDecimal (object o, int index) { return DataReader.IsDBNull(_index)? null: (Decimal?) DataReader.GetDecimal (_index); }
		public override Guid?     GetNullableGuid    (object o, int index) { return DataReader.IsDBNull(_index)? null: (Guid?)    DataReader.GetGuid    (_index); }
		public override DateTime? GetNullableDateTime(object o, int index) { return DataReader.IsDBNull(_index)? null: (DateTime?)DataReader.GetDateTime(_index); }
		public override DateTimeOffset? GetNullableDateTimeOffset(object o, int index) { return DataReader.IsDBNull(_index)? null: (DateTimeOffset?)DataReader.GetDateTime(_index); }

#if !SILVERLIGHT

		// SQL type getters.
		//
		public override SqlByte     GetSqlByte    (object o, int index) { return DataReader.IsDBNull(_index)? SqlByte.    Null: DataReader.GetByte    (_index); }
		public override SqlInt16    GetSqlInt16   (object o, int index) { return DataReader.IsDBNull(_index)? SqlInt16.   Null: DataReader.GetInt16   (_index); }
		public override SqlInt32    GetSqlInt32   (object o, int index) { return DataReader.IsDBNull(_index)? SqlInt32.   Null: DataReader.GetInt32   (_index); }
		public override SqlInt64    GetSqlInt64   (object o, int index) { return DataReader.IsDBNull(_index)? SqlInt64.   Null: DataReader.GetInt64   (_index); }
		public override SqlSingle   GetSqlSingle  (object o, int index) { return DataReader.IsDBNull(_index)? SqlSingle.  Null: DataReader.GetFloat   (_index); }
		public override SqlBoolean  GetSqlBoolean (object o, int index) { return DataReader.IsDBNull(_index)? SqlBoolean. Null: DataReader.GetBoolean (_index); }
		public override SqlDouble   GetSqlDouble  (object o, int index) { return DataReader.IsDBNull(_index)? SqlDouble.  Null: DataReader.GetDouble  (_index); }
		public override SqlDateTime GetSqlDateTime(object o, int index) { return DataReader.IsDBNull(_index)? SqlDateTime.Null: DataReader.GetDateTime(_index); }
		public override SqlDecimal  GetSqlDecimal (object o, int index) { return DataReader.IsDBNull(_index)? SqlDecimal. Null: DataReader.GetDecimal (_index); }
		public override SqlMoney    GetSqlMoney   (object o, int index) { return DataReader.IsDBNull(_index)? SqlMoney.   Null: DataReader.GetDecimal (_index); }
		public override SqlGuid     GetSqlGuid    (object o, int index) { return DataReader.IsDBNull(_index)? SqlGuid.    Null: DataReader.GetGuid    (_index); }
		public override SqlString   GetSqlString  (object o, int index) { return DataReader.IsDBNull(_index)? SqlString.  Null: DataReader.GetString  (_index); }

#endif

		#endregion
	}
}
