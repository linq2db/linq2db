using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	class SqlServer2008SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2008SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2008SqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
			StringBuilder.AppendLine(";");
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2008; }
		}
	}
}
