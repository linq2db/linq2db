namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;

	public class SqlServer2005SqlExpressionConvertVisitor : SqlServerSqlExpressionConvertVisitor
	{
		public SqlServer2005SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		protected virtual bool ProcessConversion(SqlCastExpression cast, out ISqlExpression result)
		{
			// SQL Server 2005 does not support TIME data type
			if (cast.ToType.DataType == DataType.Time)
			{
				result = cast.Expression;
				return true;
			}

			result = cast;
			return false;
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (ProcessConversion(cast, out var result))
				return result;

			return base.ConvertConversion(cast);
		}
	}
}
