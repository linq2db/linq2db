using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

	public class SqlServer2012SqlProvider : SqlServerSqlProvider
	{
		public SqlServer2012SqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2012SqlProvider(SqlProviderFlags);
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, null);
			sb.AppendLine(";");
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2012; }
		}
	}
}
