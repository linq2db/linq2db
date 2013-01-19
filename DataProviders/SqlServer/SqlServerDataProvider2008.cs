using System;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	class SqlServerDataProvider2008 : SqlServerDataProvider
	{
		public SqlServerDataProvider2008()
			: base(SqlServerVersion.v2008, new MappingSchema(ProviderName.SqlServer2008, SqlServerMappingSchema.Instance))
		{
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2008SqlProvider();
		}

		static readonly SqlProviderFlags _sqlProviderFlags = new SqlProviderFlags();

		public override SqlProviderFlags GetSqlProviderFlags()
		{
			return _sqlProviderFlags;
		}
	}
}
