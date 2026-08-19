using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2025SqlExpressionConvertVisitor : SqlServer2012SqlExpressionConvertVisitor
	{
		public SqlServer2025SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		// SQL Server 2025 adds `||` as an ANSI-SQL string-concat operator with strict
		// null propagation and auto-coerce semantics. The SQL builder emits `||` via
		// ConcatBuildStyle.Pipes; explicit CAST on non-string operands is unnecessary.
		// https://learn.microsoft.com/en-us/sql/t-sql/language-elements/string-concatenation-pipes-transact-sql
		protected override bool ConcatRequiresExplicitStringCast => false;
	}
}
