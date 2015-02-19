using System;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Sybase
{
	using Mapping;
	using SqlQuery;

	public class SybaseMappingSchema : MappingSchema
	{
		public SybaseMappingSchema() : this(ProviderName.Sybase)
		{
		}

		protected SybaseMappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(String), (sb,dt,v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,dt,v) => ConvertCharToSql  (sb, (char)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}

		static readonly char[] _escapes = { '\x0', '\'' };

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			var start = "'";

			if (value.Any(ch => ch > 127))
				start = "N'";

			DataTools.ConvertStringToSql(stringBuilder, start, "char", value);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			var start = value > 127 ? "N'" : "'";
			DataTools.ConvertCharToSql(stringBuilder, start, "char", value);
		}
	}
}
