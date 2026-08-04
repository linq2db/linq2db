using System;
using System.Globalization;
using System.Text;

using LinqToDB.DataProvider.Informix;
using LinqToDB.Internal.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix
{
	public sealed class InformixMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT               = CompositeFormat.Parse("TO_DATE('{0:yyyy-MM-dd}', '%Y-%m-%d')");
		private static readonly CompositeFormat DATETIME_FORMAT           = CompositeFormat.Parse("TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', '%Y-%m-%d %H:%M:%S')");
		private static readonly CompositeFormat DATETIME5_EXPLICIT_FORMAT = CompositeFormat.Parse("TO_DATE('{0:yyyy-MM-dd HH:mm:ss.fffff}', '%Y-%m-%d %H:%M:%S.%F5')");
		private static readonly CompositeFormat DATETIME5_FORMAT          = CompositeFormat.Parse("TO_DATE('{0:yyyy-MM-dd HH:mm:ss.fffff}', '%Y-%m-%d %H:%M:%S%F5')");
		private static readonly CompositeFormat INTERVAL5_FORMAT          = CompositeFormat.Parse("INTERVAL({0} {1:00}:{2:00}:{3:00}.{4:00000}) DAY TO FRACTION(5)");
#else
		private const string DATE_FORMAT               = "TO_DATE('{0:yyyy-MM-dd}', '%Y-%m-%d')";
		private const string DATETIME_FORMAT           = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', '%Y-%m-%d %H:%M:%S')";
		private const string DATETIME5_EXPLICIT_FORMAT = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss.fffff}', '%Y-%m-%d %H:%M:%S.%F5')";
		private const string DATETIME5_FORMAT          = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss.fffff}', '%Y-%m-%d %H:%M:%S%F5')";
		private const string INTERVAL5_FORMAT          = "INTERVAL({0} {1:00}:{2:00}:{3:00}.{4:00000}) DAY TO FRACTION(5)";
#endif

		static readonly char[] _extraEscapes = { '\r', '\n' };

		InformixMappingSchema() : base(ProviderName.Informix)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetValueToSqlConverter(typeof(bool), (sb,_,_,v) => sb.Append('\'').Append((bool)v ? 't' : 'f').Append("'::BOOLEAN"));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
			SetDataType(typeof(byte),   new SqlDataType(DataType.Int16,    typeof(byte)));

			SetValueToSqlConverter(typeof(string),         (sb, _,_,v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),           (sb, _,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(DateTime),       (sb,dt,o,v) => ConvertDateTimeToSql(sb, dt, o, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,o,v) => ConvertDateTimeToSql(sb, dt, o, ((DateTimeOffset)v).DateTime));
			SetValueToSqlConverter(typeof(TimeSpan),       (sb, _,_,v) => BuildIntervalLiteral(sb, (TimeSpan)v));

#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly), (sb,dt,_,v) => ConvertDateOnlyToSql(sb, (DateOnly)v));
#endif
		}

		internal static TimeSpan StringToTimeSpan(string raw)
		{
			if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
				return TimeSpan.FromTicks(ticks);

			var value = raw.Trim();
			var sign  = 1;

			if (value.Length > 0 && value[0] is '-' or '+')
			{
				sign  = value[0] == '-' ? -1 : 1;
				value = value.Substring(1).TrimStart();
			}

			long days         = 0;
			var  daySeparator = -1;
			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] == ' ')
				{
					daySeparator = i;
					break;
				}
			}

			if (daySeparator >= 0)
			{
				days  = long.Parse(value.AsSpan(0, daySeparator), NumberStyles.None, CultureInfo.InvariantCulture);
				value = value.Substring(daySeparator + 1).TrimStart();
			}

			var timeParts = value.Split(':');
			if (timeParts.Length != 3)
				throw new FormatException($"Invalid Informix interval value: '{raw}'.");

			var hours   = long.Parse(timeParts[0], NumberStyles.None, CultureInfo.InvariantCulture);
			var minutes = long.Parse(timeParts[1], NumberStyles.None, CultureInfo.InvariantCulture);
			var seconds = decimal.Parse(timeParts[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
			var totalTicks =
				days    * (decimal)TimeSpan.TicksPerDay    +
				hours   * (decimal)TimeSpan.TicksPerHour   +
				minutes * (decimal)TimeSpan.TicksPerMinute +
				seconds * (decimal)TimeSpan.TicksPerSecond;

			return TimeSpan.FromTicks(decimal.ToInt64(decimal.Truncate(totalTicks) * sign));
		}

		private static void BuildIntervalLiteral(StringBuilder sb, TimeSpan interval)
		{
			// for now just generate DAYS TO FRACTION(5) interval, hardly anyone needs YEAR TO MONTH one
			// and if he needs, it is easy to workaround by adding another one converter to mapping schema
			var absoluteTicks = interval.Ticks < 0
				? unchecked((ulong)(-(interval.Ticks + 1))) + 1
				: (ulong)interval.Ticks;
			var days      = absoluteTicks / (ulong)TimeSpan.TicksPerDay;
			var hours     = absoluteTicks / (ulong)TimeSpan.TicksPerHour        % 24;
			var minutes   = absoluteTicks / (ulong)TimeSpan.TicksPerMinute      % 60;
			var seconds   = absoluteTicks / (ulong)TimeSpan.TicksPerSecond      % 60;
			var fractions = absoluteTicks / 100                                % 100000;
			var dayPart   = (interval.Ticks < 0 ? "-" : string.Empty) + days.ToString(CultureInfo.InvariantCulture);
			sb.AppendFormat(
				CultureInfo.InvariantCulture,
				INTERVAL5_FORMAT,
				dayPart,
				hours,
				minutes,
				seconds,
				fractions);
		}

		static readonly Action<StringBuilder,int> _appendConversionAction = AppendConversion;

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			// chr works with values in 0..255 range, bigger/smaller values will be converted to byte
			// this is fine as long as we don't have out-of-range characters in _extraEscapes
			stringBuilder.Append(CultureInfo.InvariantCulture, $"chr({value})");
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, _appendConversionAction, value, _extraEscapes);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			switch (value)
			{
				case '\r':
				case '\n':
					AppendConversion(stringBuilder, value);
					break;
				default:
					DataTools.ConvertCharToSql(stringBuilder, "'", _appendConversionAction, value);
					break;
			}
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dataType, DataOptions options, DateTime value)
		{
			// datetime literal using TO_DATE function used because it works with all kinds of datetime ranges
			// without generation of range-specific literals
			// see Issue1307Tests tests
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
			string format;
#endif

			if ((value.Ticks % 10000000) / 100 != 0)
			{
				var ifxo = options.FindOrDefault(InformixOptions.Default);

				format = ifxo.ExplicitFractionalSecondsSeparator ?
					DATETIME5_EXPLICIT_FORMAT :
					DATETIME5_FORMAT;
			}
			else
			{
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
					? DATE_FORMAT
					: DATETIME_FORMAT;
			}

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

#if SUPPORTS_DATEONLY
		static void ConvertDateOnlyToSql(StringBuilder stringBuilder, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}
#endif

		internal static readonly InformixMappingSchema Instance = new ();

		public sealed class IfxMappingSchema() : LockedMappingSchema(ProviderName.Informix, InformixProviderAdapter.GetInstance(InformixProvider.Informix).MappingSchema, Instance);

		public sealed class DB2MappingSchema() : LockedMappingSchema(ProviderName.InformixDB2, InformixProviderAdapter.GetInstance(InformixProvider.DB2).MappingSchema, Instance);
	}
}
