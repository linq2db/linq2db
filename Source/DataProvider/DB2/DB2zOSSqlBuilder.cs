using System;

namespace LinqToDB.DataProvider.DB2
{
	using SqlProvider;

	class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2zOSSqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		protected override DB2Version Version
		{
			get { return DB2Version.zOS; }
		}
	}
}
