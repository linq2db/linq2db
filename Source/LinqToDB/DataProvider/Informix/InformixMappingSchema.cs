using System;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using System.Globalization;
	using Common;
	using Mapping;
	using SqlQuery;

	public class InformixMappingSchema : MappingSchema
	{
		static readonly char[] _extraEscapes = { '\r', '\n' };

		public InformixMappingSchema() : this(ProviderName.Informix)
		{
		}

		protected InformixMappingSchema(string configuration) : base(configuration)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetValueToSqlConverter(typeof(bool), (sb,dt,v) => sb.Append('\'').Append((bool)v ? 't' : 'f').Append('\''));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(string),   (sb,dt,v)   => ConvertStringToSql  (sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),     (sb,dt,v)   => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v)   => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, dt, v) => BuildIntervalLiteral(sb, (TimeSpan)v));
		}

		private void BuildIntervalLiteral(StringBuilder sb, TimeSpan interval)
		{
			// for now just generate DAYS TO FRACTION(5) interval, hardly anyone needs YEAR TO MONTH one
			// and if he needs, it is easy to workaround by adding another one converter to mapping schema
			var absoluteTs = interval < TimeSpan.Zero ? (TimeSpan.Zero - interval) : interval;
			sb.AppendFormat(
				CultureInfo.InvariantCulture,
				"INTERVAL({0} {1:00}:{2:00}:{3:00}.{4:00000}) DAY TO FRACTION(5)",
				interval.Days,
				absoluteTs.Hours,
				absoluteTs.Minutes,
				absoluteTs.Seconds,
				(absoluteTs.Ticks / 100) % 100000);
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			// chr works with values in 0..255 range, bigger/smaller values will be converted to byte
			// this is fine as long as we don't have out-of-range characters in _extraEscapes
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, _extraEscapes);
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
					DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
					break;
			}
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dataType, DateTime value)
		{
			// datetime literal using TO_DATE function used because it works with all kinds of datetime ranges
			// without generation of range-specific literals
			// see Issue1307Tests tests
			string format;
			if ((value.Ticks % 10000000) / 100 != 0)
				format = InformixConfiguration.ExplicitFractionalSecondsSeparator ?
					"TO_DATE('{0:yyyy-MM-dd HH:mm:ss.fffff}', '%Y-%m-%d %H:%M:%S.%F5')" :
					"TO_DATE('{0:yyyy-MM-dd HH:mm:ss.fffff}', '%Y-%m-%d %H:%M:%S%F5')";
			else
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
					? "TO_DATE('{0:yyyy-MM-dd}', '%Y-%m-%d')"
					: "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', '%Y-%m-%d %H:%M:%S')";

			stringBuilder.AppendFormat(format, value);
		}

		internal static readonly InformixMappingSchema Instance = new ();

		public class IfxMappingSchema : MappingSchema
		{
			public IfxMappingSchema()
				: base(ProviderName.Informix, Instance)
			{
			}

			public IfxMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.Informix, Array<MappingSchema>.Append(schemas, Instance))
			{
			}
		}

		public class DB2MappingSchema : MappingSchema
		{
			public DB2MappingSchema()
				: base(ProviderName.InformixDB2, Instance)
			{
			}

			public DB2MappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.InformixDB2, Array<MappingSchema>.Append(schemas, Instance))
			{
			}
		}
	}
}
