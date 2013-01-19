using System;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	class SqlServerDataProvider2012 : SqlServerDataProvider
	{
		public SqlServerDataProvider2012()
			: base(SqlServerVersion.v2012, new MappingSchema(ProviderName.SqlServer2012, SqlServerMappingSchema.Instance))
		{
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2008SqlProvider();
		}
	}
}
