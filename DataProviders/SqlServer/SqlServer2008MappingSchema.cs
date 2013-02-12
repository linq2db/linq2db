using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class SqlServer2008MappingSchema : MappingSchema
	{
		public SqlServer2008MappingSchema()
			: base(ProviderName.SqlServer2008, SqlServerMappingSchema.Instance)
		{
		}
	}
}
