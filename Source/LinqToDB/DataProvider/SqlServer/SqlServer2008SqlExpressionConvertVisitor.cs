namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.SqlQuery;

	public class SqlServer2008SqlExpressionConvertVisitor : SqlServer2005SqlExpressionConvertVisitor
	{
		public SqlServer2008SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertSqlFunction(func);
		}
	}
}
