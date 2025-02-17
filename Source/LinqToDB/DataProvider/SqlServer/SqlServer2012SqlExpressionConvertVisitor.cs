using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	public class SqlServer2012SqlExpressionConvertVisitor : SqlServer2008SqlExpressionConvertVisitor
	{
		public SqlServer2012SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case PseudoFunctions.TRY_CONVERT:
					return new SqlFunction(func.SystemType, "TRY_CONVERT", false, true, func.Parameters[0], func.Parameters[2]) { CanBeNull = true };
			}

			return base.ConvertSqlFunction(func);
		}

	}
}
