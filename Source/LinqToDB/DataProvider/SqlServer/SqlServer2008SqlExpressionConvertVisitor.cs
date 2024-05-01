namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

	public class SqlServer2008SqlExpressionConvertVisitor : SqlServer2005SqlExpressionConvertVisitor
	{
		public SqlServer2008SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		protected override bool ProcessConversion(SqlCastExpression cast, out ISqlExpression result)
		{
			result = cast;
			return false;
		}

	}
}
