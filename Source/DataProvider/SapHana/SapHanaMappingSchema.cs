using System;

namespace LinqToDB.DataProvider.SapHana
{
	using Mapping;
	using SqlQuery;
	using System.Text;

	public class SapHanaMappingSchema : MappingSchema
	{
		public SapHanaMappingSchema() : this(ProviderName.SapHana)
		{
		}

		protected SapHanaMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(String), (sb, dt, v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char), (sb, dt, v) => ConvertCharToSql(sb, (char)v));
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("char(")
				.Append(value)
				.Append(")")
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", "'", AppendConversion, value);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
		}
	}
}
