using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteMappingSchema : LockedMappingSchema
	{
		internal const string DATE_FORMAT_RAW  = "yyyy-MM-dd";
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT     = CompositeFormat.Parse("'{0:yyyy-MM-dd}'");
		private static readonly CompositeFormat DATETIME_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss.fff}'");
#else
		private  const string DATE_FORMAT      = "'{0:yyyy-MM-dd}'";
		private  const string DATETIME_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";
#endif

		SQLiteMappingSchema() : base(ProviderName.SQLite)
		{
			SetConvertExpression<string,TimeSpan>(s => DateTime.Parse(s, null, DateTimeStyles.NoCurrentDateDefault).TimeOfDay);

			SetValueToSqlConverter(typeof(Guid),     (sb, dt,_,v) => ConvertGuidToSql    (sb, dt, (Guid)v));
			SetValueToSqlConverter(typeof(DateTime), (sb, dt,_,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(string),   (sb, _,_,v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),     (sb, _,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb, _,_,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb, _,_,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

#if NET8_0_OR_GREATER
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

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dataType, DateTime value)
		{
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
			string format;
#endif
			if (dataType.Type.DataType == DataType.Date)
				format = DATE_FORMAT;
			else
				format = DATETIME_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

#if NET8_0_OR_GREATER
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
