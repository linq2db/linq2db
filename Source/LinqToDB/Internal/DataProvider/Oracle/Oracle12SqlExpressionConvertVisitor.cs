using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class Oracle12SqlExpressionConvertVisitor : OracleSqlExpressionConvertVisitor
	{
		public Oracle12SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func.Name switch
			{
				PseudoFunctions.TRY_CONVERT =>
					new SqlExpression(func.Type, "CAST({0} AS {1} DEFAULT NULL ON CONVERSION ERROR)", Precedence.Primary, func.Parameters[2], func.Parameters[0])
					{
						CanBeNull = true
					},

				PseudoFunctions.TRY_CONVERT_OR_DEFAULT =>
					new SqlExpression(func.Type, "CAST({0} AS {1} DEFAULT {2} ON CONVERSION ERROR)", Precedence.Primary, func.Parameters[2], func.Parameters[0], func.Parameters[3])
					{
						CanBeNull = func.Parameters[2].CanBeNullable(NullabilityContext) || func.Parameters[3].CanBeNullable(NullabilityContext)
					},

				_ => base.ConvertSqlFunction(func),
			};
		}
	}
}
