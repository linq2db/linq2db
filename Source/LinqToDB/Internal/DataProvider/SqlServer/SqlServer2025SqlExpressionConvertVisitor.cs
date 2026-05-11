using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServer2025SqlExpressionConvertVisitor : SqlServer2012SqlExpressionConvertVisitor
	{
		public SqlServer2025SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		// SQL Server 2025 adds `||` as an ANSI-SQL string-concat operator with strict
		// null propagation and auto-coerce semantics — use it instead of `+` so non-string
		// operands don't trip SQL-standard data-type precedence.
		// https://learn.microsoft.com/en-us/sql/t-sql/language-elements/string-concatenation-pipes-transact-sql
		protected override bool ConcatRequiresExplicitStringCast => false;

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"+" when element.SystemType == typeof(string) =>
					new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence),

				_ => base.ConvertSqlBinaryExpression(element),
			};
		}
	}
}
