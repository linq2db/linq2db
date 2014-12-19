using System;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using Mapping;

	public class MySqlMappingSchema : MappingSchema
	{
		public MySqlMappingSchema() : this(ProviderName.MySql)
		{
		}

		protected MySqlMappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(String), (sb,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,v) => ConvertCharToSql  (sb, (char)v));
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			stringBuilder
				.Append('\'')
				.Append(value.Replace("\\", "\\\\").Replace("'",  "''"))
				.Append('\'');
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			if (value == '\\')
			{
				stringBuilder.Append("\\\\");
			}
			else
			{
				stringBuilder.Append('\'');

				if (value == '\'') stringBuilder.Append("''");
				else               stringBuilder.Append(value);

				stringBuilder.Append('\'');
			}
		}
	}
}
