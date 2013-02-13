using System;
using System.Data.SqlTypes;

namespace LinqToDB.Mapping
{
	public abstract class MapDataSourceBase : IMapDataSource
	{
		#region IMapDataSource Members

		public abstract int      Count { get; }
		public abstract Type     GetFieldType(int index);
		public abstract string   GetName     (int index);
		public abstract int      GetOrdinal  (string name);
		public abstract object   GetValue    (object o, int index);
		public abstract object   GetValue    (object o, string name);

		public virtual  bool     IsNull      (object o, int index) { return GetValue(o, index) == null; }

		// Simple type getters.
		//
		public virtual  Int16    GetInt16    (object o, int index) { return Map.DefaultSchema.ConvertToInt16   (GetValue(o, index)); }
		public virtual  Int32    GetInt32    (object o, int index) { return Map.DefaultSchema.ConvertToInt32   (GetValue(o, index)); }
		public virtual  Int64    GetInt64    (object o, int index) { return Map.DefaultSchema.ConvertToInt64   (GetValue(o, index)); }

		public virtual  Byte     GetByte     (object o, int index) { return Map.DefaultSchema.ConvertToByte    (GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual  UInt16   GetUInt16   (object o, int index) { return Map.DefaultSchema.ConvertToUInt16  (GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual  UInt32   GetUInt32   (object o, int index) { return Map.DefaultSchema.ConvertToUInt32  (GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual  UInt64   GetUInt64   (object o, int index) { return Map.DefaultSchema.ConvertToUInt64  (GetValue(o, index)); }

		public virtual  Boolean  GetBoolean  (object o, int index) { return Map.DefaultSchema.ConvertToBoolean (GetValue(o, index)); }
		public virtual  Char     GetChar     (object o, int index) { return Map.DefaultSchema.ConvertToChar    (GetValue(o, index)); }
		public virtual  Single   GetSingle   (object o, int index) { return Map.DefaultSchema.ConvertToSingle  (GetValue(o, index)); }
		public virtual  Double   GetDouble   (object o, int index) { return Map.DefaultSchema.ConvertToDouble  (GetValue(o, index)); }
		public virtual  Decimal  GetDecimal  (object o, int index) { return Map.DefaultSchema.ConvertToDecimal (GetValue(o, index)); }
		public virtual  Guid     GetGuid     (object o, int index) { return Map.DefaultSchema.ConvertToGuid    (GetValue(o, index)); }
		public virtual  DateTime GetDateTime (object o, int index) { return Map.DefaultSchema.ConvertToDateTime(GetValue(o, index)); }
		public virtual DateTimeOffset GetDateTimeOffset(object o, int index) { return Map.DefaultSchema.ConvertToDateTimeOffset(GetValue(o, index)); }

		// Nullable type getters.
		//
		[CLSCompliant(false)]
		public virtual SByte?    GetNullableSByte   (object o, int index) { return Map.DefaultSchema.ConvertToNullableSByte   (GetValue(o, index)); }
		public virtual Int16?    GetNullableInt16   (object o, int index) { return Map.DefaultSchema.ConvertToNullableInt16   (GetValue(o, index)); }
		public virtual Int32?    GetNullableInt32   (object o, int index) { return Map.DefaultSchema.ConvertToNullableInt32   (GetValue(o, index)); }
		public virtual Int64?    GetNullableInt64   (object o, int index) { return Map.DefaultSchema.ConvertToNullableInt64   (GetValue(o, index)); }

		public virtual Byte?     GetNullableByte    (object o, int index) { return Map.DefaultSchema.ConvertToNullableByte    (GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt16?   GetNullableUInt16  (object o, int index) { return Map.DefaultSchema.ConvertToNullableUInt16  (GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt32?   GetNullableUInt32  (object o, int index) { return Map.DefaultSchema.ConvertToNullableUInt32  (GetValue(o, index)); }
		[CLSCompliant(false)]
		public virtual UInt64?   GetNullableUInt64  (object o, int index) { return Map.DefaultSchema.ConvertToNullableUInt64  (GetValue(o, index)); }

		public virtual Boolean?  GetNullableBoolean (object o, int index) { return Map.DefaultSchema.ConvertToNullableBoolean (GetValue(o, index)); }
		public virtual Char?     GetNullableChar    (object o, int index) { return Map.DefaultSchema.ConvertToNullableChar    (GetValue(o, index)); }
		public virtual Single?   GetNullableSingle  (object o, int index) { return Map.DefaultSchema.ConvertToNullableSingle  (GetValue(o, index)); }
		public virtual Double?   GetNullableDouble  (object o, int index) { return Map.DefaultSchema.ConvertToNullableDouble  (GetValue(o, index)); }
		public virtual Decimal?  GetNullableDecimal (object o, int index) { return Map.DefaultSchema.ConvertToNullableDecimal (GetValue(o, index)); }
		public virtual Guid?     GetNullableGuid    (object o, int index) { return Map.DefaultSchema.ConvertToNullableGuid    (GetValue(o, index)); }
		public virtual DateTime? GetNullableDateTime(object o, int index) { return Map.DefaultSchema.ConvertToNullableDateTime(GetValue(o, index)); }
		public virtual DateTimeOffset? GetNullableDateTimeOffset(object o, int index) { return Map.DefaultSchema.ConvertToNullableDateTimeOffset(GetValue(o, index)); }

#if !SILVERLIGHT

		// SQL type getters.
		//
		public virtual SqlByte     GetSqlByte     (object o, int index) { return Map.DefaultSchema.ConvertToSqlByte    (GetValue(o, index)); }
		public virtual SqlInt16    GetSqlInt16    (object o, int index) { return Map.DefaultSchema.ConvertToSqlInt16   (GetValue(o, index)); }
		public virtual SqlInt32    GetSqlInt32    (object o, int index) { return Map.DefaultSchema.ConvertToSqlInt32   (GetValue(o, index)); }
		public virtual SqlInt64    GetSqlInt64    (object o, int index) { return Map.DefaultSchema.ConvertToSqlInt64   (GetValue(o, index)); }
		public virtual SqlSingle   GetSqlSingle   (object o, int index) { return Map.DefaultSchema.ConvertToSqlSingle  (GetValue(o, index)); }
		public virtual SqlBoolean  GetSqlBoolean  (object o, int index) { return Map.DefaultSchema.ConvertToSqlBoolean (GetValue(o, index)); }
		public virtual SqlDouble   GetSqlDouble   (object o, int index) { return Map.DefaultSchema.ConvertToSqlDouble  (GetValue(o, index)); }
		public virtual SqlDateTime GetSqlDateTime (object o, int index) { return Map.DefaultSchema.ConvertToSqlDateTime(GetValue(o, index)); }
		public virtual SqlDecimal  GetSqlDecimal  (object o, int index) { return Map.DefaultSchema.ConvertToSqlDecimal (GetValue(o, index)); }
		public virtual SqlMoney    GetSqlMoney    (object o, int index) { return Map.DefaultSchema.ConvertToSqlMoney   (GetValue(o, index)); }
		public virtual SqlGuid     GetSqlGuid     (object o, int index) { return Map.DefaultSchema.ConvertToSqlGuid    (GetValue(o, index)); }
		public virtual SqlString   GetSqlString   (object o, int index) { return Map.DefaultSchema.ConvertToSqlString  (GetValue(o, index)); }

#endif

		#endregion
	}
}
