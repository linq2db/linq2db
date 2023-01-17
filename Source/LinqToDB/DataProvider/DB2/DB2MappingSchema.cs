﻿using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using Common;
	using Mapping;
	using SqlQuery;

	sealed class DB2MappingSchema : LockedMappingSchema
	{
		private const string DATE_FORMAT       = "{0:yyyy-MM-dd}";
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

		DB2MappingSchema() : base(ProviderName.DB2)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
			SetDataType(typeof(byte), new SqlDataType(DataType.Int16, typeof(byte)));
			SetDataType(typeof(byte?), new SqlDataType(DataType.Int16, typeof(byte)));

			SetValueToSqlConverter(typeof(Guid),     (sb, _,_,v) => ConvertBinaryToSql  (sb, ((Guid)v).ToByteArray()));
			SetValueToSqlConverter(typeof(string),   (sb, _,_,v) => ConvertStringToSql  (sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),     (sb, _,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb, _,_,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb, _,_,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, _,_,v) => ConvertTimeToSql    (sb, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,_,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));

			// set reader conversions from literals
			SetConverter<string, DateTime>(ParseDateTime);

#if NET6_0_OR_GREATER
			SetValueToSqlConverter(typeof(DateOnly), (sb,dt,_,v) => ConvertDateOnlyToSql(sb, dt, (DateOnly)v));
			SetConverter<string, DateOnly>(ParseDateOnly);
#endif
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

#if NET6_0_OR_GREATER
		static void ConvertDateOnlyToSql(StringBuilder stringBuilder, SqlDataType dt, DateOnly value)
		{
			stringBuilder.Append('\'');
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
			stringBuilder.Append('\'');
		}

		private static readonly string[] DateOnlyFormats = new[]
		{
			"yyyy-MM-dd",
		};

		static DateOnly ParseDateOnly(string value)
		{
			if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var res))
				return res;

			return DateOnly.ParseExact(
				value,
				DateOnlyFormats,
				CultureInfo.InvariantCulture,
				DateTimeStyles.None);
		}
#endif

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
			stringBuilder
				.Append("BX'")
				.AppendByteArrayAsHexViaLookup32(value)
				.Append('\'');
		}

		static readonly Action<StringBuilder,int> _appendConversionAction = AppendConversion;

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
			DataTools.ConvertStringToSql(stringBuilder, "||", null, _appendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", _appendConversionAction, value);
		}

		internal static readonly DB2MappingSchema Instance = new ();

		public sealed class DB2zOSMappingSchema : LockedMappingSchema
		{
			public DB2zOSMappingSchema() : base(ProviderName.DB2zOS,  DB2ProviderAdapter.Instance.MappingSchema, Instance)
			{
			}
		}

		public sealed class DB2LUWMappingSchema : LockedMappingSchema
		{
			public DB2LUWMappingSchema() : base(ProviderName.DB2LUW, DB2ProviderAdapter.Instance.MappingSchema, Instance)
			{
			}
		}
	}
}
