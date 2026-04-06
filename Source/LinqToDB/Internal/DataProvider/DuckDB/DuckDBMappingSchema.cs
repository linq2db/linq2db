using System;
using System.Globalization;
using System.Numerics;
using System.Text;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public sealed class DuckDBMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT       = CompositeFormat.Parse("'{0:yyyy-MM-dd}'::DATE");
		private static readonly CompositeFormat TIMESTAMP0_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss}'::TIMESTAMP");
		private static readonly CompositeFormat TIMESTAMP6_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss.ffffff}'::TIMESTAMP");
#else
		private const string DATE_FORMAT       = "'{0:yyyy-MM-dd}'::DATE";
		private const string TIMESTAMP0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss}'::TIMESTAMP";
		private const string TIMESTAMP6_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.ffffff}'::TIMESTAMP";
#endif

		public DuckDBMappingSchema() : base(ProviderName.DuckDB)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetValueToSqlConverter(typeof(bool),       (sb, _,_,v) => sb.Append((bool)v));
			SetValueToSqlConverter(typeof(string),     (sb, _,_,v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char),       (sb, _,_,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),     (sb, _,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Guid),       (sb, _,_,v) => sb.AppendFormat(CultureInfo.InvariantCulture, "'{0:D}'::UUID", (Guid)v));
			SetValueToSqlConverter(typeof(DateTime),   (sb,dt,_,v) => BuildDateTime(sb, (DateTime)v));
			SetValueToSqlConverter(typeof(BigInteger), (sb, _,_,v) => sb.Append(((BigInteger)v).ToString(CultureInfo.InvariantCulture)));

			SetValueToSqlConverter(typeof(float) , (sb,_,_,v) =>
			{
				var f = (float)v;
				if (float.IsNaN(f))
					sb.Append("'NaN'::FLOAT");
				else if (float.IsPositiveInfinity(f))
					sb.Append("'Infinity'::FLOAT");
				else if (float.IsNegativeInfinity(f))
					sb.Append("'-Infinity'::FLOAT");
				else
				{
					// DuckDB parses numeric literals as DOUBLE by default.
					// Cast to FLOAT explicitly to avoid precision mismatch.
					sb.Append('\'');
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", f);
					sb.Append("'::FLOAT");
				}
			});
			SetValueToSqlConverter(typeof(double), (sb,_,_,v) =>
			{
				var d = (double)v;
				if (double.IsNaN(d))
					sb.Append("'NaN'::DOUBLE");
				else if (double.IsPositiveInfinity(d))
					sb.Append("'Infinity'::DOUBLE");
				else if (double.IsNegativeInfinity(d))
					sb.Append("'-Infinity'::DOUBLE");
				else
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}", d);
			});

			AddScalarType(typeof(string),   DataType.NVarChar);
			AddScalarType(typeof(byte[]),   DataType.VarBinary);
			AddScalarType(typeof(TimeSpan), DataType.Time);

			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,_,_,v) => BuildDateTimeOffset(sb, (DateTimeOffset)v));
			SetValueToSqlConverter(typeof(TimeSpan),       (sb,_,_,v) => BuildTimeSpan(sb, (TimeSpan)v));

#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly), (sb,_,_,v) => BuildDate(sb, (DateOnly)v));
#endif
		}

		static void BuildDateTime(StringBuilder stringBuilder, DateTime value)
		{
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
			string format;
#endif

			if (value.Hour == 0 && value.Minute == 0 && value.Second == 0 && value.Ticks % TimeSpan.TicksPerSecond == 0)
				format = DATE_FORMAT;
			else if (value.Ticks % TimeSpan.TicksPerSecond == 0)
				format = TIMESTAMP0_FORMAT;
			else
				format = TIMESTAMP6_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

#if SUPPORTS_DATEONLY
		static void BuildDate(StringBuilder stringBuilder, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}
#endif

		static void BuildDateTimeOffset(StringBuilder sb, DateTimeOffset value)
		{
			// DuckDB TIMESTAMPTZ: microsecond precision, always stored as UTC
			var micros = Math.Abs(value.Ticks % TimeSpan.TicksPerSecond) / 10;
			sb.Append(CultureInfo.InvariantCulture, $"'{value.UtcDateTime:yyyy-MM-dd HH:mm:ss}");
			if (micros > 0)
				sb.Append(CultureInfo.InvariantCulture, $".{micros:000000}");
			sb.Append("+00'::TIMESTAMPTZ");
		}

		static void BuildTimeSpan(StringBuilder sb, TimeSpan value)
		{
			// DuckDB TIME: microsecond precision
			var micros = Math.Abs(value.Ticks % TimeSpan.TicksPerSecond) / 10;
			if (value.TotalHours is >= 24 or <= -24 || value < TimeSpan.Zero)
			{
				// INTERVAL for values outside TIME range
				sb.Append("INTERVAL '");
				if (value.TotalDays is >= 1 or <= -1)
					sb.Append(CultureInfo.InvariantCulture, $"{(int)value.TotalDays} days ");
				sb.Append(CultureInfo.InvariantCulture, $"{value.Hours:00}:{value.Minutes:00}:{value.Seconds:00}");
				if (micros > 0)
					sb.Append(CultureInfo.InvariantCulture, $".{micros:000000}");
				sb.Append('\'');
			}
			else
			{
				sb.Append(CultureInfo.InvariantCulture, $"'{value.Hours:00}:{value.Minutes:00}:{value.Seconds:00}");
				if (micros > 0)
					sb.Append(CultureInfo.InvariantCulture, $".{micros:000000}");
				sb.Append("'::TIME");
			}
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			// DuckDB \x escape reads exactly 2 hex chars per escape.
			// Must emit \xHH for EACH byte: '\x00\x01\x02'::BLOB
			stringBuilder.Append('\'');
			foreach (var b in value)
				stringBuilder.Append($"\\x{b:X2}");
			stringBuilder.Append("'::BLOB");
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"chr({value})");
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static MappingSchema Instance { get; } = new DuckDBMappingSchema();
	}
}
