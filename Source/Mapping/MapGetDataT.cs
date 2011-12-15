using System;
using System.Data.SqlTypes;
using LinqToDB.Extensions;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[CLSCompliant(false)]
	public static class MapGetData<T>
	{
		public abstract class MB<V>
		{
			public abstract V From(IMapDataSource s, object o, int i);
		}

		public static T From(IMapDataSource s, object o, int i)
		{
			return I.From(s, o, i);
		}

		public  static MB<T> I = GetGetter();
		private static MB<T>     GetGetter()
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
			if (ReflectionExtensions.IsNullable(t) && Nullable.GetUnderlyingType(t).IsEnum)
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
		public sealed class Default<V> : MB<V>     { public override V           From(IMapDataSource s, object o, int i) { return (V)s.GetValue    (o, i); } }

		// Scalar Types.
		//
		sealed class I8          : MB<SByte>       { public override SByte       From(IMapDataSource s, object o, int i) { return s.GetSByte       (o, i); } }
		sealed class I16         : MB<Int16>       { public override Int16       From(IMapDataSource s, object o, int i) { return s.GetInt16       (o, i); } }
		sealed class I32         : MB<Int32>       { public override Int32       From(IMapDataSource s, object o, int i) { return s.GetInt32       (o, i); } }
		sealed class I64         : MB<Int64>       { public override Int64       From(IMapDataSource s, object o, int i) { return s.GetInt64       (o, i); } }

		sealed class U8          : MB<Byte>        { public override Byte        From(IMapDataSource s, object o, int i) { return s.GetByte        (o, i); } }
		sealed class U16         : MB<UInt16>      { public override UInt16      From(IMapDataSource s, object o, int i) { return s.GetUInt16      (o, i); } }
		sealed class U32         : MB<UInt32>      { public override UInt32      From(IMapDataSource s, object o, int i) { return s.GetUInt32      (o, i); } }
		sealed class U64         : MB<UInt64>      { public override UInt64      From(IMapDataSource s, object o, int i) { return s.GetUInt64      (o, i); } }

		sealed class R4          : MB<Single>      { public override Single      From(IMapDataSource s, object o, int i) { return s.GetSingle      (o, i); } }
		sealed class R8          : MB<Double>      { public override Double      From(IMapDataSource s, object o, int i) { return s.GetDouble      (o, i); } }

		sealed class B           : MB<Boolean>     { public override Boolean     From(IMapDataSource s, object o, int i) { return s.GetBoolean     (o, i); } }
		sealed class D           : MB<Decimal>     { public override Decimal     From(IMapDataSource s, object o, int i) { return s.GetDecimal     (o, i); } }

		sealed class C           : MB<Char>        { public override Char        From(IMapDataSource s, object o, int i) { return s.GetChar        (o, i); } }
		sealed class G           : MB<Guid>        { public override Guid        From(IMapDataSource s, object o, int i) { return s.GetGuid        (o, i); } }
		sealed class DT          : MB<DateTime>    { public override DateTime    From(IMapDataSource s, object o, int i) { return s.GetDateTime    (o, i); } }
		sealed class DTO         : MB<DateTimeOffset> { public override DateTimeOffset From(IMapDataSource s, object o, int i) { return s.GetDateTimeOffset    (o, i); } }
		// Enums.
		//
		sealed class EI8<E>      : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetSByte   (o, i); } }
		sealed class EI16<E>     : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetInt16   (o, i); } }
		sealed class EI32<E>     : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetInt32   (o, i); } }
		sealed class EI64<E>     : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetInt64   (o, i); } }

		sealed class EU8<E>      : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetByte    (o, i); } }
		sealed class EU16<E>     : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetUInt16  (o, i); } }
		sealed class EU32<E>     : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetUInt32  (o, i); } }
		sealed class EU64<E>     : MB<E>           { public override E           From(IMapDataSource s, object o, int i) { return (E)(object)s.GetUInt64  (o, i); } }

		// Nullable Types.
		//
		sealed class NI8         : MB<SByte?>      { public override SByte?      From(IMapDataSource s, object o, int i) { return s.GetNullableSByte      (o, i); } }
		sealed class NI16        : MB<Int16?>      { public override Int16?      From(IMapDataSource s, object o, int i) { return s.GetNullableInt16      (o, i); } }
		sealed class NI32        : MB<Int32?>      { public override Int32?      From(IMapDataSource s, object o, int i) { return s.GetNullableInt32      (o, i); } }
		sealed class NI64        : MB<Int64?>      { public override Int64?      From(IMapDataSource s, object o, int i) { return s.GetNullableInt64      (o, i); } }

		sealed class NU8         : MB<Byte?>       { public override Byte?       From(IMapDataSource s, object o, int i) { return s.GetNullableByte       (o, i); } }
		sealed class NU16        : MB<UInt16?>     { public override UInt16?     From(IMapDataSource s, object o, int i) { return s.GetNullableUInt16     (o, i); } }
		sealed class NU32        : MB<UInt32?>     { public override UInt32?     From(IMapDataSource s, object o, int i) { return s.GetNullableUInt32     (o, i); } }
		sealed class NU64        : MB<UInt64?>     { public override UInt64?     From(IMapDataSource s, object o, int i) { return s.GetNullableUInt64     (o, i); } }

		sealed class NR4         : MB<Single?>     { public override Single?     From(IMapDataSource s, object o, int i) { return s.GetNullableSingle     (o, i); } }
		sealed class NR8         : MB<Double?>     { public override Double?     From(IMapDataSource s, object o, int i) { return s.GetNullableDouble     (o, i); } }

		sealed class NB          : MB<Boolean?>    { public override Boolean?    From(IMapDataSource s, object o, int i) { return s.GetNullableBoolean    (o, i); } }
		sealed class ND          : MB<Decimal?>    { public override Decimal?    From(IMapDataSource s, object o, int i) { return s.GetNullableDecimal    (o, i); } }

		sealed class NC          : MB<Char?>       { public override Char?       From(IMapDataSource s, object o, int i) { return s.GetNullableChar       (o, i); } }
		sealed class NG          : MB<Guid?>       { public override Guid?       From(IMapDataSource s, object o, int i) { return s.GetNullableGuid       (o, i); } }
		sealed class NDT         : MB<DateTime?>   { public override DateTime?   From(IMapDataSource s, object o, int i) { return s.GetNullableDateTime   (o, i); } }
		sealed class NDTO        : MB<DateTimeOffset?> { public override DateTimeOffset? From(IMapDataSource s, object o, int i) { return s.GetNullableDateTimeOffset    (o, i); } }

		// Nullable Enums.
		//
		sealed class NEI8<E>     : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetSByte (o, i); } }
		sealed class NEI16<E>    : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetInt16 (o, i); } }
		sealed class NEI32<E>    : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetInt32 (o, i); } }
		sealed class NEI64<E>    : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetInt64 (o, i); } }

		sealed class NEU8<E>     : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetByte  (o, i); } }
		sealed class NEU16<E>    : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetUInt16(o, i); } }
		sealed class NEU32<E>    : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetUInt32(o, i); } }
		sealed class NEU64<E>    : MB<E?> where E : struct { public override E?  From(IMapDataSource s, object o, int i) { return /*s.IsNull(o, i) ? (E?)null :*/ (E)(object)s.GetUInt64(o, i); } }

#if !SILVERLIGHT

		// SqlTypes.
		//
		sealed class dbS         : MB<SqlString>   { public override SqlString   From(IMapDataSource s, object o, int i) { return s.GetSqlString   (o, i); } }

		sealed class dbU8        : MB<SqlByte>     { public override SqlByte     From(IMapDataSource s, object o, int i) { return s.GetSqlByte     (o, i); } }
		sealed class dbI16       : MB<SqlInt16>    { public override SqlInt16    From(IMapDataSource s, object o, int i) { return s.GetSqlInt16    (o, i); } }
		sealed class dbI32       : MB<SqlInt32>    { public override SqlInt32    From(IMapDataSource s, object o, int i) { return s.GetSqlInt32    (o, i); } }
		sealed class dbI64       : MB<SqlInt64>    { public override SqlInt64    From(IMapDataSource s, object o, int i) { return s.GetSqlInt64    (o, i); } }

		sealed class dbR4        : MB<SqlSingle>   { public override SqlSingle   From(IMapDataSource s, object o, int i) { return s.GetSqlSingle   (o, i); } }
		sealed class dbR8        : MB<SqlDouble>   { public override SqlDouble   From(IMapDataSource s, object o, int i) { return s.GetSqlDouble   (o, i); } }
		sealed class dbD         : MB<SqlDecimal>  { public override SqlDecimal  From(IMapDataSource s, object o, int i) { return s.GetSqlDecimal  (o, i); } }
		sealed class dbM         : MB<SqlMoney>    { public override SqlMoney    From(IMapDataSource s, object o, int i) { return s.GetSqlMoney    (o, i); } }

		sealed class dbB         : MB<SqlBoolean>  { public override SqlBoolean  From(IMapDataSource s, object o, int i) { return s.GetSqlBoolean  (o, i); } }
		sealed class dbG         : MB<SqlGuid>     { public override SqlGuid     From(IMapDataSource s, object o, int i) { return s.GetSqlGuid     (o, i); } }
		sealed class dbDT        : MB<SqlDateTime> { public override SqlDateTime From(IMapDataSource s, object o, int i) { return s.GetSqlDateTime (o, i); } }

#endif
	}
}
