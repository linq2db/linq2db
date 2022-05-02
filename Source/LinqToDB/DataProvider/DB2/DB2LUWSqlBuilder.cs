using System;

namespace LinqToDB.DataProvider.DB2
{
	using Infrastructure;
	using Mapping;
	using SqlProvider;

	class DB2LUWSqlBuilder : DB2SqlBuilderBase
	{
		public DB2LUWSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, LinqOptionsExtension linqOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, linqOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		DB2LUWSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2LUWSqlBuilder(this);
		}

		protected override DB2Version Version => DB2Version.LUW;
	}
}
