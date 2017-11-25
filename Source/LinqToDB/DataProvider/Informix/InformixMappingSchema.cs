using System;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using Mapping;
	using SqlQuery;

	public class InformixMappingSchema : MappingSchema
	{
		static readonly char[] _extraEscapes = { '\r', '\n' };

		public InformixMappingSchema() : this(ProviderName.Informix)
		{
		}

		protected InformixMappingSchema(string configuration) : base(configuration)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetValueToSqlConverter(typeof(bool), (sb,dt,v) => sb.Append("'").Append((bool)v ? 't' : 'f').Append("'"));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(String), (sb,dt,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			// chr works with values in 0..255 range, bigger/smaller values will be converted to byte
			// this is fine as long as we don't have out-of-range characters in _extraEscapes
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(")")
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversion, value, _extraEscapes);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			switch (value)
			{
				case '\r':
				case '\n':
					AppendConversion(stringBuilder, value);
					break;
				default:
					DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
					break;
			}
		}
	}
}
