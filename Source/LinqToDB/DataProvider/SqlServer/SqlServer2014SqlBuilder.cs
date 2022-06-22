using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using Mapping;

	class SqlServer2014SqlBuilder : SqlServer2012SqlBuilder
	{
		public SqlServer2014SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2014SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2014SqlBuilder(this);
		}

		public override string  Name => ProviderName.SqlServer2014;
	}
}
