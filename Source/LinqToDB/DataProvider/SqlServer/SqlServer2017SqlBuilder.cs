namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

	class SqlServer2017SqlBuilder : SqlServer2012SqlBuilder
	{
		public SqlServer2017SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2017SqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		public override string Name => ProviderName.SqlServer2017;
	}
}
