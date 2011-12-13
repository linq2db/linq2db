using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

using LinqToDB.Common;

namespace LinqToDB.Mapping
{
	public class ScalarListMapper<T> : MapDataDestinationBase
	{
		public ScalarListMapper(MappingSchema mappingSchema, IList<T> list)
		{
			_list           = list;
			_mappingSchema  = mappingSchema;
			_nullValue      = (T)mappingSchema.GetNullValue(_type);
			_isNullable     = _type.IsGenericType && _type.GetGenericTypeDefinition() == typeof(Nullable<>);
			_underlyingType = _isNullable? Nullable.GetUnderlyingType(_type): _type;
		}

		private readonly IList<T>      _list;
		private readonly MappingSchema _mappingSchema;
		private readonly T             _nullValue;
		private readonly bool          _isNullable;
		private readonly Type          _type = typeof(T);
		private readonly Type          _underlyingType;

		#region IMapDataDestination Members

		public override Type GetFieldType(int index)   { return _type; }
		public override int  GetOrdinal  (string name) { return 0; }
		public override void SetValue    (object o, int    index, object value) { _list.Add((T)_mappingSchema.ConvertChangeType(value, _underlyingType, _isNullable)); }
		public override void SetValue    (object o, string name,  object value) { _list.Add((T)_mappingSchema.ConvertChangeType(value, _underlyingType, _isNullable)); }

		public override void SetNull     (object o, int index) { _list.Add(_nullValue); }

		public override bool SupportsTypedValues(int index)    { return true; }

		// Simple types setters.
		//
		[CLSCompliant(false)]
		public override void SetSByte    (object o, int index, SByte    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetInt16    (object o, int index, Int16    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetInt32    (object o, int index, Int32    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetInt64    (object o, int index, Int64    value) { _list.Add(ConvertTo<T>.From(value)); }

		public override void SetByte     (object o, int index, Byte     value) { _list.Add(ConvertTo<T>.From(value)); }
		[CLSCompliant(false)]
		public override void SetUInt16   (object o, int index, UInt16   value) { _list.Add(ConvertTo<T>.From(value)); }
		[CLSCompliant(false)]
		public override void SetUInt32   (object o, int index, UInt32   value) { _list.Add(ConvertTo<T>.From(value)); }
		[CLSCompliant(false)]
		public override void SetUInt64   (object o, int index, UInt64   value) { _list.Add(ConvertTo<T>.From(value)); }

		public override void SetBoolean  (object o, int index, Boolean  value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetChar     (object o, int index, Char     value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSingle   (object o, int index, Single   value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetDouble   (object o, int index, Double   value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetDecimal  (object o, int index, Decimal  value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetGuid     (object o, int index, Guid     value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetDateTime (object o, int index, DateTime value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetDateTimeOffset(object o, int index, DateTimeOffset value) { _list.Add(ConvertTo<T>.From(value)); }

		// Nullable types setters.
		//
		[CLSCompliant(false)]
		public override void SetNullableSByte   (object o, int index, SByte?    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableInt16   (object o, int index, Int16?    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableInt32   (object o, int index, Int32?    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableInt64   (object o, int index, Int64?    value) { _list.Add(ConvertTo<T>.From(value)); }

		public override void SetNullableByte    (object o, int index, Byte?     value) { _list.Add(ConvertTo<T>.From(value)); }
		[CLSCompliant(false)]
		public override void SetNullableUInt16  (object o, int index, UInt16?   value) { _list.Add(ConvertTo<T>.From(value)); }
		[CLSCompliant(false)]
		public override void SetNullableUInt32  (object o, int index, UInt32?   value) { _list.Add(ConvertTo<T>.From(value)); }
		[CLSCompliant(false)]
		public override void SetNullableUInt64  (object o, int index, UInt64?   value) { _list.Add(ConvertTo<T>.From(value)); }

		public override void SetNullableBoolean (object o, int index, Boolean?  value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableChar    (object o, int index, Char?     value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableSingle  (object o, int index, Single?   value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableDouble  (object o, int index, Double?   value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableDecimal (object o, int index, Decimal?  value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableGuid    (object o, int index, Guid?     value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableDateTime(object o, int index, DateTime? value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetNullableDateTimeOffset(object o, int index, DateTimeOffset? value) { _list.Add(ConvertTo<T>.From(value)); }

#if !SILVERLIGHT

		// SQL type setters.
		//
		public override void SetSqlByte    (object o, int index, SqlByte     value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlInt16   (object o, int index, SqlInt16    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlInt32   (object o, int index, SqlInt32    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlInt64   (object o, int index, SqlInt64    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlSingle  (object o, int index, SqlSingle   value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlBoolean (object o, int index, SqlBoolean  value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlDouble  (object o, int index, SqlDouble   value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlDateTime(object o, int index, SqlDateTime value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlDecimal (object o, int index, SqlDecimal  value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlMoney   (object o, int index, SqlMoney    value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlGuid    (object o, int index, SqlGuid     value) { _list.Add(ConvertTo<T>.From(value)); }
		public override void SetSqlString  (object o, int index, SqlString   value) { _list.Add(ConvertTo<T>.From(value)); }

#endif

		#endregion
	}
}
