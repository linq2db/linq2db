﻿using System;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using Mapping;
	using SqlQuery;

	public class MySqlMappingSchema : MappingSchema
	{
		public MySqlMappingSchema() : this(ProviderName.MySql)
		{
		}

		protected MySqlMappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(String), (sb,dt,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,dt,v) => ConvertCharToSql  (sb, (char)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
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
