using System;
using System.Globalization;
using System.Text;

using LinqToDB.DataProvider.Informix;
using LinqToDB.Internal.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Informix
{
	sealed class InformixMappingSchema : LockedMappingSchema
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

			SetValueToSqlConverter(typeof(string),   (sb, _,_,v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),     (sb, _,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,o,v) => ConvertDateTimeToSql(sb, dt, o, (DateTime)v));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, _,_,v) => BuildIntervalLiteral(sb, (TimeSpan)v));

#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly), (sb,dt,_,v) => ConvertDateOnlyToSql(sb, dt, (DateOnly)v));
#endif
		}

		private static void BuildIntervalLiteral(StringBuilder sb, TimeSpan interval)
		{
			// for now just generate DAYS TO FRACTION(5) interval, hardly anyone needs YEAR TO MONTH one
			// and if he needs, it is easy to workaround by adding another one converter to mapping schema
			var absoluteTs = interval < TimeSpan.Zero ? (TimeSpan.Zero - interval) : interval;
			sb.AppendFormat(
				CultureInfo.InvariantCulture,
				INTERVAL5_FORMAT,
				interval.Days,
				absoluteTs.Hours,
				absoluteTs.Minutes,
				absoluteTs.Seconds,
				(absoluteTs.Ticks / 100) % 100000);
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
		static void ConvertDateOnlyToSql(StringBuilder stringBuilder, SqlDataType dataType, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}
#endif

		internal static readonly InformixMappingSchema Instance = new ();

		public sealed class IfxMappingSchema : LockedMappingSchema
		{
			public IfxMappingSchema() : base(ProviderName.Informix, InformixProviderAdapter.GetInstance(InformixProvider.Informix).MappingSchema, Instance)
			{
			}
		}

		public sealed class DB2MappingSchema : LockedMappingSchema
		{
			public DB2MappingSchema() : base(ProviderName.InformixDB2, InformixProviderAdapter.GetInstance(InformixProvider.DB2).MappingSchema, Instance)
			{
			}
		}
	}
}
