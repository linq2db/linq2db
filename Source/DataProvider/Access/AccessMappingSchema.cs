using System;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;

	public class AccessMappingSchema : MappingSchema
	{
		public AccessMappingSchema() : this(ProviderName.Access)
		{
		}

		protected AccessMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(DateTime), DataType.DateTime);

			SetValueToSqlConverter(typeof(bool),     (sb,v) => sb.Append(v));
			SetValueToSqlConverter(typeof(Guid),     (sb,v) => sb.Append("'").Append(((Guid)v).ToString("B")).Append("'"));
			SetValueToSqlConverter(typeof(DateTime), (sb,v) => ConvertDateTimeToSql(sb, (DateTime)v));
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			var format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
				"#{0:yyyy-MM-dd}#" :
				"#{0:yyyy-MM-dd HH:mm:ss}#";

			stringBuilder.AppendFormat(format, value);
		}
	}
}
