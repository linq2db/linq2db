using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	class SqlServer2008SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2008SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2008SqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertOrUpdateQueryAsMerge(null);
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
