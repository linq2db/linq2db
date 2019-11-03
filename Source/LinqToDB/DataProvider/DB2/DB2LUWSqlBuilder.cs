using System;

namespace LinqToDB.DataProvider.DB2
{
	using LinqToDB.Mapping;
	using SqlProvider;

	class DB2LUWSqlBuilder : DB2SqlBuilderBase
	{
		public DB2LUWSqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2LUWSqlBuilder(MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override DB2Version Version => DB2Version.LUW;
	}
}
