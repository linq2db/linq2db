using System;
using System.Data.SqlTypes;

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	public interface IMapDataDestination
	{
		Type GetFieldType (int index);
		int  GetOrdinal   (string name);
		void SetValue     (object o, int index,   object value);
		void SetValue     (object o, string name, object value);

		void SetNull      (object o, int index);

		// Simple type setters.
		//
		[CLSCompliant(false)]
		void SetSByte     (object o, int index, SByte    value);
		void SetInt16     (object o, int index, Int16    value);
		void SetInt32     (object o, int index, Int32    value);
		void SetInt64     (object o, int index, Int64    value);

		void SetByte      (object o, int index, Byte     value);
		[CLSCompliant(false)]
		void SetUInt16    (object o, int index, UInt16   value);
		[CLSCompliant(false)]
		void SetUInt32    (object o, int index, UInt32   value);
		[CLSCompliant(false)]
		void SetUInt64    (object o, int index, UInt64   value);

		void SetBoolean   (object o, int index, Boolean  value);
		void SetChar      (object o, int index, Char     value);
		void SetSingle    (object o, int index, Single   value);
		void SetDouble    (object o, int index, Double   value);
		void SetDecimal   (object o, int index, Decimal  value);
		void SetGuid      (object o, int index, Guid     value);
		void SetDateTime  (object o, int index, DateTime value);
		void SetDateTimeOffset(object o, int index, DateTimeOffset value);

		// Simple type setters.
		//
		[CLSCompliant(false)]
		void SetNullableSByte     (object o, int index, SByte?    value);
		void SetNullableInt16     (object o, int index, Int16?    value);
		void SetNullableInt32     (object o, int index, Int32?    value);
		void SetNullableInt64     (object o, int index, Int64?    value);

		void SetNullableByte      (object o, int index, Byte?     value);
		[CLSCompliant(false)]
		void SetNullableUInt16    (object o, int index, UInt16?   value);
		[CLSCompliant(false)]
		void SetNullableUInt32    (object o, int index, UInt32?   value);
		[CLSCompliant(false)]
		void SetNullableUInt64    (object o, int index, UInt64?   value);

		void SetNullableBoolean   (object o, int index, Boolean?  value);
		void SetNullableChar      (object o, int index, Char?     value);
		void SetNullableSingle    (object o, int index, Single?   value);
		void SetNullableDouble    (object o, int index, Double?   value);
		void SetNullableDecimal   (object o, int index, Decimal?  value);
		void SetNullableGuid      (object o, int index, Guid?     value);
		void SetNullableDateTime  (object o, int index, DateTime? value);
		void SetNullableDateTimeOffset(object o, int index, DateTimeOffset? value);

#if !SILVERLIGHT

		// SQL type setters.
		//
		void SetSqlByte    (object o, int index, SqlByte     value);
		void SetSqlInt16   (object o, int index, SqlInt16    value);
		void SetSqlInt32   (object o, int index, SqlInt32    value);
		void SetSqlInt64   (object o, int index, SqlInt64    value);
		void SetSqlSingle  (object o, int index, SqlSingle   value);
		void SetSqlBoolean (object o, int index, SqlBoolean  value);
		void SetSqlDouble  (object o, int index, SqlDouble   value);
		void SetSqlDateTime(object o, int index, SqlDateTime value);
		void SetSqlDecimal (object o, int index, SqlDecimal  value);
		void SetSqlMoney   (object o, int index, SqlMoney    value);
		void SetSqlGuid    (object o, int index, SqlGuid     value);
		void SetSqlString  (object o, int index, SqlString   value);

#endif
	}
}
