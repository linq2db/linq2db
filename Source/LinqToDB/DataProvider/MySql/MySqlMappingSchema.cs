using System.Data.Linq;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using LinqToDB.Common;
	using Mapping;
	using SqlQuery;

	public class MySqlMappingSchema : MappingSchema
	{
		public MySqlMappingSchema() : this(ProviderName.MySql)
		{
		}

		protected MySqlMappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(string), (sb,dt,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(char),   (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string)));
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

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));
		}

		internal static readonly MappingSchema Instance = new MySqlMappingSchema();

		public class MySqlOfficialMappingSchema : MappingSchema
		{
			public MySqlOfficialMappingSchema()
				: base(ProviderName.MySqlOfficial, Instance)
			{
			}

			public MySqlOfficialMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.MySqlOfficial, Array<MappingSchema>.Append(schemas, Instance))
			{
			}
		}

		public class MySqlConnectorMappingSchema : MappingSchema
		{
			public MySqlConnectorMappingSchema()
				: base(ProviderName.MySqlConnector, Instance)
			{
			}

			public MySqlConnectorMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.MySqlConnector, Array<MappingSchema>.Append(schemas, Instance))
			{
			}
		}
	}
}
