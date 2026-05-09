using System;
using System.Collections;
using System.Data.Linq;
using System.Globalization;
using System.Numerics;
using System.Text;

using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public sealed class DuckDBMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		internal static readonly CompositeFormat DATE_FORMAT         = CompositeFormat.Parse("'{0:yyyy-MM-dd}'::DATE");
		private  static readonly CompositeFormat TIMESTAMP_S_FORMAT  = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss}'::TIMESTAMP_S");
		private  static readonly CompositeFormat TIMESTAMP_MS_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss.fff}'::TIMESTAMP_MS");
		private  static readonly CompositeFormat TIMESTAMP_FORMAT    = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss.ffffff}'::TIMESTAMP");
		private  static readonly CompositeFormat TIMESTAMP_NS_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss.fffffff}'::TIMESTAMP_NS");
#else
		internal const string DATE_FORMAT         = "'{0:yyyy-MM-dd}'::DATE";
		private  const string TIMESTAMP_S_FORMAT  = "'{0:yyyy-MM-dd HH:mm:ss}'::TIMESTAMP_S";
		private  const string TIMESTAMP_MS_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'::TIMESTAMP_MS";
		private  const string TIMESTAMP_FORMAT    = "'{0:yyyy-MM-dd HH:mm:ss.ffffff}'::TIMESTAMP";
		private  const string TIMESTAMP_NS_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fffffff}'::TIMESTAMP_NS";
#endif

		public DuckDBMappingSchema() : base(ProviderName.DuckDB, DuckDBProviderAdapter.Instance.MappingSchema)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			AddScalarType(typeof(string),     DataType.NVarChar);
			AddScalarType(typeof(byte[]),     DataType.VarBinary);
			AddScalarType(typeof(TimeSpan),   DataType.Interval);
			AddScalarType(typeof(BigInteger), DataType.VarNumeric);
			AddScalarType(typeof(BitArray),   DataType.BitArray);

			SetValueToSqlConverter(typeof(bool          ), (sb, _, _, v) => sb.Append             ((bool)v));
			SetValueToSqlConverter(typeof(string        ), (sb,dt, _, v) => ConvertStringToSql    (sb, (string)v, dt));
			SetValueToSqlConverter(typeof(char          ), (sb, _, _, v) => ConvertCharToSql      (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]        ), (sb,dt, _, v) => ConvertBinaryToSql    (sb, (byte[])v, dt));
			SetValueToSqlConverter(typeof(Binary        ), (sb,dt, _, v) => ConvertBinaryToSql    (sb, ((Binary)v).ToArray(), dt));
			SetValueToSqlConverter(typeof(Guid          ), (sb, _, _, v) => sb.AppendFormat       (CultureInfo.InvariantCulture, "'{0:D}'::UUID", (Guid)v));
			SetValueToSqlConverter(typeof(DateTime      ), (sb,dt, _, v) => BuildDateTime         (sb, (DateTime)v, dt));
			SetValueToSqlConverter(typeof(BigInteger    ), (sb,dt, _, v) => BuildBigIntegerLiteral(sb, (BigInteger)v, dt));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt, _, v) => BuildDateTimeOffset   (sb, (DateTimeOffset)v, dt));
			SetValueToSqlConverter(typeof(TimeSpan      ), (sb,dt, _, v) => BuildTimeSpan         (sb, (TimeSpan)v, dt));
			SetValueToSqlConverter(typeof(BitArray      ), (sb, _, _, v) => BuildBitArray         (sb, (BitArray)v));
			SetValueToSqlConverter(typeof(float         ), (sb, _, _, v) => BuildFloat            (sb, (float)v));
			SetValueToSqlConverter(typeof(double        ), (sb, _, _, v) => BuildDouble           (sb, (double)v));
#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly      ), (sb, _, _, v) => BuildDateOnly         (sb, (DateOnly)v));
			SetValueToSqlConverter(typeof(TimeOnly      ), (sb,dt, _, v) => BuildTimeOnly         (sb, (TimeOnly)v, dt));
#endif
		}

		private static void BuildDouble(StringBuilder sb, double value)
		{
			if (double.IsNaN(value))
				sb.Append("'NaN'::DOUBLE");
			else if (double.IsPositiveInfinity(value))
				sb.Append("'Infinity'::DOUBLE");
			else if (double.IsNegativeInfinity(value))
				sb.Append("'-Infinity'::DOUBLE");
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}", value);
		}

		private static void BuildFloat(StringBuilder sb, float value)
		{
			if (float.IsNaN(value))
				sb.Append("'NaN'::FLOAT");
			else if (float.IsPositiveInfinity(value))
				sb.Append("'Infinity'::FLOAT");
			else if (float.IsNegativeInfinity(value))
				sb.Append("'-Infinity'::FLOAT");
			else
			{
				// DuckDB parses numeric literals as DOUBLE by default.
				// Cast to FLOAT explicitly to avoid precision mismatch.
				sb.Append('\'');
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", value);
				sb.Append("'::FLOAT");
			}
		}

		internal static StringBuilder BuildBigIntegerLiteral(StringBuilder sb, BigInteger value, SqlDataType type)
		{
			if (type.Type.DataType is DataType.VarNumeric)
			{
				return sb
					.Append('\'')
					.Append(value.ToString(CultureInfo.InvariantCulture))
					.Append("'::BIGNUM")
					;
			}
			else
			{
				return sb.Append(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		static void BuildDateTime(StringBuilder stringBuilder, DateTime value, SqlDataType type)
		{
			var format = (type.Type.DataType, type.Type.Precision) switch
			{
				(DataType.Date, _) => DATE_FORMAT,
				(_, > 6)           => TIMESTAMP_NS_FORMAT,
				(_, null or > 3)   => TIMESTAMP_FORMAT,
				(_, > 0)           => TIMESTAMP_MS_FORMAT,
				(_, 0)             => TIMESTAMP_S_FORMAT,
				_                  => TIMESTAMP_FORMAT,
			};

			if (format == TIMESTAMP_NS_FORMAT)
			{
				if (IsPositiveInfinityTsNs(value.Year, (byte)value.Month, (byte)value.Day, (byte)value.Hour, (byte)value.Minute, (byte)value.Second, (int)(value.Ticks % TimeSpan.TicksPerSecond) * 100))
				{
					stringBuilder.Append("'infinity'::TIMESTAMP_NS");
					return;
				}

				if (IsNegativeInfinityTsNs(value.Year, (byte)value.Month, (byte)value.Day, (byte)value.Hour, (byte)value.Minute, (byte)value.Second, (int)(value.Ticks % TimeSpan.TicksPerSecond) * 100))
				{
					stringBuilder.Append("'-infinity'::TIMESTAMP_NS");
					return;
				}
			}

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

#if SUPPORTS_DATEONLY
		static void BuildDateOnly(StringBuilder stringBuilder, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}

		static void BuildTimeOnly(StringBuilder sb, TimeOnly value, SqlDataType type)
		{
			sb.Append(CultureInfo.InvariantCulture, $"'{value.Hour:00}:{value.Minute:00}:{value.Second:00}");

			if (value.Microsecond > 0)
			{
				if (type.Type.Precision > 6 || type.Type.DataType == DataType.TimeTZ)
					sb.Append(CultureInfo.InvariantCulture, $".{value.Microsecond:0000000}");
				else
					sb.Append(CultureInfo.InvariantCulture, $".{value.Microsecond:000000}");
			}

			if (type.Type.DataType == DataType.TimeTZ)
			{
				sb.Append("+00:00'::TIMETZ");
			}
			else
			{
				sb.Append("'::");
				sb.Append(type.Type.Precision > 6 ? "TIME_NS" : "TIME");
			}
		}
#endif

		static void BuildDateTimeOffset(StringBuilder sb, DateTimeOffset value, SqlDataType type)
		{
			if (type.Type.DataType == DataType.TimeTZ)
			{
				sb.Append(CultureInfo.InvariantCulture, $"'{value:HH:mm:ss.ffffffzzz}'::TIMETZ");
				return;
			}

			if (type.Type.DataType == DataType.DateTime)
			{
				// just throw-away offset
				BuildDateTime(sb, value.DateTime, type);
				return;
			}

			// DuckDB TIMESTAMPTZ: microsecond precision, always stored as UTC
			var micros = Math.Abs(value.Ticks % TimeSpan.TicksPerSecond) / 10;
			sb.Append(CultureInfo.InvariantCulture, $"'{value.UtcDateTime:yyyy-MM-dd HH:mm:ss}");
			if (micros > 0)
				sb.Append(CultureInfo.InvariantCulture, $".{micros:000000}");
			sb.Append("+00'::TIMESTAMPTZ");
		}

		static void BuildTimeSpan(StringBuilder sb, TimeSpan value, SqlDataType type)
		{
			if (type.Type.DataType == DataType.Int64)
			{
				sb.Append(value.Ticks.ToString(CultureInfo.InvariantCulture));
				return;
			}

			// DuckDB TIME: microsecond precision
			var micros = Math.Abs(value.Ticks % TimeSpan.TicksPerSecond) / 10;
			if (value.TotalHours is >= 24 or <= -24 || value < TimeSpan.Zero)
			{
				// INTERVAL for values outside TIME range
				sb.Append("INTERVAL '");
				if (value.TotalDays is >= 1 or <= -1)
					sb.Append(CultureInfo.InvariantCulture, $"{(int)value.TotalDays} days ");

				if (value < TimeSpan.Zero)
					sb.Append(CultureInfo.InvariantCulture, $"{-value.Hours:00}:{-value.Minutes:00}:{-value.Seconds:00}");
				else
					sb.Append(CultureInfo.InvariantCulture, $"{value.Hours:00}:{value.Minutes:00}:{value.Seconds:00}");

				if (micros > 0)
					sb.Append(CultureInfo.InvariantCulture, $".{micros:000000}");
				sb.Append('\'');
			}
			else
			{
				var typeName = (type.Type.DataType, type.Type.DbType) switch
				{
					(_, { } dbType)         => dbType,
					(DataType.Time, null)   => "TIME",
					(DataType.TimeTZ, null) => "TIMETZ",
					_                       => "INTERVAL",
				};

				sb.Append(CultureInfo.InvariantCulture, $"'{value.Hours:00}:{value.Minutes:00}:{value.Seconds:00}");

				if (micros > 0)
					sb.Append(CultureInfo.InvariantCulture, $".{micros:000000}");

				if (type.Type.DataType == DataType.TimeTZ)
					sb.Append("+00:00");

				sb.Append("'::").Append(typeName);
			}
		}

		static void BuildBitArray(StringBuilder sb, BitArray value)
		{
			sb.Append('\'');

			for (var i = 0; i < value.Length; i++)
				sb.Append(value.Get(i) ? '1' : '0');

			sb.Append("'::BITSTRING");
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value, SqlDataType type)
		{
			if (type.Type.DataType == DataType.BitArray)
			{
				BuildBitArray(stringBuilder, new BitArray(value));
				return;
			}

			// DuckDB \x escape reads exactly 2 hex chars per escape.
			// Must emit \xHH for EACH byte: '\x00\x01\x02'::BLOB
			stringBuilder.Append('\'');
			foreach (var b in value)
				stringBuilder.Append(provider: null, $"\\x{b:X2}");
			stringBuilder.Append("'::BLOB");
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"chr({value})");
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value, SqlDataType type)
		{
			if (type.Type.DataType == DataType.BitArray)
			{
				// no escaping, as it must contain only 0/1 or fail with sql error
				stringBuilder
					.Append('\'')
					.Append(value)
					.Append("'::BITSTRING");
				return;
			}

			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, ['\x01']);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static bool IsNegativeInfinityTsNs(int year, byte month, byte day, byte hour, byte min, byte sec, int nanosecond)
		{
			// < 1677-09-21 00:12:43.145224192
			if (year  != 1677) return year  < 1677;
			if (month != 9)    return month < 9;
			if (day   != 21)   return day   < 21;
			if (hour  != 0)    return false;
			if (min   != 12)   return min   < 12;
			if (sec   != 43)   return sec   < 43;
			return nanosecond < 145224192;
		}

		internal static bool IsPositiveInfinityTsNs(int year, byte month, byte day, byte hour, byte min, byte sec, int nanosecond)
		{
			// 2262-04-11 23:47:16.854775807
			if (year  != 2262) return year  > 2262;
			if (month != 4)    return month > 4;
			if (day   != 11)   return day   > 11;
			if (hour  != 23)   return hour  > 23;
			if (min   != 47)   return min   > 47;
			if (sec   != 16)   return sec   > 16;
			return nanosecond > 854775807;
		}

		internal static MappingSchema Instance { get; } = new DuckDBMappingSchema();
	}
}
