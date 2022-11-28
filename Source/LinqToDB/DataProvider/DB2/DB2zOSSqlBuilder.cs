using System;

namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlProvider;

	sealed class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		DB2zOSSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2zOSSqlBuilder(this);
		}

		protected override DB2Version Version => DB2Version.zOS;
	}
}
