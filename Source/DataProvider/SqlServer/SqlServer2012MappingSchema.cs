using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;

	public class SqlServer2012MappingSchema : MappingSchema
	{
		public SqlServer2012MappingSchema()
			: base(ProviderName.SqlServer2012, SqlServerMappingSchema.Instance)
		{
		}
	}
}
