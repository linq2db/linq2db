using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class SqlServer2005MappingSchema : MappingSchema
	{
		public SqlServer2005MappingSchema()
			: base(ProviderName.SqlServer2005, SqlServerMappingSchema.Instance)
		{
		}
	}
}
