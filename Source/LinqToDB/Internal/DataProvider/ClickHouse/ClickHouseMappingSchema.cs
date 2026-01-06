using System;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

using LinqToDB.Common;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	public sealed class ClickHouseMappingSchema : LockedMappingSchema
	{
		// we need defaults for length/precision/scale for some types if used didn't specified them
		// Don't like those defaults? Specify missing values in ur mapping...
		internal const int  DEFAULT_FIXED_STRING_LENGTH  = 100;
		internal const int  DEFAULT_DECIMAL_PRECISION    = 29;
		internal const int  DEFAULT_DECIMAL_SCALE        = 10;
		internal const byte DEFAULT_DATETIME64_PRECISION = 7;

		ClickHouseMappingSchema() : base(ProviderName.ClickHouse)
		{
			//Sql* types skipped intentionally: as we should stop using them for providers except SQL Server and SQL CE (they were designed for them)

			// override default type mappings for some types
			AddScalarType(typeof(DateTime      ), new SqlDataType(new DbDataType(typeof(DateTime      ), DataType.DateTime64, null, null, null, DEFAULT_DATETIME64_PRECISION)));
			AddScalarType(typeof(DateTimeOffset), new SqlDataType(new DbDataType(typeof(DateTimeOffset), DataType.DateTime64, null, null, null, DEFAULT_DATETIME64_PRECISION)));
			// .net decimal has precision 29, so we map it to Decimal128(10) by default
			AddScalarType(typeof(decimal       ), new SqlDataType(new DbDataType(typeof(decimal       ), DataType.Decimal128, null, null, DEFAULT_DECIMAL_PRECISION, DEFAULT_DECIMAL_SCALE)));
			// NOTE: Interval type cannot be used for columns and user will need to configure own mappings, e.g. DataType.Int64 for it
			AddScalarType(typeof(TimeSpan)  , DataType.IntervalSecond);
			AddScalarType(typeof(IPAddress) , DataType.IPv6);
			AddScalarType(typeof(bool)      , DataType.Boolean);
			AddScalarType(typeof(BigInteger), DataType.Int256);
			AddScalarType(typeof(byte[])    , DataType.VarBinary);
			AddScalarType(typeof(string)    , DataType.NVarChar);
#if SUPPORTS_DATEONLY
			AddScalarType(typeof(DateOnly)  , DataType.Date32);
#endif

			// type to literal converters
			SetValueToSqlConverter(typeof(string)        , (sb,dt,_,v) => ConvertString        (sb, dt, (string)v));
			SetValueToSqlConverter(typeof(char)          , (sb, _,_,v) => BuildCharLiteral     (sb,     (char)v));
			SetValueToSqlConverter(typeof(byte[])        , (sb,dt,_,v) => ConvertByteArray     (sb, dt, (byte[])v));
			SetValueToSqlConverter(typeof(Binary)        , (sb,dt,_,v) => ConvertByteArray     (sb, dt, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(IPAddress)     , (sb,dt,_,v) => ConvertIPAddress     (sb, dt, (IPAddress)v));
			SetValueToSqlConverter(typeof(Guid)          , (sb,dt,_,v) => ConvertGuid          (sb, dt, (Guid)v));
			SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpan      (sb, dt, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTime      (sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffset(sb, dt, (DateTimeOffset)v));
#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateOnly      (sb, dt, (DateOnly)v));
#endif
			SetValueToSqlConverter(typeof(byte)          , (sb,dt,_,v) => ConvertByte          (sb, dt, (byte)v));
			SetValueToSqlConverter(typeof(sbyte)         , (sb,dt,_,v) => ConvertSByte         (sb, dt, (sbyte)v));
			SetValueToSqlConverter(typeof(short)         , (sb,dt,_,v) => ConvertInt16         (sb, dt, (short)v));
			SetValueToSqlConverter(typeof(ushort)        , (sb,dt,_,v) => ConvertUInt16        (sb, dt, (ushort)v));
			SetValueToSqlConverter(typeof(int)           , (sb,dt,_,v) => ConvertInt32         (sb, dt, (int)v));
			SetValueToSqlConverter(typeof(uint)          , (sb,dt,_,v) => ConvertUInt32        (sb, dt, (uint)v));
			SetValueToSqlConverter(typeof(long)          , (sb,dt,_,v) => ConvertInt64         (sb, dt, (long)v));
			SetValueToSqlConverter(typeof(ulong)         , (sb,dt,_,v) => ConvertUInt64        (sb, dt, (ulong)v));
			SetValueToSqlConverter(typeof(BigInteger)    , (sb,dt,_,v) => ConvertBigInteger    (sb, dt, (BigInteger)v));
			SetValueToSqlConverter(typeof(float)         , (sb,dt,_,v) => ConvertFloat         (sb, dt, (float)v));
			SetValueToSqlConverter(typeof(double)        , (sb,dt,_,v) => ConvertDouble        (sb, dt, (double)v));
			SetValueToSqlConverter(typeof(decimal)       , (sb,dt,_,v) => ConvertDecimal       (sb, dt, (decimal)v));
			SetValueToSqlConverter(typeof(bool)          , (sb,dt,_,v) => ConvertBool          (sb, dt, (bool)v));

			// some custom type conversions, not suitable for registration in default converter (for all providers)

			// conversions to DateTimeOffset
			SetConvertExpression((DateTime v) => new DateTimeOffset(v.Ticks, default));
#if SUPPORTS_DATEONLY
			SetConvertExpression((DateOnly       v) => new DateTimeOffset(v.ToDateTime(TimeOnly.MinValue), default));
			SetConvertExpression((DateTimeOffset v) => new DateOnly(v.Year, v.Month, v.Day));
#endif

			// IPAddress <=> uint (IPv4)
			SetConvertExpression((IPAddress v) => IPAddressToUInt(v));
			SetConvertExpression((uint v) => new IPAddress(new byte[] { (byte)((v >> 24) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF) }));

			// IPAddress <=> byte[4/16] (IPv6)
			SetConvertExpression((byte[] v) => new IPAddress(v));
			SetConvertExpression((IPAddress v) => v.GetAddressBytes());

			// https://github.com/ClickHouse/ClickHouse/issues/38790
			// byte[] <=> string
			// Binary <=> string
			SetConvertExpression((string v) => Encoding.UTF8.GetBytes(v));
			SetConvertExpression((byte[] v) => Encoding.UTF8.GetString(v));
			SetConvertExpression((string v) => new Binary(Encoding.UTF8.GetBytes(v)));
			SetConvertExpression((Binary v) => Encoding.UTF8.GetString(v.ToArray()));
			// byte[] <=> char
			SetConvertExpression((char v) => Encoding.UTF8.GetBytes(new[] { v }));
			SetConvertExpression((byte[] v) => Encoding.UTF8.GetChars(v).Single());

			// NaN handling
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (decimal)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (decimal)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (byte)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (byte)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (sbyte)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (sbyte)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (short)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (short)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (ushort)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (ushort)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (int)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (int)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (uint)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (uint)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (long)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (long)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? default : (ulong)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? default : (ulong)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (decimal?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (decimal?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (byte?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (byte?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (sbyte?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (sbyte?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (short?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (short?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (ushort?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (ushort?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (int?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (int?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (uint?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (uint?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (long?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (long?)v);
			SetConvertExpression((double v) => double.IsNaN(v) ? null    : (ulong?)v);
			SetConvertExpression((float  v) => float. IsNaN(v) ? null    : (ulong?)v);
		}

#region Type converters

		static uint IPAddressToUInt(IPAddress address)
		{
			// BitConverter.ToUInt32 uses wrong byte order for this case
			if (address.AddressFamily == AddressFamily.InterNetworkV6)
				throw new LinqToDBConvertException("Cannot convert IPv6 address to UInt32");

			var bytes = address.GetAddressBytes();

			return unchecked((uint)(
				bytes[3] |
				bytes[2] << 8 |
				bytes[1] << 16 |
				bytes[0] << 24));
		}

#endregion

#region Type to SQL converters (for multi-bindings)

		private static void ConvertString(StringBuilder stringBuilder, SqlDataType dt, string value)
		{
			BuildStringLiteral(stringBuilder, value);

			// apply type for non-String types
			switch (dt.Type.DataType)
			{
				case DataType.IPv4      :
					stringBuilder.Append("::IPv4");
					break;
				case DataType.IPv6      :
					stringBuilder.Append("::IPv6");
					break;
				case DataType.Decimal32 :
				case DataType.Decimal64 :
				case DataType.Decimal128:
				case DataType.Decimal256:
				{
					var dataType = dt.Type.DataType;
					var scale    = dt.Type.Scale ?? DEFAULT_DECIMAL_SCALE;

					stringBuilder.Append("::");

					switch (dataType)
					{
						case DataType.Decimal32 : stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Decimal32({0})", scale); break;
						case DataType.Decimal64 : stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Decimal64({0})", scale); break;
						case DataType.Decimal128: stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Decimal128({0})", scale); break;
						case DataType.Decimal256: stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Decimal256({0})", scale); break;
					}

					break;
				}
			}
		}

		private static void ConvertByteArray(StringBuilder stringBuilder, SqlDataType dt, byte[] value)
		{
			if (dt.Type.DataType == DataType.IPv6)
			{
				if (value.Length is not 4 and not 16)
					throw new LinqToDBConvertException($"IPv6 address should have 4 or 16 bytes, but got {value.Length}");

				stringBuilder.Append('\'');

				if (value.Length == 4)
				{
					// IPv6 mapping from IPv4
					stringBuilder.Append("::ffff:");
					for (var i = 0; i < value.Length; i++)
					{
						if (i > 0)
							stringBuilder.Append('.');

						stringBuilder.Append(value[i].ToString(CultureInfo.InvariantCulture));
					}
				}
				else
				{
					for (var i = 0; i < value.Length; i += 2)
					{
						if (i > 0)
							stringBuilder.Append(':');

						stringBuilder.Append(CultureInfo.InvariantCulture, $"{value[i]:x2}{value[i + 1]:x2}");
					}
				}

				stringBuilder.Append("'::IPv6");
				return;
			}

			BuildBinaryLiteral(stringBuilder, value);
		}

		private static void ConvertIPAddress(StringBuilder sb, SqlDataType dt, IPAddress address)
		{
			switch ((dt.Type.DataType, address.AddressFamily))
			{
				case (DataType.IPv6, AddressFamily.InterNetwork):
				case (DataType.IPv6, AddressFamily.InterNetworkV6):
				case (DataType.Undefined, AddressFamily.InterNetworkV6):
					sb
						.Append("toIPv6('")
						.Append(address.ToString())
						.Append("')");
					break;
				case (DataType.IPv4, AddressFamily.InterNetwork):
				case (DataType.Undefined, AddressFamily.InterNetwork):
					sb
						.Append("toIPv4('")
						.Append(address.ToString())
						.Append("')");
					break;
				default:
					// there are so many prehistoric values in AddressFamily nobody would ever use
					throw new LinqToDBConvertException($"Unsupported AddressFamily/DataType combination: ({address.AddressFamily} + {dt.Type.DataType})");
			}
		}

		private static void ConvertGuid(StringBuilder sb, SqlDataType dt, Guid value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.NVarChar:
				case DataType.VarChar :
				case DataType.Char    :
				case DataType.NChar   :
					BuildStringLiteral(sb, value.ToString("D"));
					break;
				case DataType.Binary  :
					BuildBinaryLiteral(sb, value.ToByteArray());
					break;
				case DataType.Guid    :
				default               :
					BuildUUIDLiteral(sb, value);
					break;
			}
		}

		private static void ConvertTimeSpan(StringBuilder sb, SqlDataType dt, TimeSpan value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Int64            : ConvertInt64(sb, dt, value.Ticks);                 break;
				// ConvertInt64 also handles all intervals including not supported here
				case DataType.Undefined        :
				case DataType.IntervalSecond   : ConvertInt64(sb, dt, (long)value.TotalSeconds);    break;
				case DataType.IntervalMinute   : ConvertInt64(sb, dt, (long)value.TotalMinutes);    break;
				case DataType.IntervalHour     : ConvertInt64(sb, dt, (long)value.TotalHours);      break;
				case DataType.IntervalDay      : ConvertInt64(sb, dt, (long)value.TotalDays);       break;
				case DataType.IntervalWeek     : ConvertInt64(sb, dt, ((long)value.TotalDays) / 7); break;
				// cannot be mapped to timespan as it cannot be represented as fixed-value time interval
				//case DataType.IntervalMonth  :
				//case DataType.IntervalQuarter:
				//case DataType.IntervalYear   :
				default                        :
					throw new LinqToDBConvertException($"Unsupported TimeSpan type mapping: {dt.Type.DataType}");
			}
		}

		private static void ConvertDateTime(StringBuilder sb, SqlDataType dt, DateTime value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Date      : BuildDateLiteral(sb, value.Date);                                                     break;
				case DataType.Date32    : BuildDate32Literal(sb, value.Date);                                                   break;
				case DataType.DateTime  : BuildDateTimeLiteral(sb, value);                                                      break;
				case DataType.Undefined :
				case DataType.DateTime64: BuildDateTime64Literal(sb, value, dt.Type.Precision ?? DEFAULT_DATETIME64_PRECISION); break;
				default                 :
					throw new LinqToDBConvertException($"Unsupported DateTime type mapping: {dt.Type.DataType}");
			}
		}

		private static void ConvertByte(StringBuilder sb, SqlDataType dt, byte value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value)); return;
				case DataType.Undefined:
				case DataType.Byte     : BuildByteLiteral(sb, value);                  return;
				case DataType.UInt16   : BuildUInt16Literal(sb, value);                return;
				case DataType.Int16    : BuildInt16Literal(sb, value);                 return;
				case DataType.UInt32   : BuildUInt32Literal(sb, value);                return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                 return;
				case DataType.UInt64   : BuildUInt64Literal(sb, value);                return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                 return;
				case DataType.UInt128  : BuildUInt128Literal(sb, value);               return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                return;
				case DataType.UInt256  : BuildUInt256Literal(sb, value);               return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported Byte type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertSByte(StringBuilder sb, SqlDataType dt, sbyte value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));                  return;
				case DataType.Undefined:
				case DataType.Enum8    :
				case DataType.SByte    : BuildSByteLiteral(sb, value);                                return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value));              return;
				case DataType.Int16    : BuildInt16Literal(sb, value);                                return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));                return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                                return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));               return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                                return;
				case DataType.UInt128  : BuildUInt128Literal(sb, checked((BigInteger)(ushort)value)); return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                               return;
				case DataType.UInt256  : BuildUInt256Literal(sb, checked((BigInteger)(ushort)value)); return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                               return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported SByte type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertInt16(StringBuilder sb, SqlDataType dt, short value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));                return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));              return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value));            return;
				case DataType.Undefined:
				case DataType.Int16    : BuildInt16Literal(sb, value);                              return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));              return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                              return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));             return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                              return;
				case DataType.UInt128  : BuildUInt128Literal(sb, checked((BigInteger)(uint)value)); return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                             return;
				case DataType.UInt256  : BuildUInt256Literal(sb, checked((BigInteger)(uint)value)); return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                             return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported Int16 type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertUInt16(StringBuilder sb, SqlDataType dt, ushort value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));    return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));  return;
				case DataType.Undefined:
				case DataType.UInt16   : BuildUInt16Literal(sb, value);                 return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));  return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));  return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                  return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value)); return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                  return;
				case DataType.UInt128  : BuildUInt128Literal(sb, value);                return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                 return;
				case DataType.UInt256  : BuildUInt256Literal(sb, value);                return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                 return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported UInt16 type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertInt32(StringBuilder sb, SqlDataType dt, int value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));                 return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));               return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));               return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value));             return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((ushort)value));             return;
				case DataType.Undefined:
				case DataType.Int32    : BuildInt32Literal(sb, value);                               return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));              return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                               return;
				case DataType.UInt128  : BuildUInt128Literal(sb, checked((BigInteger)(ulong)value)); return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                              return;
				case DataType.UInt256  : BuildUInt256Literal(sb, checked((BigInteger)(ulong)value)); return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                              return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported Int32 type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertUInt32(StringBuilder sb, SqlDataType dt, uint value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				case DataType.Undefined:
				case DataType.UInt32   : BuildUInt32Literal(sb, value);                  return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));     return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.UInt128  : BuildUInt128Literal(sb, value);                 return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                  return;
				case DataType.UInt256  : BuildUInt256Literal(sb, value);                 return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                  return;
				case DataType.IPv4     : BuildIPv4Literal(sb, value);                    return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported UInt32 type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertInt64(StringBuilder sb, SqlDataType dt, long value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));     return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));   return;
				case DataType.Undefined:
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                  return;
				case DataType.UInt128  :
					if (value < 0)
						throw new LinqToDBConvertException(string.Create(CultureInfo.InvariantCulture, $"Value {value} cannot be converted to unsigned UInt128 literal"));
					BuildUInt128Literal(sb, value);                                      return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                  return;
				case DataType.UInt256  :
					if (value < 0)
						throw new LinqToDBConvertException(string.Create(CultureInfo.InvariantCulture, $"Value {value} cannot be converted to unsigned UInt256 literal"));
					BuildUInt256Literal(sb, value);                                      return;
			}

			var format = GetIntervalLiteralFormat(dt.Type.DataType)
				?? throw new LinqToDBConvertException($"Unsupported Int64 type mapping: {dt.Type.DataType}");

			sb.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		private static void ConvertUInt64(StringBuilder sb, SqlDataType dt, ulong value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));     return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));   return;
				case DataType.Undefined:
				case DataType.UInt64   : BuildUInt64Literal(sb, value);                  return;
				case DataType.Int64    : BuildInt64Literal(sb, checked((long)value));    return;
				case DataType.UInt128  : BuildUInt128Literal(sb, value);                 return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                  return;
				case DataType.UInt256  : BuildUInt256Literal(sb, value);                 return;
				case DataType.Int256   : BuildInt256Literal(sb, value);                  return;

			}

			// no interval support as it doesn't accept ulong values
			throw new LinqToDBConvertException($"Unsupported UInt64 type mapping: {dt.Type.DataType}");
		}

		private static void ConvertBigInteger(StringBuilder sb, SqlDataType dt, BigInteger value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));     return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));   return;
				case DataType.Int64    : BuildInt64Literal(sb, checked((long)value));    return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int128   : BuildInt128Literal(sb, value);                  return;
				case DataType.UInt128  :
					if (value < 0)
						throw new LinqToDBConvertException(string.Create(CultureInfo.InvariantCulture, $"Value {value} cannot be converted to unsigned UInt128 literal"));
					BuildUInt128Literal(sb, value);                                      return;
				case DataType.Undefined:
				case DataType.Int256   : BuildInt256Literal(sb, value);                  return;
				case DataType.UInt256  :
					if (value < 0)
						throw new LinqToDBConvertException(string.Create(CultureInfo.InvariantCulture, $"Value {value} cannot be converted to unsigned UInt256 literal"));
					BuildUInt256Literal(sb, value);                                      return;
			}

			throw new LinqToDBConvertException($"Unsupported BigInteger type mapping: {dt.Type.DataType}");
		}

		private static void ConvertDateTimeOffset(StringBuilder sb, SqlDataType dt, DateTimeOffset value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Date          : BuildDateLiteral(sb, value.Date);                                                                 break;
				case DataType.Date32        : BuildDate32Literal(sb, value.Date);                                                               break;
				case DataType.DateTime      : BuildDateTimeLiteral(sb, value.UtcDateTime);                                                      break;
				case DataType.Undefined     :
				case DataType.DateTime2     :
				case DataType.DateTime64    :
				case DataType.SmallDateTime :
				case DataType.DateTimeOffset: BuildDateTime64Literal(sb, value.UtcDateTime, dt.Type.Precision ?? DEFAULT_DATETIME64_PRECISION); break;
				default                     : throw new LinqToDBConvertException($"Unsupported DateTimeOffset type mapping: {dt.Type.DataType}");
			}
		}

#if SUPPORTS_DATEONLY
		private static void ConvertDateOnly(StringBuilder sb, SqlDataType dt, DateOnly value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Date     : BuildDateLiteral(sb, value.ToDateTime(default)); break;
				case DataType.Undefined:
				case DataType.Date32   : BuildDate32Literal(sb, value.ToDateTime(default)); break;
				default:
					throw new LinqToDBConvertException($"Unsupported DateOnly type mapping: {dt.Type.DataType}");
			}
		}
#endif

		private static void ConvertFloat(StringBuilder sb, SqlDataType dt, float value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Undefined:
				case DataType.Single   : BuildFloatLiteral(sb, value); return;
			}

			throw new LinqToDBConvertException($"Unsupported Float type mapping: {dt.Type.DataType}");
		}

		private static void ConvertDouble(StringBuilder sb, SqlDataType dt, double value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Single   : BuildFloatLiteral(sb, checked((float)value)); return;
				case DataType.Undefined:
				case DataType.Int32:
				case DataType.Double   : BuildDoubleLiteral(sb, value); return;
				case DataType.Decimal64: BuildDecimal64Literal(sb, (decimal)value, DEFAULT_DECIMAL_SCALE); return;
			}

			throw new LinqToDBConvertException($"Unsupported Double type mapping: {dt.Type.DataType}");
		}

		private static void ConvertDecimal(StringBuilder sb, SqlDataType dt, decimal value)
		{
			var scale = dt.Type.Scale ?? DEFAULT_DECIMAL_SCALE;

			switch (dt.Type.DataType)
			{
				case DataType.Int32     : BuildDecimal32Literal(sb, value, 0);      return;
				case DataType.Decimal32 : BuildDecimal32Literal(sb, value, scale);  return;
				case DataType.Undefined :
				case DataType.Decimal64 : BuildDecimal64Literal(sb, value, scale);  return;
				case DataType.Decimal128: BuildDecimal128Literal(sb, value, scale); return;
				case DataType.Decimal256: BuildDecimal256Literal(sb, value, scale); return;
			}

			throw new LinqToDBConvertException($"Unsupported Decimal type mapping: {dt.Type.DataType}");
		}

		private static void ConvertBool(StringBuilder sb, SqlDataType dt, bool value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, value ? (byte)1 : (byte)0); return;
				case DataType.Undefined:
				case DataType.Boolean  : BuildBooleanLiteral(sb, value);                  return;
			}

			throw new LinqToDBConvertException($"Unsupported Boolean type mapping: {dt.Type.DataType}");
		}
#endregion

#region Literal generators

		// https://clickhouse.com/docs/en/sql-reference/syntax#string
		private static void BuildStringLiteral(StringBuilder stringBuilder, string value)
		{
			stringBuilder.Append('\'');

			foreach (var chr in value)
			{
				if (chr is '\\' or '\'')
					stringBuilder.Append('\\');

				stringBuilder.Append(chr);
			}

			stringBuilder.Append('\'');
		}

		private static void BuildCharLiteral(StringBuilder stringBuilder, char value)
		{
			stringBuilder.Append('\'');

			if (value is '\\' or '\'')
				stringBuilder.Append('\\');

			stringBuilder
				.Append(value)
				.Append('\'');
		}

		private static void BuildBinaryLiteral(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append('\'');

			foreach (var @byte in value)
				stringBuilder
					.Append("\\x")
					.AppendByteAsHexViaLookup32(@byte);

			stringBuilder
				.Append('\'');
		}

		private static void BuildUUIDLiteral(StringBuilder sb, Guid value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUUID('{0:d}')", value);
		}

		private static void BuildDateLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDate('{0:yyyy-MM-dd}')", value);
		}

		private static void BuildDate32Literal(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDate32('{0:yyyy-MM-dd}')", value);
		}

		private static void BuildDateTimeLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDateTime('{0:yyyy-MM-dd HH:mm:ss}')", value);
		}

		private static readonly string[] DATETIME64_FORMATS = new[]
		{
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss}', 0)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.f}', 1)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.ff}', 2)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.fff}', 3)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.ffff}', 4)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.fffff}', 5)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.ffffff}', 6)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 7)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 8)",
			"toDateTime64('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 9)",
		};

		private static void BuildDateTime64Literal(StringBuilder sb, DateTime value, int precision)
		{
			if (precision < 0)
				throw new LinqToDBConvertException(string.Create(CultureInfo.InvariantCulture, $"Invalid DateTime64 precision: {precision}"));

			if (precision > 9)
				precision = 9;

			sb.AppendFormat(CultureInfo.InvariantCulture, DATETIME64_FORMATS[precision], value);
		}

		private static void BuildByteLiteral(StringBuilder sb, byte value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUInt8({0})", value);
		}

		private static void BuildSByteLiteral(StringBuilder sb, sbyte value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toInt8({0})", value);
		}

		private static void BuildInt16Literal(StringBuilder sb, short value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toInt16({0})", value);
		}

		private static void BuildUInt16Literal(StringBuilder sb, ushort value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUInt16({0})", value);
		}

		private static void BuildInt32Literal(StringBuilder sb, int value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
		}

		private static void BuildUInt32Literal(StringBuilder sb, uint value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUInt32({0})", value);
		}

		private static void BuildInt64Literal(StringBuilder sb, long value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toInt64({0})", value);
		}

		private static void BuildUInt64Literal(StringBuilder sb, ulong value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUInt64({0})", value);
		}

		private static void BuildInt128Literal(StringBuilder sb, BigInteger value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toInt128('{0}')", value);
		}

		private static void BuildUInt128Literal(StringBuilder sb, BigInteger value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUInt128('{0}')", value);
		}

		private static void BuildInt256Literal(StringBuilder sb, BigInteger value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toInt256('{0}')", value);
		}

		private static void BuildUInt256Literal(StringBuilder sb, BigInteger value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toUInt256('{0}')", value);
		}

		private static string? GetIntervalLiteralFormat(DataType dataType)
		{
			// multi-component intervals generation not supported currently (not needed)
			// https://github.com/ClickHouse/ClickHouse/pull/42195
			return dataType switch
			{
				DataType.IntervalSecond  => "INTERVAL {0} SECOND",
				DataType.IntervalMinute  => "INTERVAL {0} MINUTE",
				DataType.IntervalHour    => "INTERVAL {0} HOUR",
				DataType.IntervalDay     => "INTERVAL {0} DAY",
				DataType.IntervalWeek    => "INTERVAL {0} WEEK",
				DataType.IntervalMonth   => "INTERVAL {0} MONTH",
				DataType.IntervalQuarter => "INTERVAL {0} QUARTER",
				DataType.IntervalYear    => "INTERVAL {0} YEAR",
				_                        => null,
			};
		}

		private static void BuildFloatLiteral(StringBuilder sb, float value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toFloat32({0:G9})", value);
		}

		private static void BuildDoubleLiteral(StringBuilder sb, double value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toFloat64({0:G17})", value);
		}

		private static void BuildDecimal32Literal(StringBuilder sb, decimal value, int scale)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal32('{0}', {1})", value, scale);
		}

		private static void BuildDecimal64Literal(StringBuilder sb, decimal value, int scale)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal64('{0}', {1})", value, scale);
		}

		private static void BuildDecimal128Literal(StringBuilder sb, decimal value, int scale)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal128('{0}', {1})", value, scale);
		}

		private static void BuildDecimal256Literal(StringBuilder sb, decimal value, int scale)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal256('{0}', {1})", value, scale);
		}

		private static void BuildBooleanLiteral(StringBuilder sb, bool value)
		{
			sb.Append(value ? "true" : "false");
		}

		private static void BuildIPv4Literal(StringBuilder sb, uint value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, "toIPv4('{0}.{1}.{2}.{3}')", (value >> 24) & 0xFF, (value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
		}

#endregion

		internal static ClickHouseMappingSchema Instance = new ();

		public sealed class OctonicaMappingSchema : LockedMappingSchema
		{
			public OctonicaMappingSchema() : base(ProviderName.ClickHouseOctonica, Instance)
			{
				SetConvertExpression((byte[] v) => ByteArrayToGuid(v));
				SetConvertExpression((DateTimeOffset v) => v.UtcDateTime);
			}

			static Guid ByteArrayToGuid(byte[] raw)
			{
				if (raw.Length == 16)
					return new Guid(raw);

				return Guid.Parse(Encoding.UTF8.GetString(raw));
			}
		}

		public sealed class ClientMappingSchema : LockedMappingSchema
		{
			public ClientMappingSchema() : base(ProviderName.ClickHouseDriver, new MappingSchema?[] { ClickHouseProviderAdapter.GetInstance(ClickHouseProvider.ClickHouseDriver).MappingSchema, Instance }.Where(_ => _ != null).ToArray()!)
			{
				var adapter = ClickHouseProviderAdapter.GetInstance(ClickHouseProvider.ClickHouseDriver);

				if (adapter.DriverDecimalType != null)
				{
					SetValueToSqlConverter(adapter.DriverDecimalType, (sb,dt,_,v) => ConvertClientDecimal(sb, dt, adapter.DriverDecimalToStringConverter!(v)));
				}
			}

			private static void ConvertClientDecimal(StringBuilder sb, SqlDataType dt, string value)
			{
				var scale = dt.Type.Scale ?? DEFAULT_DECIMAL_SCALE;

				switch (dt.Type.DataType)
				{
					case DataType.Decimal32 :
						sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal32('{0}', {1})", value, scale);
						break;
					case DataType.Undefined :
					case DataType.Decimal64 :
						sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal64('{0}', {1})", value, scale);
						break;
					case DataType.Decimal128:
						sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal128('{0}', {1})", value, scale);
						break;
					case DataType.Decimal256:
						sb.AppendFormat(CultureInfo.InvariantCulture, "toDecimal256('{0}', {1})", value, scale);
						break;
					default:
						throw new LinqToDBConvertException($"Unsupported Decimal type mapping: {dt.Type.DataType}");
				}
			}
		}

		public sealed class MySqlMappingSchema : LockedMappingSchema
		{
			public MySqlMappingSchema() : base(ProviderName.ClickHouseMySql, Instance)
			{
				// all conversions below are due to single issue in CH:
				// https://github.com/ClickHouse/ClickHouse/issues/39297
				SetConvertExpression((string v) => StringToFloat (v));
				SetConvertExpression((string v) => StringToDouble(v));
				SetConvertExpression((string v) => StringToDateTimeOffset(v));
				SetConvertExpression((string v) => StringToTimeSpan(v));
				SetConvertExpression((string v) => v == "nan" ? default :           StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (byte    )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (sbyte   )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (short   )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (ushort  )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (int     )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (uint    )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (long    )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? default : (ulong   )StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (decimal?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (byte   ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (sbyte  ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (short  ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (ushort ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (int    ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (uint   ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (long   ?)StringToDecimal(v));
				SetConvertExpression((string v) => v == "nan" ? null    : (ulong  ?)StringToDecimal(v));
			}

			static decimal StringToDecimal(string raw)
			{
				// try to parse value as decimal first as it supports more digits
				if (decimal.TryParse(raw, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var dec))
					return dec;

				// if failed (e.g. value has exponent) - parse as double
				return checked((decimal)double.Parse(raw, CultureInfo.InvariantCulture));
			}

			static DateTimeOffset StringToDateTimeOffset(string raw)
			{
				if (DateTimeOffset.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
					return date;

				return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
			}

			static float StringToFloat(string raw)
			{
				if (string.Equals(raw, "-inf", StringComparison.Ordinal)) return float.NegativeInfinity;
				if (string.Equals(raw, "inf", StringComparison.Ordinal)) return float.PositiveInfinity;
				if (string.Equals(raw, "nan", StringComparison.Ordinal)) return float.NaN;

				return float.Parse(raw, CultureInfo.InvariantCulture);
			}

			static double StringToDouble(string raw)
			{
				if (string.Equals(raw, "-inf", StringComparison.Ordinal)) return double.NegativeInfinity;
				if (string.Equals(raw, "inf", StringComparison.Ordinal)) return double.PositiveInfinity;
				if (string.Equals(raw, "nan", StringComparison.Ordinal)) return double.NaN;

				return double.Parse(raw, CultureInfo.InvariantCulture);
			}

			static TimeSpan StringToTimeSpan(string raw)
			{
				if (long.TryParse(raw, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var ticks))
					return TimeSpan.FromTicks(ticks);

				return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
			}
		}
	}
}
