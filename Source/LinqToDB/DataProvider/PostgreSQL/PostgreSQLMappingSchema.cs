#nullable disable
using System;
using System.Net.NetworkInformation;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using LinqToDB.SqlQuery;
	using Mapping;
	using System.Data.Linq;

	public class PostgreSQLMappingSchema : MappingSchema
	{
		public PostgreSQLMappingSchema() : this(ProviderName.PostgreSQL)
		{
		}

		protected PostgreSQLMappingSchema(string configuration) : base(configuration)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			AddScalarType(typeof(PhysicalAddress), DataType.Udt);

			SetValueToSqlConverter(typeof(bool),     (sb,dt,v) => sb.Append(v));
			SetValueToSqlConverter(typeof(String),   (sb,dt,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),     (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => BuildDateTime(sb, dt, (DateTime)v));
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			string dbType;
			string format;

			if (value.Millisecond == 0)
			{
				if (value.Hour == 0 && value.Minute == 0 && value.Second == 0)
				{
					format = "'{0:yyyy-MM-dd}'::{1}";
					dbType = dt.DbType ?? "date";
				}
				else
				{
					format = "'{0:yyyy-MM-dd HH:mm:ss}'::{1}";
					dbType = dt.DbType ?? "timestamp";
				}
			}
			else
			{
				format = "'{0:yyyy-MM-dd HH:mm:ss.fff}'::{1}";
				dbType = dt.DbType ?? "timestamp";
			}

			stringBuilder.AppendFormat(format, value, dbType);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("E'\\\\x");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));

			stringBuilder.Append("'");
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
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
		}
	}
}
