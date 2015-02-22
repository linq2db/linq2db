using System;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider.SQLite
{
	using Mapping;
	using SqlQuery;

	public class SQLiteMappingSchema : MappingSchema
	{
		public SQLiteMappingSchema() : this(ProviderName.SQLite)
		{
		}

		protected SQLiteMappingSchema(string configuration) : base(configuration)
		{
			SetConvertExpression<string,TimeSpan>(s => DateTime.Parse(s, null, DateTimeStyles.NoCurrentDateDefault).TimeOfDay);

			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v) => ConvertGuidToSql    (sb, (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, (DateTime)v));
			SetValueToSqlConverter(typeof(String),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
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
			if (value.Millisecond == 0)
			{
				var format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
					"'{0:yyyy-MM-dd}'" :
					"'{0:yyyy-MM-dd HH:mm:ss}'";

				stringBuilder.AppendFormat(format, value);
			}
			else
			{
				stringBuilder
					.Append(string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}", value).TrimEnd('0'))
					.Append('\'');
			}
		}

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
			DataTools.ConvertStringToSql(stringBuilder, "+", "'", AppendConversion, value);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
		}
	}
}
