using System;

namespace LinqToDB.DataProvider.MySql
{
	using Mapping;

	public class MySqlMappingSchema : MappingSchema
	{
		public MySqlMappingSchema() : base(ProviderName.MySql)
		{
		}

		protected MySqlMappingSchema(string configuration) : base(configuration)
		{
		}
	}
}
