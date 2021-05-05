using System;
using System.Globalization;
using System.Text;


namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;

	public class SQLiteMappingSchema : MappingSchema
	{
		private const string DATE_FORMAT      = "'{0:yyyy-MM-dd}'";
		private const string DATETIME0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss}'";
		private const string DATETIME1_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.f}'";
		private const string DATETIME2_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.ff}'";
		private const string DATETIME3_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";

		public SQLiteMappingSchema() : this(ProviderName.SQLite)
		{
		}

		protected SQLiteMappingSchema(string configuration) : base(configuration)
		{
			SetConvertExpression<string,TimeSpan>(s => DateTime.Parse(s, null, DateTimeStyles.NoCurrentDateDefault).TimeOfDay);

			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v) => ConvertGuidToSql    (sb, (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, (DateTime)v));
			SetValueToSqlConverter(typeof(string),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("X'");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append('\'');
		}

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
				.Append("' as blob)")
				;
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			string format;
			if (value.Millisecond == 0)
			{
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
					DATE_FORMAT :
					DATETIME0_FORMAT;
			}
			// TODO: code below should be gone after we implement proper date/time support for sqlite
			// This actually doesn't make sense and exists only for our tests to work in cases where literals
			// compared as strings
			// E.g. see DateTimeArray2/DateTimeArray3 tests
			else if (value.Millisecond % 100 == 0)
				format = DATETIME1_FORMAT;
			else if (value.Millisecond % 10 == 0)
				format = DATETIME2_FORMAT;
			else
				format = DATETIME3_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("char(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "+", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static readonly SQLiteMappingSchema Instance = new ();

		public class ClassicMappingSchema : MappingSchema
		{
			public ClassicMappingSchema()
				: base(ProviderName.SQLiteClassic, Instance)
			{
			}
		}

		public class MicrosoftMappingSchema : MappingSchema
		{
			public MicrosoftMappingSchema()
				: base(ProviderName.SQLiteMS, Instance)
			{
			}
		}
	}
}
