using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	public class SqlServer2008SqlProvider : SqlServerSqlProvider
	{
		public SqlServer2008SqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2008SqlProvider(SqlProviderFlags);
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
