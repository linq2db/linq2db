using System;
using System.Data.SqlTypes;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace LinqToDB.Reflection
{
	using Common;

	abstract class ExprMemberAccessor : MemberAccessor
	{
		protected ExprMemberAccessor(TypeAccessor typeAccessor, MemberInfo memberInfo)
			: base(typeAccessor, memberInfo)
		{
		}

		#region Public Properties

		protected bool HasSetterValue; 
		public override bool HasGetter { get { return true;           } }
		public override bool HasSetter { get { return HasSetterValue; } }

		#endregion

		static public MemberAccessor GetMemberAccessor(TypeAccessor typeAccessor, string memberName)
		{
			var par  = Expression.Parameter(typeof(object), "obj");
			var expr = Expression.PropertyOrField(Expression.Convert(par, typeAccessor.Type), memberName);

			var type = expr.Type;

			var underlyingType = type;
			var isNullable     = false;

			if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				underlyingType = underlyingType.GetGenericArguments()[0];
				isNullable     = true;
			}

			if (underlyingType.IsEnum)
				underlyingType = Enum.GetUnderlyingType(underlyingType);

			if (isNullable)
			{
				switch (Type.GetTypeCode(underlyingType))
				{
					case TypeCode.Boolean  : return new NullableBooleanAccessor (typeAccessor, par, expr);
					case TypeCode.Char     : return new NullableCharAccessor    (typeAccessor, par, expr);
					case TypeCode.SByte    : return new NullableSByteAccessor   (typeAccessor, par, expr);
					case TypeCode.Byte     : return new NullableByteAccessor    (typeAccessor, par, expr);
					case TypeCode.Int16    : return new NullableInt16Accessor   (typeAccessor, par, expr);
					case TypeCode.UInt16   : return new NullableUInt16Accessor  (typeAccessor, par, expr);
					case TypeCode.Int32    : return new NullableInt32Accessor   (typeAccessor, par, expr);
					case TypeCode.UInt32   : return new NullableUInt32Accessor  (typeAccessor, par, expr);
					case TypeCode.Int64    : return new NullableInt64Accessor   (typeAccessor, par, expr);
					case TypeCode.UInt64   : return new NullableUInt64Accessor  (typeAccessor, par, expr);
					case TypeCode.Single   : return new NullableSingleAccessor  (typeAccessor, par, expr);
					case TypeCode.Double   : return new NullableDoubleAccessor  (typeAccessor, par, expr);
					case TypeCode.Decimal  : return new NullableDecimalAccessor (typeAccessor, par, expr);
					case TypeCode.DateTime : return new NullableDateTimeAccessor(typeAccessor, par, expr);
					case TypeCode.Object   :
						if (type == typeof(Guid))           return new NullableGuidAccessor          (typeAccessor, par, expr);
						if (type == typeof(DateTimeOffset)) return new NullableDateTimeOffsetAccessor(typeAccessor, par, expr);
						if (type == typeof(TimeSpan))       return new NullableTimeSpanAccessor      (typeAccessor, par, expr);
						break;
					default                : break;
				}
			}
			else
			{
				switch (Type.GetTypeCode(underlyingType))
				{
					case TypeCode.Boolean  : return new BooleanAccessor (typeAccessor, par, expr);
					case TypeCode.Char     : return new CharAccessor    (typeAccessor, par, expr);
					case TypeCode.SByte    : return new SByteAccessor   (typeAccessor, par, expr);
					case TypeCode.Byte     : return new ByteAccessor    (typeAccessor, par, expr);
					case TypeCode.Int16    : return new Int16Accessor   (typeAccessor, par, expr);
					case TypeCode.UInt16   : return new UInt16Accessor  (typeAccessor, par, expr);
					case TypeCode.Int32    : return new Int32Accessor   (typeAccessor, par, expr);
					case TypeCode.UInt32   : return new UInt32Accessor  (typeAccessor, par, expr);
					case TypeCode.Int64    : return new Int64Accessor   (typeAccessor, par, expr);
					case TypeCode.UInt64   : return new UInt64Accessor  (typeAccessor, par, expr);
					case TypeCode.Single   : return new SingleAccessor  (typeAccessor, par, expr);
					case TypeCode.Double   : return new DoubleAccessor  (typeAccessor, par, expr);
					case TypeCode.Decimal  : return new DecimalAccessor (typeAccessor, par, expr);
					case TypeCode.DateTime : return new DateTimeAccessor(typeAccessor, par, expr);
					case TypeCode.Object   :
						if (type == typeof(Guid))           return new GuidAccessor          (typeAccessor, par, expr);
						if (type == typeof(DateTimeOffset)) return new DateTimeOffsetAccessor(typeAccessor, par, expr);
						if (type == typeof(TimeSpan))       return new TimeSpanAccessor      (typeAccessor, par, expr);
						break;
					default                : break;
				}
			}

#if !SILVERLIGHT

			if (type == typeof(SqlByte))     return new SqlByteAccessor    (typeAccessor, par, expr);
			if (type == typeof(SqlInt16))    return new SqlInt16Accessor   (typeAccessor, par, expr);
			if (type == typeof(SqlInt32))    return new SqlInt32Accessor   (typeAccessor, par, expr);
			if (type == typeof(SqlInt64))    return new SqlInt64Accessor   (typeAccessor, par, expr);
			if (type == typeof(SqlSingle))   return new SqlSingleAccessor  (typeAccessor, par, expr);
			if (type == typeof(SqlBoolean))  return new SqlBooleanAccessor (typeAccessor, par, expr);
			if (type == typeof(SqlDouble))   return new SqlDoubleAccessor  (typeAccessor, par, expr);
			if (type == typeof(SqlDateTime)) return new SqlDateTimeAccessor(typeAccessor, par, expr);
			if (type == typeof(SqlDecimal))  return new SqlDecimalAccessor (typeAccessor, par, expr);
			if (type == typeof(SqlMoney))    return new SqlMoneyAccessor   (typeAccessor, par, expr);
			if (type == typeof(SqlString))   return new SqlStringAccessor  (typeAccessor, par, expr);
			if (type == typeof(SqlGuid))     return new SqlGuidAccessor    (typeAccessor, par, expr);

#endif

			return (MemberAccessor)Activator.CreateInstance(
				typeof(BaseAccessor<>).MakeGenericType(type),
				new object[] { typeAccessor, par, expr });
		}

		class BaseAccessor<T> : ExprMemberAccessor
		{
			protected readonly Func  <object,T> Getter;
			protected readonly Action<object,T> Setter;

			static int _counter;

			public BaseAccessor(TypeAccessor typeAccessor, ParameterExpression par, MemberExpression expr)
				: base(typeAccessor, expr.Member)
			{
				Expression ex = expr;

				if (ex.Type.IsEnum && ex.Type != typeof(T))
					ex = Expression.Convert(ex, typeof(T));

				Getter = Expression.Lambda<Func<object,T>>(ex, par).Compile();

				var mi = expr.Member;

				HasSetterValue = !(mi is PropertyInfo) || ((PropertyInfo)mi).GetSetMethod(true) != null;

				if (HasSetterValue)
				{
					var dm = new DynamicMethod(
						"setter_" + mi.Name + "_" + ++_counter,
						typeof(void),
						new[] { typeof(object), typeof(T) },
						typeAccessor.Type);
					var il = dm.GetILGenerator();

					il.Emit(OpCodes.Ldarg_0);
					il.Emit(typeAccessor.Type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, typeAccessor.Type);
					il.Emit(OpCodes.Ldarg_1);

					if (mi is FieldInfo) 
						il.Emit(OpCodes.Stfld,    (FieldInfo)mi);
					else
						il.Emit(OpCodes.Callvirt, ((PropertyInfo)mi).GetSetMethod(true));

					il.Emit(OpCodes.Ret);

					Setter = (Action<object,T>)dm.CreateDelegate(typeof(Action<object,T>));
				}
				else
				{
					Setter = (_,__) => {};
				}
			}

			public override object GetValue(object obj)
			{
				return Getter(obj);
			}

			public override void SetValue(object obj, object value)
			{
				Setter(obj, ConvertTo<T>.From(value));
			}
		}

		class NullableAccessor<T> : BaseAccessor<T?>
			where T : struct
		{
			public NullableAccessor(TypeAccessor typeAccessor, ParameterExpression par, MemberExpression member)
				: base(typeAccessor, par, member)
			{
			}

			public override bool IsNull(object obj)
			{
				return Getter(obj) == null;
			}
		}

		#region Basic Types

		class BooleanAccessor : BaseAccessor<Boolean>
		{
			public BooleanAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Boolean GetBoolean(object obj)                { return Getter(obj); }
			public override void    SetBoolean(object obj, Boolean value) { Setter(obj, value); }
		}

		class CharAccessor : BaseAccessor<Char>
		{
			public CharAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Char GetChar(object obj)             { return Getter(obj); }
			public override void SetChar(object obj, Char value) { Setter(obj, value); }
		}

		class SByteAccessor : BaseAccessor<SByte>
		{
			public SByteAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SByte GetSByte(object obj)              { return Getter(obj); }
			public override void  SetSByte(object obj, SByte value) { Setter(obj, value); }
		}

		class ByteAccessor : BaseAccessor<Byte>
		{
			public ByteAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Byte GetByte(object obj)             { return Getter(obj); }
			public override void SetByte(object obj, Byte value) { Setter(obj, value); }
		}

		class Int16Accessor : BaseAccessor<Int16>
		{
			public Int16Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Int16 GetInt16(object obj)              { return Getter(obj); }
			public override void  SetInt16(object obj, Int16 value) { Setter(obj, value); }
		}

		class UInt16Accessor : BaseAccessor<UInt16>
		{
			public UInt16Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override UInt16 GetUInt16(object obj)               { return Getter(obj); }
			public override void   SetUInt16(object obj, UInt16 value) { Setter(obj, value); }
		}

		class Int32Accessor : BaseAccessor<Int32>
		{
			public Int32Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Int32 GetInt32(object obj)              { return Getter(obj); }
			public override void  SetInt32(object obj, Int32 value) { Setter(obj, value); }
		}

		class UInt32Accessor : BaseAccessor<UInt32>
		{
			public UInt32Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override UInt32 GetUInt32(object obj)               { return Getter(obj); }
			public override void   SetUInt32(object obj, UInt32 value) { Setter(obj, value); }
		}

		class Int64Accessor : BaseAccessor<Int64>
		{
			public Int64Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Int64 GetInt64(object obj)              { return Getter(obj); }
			public override void  SetInt64(object obj, Int64 value) { Setter(obj, value); }
		}

		class UInt64Accessor : BaseAccessor<UInt64>
		{
			public UInt64Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override UInt64 GetUInt64(object obj)               { return Getter(obj); }
			public override void   SetUInt64(object obj, UInt64 value) { Setter(obj, value); }
		}

		class SingleAccessor : BaseAccessor<Single>
		{
			public SingleAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Single GetSingle(object obj)               { return Getter(obj); }
			public override void   SetSingle(object obj, Single value) { Setter(obj, value); }
		}

		class DoubleAccessor : BaseAccessor<Double>
		{
			public DoubleAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Double GetDouble(object obj)               { return Getter(obj); }
			public override void   SetDouble(object obj, Double value) { Setter(obj, value); }
		}

		class DecimalAccessor : BaseAccessor<Decimal>
		{
			public DecimalAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Decimal GetDecimal(object obj)                { return Getter(obj); }
			public override void    SetDecimal(object obj, Decimal value) { Setter(obj, value); }
		}

		class DateTimeAccessor : BaseAccessor<DateTime>
		{
			public DateTimeAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override DateTime GetDateTime(object obj)                 { return Getter(obj); }
			public override void     SetDateTime(object obj, DateTime value) { Setter(obj, value); }
		}

		class GuidAccessor : BaseAccessor<Guid>
		{
			public GuidAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Guid GetGuid(object obj)             { return Getter(obj); }
			public override void SetGuid(object obj, Guid value) { Setter(obj, value); }
		}

		class DateTimeOffsetAccessor : BaseAccessor<DateTimeOffset>
		{
			public DateTimeOffsetAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override DateTimeOffset GetDateTimeOffset(object obj)                       { return Getter(obj); }
			public override void           SetDateTimeOffset(object obj, DateTimeOffset value) { Setter(obj, value); }
		}

		class TimeSpanAccessor : BaseAccessor<TimeSpan>
		{
			public TimeSpanAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override TimeSpan GetTimeSpan(object obj)                 { return Getter(obj); }
			public override void     SetTimeSpan(object obj, TimeSpan value) { Setter(obj, value); }
		}

		#endregion

		#region Nullable

		class NullableBooleanAccessor : NullableAccessor<Boolean>
		{
			public NullableBooleanAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Boolean? GetNullableBoolean(object obj)                 { return Getter(obj); }
			public override void     SetNullableBoolean(object obj, Boolean? value) { Setter(obj, value); }
		}

		class NullableCharAccessor : NullableAccessor<Char>
		{
			public NullableCharAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Char? GetNullableChar(object obj)              { return Getter(obj); }
			public override void  SetNullableChar(object obj, Char? value) { Setter(obj, value); }
		}

		class NullableSByteAccessor : NullableAccessor<SByte>
		{
			public NullableSByteAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SByte? GetNullableSByte(object obj)               { return Getter(obj); }
			public override void   SetNullableSByte(object obj, SByte? value) { Setter(obj, value); }
		}

		class NullableByteAccessor : NullableAccessor<Byte>
		{
			public NullableByteAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Byte? GetNullableByte(object obj)              { return Getter(obj); }
			public override void  SetNullableByte(object obj, Byte? value) { Setter(obj, value); }
		}

		class NullableInt16Accessor : NullableAccessor<Int16>
		{
			public NullableInt16Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Int16? GetNullableInt16(object obj)               { return Getter(obj); }
			public override void   SetNullableInt16(object obj, Int16? value) { Setter(obj, value); }
		}

		class NullableUInt16Accessor : NullableAccessor<UInt16>
		{
			public NullableUInt16Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override UInt16? GetNullableUInt16(object obj)                { return Getter(obj); }
			public override void    SetNullableUInt16(object obj, UInt16? value) { Setter(obj, value); }
		}

		class NullableInt32Accessor : NullableAccessor<Int32>
		{
			public NullableInt32Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Int32? GetNullableInt32(object obj)               { return Getter(obj); }
			public override void   SetNullableInt32(object obj, Int32? value) { Setter(obj, value); }
		}

		class NullableUInt32Accessor : NullableAccessor<UInt32>
		{
			public NullableUInt32Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override UInt32? GetNullableUInt32(object obj)                { return Getter(obj); }
			public override void    SetNullableUInt32(object obj, UInt32? value) { Setter(obj, value); }
		}

		class NullableInt64Accessor : NullableAccessor<Int64>
		{
			public NullableInt64Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Int64? GetNullableInt64(object obj)               { return Getter(obj); }
			public override void   SetNullableInt64(object obj, Int64? value) { Setter(obj, value); }
		}

		class NullableUInt64Accessor : NullableAccessor<UInt64>
		{
			public NullableUInt64Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override UInt64? GetNullableUInt64(object obj)                { return Getter(obj); }
			public override void    SetNullableUInt64(object obj, UInt64? value) { Setter(obj, value); }
		}

		class NullableSingleAccessor : NullableAccessor<Single>
		{
			public NullableSingleAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Single? GetNullableSingle(object obj)                { return Getter(obj); }
			public override void    SetNullableSingle(object obj, Single? value) { Setter(obj, value); }
		}

		class NullableDoubleAccessor : NullableAccessor<Double>
		{
			public NullableDoubleAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Double? GetNullableDouble(object obj)                { return Getter(obj); }
			public override void    SetNullableDouble(object obj, Double? value) { Setter(obj, value); }
		}

		class NullableDecimalAccessor : NullableAccessor<Decimal>
		{
			public NullableDecimalAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Decimal? GetNullableDecimal(object obj)                 { return Getter(obj); }
			public override void     SetNullableDecimal(object obj, Decimal? value) { Setter(obj, value); }
		}

		class NullableDateTimeAccessor : NullableAccessor<DateTime>
		{
			public NullableDateTimeAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override DateTime? GetNullableDateTime(object obj)                  { return Getter(obj); }
			public override void      SetNullableDateTime(object obj, DateTime? value) { Setter(obj, value); }
		}

		class NullableGuidAccessor : NullableAccessor<Guid>
		{
			public NullableGuidAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override Guid? GetNullableGuid(object obj)              { return Getter(obj); }
			public override void  SetNullableGuid(object obj, Guid? value) { Setter(obj, value); }
		}

		class NullableDateTimeOffsetAccessor : NullableAccessor<DateTimeOffset>
		{
			public NullableDateTimeOffsetAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override DateTimeOffset? GetNullableDateTimeOffset(object obj)                        { return Getter(obj); }
			public override void            SetNullableDateTimeOffset(object obj, DateTimeOffset? value) { Setter(obj, value); }
		}

		class NullableTimeSpanAccessor : NullableAccessor<TimeSpan>
		{
			public NullableTimeSpanAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override TimeSpan? GetNullableTimeSpan(object obj)                  { return Getter(obj); }
			public override void      SetNullableTimeSpan(object obj, TimeSpan? value) { Setter(obj, value); }
		}

		#endregion

		#region Sql Types

#if !SILVERLIGHT

		class SqlByteAccessor : BaseAccessor<SqlByte>
		{
			public SqlByteAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlByte GetSqlByte(object obj)                { return Getter(obj); }
			public override void    SetSqlByte(object obj, SqlByte value) { Setter(obj, value); }
		}

		class SqlInt16Accessor : BaseAccessor<SqlInt16>
		{
			public SqlInt16Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlInt16 GetSqlInt16(object obj)                 { return Getter(obj); }
			public override void     SetSqlInt16(object obj, SqlInt16 value) { Setter(obj, value); }
		}

		class SqlInt32Accessor : BaseAccessor<SqlInt32>
		{
			public SqlInt32Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlInt32 GetSqlInt32(object obj)                 { return Getter(obj); }
			public override void     SetSqlInt32(object obj, SqlInt32 value) { Setter(obj, value); }
		}

		class SqlInt64Accessor : BaseAccessor<SqlInt64>
		{
			public SqlInt64Accessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlInt64 GetSqlInt64(object obj)                 { return Getter(obj); }
			public override void     SetSqlInt64(object obj, SqlInt64 value) { Setter(obj, value); }
		}

		class SqlSingleAccessor : BaseAccessor<SqlSingle>
		{
			public SqlSingleAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlSingle GetSqlSingle(object obj)                  { return Getter(obj); }
			public override void      SetSqlSingle(object obj, SqlSingle value) { Setter(obj, value); }
		}

		class SqlBooleanAccessor : BaseAccessor<SqlBoolean>
		{
			public SqlBooleanAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlBoolean GetSqlBoolean(object obj)                   { return Getter(obj); }
			public override void       SetSqlBoolean(object obj, SqlBoolean value) { Setter(obj, value); }
		}

		class SqlDoubleAccessor : BaseAccessor<SqlDouble>
		{
			public SqlDoubleAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlDouble GetSqlDouble(object obj)                  { return Getter(obj); }
			public override void      SetSqlDouble(object obj, SqlDouble value) { Setter(obj, value); }
		}

		class SqlDateTimeAccessor : BaseAccessor<SqlDateTime>
		{
			public SqlDateTimeAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlDateTime GetSqlDateTime(object obj)                    { return Getter(obj); }
			public override void        SetSqlDateTime(object obj, SqlDateTime value) { Setter(obj, value); }
		}

		class SqlDecimalAccessor : BaseAccessor<SqlDecimal>
		{
			public SqlDecimalAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlDecimal GetSqlDecimal(object obj)                   { return Getter(obj); }
			public override void       SetSqlDecimal(object obj, SqlDecimal value) { Setter(obj, value); }
		}

		class SqlMoneyAccessor : BaseAccessor<SqlMoney>
		{
			public SqlMoneyAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlMoney GetSqlMoney(object obj)                 { return Getter(obj); }
			public override void     SetSqlMoney(object obj, SqlMoney value) { Setter(obj, value); }
		}

		class SqlStringAccessor : BaseAccessor<SqlString>
		{
			public SqlStringAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlString GetSqlString(object obj)                  { return Getter(obj); }
			public override void      SetSqlString(object obj, SqlString value) { Setter(obj, value); }
		}

		class SqlGuidAccessor : BaseAccessor<SqlGuid>
		{
			public SqlGuidAccessor(TypeAccessor accessor, ParameterExpression expression, MemberExpression expr)
				: base(accessor, expression, expr)
			{
			}

			public override SqlGuid GetSqlGuid(object obj)                 { return Getter(obj); }
			public override void     SetSqlGuid(object obj, SqlGuid value) { Setter(obj, value); }
		}

#endif

		#endregion
	}
}
