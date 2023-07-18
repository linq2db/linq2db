namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertSqlFunction(func);
		}
	}
}
