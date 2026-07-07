using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public sealed class SQLiteMappingSchema : LockedMappingSchema
	{
		internal const string DATE_FORMAT_RAW = "yyyy-MM-dd";
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd}'");
#else
		private const string DATE_FORMAT = "'{0:yyyy-MM-dd}'";
#endif

		SQLiteMappingSchema() : base(ProviderName.SQLite)
		{
			SetConvertExpression<string,TimeSpan>(s => DateTime.Parse(s, null, DateTimeStyles.NoCurrentDateDefault).TimeOfDay);

			SetValueToSqlConverter(typeof(Guid),           (sb,dt,_,v) => ConvertGuidToSql    (sb, dt, (Guid)v));
			SetValueToSqlConverter(typeof(DateTime),       (sb,dt,_,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb, _,_,v) => ConvertDateTimeOffsetToSql(sb, (DateTimeOffset)v));
			SetValueToSqlConverter(typeof(string),         (sb, _,_,v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),           (sb, _,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb, _,_,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb, _,_,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

#if SUPPORTS_DATEONLY
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

		// milliseconds are always rendered as 3 digits: the shape strftime('%f') produces and all
		// existing linq2db-written data has. Sub-millisecond ticks are appended only when present:
		// truncating them would lose data on write and break comparisons against values stored at
		// full precision — comparison operands are normalized via strftime, which rounds to the
		// nearest millisecond, so a truncated operand can normalize one millisecond below the
		// same instant stored untruncated
		internal static string FormatDateTime(DateTime value)
		{
			var result = value.ToString("yyyy-MM-dd HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);

			var subMillisecondTicks = (int)(value.Ticks % TimeSpan.TicksPerMillisecond);
			if (subMillisecondTicks != 0)
				result += subMillisecondTicks.ToString("D4", CultureInfo.InvariantCulture).TrimEnd('0');

			return result;
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dataType, DateTime value)
		{
			if (dataType.Type.DataType == DataType.Date)
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
			else
				stringBuilder.Append('\'').Append(FormatDateTime(value)).Append('\'');
		}

		static void ConvertDateTimeOffsetToSql(StringBuilder stringBuilder, DateTimeOffset value)
		{
			stringBuilder
				.Append('\'')
				.Append(FormatDateTime(value.DateTime))
				.Append(value.ToString("zzz", CultureInfo.InvariantCulture))
				.Append('\'');
		}

#if SUPPORTS_DATEONLY
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

		public sealed class ClassicMappingSchema() : LockedMappingSchema(ProviderName.SQLiteClassic, Instance);

		public sealed class MicrosoftMappingSchema() : LockedMappingSchema(ProviderName.SQLiteMS, Instance);
	}
}
