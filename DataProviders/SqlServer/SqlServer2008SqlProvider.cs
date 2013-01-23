using System;
using System.Text;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class SqlServer2008SqlProvider : SqlServerSqlProvider
	{
		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2008SqlProvider();
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
