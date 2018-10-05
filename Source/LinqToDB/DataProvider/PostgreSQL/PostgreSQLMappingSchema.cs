using System;
using System.Net.NetworkInformation;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
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

			SetDataType(typeof(string), DataType.Undefined);

			AddScalarType(typeof(PhysicalAddress), DataType.Udt);

			SetValueToSqlConverter(typeof(bool),   (sb,dt,v) => sb.Append(v));
			SetValueToSqlConverter(typeof(String), (sb,dt,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
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
