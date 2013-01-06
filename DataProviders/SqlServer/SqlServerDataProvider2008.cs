using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class SqlServerDataProvider2008 : SqlServerDataProvider
	{
		public SqlServerDataProvider2008()
			: base(SqlServerVersion.v2008, new MappingSchema(ProviderName.SqlServer2008, SqlServerMappingSchema.Instance))
		{
		}
	}
}
