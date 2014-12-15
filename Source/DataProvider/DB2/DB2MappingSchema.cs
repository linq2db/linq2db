using System;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using Mapping;

	public class DB2MappingSchema : MappingSchema
	{
		public DB2MappingSchema() : this(ProviderName.DB2)
		{
		}

		protected DB2MappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(Guid), (sb,v) => ConvertGuidToSql(sb, (Guid)v));
		}

		internal static readonly DB2MappingSchema Instance = new DB2MappingSchema();


		static void ConvertGuidToSql(StringBuilder stringBuilder, Guid value)
		{
			var s = value.ToString("N");

			stringBuilder
				.Append("Cast(x'")
				.Append(s.Substring( 6,  2))
				.Append(s.Substring( 4,  2))
				.Append(s.Substring( 2,  2))
				.Append(s.Substring( 0,  2))
				.Append(s.Substring(10,  2))
				.Append(s.Substring( 8,  2))
				.Append(s.Substring(14,  2))
				.Append(s.Substring(12,  2))
				.Append(s.Substring(16, 16))
				.Append("' as char(16) for bit data)")
				;
		}
	}

	public class DB2zOSMappingSchema : MappingSchema
	{
		public DB2zOSMappingSchema()
			: base(ProviderName.DB2zOS, DB2MappingSchema.Instance)
		{
		}
	}

	public class DB2LUWMappingSchema : MappingSchema
	{
		public DB2LUWMappingSchema()
			: base(ProviderName.DB2LUW, DB2MappingSchema.Instance)
		{
		}
	}
}
