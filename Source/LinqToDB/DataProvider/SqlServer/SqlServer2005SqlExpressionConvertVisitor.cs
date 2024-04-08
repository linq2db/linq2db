namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			// SQL Server 2005 does not support TIME data type
			if (cast.ToType.DataType == DataType.Time)
			{
				return cast.Expression;
			}

			return base.ConvertConversion(cast);
		}
	}
}
