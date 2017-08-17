namespace LinqToDB.DataProvider.Firebird
{
	using Mapping;
	using SqlQuery;
	using System.Text;

	public class FirebirdMappingSchema : MappingSchema
	{
		public FirebirdMappingSchema() : this(ProviderName.Firebird)
		{
		}

		protected FirebirdMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			// firebird string literals can contain only limited set of characters, so we should encode them
			SetValueToSqlConverter(typeof(string), (sb, dt, v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char)  , (sb, dt, v) => MakeUtf8Literal(sb, Encoding.UTF8.GetBytes(new[] { (char)v })));
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			if (value == string.Empty)
				stringBuilder.Append("''");
			else
				MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(value));
		}

		private static void MakeUtf8Literal(StringBuilder stringBuilder, byte[] bytes)
		{
			stringBuilder.Append("_utf8 x'");

			foreach (var bt in bytes)
			{
				stringBuilder.AppendFormat("{0:X2}", bt);
			}

			stringBuilder.Append("'");
		}
	}
}
