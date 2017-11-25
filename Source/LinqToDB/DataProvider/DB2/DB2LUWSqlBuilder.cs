using System;

namespace LinqToDB.DataProvider.DB2
{
	using SqlProvider;

	class DB2LUWSqlBuilder : DB2SqlBuilderBase
	{
		public DB2LUWSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2LUWSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override DB2Version Version
		{
			get { return DB2Version.LUW; }
		}
	}
}
