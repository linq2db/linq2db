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
	}
}
