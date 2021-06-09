using System;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using LinqToDB.Common;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;
	using System.Globalization;

	public class DB2MappingSchema : MappingSchema
	{
		private const string DATETIME_FORMAT   = "{0:yyyy-MM-dd-HH.mm.ss}";

		private const string TIMESTAMP0_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss}";
		private const string TIMESTAMP1_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.f}";
		private const string TIMESTAMP2_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.ff}";
		private const string TIMESTAMP3_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.fff}";
		private const string TIMESTAMP4_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.ffff}";
		private const string TIMESTAMP5_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.fffff}";
		private const string TIMESTAMP6_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.ffffff}";
		private const string TIMESTAMP7_FORMAT = "{0:yyyy-MM-dd-HH.mm.ss.fffffff}";

		private static readonly string[] DateParseFormats = new[]
		{
			"yyyy-MM-dd",
			"yyyy-MM-dd-HH.mm.ss",
			"yyyy-MM-dd-HH.mm.ss.f",
			"yyyy-MM-dd-HH.mm.ss.ff",
			"yyyy-MM-dd-HH.mm.ss.fff",
			"yyyy-MM-dd-HH.mm.ss.ffff",
			"yyyy-MM-dd-HH.mm.ss.fffff",
			"yyyy-MM-dd-HH.mm.ss.ffffff",
			"yyyy-MM-dd-HH.mm.ss.fffffff",
			"yyyy-MM-dd-HH.mm.ss.ffffffff",
			"yyyy-MM-dd-HH.mm.ss.fffffffff",
			"yyyy-MM-dd-HH.mm.ss.ffffffffff",
			"yyyy-MM-dd-HH.mm.ss.fffffffffff",
			"yyyy-MM-dd-HH.mm.ss.ffffffffffff",
		};

		public DB2MappingSchema() : this(ProviderName.DB2)
		{
		}

		protected DB2MappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(Guid), (sb,dt,v) => ConvertGuidToSql(sb, (Guid)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(string),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(TimeSpan), (sb,dt,v) => ConvertTimeToSql    (sb, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));

			// set reader conversions from literals
			SetConverter<string, DateTime>(ParseDateTime);
		}

		static DateTime ParseDateTime(string value)
		{
			if (DateTime.TryParse(value, out var res))
				return res;

			return DateTime.ParseExact(
				value,
				DateParseFormats,
				CultureInfo.InvariantCulture,
				DateTimeStyles.None);
		}

		static void ConvertTimeToSql(StringBuilder stringBuilder, TimeSpan time)
		{
			stringBuilder.Append($"'{time:hh\\:mm\\:ss}'");
		}

		static string GetTimestampFormat(SqlDataType type)
		{
			var precision = type.Type.Precision;

			if (precision == null && type.Type.DbType != null)
			{
				var dbtype = type.Type.DbType.ToLowerInvariant();
				if (dbtype.StartsWith("timestamp("))
				{
					if (int.TryParse(dbtype.Substring(10, dbtype.Length - 11), out var fromDbType))
						precision = fromDbType;
				}
			}

			precision = precision == null || precision < 0 ? 6 : (precision > 7 ? 7 : precision);
			return precision switch
			{
				0    => TIMESTAMP0_FORMAT,
				1    => TIMESTAMP1_FORMAT,
				2    => TIMESTAMP2_FORMAT,
				3    => TIMESTAMP3_FORMAT,
				4    => TIMESTAMP4_FORMAT,
				5    => TIMESTAMP5_FORMAT,
				>= 7 => TIMESTAMP7_FORMAT, // DB2 supports up to 12 digits
				_    => TIMESTAMP6_FORMAT, // default precision
			};
		}


		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType type, DateTime value)
		{
			stringBuilder.Append('\'');
			if (type.Type.DataType == DataType.Date || "date".Equals(type.Type.DbType, StringComparison.OrdinalIgnoreCase))
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
			else
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, GetTimestampFormat(type), value);
			stringBuilder.Append('\'');
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("BX'");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append('\'');
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static readonly DB2MappingSchema Instance = new ();

		static void ConvertGuidToSql(StringBuilder stringBuilder, Guid value)
		{
			var s = value.ToString("N");

			stringBuilder
				.Append("Cast(x'")
				.Append(s.Substring( 6,  2))
				.Append(s.Substring( 4,  2))
				.Append(s.Substring( 2,  2))
				.Append(s.Substring( 0,  2))
				.Append(s.Substring(10,  2))
				.Append(s.Substring( 8,  2))
				.Append(s.Substring(14,  2))
				.Append(s.Substring(12,  2))
				.Append(s.Substring(16, 16))
				.Append("' as char(16) for bit data)")
				;
		}
	}

	public class DB2zOSMappingSchema : MappingSchema
	{
		public DB2zOSMappingSchema()
			: base(ProviderName.DB2zOS, DB2MappingSchema.Instance)
		{
		}

		public DB2zOSMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.DB2zOS, Array<MappingSchema>.Append(schemas, DB2MappingSchema.Instance))
		{
		}
	}

	public class DB2LUWMappingSchema : MappingSchema
	{
		public DB2LUWMappingSchema()
			: base(ProviderName.DB2LUW, DB2MappingSchema.Instance)
		{
		}

		public DB2LUWMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.DB2LUW, Array<MappingSchema>.Append(schemas, DB2MappingSchema.Instance))
		{
		}
	}
}
