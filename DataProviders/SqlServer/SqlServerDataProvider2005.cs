using System;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class SqlServerDataProvider2005 : SqlServerDataProvider
	{
		public SqlServerDataProvider2005()
			: base(SqlServerVersion.v2005, new MappingSchema(ProviderName.SqlServer2005, SqlServerMappingSchema.Instance))
		{
		}
	}
}
