using System;

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
		}

		internal static readonly DB2MappingSchema Instance = new DB2MappingSchema();
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
