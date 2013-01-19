using System;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	class SqlServerDataProvider2005 : SqlServerDataProvider
	{
		public SqlServerDataProvider2005()
			: base(SqlServerVersion.v2005, new MappingSchema(ProviderName.SqlServer2005, SqlServerMappingSchema.Instance))
		{
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2005SqlProvider();
		}
	}
}
