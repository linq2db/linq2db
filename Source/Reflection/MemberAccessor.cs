using System;
using System.Data.SqlTypes;
using System.Reflection;

namespace LinqToDB.Reflection
{
	public abstract class MemberAccessor
	{
		protected MemberAccessor(TypeAccessor typeAccessor, MemberInfo memberInfo)
		{
			TypeAccessor = typeAccessor;
			MemberInfo   = memberInfo;
		}

		#region Public Properties

		public MemberInfo   MemberInfo   { get; private set; }
		public TypeAccessor TypeAccessor { get; private set; }

		public virtual bool HasGetter { get { return false; } }
		public virtual bool HasSetter { get { return false; } }

		public Type Type
		{
			get
			{
				return MemberInfo is PropertyInfo?
					((PropertyInfo)MemberInfo).PropertyType:
					((FieldInfo)   MemberInfo).FieldType;
			}
		}

		public string Name
		{
			get { return MemberInfo.Name; }
		}

		private Type _underlyingType;
		public  Type  UnderlyingType
		{
			get { return _underlyingType ?? (_underlyingType = TypeHelper.GetUnderlyingType(Type)); }
		}

		#endregion

		#region Public Methods

		public bool IsDefined<T>() where T : Attribute
		{
			return MemberInfo.IsDefined(typeof(T), true);
		}

		public T GetAttribute<T>() where T : Attribute
		{
			var attrs = MemberInfo.GetCustomAttributes(typeof(T), true);

			return attrs.Length > 0? (T)attrs[0]: null;
		}

		public T[] GetAttributes<T>() where T : Attribute
		{
			Array attrs = MemberInfo.GetCustomAttributes(typeof(T), true);

			return attrs.Length > 0? (T[])attrs: null;
		}

		public object[] GetAttributes()
		{
			var attrs = MemberInfo.GetCustomAttributes(true);

			return attrs.Length > 0? attrs: null;
		}

		public object[] GetTypeAttributes(Type attributeType)
		{
			return TypeHelper.GetAttributes(TypeAccessor.OriginalType, attributeType);
		}

		#endregion

		#region Set/Get Value

		public virtual Boolean IsNull(object o)
		{
			return true;
		}

		public virtual object GetValue(object o)
		{
			return null;
		}

		public virtual void SetValue(object o, object value)
		{
		}

		public virtual void CloneValue(object source, object dest)
		{
			var value = GetValue(source);

			SetValue(dest, value is ICloneable? ((ICloneable)value).Clone(): value);
		}


		// Simple types getters.
		//
		[CLSCompliant(false)]
		public virtual SByte    GetSByte   (object o) { return (SByte)   GetValue(o); }
		public virtual Int16    GetInt16   (object o) { return (Int16)   GetValue(o); }
		public virtual Int32    GetInt32   (object o) { return (Int32)   GetValue(o); }
		public virtual Int64    GetInt64   (object o) { return (Int64)   GetValue(o); }

		public virtual Byte     GetByte    (object o) { return (Byte)    GetValue(o); }
		[CLSCompliant(false)]
		public virtual UInt16   GetUInt16  (object o) { return (UInt16)  GetValue(o); }
		[CLSCompliant(false)]
		public virtual UInt32   GetUInt32  (object o) { return (UInt32)  GetValue(o); }
		[CLSCompliant(false)]
		public virtual UInt64   GetUInt64  (object o) { return (UInt64)  GetValue(o); }

		public virtual Boolean  GetBoolean (object o) { return (Boolean) GetValue(o); }
		public virtual Char     GetChar    (object o) { return (Char)    GetValue(o); }
		public virtual Single   GetSingle  (object o) { return (Single)  GetValue(o); }
		public virtual Double   GetDouble  (object o) { return (Double)  GetValue(o); }
		public virtual Decimal  GetDecimal (object o) { return (Decimal) GetValue(o); }
		public virtual Guid     GetGuid    (object o) { return (Guid)    GetValue(o); }
		public virtual DateTime GetDateTime(object o) { return (DateTime)GetValue(o); }
		public virtual TimeSpan GetTimeSpan(object o) { return (TimeSpan)GetValue(o); }
		public virtual DateTimeOffset GetDateTimeOffset(object o) { return (DateTimeOffset)GetValue(o); }

		// Nullable types getters.
		//
		[CLSCompliant(false)]
		public virtual SByte?    GetNullableSByte   (object o) { return (SByte?)   GetValue(o); }
		public virtual Int16?    GetNullableInt16   (object o) { return (Int16?)   GetValue(o); }
		public virtual Int32?    GetNullableInt32   (object o) { return (Int32?)   GetValue(o); }
		public virtual Int64?    GetNullableInt64   (object o) { return (Int64?)   GetValue(o); }

		public virtual Byte?     GetNullableByte    (object o) { return (Byte?)    GetValue(o); }
		[CLSCompliant(false)]
		public virtual UInt16?   GetNullableUInt16  (object o) { return (UInt16?)  GetValue(o); }
		[CLSCompliant(false)]
		public virtual UInt32?   GetNullableUInt32  (object o) { return (UInt32?)  GetValue(o); }
		[CLSCompliant(false)]
		public virtual UInt64?   GetNullableUInt64  (object o) { return (UInt64?)  GetValue(o); }

		public virtual Boolean?  GetNullableBoolean (object o) { return (Boolean?) GetValue(o); }
		public virtual Char?     GetNullableChar    (object o) { return (Char?)    GetValue(o); }
		public virtual Single?   GetNullableSingle  (object o) { return (Single?)  GetValue(o); }
		public virtual Double?   GetNullableDouble  (object o) { return (Double?)  GetValue(o); }
		public virtual Decimal?  GetNullableDecimal (object o) { return (Decimal?) GetValue(o); }
		public virtual Guid?     GetNullableGuid    (object o) { return (Guid?)    GetValue(o); }
		public virtual DateTime? GetNullableDateTime(object o) { return (DateTime?)GetValue(o); }
		public virtual TimeSpan? GetNullableTimeSpan(object o) { return (TimeSpan?)GetValue(o); }
		public virtual DateTimeOffset? GetNullableDateTimeOffset(object o) { return (DateTimeOffset?)GetValue(o); }

#if !SILVERLIGHT

		// SQL type getters.
		//
		public virtual SqlByte     GetSqlByte    (object o) { return (SqlByte)    GetValue(o); }
		public virtual SqlInt16    GetSqlInt16   (object o) { return (SqlInt16)   GetValue(o); }
		public virtual SqlInt32    GetSqlInt32   (object o) { return (SqlInt32)   GetValue(o); }
		public virtual SqlInt64    GetSqlInt64   (object o) { return (SqlInt64)   GetValue(o); }
		public virtual SqlSingle   GetSqlSingle  (object o) { return (SqlSingle)  GetValue(o); }
		public virtual SqlBoolean  GetSqlBoolean (object o) { return (SqlBoolean) GetValue(o); }
		public virtual SqlDouble   GetSqlDouble  (object o) { return (SqlDouble)  GetValue(o); }
		public virtual SqlDateTime GetSqlDateTime(object o) { return (SqlDateTime)GetValue(o); }
		public virtual SqlDecimal  GetSqlDecimal (object o) { return (SqlDecimal) GetValue(o); }
		public virtual SqlMoney    GetSqlMoney   (object o) { return (SqlMoney)   GetValue(o); }
		public virtual SqlGuid     GetSqlGuid    (object o) { return (SqlGuid)    GetValue(o); }
		public virtual SqlString   GetSqlString  (object o) { return (SqlString)  GetValue(o); }

#endif

		// Simple type setters.
		//
		[CLSCompliant(false)]
		public virtual void    SetSByte   (object o, SByte    value) { SetValue(o, value); }
		public virtual void    SetInt16   (object o, Int16    value) { SetValue(o, value); }
		public virtual void    SetInt32   (object o, Int32    value) { SetValue(o, value); }
		public virtual void    SetInt64   (object o, Int64    value) { SetValue(o, value); }

		public virtual void    SetByte    (object o, Byte     value) { SetValue(o, value); }
		[CLSCompliant(false)]
		public virtual void    SetUInt16  (object o, UInt16   value) { SetValue(o, value); }
		[CLSCompliant(false)]
		public virtual void    SetUInt32  (object o, UInt32   value) { SetValue(o, value); }
		[CLSCompliant(false)]
		public virtual void    SetUInt64  (object o, UInt64   value) { SetValue(o, value); }

		public virtual void    SetBoolean (object o, Boolean  value) { SetValue(o, value); }
		public virtual void    SetChar    (object o, Char     value) { SetValue(o, value); }
		public virtual void    SetSingle  (object o, Single   value) { SetValue(o, value); }
		public virtual void    SetDouble  (object o, Double   value) { SetValue(o, value); }
		public virtual void    SetDecimal (object o, Decimal  value) { SetValue(o, value); }
		public virtual void    SetGuid    (object o, Guid     value) { SetValue(o, value); }
		public virtual void    SetDateTime(object o, DateTime value) { SetValue(o, value); }
		public virtual void    SetTimeSpan(object o, TimeSpan value) { SetValue(o, value); }
		public virtual void    SetDateTimeOffset(object o, DateTimeOffset value) { SetValue(o, value); }

		// Simple type setters.
		//
		[CLSCompliant(false)]
		public virtual void    SetNullableSByte   (object o, SByte?    value) { SetValue(o, value); }
		public virtual void    SetNullableInt16   (object o, Int16?    value) { SetValue(o, value); }
		public virtual void    SetNullableInt32   (object o, Int32?    value) { SetValue(o, value); }
		public virtual void    SetNullableInt64   (object o, Int64?    value) { SetValue(o, value); }

		public virtual void    SetNullableByte    (object o, Byte?     value) { SetValue(o, value); }
		[CLSCompliant(false)]
		public virtual void    SetNullableUInt16  (object o, UInt16?   value) { SetValue(o, value); }
		[CLSCompliant(false)]
		public virtual void    SetNullableUInt32  (object o, UInt32?   value) { SetValue(o, value); }
		[CLSCompliant(false)]
		public virtual void    SetNullableUInt64  (object o, UInt64?   value) { SetValue(o, value); }

		public virtual void    SetNullableBoolean (object o, Boolean?  value) { SetValue(o, value); }
		public virtual void    SetNullableChar    (object o, Char?     value) { SetValue(o, value); }
		public virtual void    SetNullableSingle  (object o, Single?   value) { SetValue(o, value); }
		public virtual void    SetNullableDouble  (object o, Double?   value) { SetValue(o, value); }
		public virtual void    SetNullableDecimal (object o, Decimal?  value) { SetValue(o, value); }
		public virtual void    SetNullableGuid    (object o, Guid?     value) { SetValue(o, value); }
		public virtual void    SetNullableDateTime(object o, DateTime? value) { SetValue(o, value); }
		public virtual void    SetNullableTimeSpan(object o, TimeSpan? value) { SetValue(o, value); }
		public virtual void    SetNullableDateTimeOffset(object o, DateTimeOffset? value) { SetValue(o, value); }

#if !SILVERLIGHT

		// SQL type setters.
		//
		public virtual void SetSqlByte    (object o, SqlByte     value) { SetValue(o, value); }
		public virtual void SetSqlInt16   (object o, SqlInt16    value) { SetValue(o, value); }
		public virtual void SetSqlInt32   (object o, SqlInt32    value) { SetValue(o, value); }
		public virtual void SetSqlInt64   (object o, SqlInt64    value) { SetValue(o, value); }
		public virtual void SetSqlSingle  (object o, SqlSingle   value) { SetValue(o, value); }
		public virtual void SetSqlBoolean (object o, SqlBoolean  value) { SetValue(o, value); }
		public virtual void SetSqlDouble  (object o, SqlDouble   value) { SetValue(o, value); }
		public virtual void SetSqlDateTime(object o, SqlDateTime value) { SetValue(o, value); }
		public virtual void SetSqlDecimal (object o, SqlDecimal  value) { SetValue(o, value); }
		public virtual void SetSqlMoney   (object o, SqlMoney    value) { SetValue(o, value); }
		public virtual void SetSqlGuid    (object o, SqlGuid     value) { SetValue(o, value); }
		public virtual void SetSqlString  (object o, SqlString   value) { SetValue(o, value); }

#endif

		#endregion
	}
}
