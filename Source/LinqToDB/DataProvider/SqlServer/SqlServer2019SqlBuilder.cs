using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Infrastructure;
	using Mapping;
	using SqlProvider;

	class SqlServer2019SqlBuilder : SqlServer2017SqlBuilder
	{
		public SqlServer2019SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, LinqOptionsExtension linqOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, linqOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SqlServer2019SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2019SqlBuilder(this);
		}

		public override string Name => ProviderName.SqlServer2019;
	}
}
