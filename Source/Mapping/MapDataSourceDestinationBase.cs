using System;
using System.Data.SqlTypes;

namespace LinqToDB.Mapping
{
	public abstract class MapDataSourceDestinationBase : MapDataSourceBase, IMapDataDestination
	{
		#region IMapDataDestination Members

		public abstract void SetValue  (object o, int index, object value);
		public abstract void SetValue  (object o, string name, object value);

		public virtual  void SetNull   (object o, int index)                { SetValue(o, index, null); }

		// Simple types setters.
		//
		[CLSCompliant(false)]
		public virtual void SetSByte   (object o, int index, SByte    value) { SetValue(o, index, value); }
		public virtual void SetInt16   (object o, int index, Int16    value) { SetValue(o, index, value); }
		public virtual void SetInt32   (object o, int index, Int32    value) { SetValue(o, index, value); }
		public virtual void SetInt64   (object o, int index, Int64    value) { SetValue(o, index, value); }

		public virtual void SetByte    (object o, int index, Byte     value) { SetValue(o, index, value); }
		[CLSCompliant(false)]
		public virtual void SetUInt16  (object o, int index, UInt16   value) { SetValue(o, index, value); }
		[CLSCompliant(false)]
		public virtual void SetUInt32  (object o, int index, UInt32   value) { SetValue(o, index, value); }
		[CLSCompliant(false)]
		public virtual void SetUInt64  (object o, int index, UInt64   value) { SetValue(o, index, value); }

		public virtual void SetBoolean (object o, int index, Boolean  value) { SetValue(o, index, value); }
		public virtual void SetChar    (object o, int index, Char     value) { SetValue(o, index, value); }
		public virtual void SetSingle  (object o, int index, Single   value) { SetValue(o, index, value); }
		public virtual void SetDouble  (object o, int index, Double   value) { SetValue(o, index, value); }
		public virtual void SetDecimal (object o, int index, Decimal  value) { SetValue(o, index, value); }
		public virtual void SetGuid    (object o, int index, Guid     value) { SetValue(o, index, value); }
		public virtual void SetDateTime(object o, int index, DateTime value) { SetValue(o, index, value); }
		public virtual void SetDateTimeOffset(object o, int index, DateTimeOffset value) { SetValue(o, index, value); }

		// Nullable types setters.
		//
		[CLSCompliant(false)]
		public virtual void SetNullableSByte   (object o, int index, SByte?    value) { SetValue(o, index, value); }
		public virtual void SetNullableInt16   (object o, int index, Int16?    value) { SetValue(o, index, value); }
		public virtual void SetNullableInt32   (object o, int index, Int32?    value) { SetValue(o, index, value); }
		public virtual void SetNullableInt64   (object o, int index, Int64?    value) { SetValue(o, index, value); }

		public virtual void SetNullableByte    (object o, int index, Byte?     value) { SetValue(o, index, value); }
		[CLSCompliant(false)]
		public virtual void SetNullableUInt16  (object o, int index, UInt16?   value) { SetValue(o, index, value); }
		[CLSCompliant(false)]
		public virtual void SetNullableUInt32  (object o, int index, UInt32?   value) { SetValue(o, index, value); }
		[CLSCompliant(false)]
		public virtual void SetNullableUInt64  (object o, int index, UInt64?   value) { SetValue(o, index, value); }

		public virtual void SetNullableBoolean (object o, int index, Boolean?  value) { SetValue(o, index, value); }
		public virtual void SetNullableChar    (object o, int index, Char?     value) { SetValue(o, index, value); }
		public virtual void SetNullableSingle  (object o, int index, Single?   value) { SetValue(o, index, value); }
		public virtual void SetNullableDouble  (object o, int index, Double?   value) { SetValue(o, index, value); }
		public virtual void SetNullableDecimal (object o, int index, Decimal?  value) { SetValue(o, index, value); }
		public virtual void SetNullableGuid    (object o, int index, Guid?     value) { SetValue(o, index, value); }
		public virtual void SetNullableDateTime(object o, int index, DateTime? value) { SetValue(o, index, value); }
		public virtual void SetNullableDateTimeOffset(object o, int index, DateTimeOffset? value) { SetValue(o, index, value); }

#if !SILVERLIGHT

		// SQL type setters.
		//
		public virtual void SetSqlByte    (object o, int index, SqlByte     value) { SetValue(o, index, value); }
		public virtual void SetSqlInt16   (object o, int index, SqlInt16    value) { SetValue(o, index, value); }
		public virtual void SetSqlInt32   (object o, int index, SqlInt32    value) { SetValue(o, index, value); }
		public virtual void SetSqlInt64   (object o, int index, SqlInt64    value) { SetValue(o, index, value); }
		public virtual void SetSqlSingle  (object o, int index, SqlSingle   value) { SetValue(o, index, value); }
		public virtual void SetSqlBoolean (object o, int index, SqlBoolean  value) { SetValue(o, index, value); }
		public virtual void SetSqlDouble  (object o, int index, SqlDouble   value) { SetValue(o, index, value); }
		public virtual void SetSqlDateTime(object o, int index, SqlDateTime value) { SetValue(o, index, value); }
		public virtual void SetSqlDecimal (object o, int index, SqlDecimal  value) { SetValue(o, index, value); }
		public virtual void SetSqlMoney   (object o, int index, SqlMoney    value) { SetValue(o, index, value); }
		public virtual void SetSqlGuid    (object o, int index, SqlGuid     value) { SetValue(o, index, value); }
		public virtual void SetSqlString  (object o, int index, SqlString   value) { SetValue(o, index, value); }

#endif

		#endregion
	}
}
