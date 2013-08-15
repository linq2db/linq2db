using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	public class SqlServer2008SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2008SqlBuilder(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlProvider()
		{
			return new SqlServer2008SqlBuilder(SqlProviderFlags);
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, null);
			sb.AppendLine(";");
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(sb, func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2008; }
		}
	}
}
