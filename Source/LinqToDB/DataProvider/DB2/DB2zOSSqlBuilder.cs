using System;

namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlProvider;

	class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, LinqOptions linqOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, linqOptions, sqlOptimizer, sqlProviderFlags)
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
