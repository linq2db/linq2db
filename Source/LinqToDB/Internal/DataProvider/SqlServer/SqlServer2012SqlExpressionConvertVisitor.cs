using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
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
					return new SqlFunction(func.Type, "TRY_CONVERT", true, func.Parameters[0], func.Parameters[2]);
			}

			return base.ConvertSqlFunction(func);
		}
	}
}
