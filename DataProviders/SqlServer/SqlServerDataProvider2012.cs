using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class SqlServerDataProvider2012 : SqlServerDataProvider
	{
		public SqlServerDataProvider2012()
			: base(SqlServerVersion.v2012, new MappingSchema(ProviderName.SqlServer2012, SqlServerMappingSchema.Instance))
		{
		}
	}
}
