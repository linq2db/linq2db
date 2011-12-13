using System;

#if !SILVERLIGHT
using System.Data.SqlTypes;
#endif

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	public interface IMapDataSource
	{
		int      Count { get; }

		Type     GetFieldType (int index);
		string   GetName      (int index);
		int      GetOrdinal   (string name);
		object   GetValue     (object o, int index);
		object   GetValue     (object o, string name);

		bool     IsNull       (object o, int index);

		bool     SupportsTypedValues(int index);

		// Simple type getters.
		//
		[CLSCompliant(false)]
		SByte    GetSByte     (object o, int index);
		Int16    GetInt16     (object o, int index);
		Int32    GetInt32     (object o, int index);
		Int64    GetInt64     (object o, int index);

		Byte     GetByte      (object o, int index);
		[CLSCompliant(false)]
		UInt16   GetUInt16    (object o, int index);
		[CLSCompliant(false)]
		UInt32   GetUInt32    (object o, int index);
		[CLSCompliant(false)]
		UInt64   GetUInt64    (object o, int index);

		Boolean  GetBoolean   (object o, int index);
		Char     GetChar      (object o, int index);
		Single   GetSingle    (object o, int index);
		Double   GetDouble    (object o, int index);
		Decimal  GetDecimal   (object o, int index);
		DateTime GetDateTime  (object o, int index);
		DateTimeOffset GetDateTimeOffset(object o, int index);
		Guid     GetGuid      (object o, int index);

		// Simple type getters.
		//
		[CLSCompliant(false)]
		SByte?    GetNullableSByte   (object o, int index);
		Int16?    GetNullableInt16   (object o, int index);
		Int32?    GetNullableInt32   (object o, int index);
		Int64?    GetNullableInt64   (object o, int index);

		Byte?     GetNullableByte    (object o, int index);
		[CLSCompliant(false)]
		UInt16?   GetNullableUInt16  (object o, int index);
		[CLSCompliant(false)]
		UInt32?   GetNullableUInt32  (object o, int index);
		[CLSCompliant(false)]
		UInt64?   GetNullableUInt64  (object o, int index);

		Boolean?  GetNullableBoolean (object o, int index);
		Char?     GetNullableChar    (object o, int index);
		Single?   GetNullableSingle  (object o, int index);
		Double?   GetNullableDouble  (object o, int index);
		Decimal?  GetNullableDecimal (object o, int index);
		DateTime? GetNullableDateTime(object o, int index);
		DateTimeOffset? GetNullableDateTimeOffset(object o, int index);
		Guid?     GetNullableGuid    (object o, int index);

#if !SILVERLIGHT

		// SQL type getters.
		//
		SqlByte     GetSqlByte     (object o, int index);
		SqlInt16    GetSqlInt16    (object o, int index);
		SqlInt32    GetSqlInt32    (object o, int index);
		SqlInt64    GetSqlInt64    (object o, int index);
		SqlSingle   GetSqlSingle   (object o, int index);
		SqlBoolean  GetSqlBoolean  (object o, int index);
		SqlDouble   GetSqlDouble   (object o, int index);
		SqlDateTime GetSqlDateTime (object o, int index);
		SqlDecimal  GetSqlDecimal  (object o, int index);
		SqlMoney    GetSqlMoney    (object o, int index);
		SqlGuid     GetSqlGuid     (object o, int index);
		SqlString   GetSqlString   (object o, int index);

#endif
	}
}
