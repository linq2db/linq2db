using System;
using System.Data.SqlTypes;

using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	public static class MapSetData<T>
	{
		public abstract class MB<V>
		{
			public abstract void To(IMapDataDestination d, object o, int i, V v);
		}

		public static void To(IMapDataDestination d, object o, int i, T v)
		{
			I.To(d, o, i, v);
		}

		public  static MB<T> I = GetSetter();
		private static MB<T>     GetSetter()
		{
			Type t = typeof(T);

			// Scalar Types.
			//
			if (t == typeof(SByte))        return (MB<T>)(object)(new I8());
			if (t == typeof(Int16))        return (MB<T>)(object)(new I16());
			if (t == typeof(Int32))        return (MB<T>)(object)(new I32());
			if (t == typeof(Int64))        return (MB<T>)(object)(new I64());

			if (t == typeof(Byte))         return (MB<T>)(object)(new U8());
			if (t == typeof(UInt16))       return (MB<T>)(object)(new U16());
			if (t == typeof(UInt32))       return (MB<T>)(object)(new U32());
			if (t == typeof(UInt64))       return (MB<T>)(object)(new U64());

			if (t == typeof(Single))       return (MB<T>)(object)(new R4());
			if (t == typeof(Double))       return (MB<T>)(object)(new R8());

			if (t == typeof(Boolean))      return (MB<T>)(object)(new B());
			if (t == typeof(Decimal))      return (MB<T>)(object)(new D());

			if (t == typeof(Char))         return (MB<T>)(object)(new C());
			if (t == typeof(Guid))         return (MB<T>)(object)(new G());
			if (t == typeof(DateTime))     return (MB<T>)(object)(new DT());
			if (t == typeof(DateTimeOffset)) return (MB<T>)(object)(new DTO());

			// Enums.
			//
			if (t.IsEnum)
			{
				t = Enum.GetUnderlyingType(t);

				if (t == typeof(SByte))        return new EI8<T>();
				if (t == typeof(Int16))        return new EI16<T>();
				if (t == typeof(Int32))        return new EI32<T>();
				if (t == typeof(Int64))        return new EI64<T>();

				if (t == typeof(Byte))         return new EU8<T>();
				if (t == typeof(UInt16))       return new EU16<T>();
				if (t == typeof(UInt32))       return new EU32<T>();
				if (t == typeof(UInt64))       return new EU64<T>();
			}

			// Nullable Types.
			//
			if (t == typeof(SByte?))       return (MB<T>)(object)(new NI8());
			if (t == typeof(Int16?))       return (MB<T>)(object)(new NI16());
			if (t == typeof(Int32?))       return (MB<T>)(object)(new NI32());
			if (t == typeof(Int64?))       return (MB<T>)(object)(new NI64());

			if (t == typeof(Byte?))        return (MB<T>)(object)(new NU8());
			if (t == typeof(UInt16?))      return (MB<T>)(object)(new NU16());
			if (t == typeof(UInt32?))      return (MB<T>)(object)(new NU32());
			if (t == typeof(UInt64?))      return (MB<T>)(object)(new NU64());

			if (t == typeof(Single?))      return (MB<T>)(object)(new NR4());
			if (t == typeof(Double?))      return (MB<T>)(object)(new NR8());

			if (t == typeof(Boolean?))     return (MB<T>)(object)(new NB());
			if (t == typeof(Decimal?))     return (MB<T>)(object)(new ND());

			if (t == typeof(Char?))        return (MB<T>)(object)(new NC());
			if (t == typeof(Guid?))        return (MB<T>)(object)(new NG());
			if (t == typeof(DateTime?))    return (MB<T>)(object)(new NDT());
			if (t == typeof(DateTimeOffset?)) return (MB<T>)(object)(new NDTO());

			// Nullable Enums.
			//
			if (TypeHelper.IsNullable(t) && Nullable.GetUnderlyingType(t).IsEnum)
			{
				Type enumType = Nullable.GetUnderlyingType(t);
				t = Enum.GetUnderlyingType(enumType);

				if (t == typeof(SByte))  return (MB<T>)Activator.CreateInstance(typeof(NEI8<>).MakeGenericType(typeof(T), enumType));
				if (t == typeof(Int16))  return (MB<T>)Activator.CreateInstance(typeof(NEI16<>).MakeGenericType(typeof(T), enumType));
				if (t == typeof(Int32))  return (MB<T>)Activator.CreateInstance(typeof(NEI32<>).MakeGenericType(typeof(T), enumType));
				if (t == typeof(Int64))  return (MB<T>)Activator.CreateInstance(typeof(NEI64<>).MakeGenericType(typeof(T), enumType));

				if (t == typeof(Byte))   return (MB<T>)Activator.CreateInstance(typeof(NEU8<>).MakeGenericType(typeof(T), enumType));
				if (t == typeof(UInt16)) return (MB<T>)Activator.CreateInstance(typeof(NEU16<>).MakeGenericType(typeof(T), enumType));
				if (t == typeof(UInt32)) return (MB<T>)Activator.CreateInstance(typeof(NEU32<>).MakeGenericType(typeof(T), enumType));
				if (t == typeof(UInt64)) return (MB<T>)Activator.CreateInstance(typeof(NEU64<>).MakeGenericType(typeof(T), enumType));
			}

#if !SILVERLIGHT

			// SqlTypes.
			//
			if (t == typeof(SqlString))    return (MB<T>)(object)(new dbS());

			if (t == typeof(SqlByte))      return (MB<T>)(object)(new dbU8());
			if (t == typeof(SqlInt16))     return (MB<T>)(object)(new dbI16());
			if (t == typeof(SqlInt32))     return (MB<T>)(object)(new dbI32());
			if (t == typeof(SqlInt64))     return (MB<T>)(object)(new dbI64());

			if (t == typeof(SqlSingle))    return (MB<T>)(object)(new dbR4());
			if (t == typeof(SqlDouble))    return (MB<T>)(object)(new dbR8());
			if (t == typeof(SqlDecimal))   return (MB<T>)(object)(new dbD());
			if (t == typeof(SqlMoney))     return (MB<T>)(object)(new dbM());

			if (t == typeof(SqlBoolean))   return (MB<T>)(object)(new dbB());
			if (t == typeof(SqlGuid))      return (MB<T>)(object)(new dbG());
			if (t == typeof(SqlDateTime))  return (MB<T>)(object)(new dbDT());

#endif

			return new Default<T>();
		}

		// Default setter.
		//
		public sealed class Default<V> : MB<V>     { public override  void To(IMapDataDestination d, object o, int i, V           v) { d.SetValue       (o, i, v); } }

		// Scalar Types.
		//
		sealed class I8    : MB<SByte>       { public override  void To(IMapDataDestination d, object o, int i, SByte       v) { d.SetSByte       (o, i, v); } }
		sealed class I16   : MB<Int16>       { public override  void To(IMapDataDestination d, object o, int i, Int16       v) { d.SetInt16       (o, i, v); } }
		sealed class I32   : MB<Int32>       { public override  void To(IMapDataDestination d, object o, int i, Int32       v) { d.SetInt32       (o, i, v); } }
		sealed class I64   : MB<Int64>       { public override  void To(IMapDataDestination d, object o, int i, Int64       v) { d.SetInt64       (o, i, v); } }

		sealed class U8    : MB<Byte>        { public override  void To(IMapDataDestination d, object o, int i, Byte        v) { d.SetByte        (o, i, v); } }
		sealed class U16   : MB<UInt16>      { public override  void To(IMapDataDestination d, object o, int i, UInt16      v) { d.SetUInt16      (o, i, v); } }
		sealed class U32   : MB<UInt32>      { public override  void To(IMapDataDestination d, object o, int i, UInt32      v) { d.SetUInt32      (o, i, v); } }
		sealed class U64   : MB<UInt64>      { public override  void To(IMapDataDestination d, object o, int i, UInt64      v) { d.SetUInt64      (o, i, v); } }

		sealed class R4    : MB<Single>      { public override  void To(IMapDataDestination d, object o, int i, Single      v) { d.SetSingle      (o, i, v); } }
		sealed class R8    : MB<Double>      { public override  void To(IMapDataDestination d, object o, int i, Double      v) { d.SetDouble      (o, i, v); } }

		sealed class B     : MB<Boolean>     { public override  void To(IMapDataDestination d, object o, int i, Boolean     v) { d.SetBoolean     (o, i, v); } }
		sealed class D     : MB<Decimal>     { public override  void To(IMapDataDestination d, object o, int i, Decimal     v) { d.SetDecimal     (o, i, v); } }

		sealed class C     : MB<Char>        { public override  void To(IMapDataDestination d, object o, int i, Char        v) { d.SetChar        (o, i, v); } }
		sealed class G     : MB<Guid>        { public override  void To(IMapDataDestination d, object o, int i, Guid        v) { d.SetGuid        (o, i, v); } }
		sealed class DT    : MB<DateTime>    { public override  void To(IMapDataDestination d, object o, int i, DateTime    v) { d.SetDateTime    (o, i, v); } }
		sealed class DTO   : MB<DateTimeOffset>{ public override  void To(IMapDataDestination d, object o, int i, DateTimeOffset    v) { d.SetDateTimeOffset    (o, i, v); } }

		// Enums.
		//
		sealed class EI8<E>  : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetSByte       (o, i, (SByte)(object)v); } }
		sealed class EI16<E> : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetInt16       (o, i, (Int16)(object)v); } }
		sealed class EI32<E> : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetInt32       (o, i, (Int32)(object)v); } }
		sealed class EI64<E> : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetInt64       (o, i, (Int64)(object)v); } }

		sealed class EU8<E>  : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetByte        (o, i, (Byte)(object)v); } }
		sealed class EU16<E> : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetUInt16      (o, i, (UInt16)(object)v); } }
		sealed class EU32<E> : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetUInt32      (o, i, (UInt32)(object)v); } }
		sealed class EU64<E> : MB<E>         { public override  void To(IMapDataDestination d, object o, int i, E           v) { d.SetUInt64      (o, i, (UInt64)(object)v); } }

		// Nullable Types.
		//
		sealed class NI8   : MB<SByte?>      { public override  void To(IMapDataDestination d, object o, int i, SByte?      v) { d.SetNullableSByte      (o, i, v); } }
		sealed class NI16  : MB<Int16?>      { public override  void To(IMapDataDestination d, object o, int i, Int16?      v) { d.SetNullableInt16      (o, i, v); } }
		sealed class NI32  : MB<Int32?>      { public override  void To(IMapDataDestination d, object o, int i, Int32?      v) { d.SetNullableInt32      (o, i, v); } }
		sealed class NI64  : MB<Int64?>      { public override  void To(IMapDataDestination d, object o, int i, Int64?      v) { d.SetNullableInt64      (o, i, v); } }

		sealed class NU8   : MB<Byte?>       { public override  void To(IMapDataDestination d, object o, int i, Byte?       v) { d.SetNullableByte       (o, i, v); } }
		sealed class NU16  : MB<UInt16?>     { public override  void To(IMapDataDestination d, object o, int i, UInt16?     v) { d.SetNullableUInt16     (o, i, v); } }
		sealed class NU32  : MB<UInt32?>     { public override  void To(IMapDataDestination d, object o, int i, UInt32?     v) { d.SetNullableUInt32     (o, i, v); } }
		sealed class NU64  : MB<UInt64?>     { public override  void To(IMapDataDestination d, object o, int i, UInt64?     v) { d.SetNullableUInt64     (o, i, v); } }

		sealed class NR4   : MB<Single?>     { public override  void To(IMapDataDestination d, object o, int i, Single?     v) { d.SetNullableSingle     (o, i, v); } }
		sealed class NR8   : MB<Double?>     { public override  void To(IMapDataDestination d, object o, int i, Double?     v) { d.SetNullableDouble     (o, i, v); } }

		sealed class NB    : MB<Boolean?>    { public override  void To(IMapDataDestination d, object o, int i, Boolean?    v) { d.SetNullableBoolean    (o, i, v); } }
		sealed class ND    : MB<Decimal?>    { public override  void To(IMapDataDestination d, object o, int i, Decimal?    v) { d.SetNullableDecimal    (o, i, v); } }

		sealed class NC    : MB<Char?>       { public override  void To(IMapDataDestination d, object o, int i, Char?       v) { d.SetNullableChar       (o, i, v); } }
		sealed class NG    : MB<Guid?>       { public override  void To(IMapDataDestination d, object o, int i, Guid?       v) { d.SetNullableGuid       (o, i, v); } }
		sealed class NDT   : MB<DateTime?>   { public override  void To(IMapDataDestination d, object o, int i, DateTime?   v) { d.SetNullableDateTime   (o, i, v); } }
		sealed class NDTO  : MB<DateTimeOffset?>{ public override  void To(IMapDataDestination d, object o, int i, DateTimeOffset?    v) { d.SetNullableDateTimeOffset    (o, i, v); } }

		// Nullable Enums.
		//
		sealed class NEI8<E>  : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetSByte (o, i, (SByte)(object)v.Value); } }
		sealed class NEI16<E> : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetInt16 (o, i, (Int16)(object)v.Value); } }
		sealed class NEI32<E> : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetInt32 (o, i, (Int32)(object)v.Value); } }
		sealed class NEI64<E> : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetInt64 (o, i, (Int64)(object)v.Value); } }

		sealed class NEU8<E>  : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetByte  (o, i, (Byte)(object)v.Value); } }
		sealed class NEU16<E> : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetUInt16(o, i, (UInt16)(object)v.Value); } }
		sealed class NEU32<E> : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetUInt32(o, i, (UInt32)(object)v.Value); } }
		sealed class NEU64<E> : MB<E?> where E : struct { public override  void To(IMapDataDestination d, object o, int i, E? v) { /*if (null == v) d.SetNull(o, i); else*/ d.SetUInt64(o, i, (UInt64)(object)v.Value); } }

#if !SILVERLIGHT

		// SqlTypes.
		//
		sealed class dbS   : MB<SqlString>   { public override  void To(IMapDataDestination d, object o, int i, SqlString   v) { d.SetSqlString   (o, i, v); } }

		sealed class dbU8  : MB<SqlByte>     { public override  void To(IMapDataDestination d, object o, int i, SqlByte     v) { d.SetSqlByte     (o, i, v); } }
		sealed class dbI16 : MB<SqlInt16>    { public override  void To(IMapDataDestination d, object o, int i, SqlInt16    v) { d.SetSqlInt16    (o, i, v); } }
		sealed class dbI32 : MB<SqlInt32>    { public override  void To(IMapDataDestination d, object o, int i, SqlInt32    v) { d.SetSqlInt32    (o, i, v); } }
		sealed class dbI64 : MB<SqlInt64>    { public override  void To(IMapDataDestination d, object o, int i, SqlInt64    v) { d.SetSqlInt64    (o, i, v); } }

		sealed class dbR4  : MB<SqlSingle>   { public override  void To(IMapDataDestination d, object o, int i, SqlSingle   v) { d.SetSqlSingle   (o, i, v); } }
		sealed class dbR8  : MB<SqlDouble>   { public override  void To(IMapDataDestination d, object o, int i, SqlDouble   v) { d.SetSqlDouble   (o, i, v); } }
		sealed class dbD   : MB<SqlDecimal>  { public override  void To(IMapDataDestination d, object o, int i, SqlDecimal  v) { d.SetSqlDecimal  (o, i, v); } }
		sealed class dbM   : MB<SqlMoney>    { public override  void To(IMapDataDestination d, object o, int i, SqlMoney    v) { d.SetSqlMoney    (o, i, v); } }

		sealed class dbB   : MB<SqlBoolean>  { public override  void To(IMapDataDestination d, object o, int i, SqlBoolean  v) { d.SetSqlBoolean  (o, i, v); } }
		sealed class dbG   : MB<SqlGuid>     { public override  void To(IMapDataDestination d, object o, int i, SqlGuid     v) { d.SetSqlGuid     (o, i, v); } }
		sealed class dbDT  : MB<SqlDateTime> { public override  void To(IMapDataDestination d, object o, int i, SqlDateTime v) { d.SetSqlDateTime (o, i, v); } }

#endif
	}
}
