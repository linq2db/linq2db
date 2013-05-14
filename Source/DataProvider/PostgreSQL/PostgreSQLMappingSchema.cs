using System;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Mapping;

	public class PostgreSQLMappingSchema : MappingSchema
	{
		public PostgreSQLMappingSchema() : this(ProviderName.PostgreSQL)
		{
		}

		protected PostgreSQLMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(string), DataType.Undefined);
		}
	}
}
