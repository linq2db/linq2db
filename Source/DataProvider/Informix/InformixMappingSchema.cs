using System;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using Mapping;
	using SqlQuery;

	public class InformixMappingSchema : MappingSchema
	{
		public InformixMappingSchema() : this(ProviderName.Informix)
		{
		}

		protected InformixMappingSchema(string configuration) : base(configuration)
		{
			ColumnComparisonOption = StringComparison.OrdinalIgnoreCase;

			SetValueToSqlConverter(typeof(bool), (sb,dt,v) => sb.Append("'").Append((bool)v ? 't' : 'f').Append("'"));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(String),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			// chr works with values in 0..255 range, bigger/smaller values will be converted to byte
			// this is fine as long as we use it only for \0 character
			stringBuilder
				.Append("chr(")
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
