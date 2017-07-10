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

			SetValueToSqlConverter(typeof(bool), (sb,dt,v) => sb.Append("'").Append((bool)v ? 't' : 'f').Append("'::BOOLEAN"));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) =>
			{
				var value = (DateTime)v;
				var format = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";

				if (value.Millisecond == 0)
				{
					format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
						"'{0:yyyy-MM-dd}'" :
						"'{0:yyyy-MM-dd HH:mm:ss}'";
				}

				switch (dt.DataType)
				{
					case DataType.Date:
						format += "::DATETIME YEAR TO DAY";
						break;
					default:
						format += "::DATETIME YEAR TO FRACTION";
						break;
				}

				sb.AppendFormat(format, value);
			});

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
			DataTools.ConvertStringToSql(stringBuilder, "||", "'", AppendConversion, value.Replace("\r", "\\r").Replace("\n", "\\n"));
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			switch (value)
			{
				case '\r':
					stringBuilder.Append("'\\r'");
					break;
				case '\n':
					stringBuilder.Append("'\\n'");
					break;
				default:
					DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
					break;
			}
		}
	}
}
