using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	public class MsSql2008SqlProvider : MsSqlSqlProvider
	{
		protected override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2008SqlProvider();
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
