using System;
using System.Text;

namespace LinqToDB.DataProvider
{
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

		public override string  Name
		{
			get { return ProviderName.SqlServer2008; }
		}
	}
}
