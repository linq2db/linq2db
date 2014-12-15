using System;
using System.Text;

namespace LinqToDB.DataProvider.Sybase
{
	using Mapping;

	public class SybaseMappingSchema : MappingSchema
	{
		public SybaseMappingSchema() : this(ProviderName.Sybase)
		{
		}

		protected SybaseMappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(String), (sb,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,v) => ConvertCharToSql  (sb, (char)v));
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			foreach (var ch in value)
			{
				if (ch > 127)
				{
					stringBuilder.Append("N");
					break;
				}
			}

			stringBuilder
				.Append('\'')
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			if (value > 127)
				stringBuilder.Append("N");

			stringBuilder.Append('\'');

			if (value == '\'') stringBuilder.Append("''");
			else               stringBuilder.Append(value);

			stringBuilder.Append('\'');
		}
	}
}
