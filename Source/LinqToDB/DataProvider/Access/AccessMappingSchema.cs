using System;
using System.Text;
using System.Globalization;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;

	public class AccessMappingSchema : MappingSchema
	{
		public AccessMappingSchema() : this(ProviderName.Access)
		{
		}

		protected AccessMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(DateTime), DataType.DateTime);

			SetValueToSqlConverter(typeof(bool),     (sb,dt,v) => sb.Append(v));
			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v) => sb.Append("'").Append(((Guid)v).ToString("B")).Append("'"));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, (DateTime)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(String),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(")")
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "+", null, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			var format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
				"#{0:yyyy-MM-dd}#" :
				"#{0:yyyy-MM-dd HH:mm:ss}#";

			stringBuilder.AppendFormat(format, value);
		}
	}
}
