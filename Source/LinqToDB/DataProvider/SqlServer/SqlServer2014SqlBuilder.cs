using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlProvider;

	class SqlServer2014SqlBuilder : SqlServer2012SqlBuilder
	{
		public SqlServer2014SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, LinqOptions linqOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, linqOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2014SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2014SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2014;
	}
}
