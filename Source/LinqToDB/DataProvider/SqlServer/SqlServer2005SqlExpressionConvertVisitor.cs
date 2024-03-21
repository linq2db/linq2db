namespace LinqToDB.DataProvider.SqlServer
{
	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}
	}
}
