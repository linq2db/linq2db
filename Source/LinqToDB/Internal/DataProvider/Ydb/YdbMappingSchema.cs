using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public sealed class YdbMappingSchema : LockedMappingSchema
	{
		// provider hardcodes precision/scale to those values for parameters, so we will use them as safe defaults for now
		internal const int   DEFAULT_DECIMAL_PRECISION = 22;
		internal const int   DEFAULT_DECIMAL_SCALE     = 9;
		internal const string DEFAULT_TIMEZONE         = "GMT";

#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT         = CompositeFormat.Parse("Date('{0:yyyy-MM-dd}')");
		private static readonly CompositeFormat DATETIME_FORMAT     = CompositeFormat.Parse("Datetime('{0:yyyy-MM-ddTHH:mm:ssZ}')");
		private static readonly CompositeFormat TIMESTAMP_FORMAT    = CompositeFormat.Parse("Timestamp('{0:yyyy-MM-ddTHH:mm:ss.ffffffZ}')");

		private static readonly CompositeFormat TZ_DATE_FORMAT      = CompositeFormat.Parse("TzDate('{0:yyyy-MM-dd},{1}')");
		private static readonly CompositeFormat TZ_DATETIME_FORMAT  = CompositeFormat.Parse("TzDatetime('{0:yyyy-MM-ddTHH:mm:ss.fff},{1}')");
		private static readonly CompositeFormat TZ_TIMESTAMP_FORMAT = CompositeFormat.Parse("TzTimestamp('{0:yyyy-MM-ddTHH:mm:ss.fff},{1}')");

		private static readonly CompositeFormat UUID_FORMAT         = CompositeFormat.Parse("Uuid('{0:D}')");
		private static readonly CompositeFormat DECIMAL_FORMAT      = CompositeFormat.Parse("Decimal('{0}', {1}, {2})");

		private static readonly CompositeFormat INT8_FORMAT         = CompositeFormat.Parse("{0}t");
		private static readonly CompositeFormat UINT8_FORMAT        = CompositeFormat.Parse("{0}ut");
		private static readonly CompositeFormat INT16_FORMAT        = CompositeFormat.Parse("{0}s");
		private static readonly CompositeFormat UINT16_FORMAT       = CompositeFormat.Parse("{0}us");
		private static readonly CompositeFormat INT32_FORMAT        = CompositeFormat.Parse("{0}");
		private static readonly CompositeFormat UINT32_FORMAT       = CompositeFormat.Parse("{0}u");
		private static readonly CompositeFormat INT64_FORMAT        = CompositeFormat.Parse("{0}l");
		private static readonly CompositeFormat UINT64_FORMAT       = CompositeFormat.Parse("{0}ul");
		private static readonly CompositeFormat FLOAT_FORMAT        = CompositeFormat.Parse("Float('{0:G9}')");
		private static readonly CompositeFormat DOUBLE_FORMAT       = CompositeFormat.Parse("Double('{0:G17}')");
		private static readonly CompositeFormat DY_NUMBER_FORMAT    = CompositeFormat.Parse("DyNumber('{0}')");

#else
		private const           string          DATE_FORMAT         = "Date('{0:yyyy-MM-dd}')";
		private const           string          DATETIME_FORMAT     = "Datetime('{0:yyyy-MM-ddTHH:mm:ssZ}')";
		private const           string          TIMESTAMP_FORMAT    = "Timestamp('{0:yyyy-MM-ddTHH:mm:ss.ffffffZ}')";

		private const           string          TZ_DATE_FORMAT      = "TzDate('{0:yyyy-MM-dd},{1}')";
		private const           string          TZ_DATETIME_FORMAT  = "TzDatetime('{0:yyyy-MM-ddTHH:mm:ss.fff},{1}')";
		private const           string          TZ_TIMESTAMP_FORMAT = "TzTimestamp('{0:yyyy-MM-ddTHH:mm:ss.fff},{1}')";

		private const           string          UUID_FORMAT         = "Uuid('{0:D}')";
		private const           string          DECIMAL_FORMAT      = "Decimal('{0}', {1}, {2})";

		private const           string          INT8_FORMAT         = "{0}t";
		private const           string          UINT8_FORMAT        = "{0}ut";
		private const           string          INT16_FORMAT        = "{0}s";
		private const           string          UINT16_FORMAT       = "{0}us";
		private const           string          INT32_FORMAT        = "{0}";
		private const           string          UINT32_FORMAT       = "{0}u";
		private const           string          INT64_FORMAT        = "{0}l";
		private const           string          UINT64_FORMAT       = "{0}ul";
		private const           string          FLOAT_FORMAT        = "Float('{0:G9}')";
		private const           string          DOUBLE_FORMAT       = "Double('{0:G17}')";
		private const           string          DY_NUMBER_FORMAT    = "DyNumber('{0}')";
#endif

		public YdbMappingSchema() : base(ProviderName.Ydb)
		{
			AddScalarType(typeof(DateTimeOffset), DataType.DateTime2);
			AddScalarType(typeof(TimeSpan),       DataType.Interval);
			AddScalarType(typeof(MemoryStream),   DataType.VarBinary);

			SetValueToSqlConverter(typeof(string)        , (sb,dt,_,v) => ConvertString        (sb, dt, (string)v));
			SetValueToSqlConverter(typeof(char)          , (sb,dt,_,v) => ConvertString        (sb, dt, ((char)v).ToString()));
			SetValueToSqlConverter(typeof(byte[])        , (sb,dt,_,v) => ConvertByteArray     (sb, dt, (byte[])v));
			SetValueToSqlConverter(typeof(MemoryStream)  , (sb,dt,_,v) => ConvertByteArray     (sb, dt, ((MemoryStream)v).ToArray()));

			SetValueToSqlConverter(typeof(byte)          , (sb,dt,_,v) => ConvertByte          (sb, dt, (byte)v));
			SetValueToSqlConverter(typeof(sbyte)         , (sb,dt,_,v) => ConvertSByte         (sb, dt, (sbyte)v));
			SetValueToSqlConverter(typeof(short)         , (sb,dt,_,v) => ConvertInt16         (sb, dt, (short)v));
			SetValueToSqlConverter(typeof(ushort)        , (sb,dt,_,v) => ConvertUInt16        (sb, dt, (ushort)v));
			SetValueToSqlConverter(typeof(int)           , (sb,dt,_,v) => ConvertInt32         (sb, dt, (int)v));
			SetValueToSqlConverter(typeof(uint)          , (sb,dt,_,v) => ConvertUInt32        (sb, dt, (uint)v));
			SetValueToSqlConverter(typeof(long)          , (sb,dt,_,v) => ConvertInt64         (sb, dt, (long)v));
			SetValueToSqlConverter(typeof(ulong)         , (sb,dt,_,v) => ConvertUInt64        (sb, dt, (ulong)v));
			SetValueToSqlConverter(typeof(float)         , (sb,dt,_,v) => ConvertFloat         (sb, dt, (float)v));
			SetValueToSqlConverter(typeof(double)        , (sb,dt,_,v) => ConvertDouble        (sb, dt, (double)v));
			SetValueToSqlConverter(typeof(decimal)       , (sb,dt,_,v) => ConvertDecimal       (sb, dt, (decimal)v));

			SetValueToSqlConverter(typeof(Guid)          , (sb,dt,_,v) => ConvertGuid          (sb, dt, (Guid)v));
			SetValueToSqlConverter(typeof(bool)          , (sb,dt,_,v) => ConvertBool          (sb, dt, (bool)v));

			SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpan      (sb, dt, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTime      (sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffset(sb, dt, (DateTimeOffset)v));
#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateOnly      (sb, dt, (DateOnly)v));
#endif
		}

		#region Type to SQL converters (for multi-bindings)

		private static void ConvertString(StringBuilder stringBuilder, SqlDataType dt, string value)
		{
			if (dt.Type.DataType == DataType.DecFloat)
			{
				BuildDyNumberLiteral(stringBuilder, value);
			}
			else if (dt.Type.DataType == DataType.Decimal)
			{
				BuildDecimalLiteral(stringBuilder, value, dt);
			}
			else if (dt.Type.DataType == DataType.BinaryJson)
			{
				stringBuilder.Append("JsonDocument(");
				BuildStringLiteral(stringBuilder, value);
				stringBuilder.Append(')');
			}
			else
			{
				BuildStringLiteral(stringBuilder, value);

				// apply type for non-String types
				var suffix = dt.Type.DataType switch
				{
					DataType.DecFloat or DataType.Decimal or DataType.VarBinary or DataType.Binary => 's',
					DataType.Json => 'j',
					DataType.Yson => 'y',
					_ => 'u'
				};

				stringBuilder.Append(suffix);
			}
		}

		private static void ConvertByteArray(StringBuilder stringBuilder, SqlDataType dt, byte[] value)
		{
			if (dt.Type.DataType == DataType.BinaryJson)
			{
				stringBuilder.Append("JsonDocument(");
				BuildBinaryLiteral(stringBuilder, value);
				stringBuilder.Append(')');
			}
			else
			{
				BuildBinaryLiteral(stringBuilder, value);

				// apply type for non-String types
				var suffix = dt.Type.DataType switch
				{
					DataType.Json => 'j',
					DataType.Yson => 'y',
					_ => 's'
				};

				stringBuilder.Append(suffix);
			}
		}

		private static void ConvertGuid(StringBuilder sb, SqlDataType dt, Guid value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.VarChar:
				case DataType.NVarChar:
					ConvertString(sb, dt, value.ToString("D"));
					break;
				case DataType.Binary  :
					ConvertByteArray(sb, dt, value.ToByteArray());
					break;
				case DataType.Guid    :
				default               :
					BuildUUIDLiteral(sb, value);
					break;
			}
		}

		private static void ConvertByte(StringBuilder sb, SqlDataType dt, byte value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value)); return;
				default                :
				case DataType.Byte     : BuildByteLiteral(sb, value);                  return;
				case DataType.UInt16   : BuildUInt16Literal(sb, value);                return;
				case DataType.Int16    : BuildInt16Literal(sb, value);                 return;
				case DataType.UInt32   : BuildUInt32Literal(sb, value);                return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                 return;
				case DataType.UInt64   : BuildUInt64Literal(sb, value);                return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                 return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                 return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);           return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);     return;
			}
		}

		private static void ConvertSByte(StringBuilder sb, SqlDataType dt, sbyte value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				default                :
				case DataType.SByte    : BuildSByteLiteral(sb, value);                   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				case DataType.Int16    : BuildInt16Literal(sb, value);                   return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));   return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                   return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                   return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                  return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);             return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);       return;
			}
		}

		private static void ConvertInt16(StringBuilder sb, SqlDataType dt, short value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				default                :
				case DataType.Int16    : BuildInt16Literal(sb, value);                   return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));   return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                   return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                   return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                  return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);             return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);       return;
			}
		}

		private static void ConvertUInt16(StringBuilder sb, SqlDataType dt, ushort value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));    return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));  return;
				default                :
				case DataType.UInt16   : BuildUInt16Literal(sb, value);                 return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));  return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));  return;
				case DataType.Int32    : BuildInt32Literal(sb, value);                  return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value)); return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                  return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                  return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                 return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);            return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);      return;
			}
		}

		private static void ConvertInt32(StringBuilder sb, SqlDataType dt, int value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));   return;
				default                :
				case DataType.Int32    : BuildInt32Literal(sb, value);                   return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                   return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                  return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);             return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);       return;
			}
		}

		private static void ConvertUInt32(StringBuilder sb, SqlDataType dt, uint value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));     return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));   return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));   return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value)); return;
				default                :
				case DataType.UInt32   : BuildUInt32Literal(sb, value);                  return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));     return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                   return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                  return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);             return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);       return;
			}
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
				default                :
				case DataType.Int64    : BuildInt64Literal(sb, value);                   return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));  return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                   return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                  return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);             return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);       return;
			}
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
				default                :
				case DataType.UInt64   : BuildUInt64Literal(sb, value);                  return;
				case DataType.Int64    : BuildInt64Literal(sb, checked((long)value));    return;
				case DataType.Single   : BuildFloatLiteral(sb, value);                   return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                  return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, value, dt);             return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (decimal)value);       return;
			}
		}

		private static void ConvertFloat(StringBuilder sb, SqlDataType dt, float value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));           return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));         return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));         return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value));       return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));           return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));         return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));        return;
				case DataType.Int64    : BuildInt64Literal(sb, checked((long)value));          return;
				default                :
				case DataType.Single   : BuildFloatLiteral(sb, value);                         return;
				case DataType.Double   : BuildDoubleLiteral(sb, value);                        return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, checked((decimal)value), dt); return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, (double)value);              return;
			}
		}

		private static void ConvertDouble(StringBuilder sb, SqlDataType dt, double value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));           return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));         return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));         return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value));       return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));           return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));         return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));        return;
				case DataType.Int64    : BuildInt64Literal(sb, checked((long)value));          return;
				case DataType.Single   : BuildFloatLiteral(sb, checked((float)value));         return;
				default                :
				case DataType.Double   : BuildDoubleLiteral(sb, value);                        return;
				case DataType.Decimal  : BuildDecimalLiteral(sb, checked((decimal)value), dt); return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, value);                      return;
			}
		}

		private static void ConvertDecimal(StringBuilder sb, SqlDataType dt, decimal value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, checked((byte)value));           return;
				case DataType.SByte    : BuildSByteLiteral(sb, checked((sbyte)value));         return;
				case DataType.Int16    : BuildInt16Literal(sb, checked((short)value));         return;
				case DataType.UInt16   : BuildUInt16Literal(sb, checked((ushort)value));       return;
				case DataType.Int32    : BuildInt32Literal(sb, checked((int)value));           return;
				case DataType.UInt32   : BuildUInt32Literal(sb, checked((uint)value));         return;
				case DataType.UInt64   : BuildUInt64Literal(sb, checked((ulong)value));        return;
				case DataType.Int64    : BuildInt64Literal(sb, checked((long)value));          return;
				case DataType.Single   : BuildFloatLiteral(sb, checked((float)value));         return;
				case DataType.Double   : BuildDoubleLiteral(sb, checked((double)value));       return;
				default                :
				case DataType.Decimal  : BuildDecimalLiteral(sb, checked((decimal)value), dt); return;
				case DataType.DecFloat : BuildDyNumberLiteral(sb, value);                      return;
			}
		}

		private static void ConvertTimeSpan(StringBuilder sb, SqlDataType dt, TimeSpan value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Int64    : ConvertInt64(sb, dt, value.Ticks); break;
				default                :
				case DataType.Interval : BuildIntervalLiteral(sb, value);   break;
			}
		}

		private static void ConvertDateTime(StringBuilder sb, SqlDataType dt, DateTime value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Date       : BuildDateLiteral(sb, value.Date);   break;
				case DataType.DateTz     : BuildDateTzLiteral(sb, value.Date); break;
				case DataType.DateTime   : BuildDateTimeLiteral(sb, value);    break;
				case DataType.DateTimeTz : BuildDateTimeTzLiteral(sb, value);  break;
				default                  :
				case DataType.DateTime2  : BuildTimestampLiteral(sb, value);   break;
				case DataType.DateTime2Tz: BuildTimestampTzLiteral(sb, value); break;
			}
		}

		private static void ConvertDateTimeOffset(StringBuilder sb, SqlDataType dt, DateTimeOffset value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Date       : BuildDateLiteral(sb, value.Date);                 break;
				case DataType.DateTz     : BuildDateTzLiteral(sb, value.LocalDateTime.Date); break;
				case DataType.DateTime   : BuildDateTimeLiteral(sb, value.LocalDateTime);    break;
				case DataType.DateTimeTz : BuildDateTimeTzLiteral(sb, value.UtcDateTime);    break;
				default                  :
				case DataType.DateTime2  : BuildTimestampLiteral(sb, value.UtcDateTime);     break;
				case DataType.DateTime2Tz: BuildTimestampTzLiteral(sb, value.UtcDateTime);   break;
			}
		}

#if SUPPORTS_DATEONLY
		private static void ConvertDateOnly(StringBuilder sb, SqlDataType dt, DateOnly value)
		{
			switch (dt.Type.DataType)
			{
				default             :
				case DataType.Date  : BuildDateLiteral(sb, value.ToDateTime(default));   break;
				case DataType.DateTz: BuildDateTzLiteral(sb, value.ToDateTime(default)); break;
			}
		}
#endif

		private static void ConvertBool(StringBuilder sb, SqlDataType dt, bool value)
		{
			switch (dt.Type.DataType)
			{
				case DataType.Byte     : BuildByteLiteral(sb, value ? (byte)1 : (byte)0); return;
				default                :
				case DataType.Boolean  : BuildBooleanLiteral(sb, value);                  return;
			}
		}
#endregion

#region Literal generators

		private static void BuildStringLiteral(StringBuilder stringBuilder, string value)
		{
			stringBuilder.Append('\'');

			foreach (var chr in value)
			{
				switch (chr)
				{
					case '\0':
						stringBuilder.Append("\\x00");
						continue;
					case '\'':
						stringBuilder.Append("\\'");
						continue;
					case '\\':
						stringBuilder.Append("\\\\");
						continue;
				}

				stringBuilder.Append(chr);
			}

			stringBuilder.Append('\'');
		}

		private static void BuildBinaryLiteral(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append('\'');

			DataTools.BuildHexString(stringBuilder, value);

			stringBuilder
				.Append('\'');
		}

		private static void BuildUUIDLiteral(StringBuilder sb, Guid value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, UUID_FORMAT, value);
		}

		private static void BuildByteLiteral(StringBuilder sb, byte value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, UINT8_FORMAT, value);
		}

		private static void BuildSByteLiteral(StringBuilder sb, sbyte value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, INT8_FORMAT, value);
		}

		private static void BuildInt16Literal(StringBuilder sb, short value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, INT16_FORMAT, value);
		}

		private static void BuildUInt16Literal(StringBuilder sb, ushort value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, UINT16_FORMAT, value);
		}

		private static void BuildInt32Literal(StringBuilder sb, int value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, INT32_FORMAT, value);
		}

		private static void BuildUInt32Literal(StringBuilder sb, uint value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, UINT32_FORMAT, value);
		}

		private static void BuildInt64Literal(StringBuilder sb, long value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, INT64_FORMAT, value);
		}

		private static void BuildUInt64Literal(StringBuilder sb, ulong value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, UINT64_FORMAT, value);
		}

		private static void BuildFloatLiteral(StringBuilder sb, float value)
		{
			if (float.IsNegativeInfinity(value))
				sb.Append("Float('-inf')");
			else if (float.IsPositiveInfinity(value))
				sb.Append("Float('inf')");
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, FLOAT_FORMAT, value);
		}

		private static void BuildDoubleLiteral(StringBuilder sb, double value)
		{
			if (double.IsNegativeInfinity(value))
				sb.Append("Double('-inf')");
			else if (double.IsPositiveInfinity(value))
				sb.Append("Double('inf')");
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, DOUBLE_FORMAT, value);
		}

		private static void BuildDecimalLiteral(StringBuilder sb, decimal value, SqlDataType dt)
		{
			sb.AppendFormat(
				CultureInfo.InvariantCulture,
				DECIMAL_FORMAT,
				value,
				dt.Type.Precision ?? DEFAULT_DECIMAL_PRECISION,
				dt.Type.Scale ?? DEFAULT_DECIMAL_SCALE);
		}

		private static void BuildDecimalLiteral(StringBuilder sb, string value, SqlDataType dt)
		{
			sb.AppendFormat(
				CultureInfo.InvariantCulture,
				DECIMAL_FORMAT,
				value,
				dt.Type.Precision ?? DEFAULT_DECIMAL_PRECISION,
				dt.Type.Scale ?? DEFAULT_DECIMAL_SCALE);
		}

		private static void BuildDyNumberLiteral(StringBuilder sb, decimal value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, DY_NUMBER_FORMAT, value);
		}

		private static void BuildDyNumberLiteral(StringBuilder sb, string value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, DY_NUMBER_FORMAT, value);
		}

		private static void BuildDyNumberLiteral(StringBuilder sb, double value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, DY_NUMBER_FORMAT, value);
		}

		private static void BuildBooleanLiteral(StringBuilder sb, bool value)
		{
			sb.Append(value ? "true" : "false");
		}

		private static void BuildIntervalLiteral(StringBuilder sb, TimeSpan value)
		{
			sb.Append("Interval('");
			// looks like YDB doesn't support non-constant quantifiers Y and M
			// which rises question what it means to have min/max values limited by 136 years...
			value = value.Ticks % 10 != 0 ? TimeSpan.FromTicks((value.Ticks / 10) * 10) : value;
			DataTools.ConvertToIso8601Interval(sb, value);
			sb.Append("')");
		}

		private static void BuildDateLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}

		private static void BuildDateTzLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, TZ_DATE_FORMAT, value, DEFAULT_TIMEZONE);
		}

		private static void BuildDateTimeLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
		}

		private static void BuildDateTimeTzLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, TZ_DATETIME_FORMAT, value, DEFAULT_TIMEZONE);
		}

		private static void BuildTimestampLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, TIMESTAMP_FORMAT, value);
		}

		private static void BuildTimestampTzLiteral(StringBuilder sb, DateTime value)
		{
			sb.AppendFormat(CultureInfo.InvariantCulture, TZ_TIMESTAMP_FORMAT, value, DEFAULT_TIMEZONE);
		}

		#endregion

		public static MappingSchema Instance { get; } = new YdbMappingSchema();
	}
}
