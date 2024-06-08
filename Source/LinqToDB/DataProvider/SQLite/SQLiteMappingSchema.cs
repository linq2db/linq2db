using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Mapping;
	using SqlQuery;

	public class SQLiteMappingSchema : LockedMappingSchema
	{
		internal const string DATE_FORMAT_RAW  = "yyyy-MM-dd";
		private  const string DATE_FORMAT      = "'{0:yyyy-MM-dd}'";
		private  const string DATETIME0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss}'";
		private  const string DATETIME1_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.f}'";
		private  const string DATETIME2_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.ff}'";
		private  const string DATETIME3_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";

		SQLiteMappingSchema() : base(ProviderName.SQLite)
		{
			SetConvertExpression<string,TimeSpan>(s => DateTime.Parse(s, null, DateTimeStyles.NoCurrentDateDefault).TimeOfDay);

			SetValueToSqlConverter(typeof(Guid),     (sb,dt,_,v) => ConvertGuidToSql    (sb, dt, (Guid)v));
			SetValueToSqlConverter(typeof(DateTime), (sb, _,_,v) => ConvertDateTimeToSql(sb, (DateTime)v));
			SetValueToSqlConverter(typeof(string),   (sb, _,_,v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),     (sb, _,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb, _,_,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb, _,_,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

#if NET6_0_OR_GREATER
			SetValueToSqlConverter(typeof(DateOnly), (sb,_,_,v) => ConvertDateOnlyToSql(sb, (DateOnly)v));
#endif

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}

		static void ConvertGuidToSql(StringBuilder stringBuilder, SqlDataType dt, Guid value)
		{
			// keep in sync with provider's SetParameter method
			switch (dt.Type.DataType, dt.Type.DbType)
			{
				case (DataType.NChar, _) or (DataType.NVarChar, _) or (DataType.NText, _)
					or (DataType.Char, _) or (DataType.VarChar, _) or (DataType.Text, _)
					// we can add more types on request later
					or (_, "TEXT"):
					stringBuilder
						// ToUpperInvariant to match Microsoft.Data.SQLite behavior
						.Append(CultureInfo.InvariantCulture, $"'{value.ToString().ToUpperInvariant()}'");
					break;
				default:
					ConvertBinaryToSql(stringBuilder, value.ToByteArray());
					break;
			}
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append("X'")
				.AppendByteArrayAsHexViaLookup32(value)
				.Append('\'');
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

#if NET6_0_OR_GREATER
		static void ConvertDateOnlyToSql(StringBuilder stringBuilder, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}
#endif

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"char({value})");
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

		public class ClassicMappingSchema : LockedMappingSchema
		{
			public ClassicMappingSchema() : base(ProviderName.SQLiteClassic, Instance)
			{
			}
		}

		public class MicrosoftMappingSchema : LockedMappingSchema
		{
			public MicrosoftMappingSchema() : base(ProviderName.SQLiteMS, Instance)
			{
			}
		}
	}
}
