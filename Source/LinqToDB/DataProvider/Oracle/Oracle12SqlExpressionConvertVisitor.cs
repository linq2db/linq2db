using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	public class Oracle12SqlExpressionConvertVisitor : OracleSqlExpressionConvertVisitor
	{
		public Oracle12SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case PseudoFunctions.TRY_CONVERT:
					return new SqlExpression(func.SystemType, "CAST({0} AS {1} DEFAULT NULL ON CONVERSION ERROR)", Precedence.Primary, func.Parameters[2], func.Parameters[0])
					{
						CanBeNull = true
					};

				case PseudoFunctions.TRY_CONVERT_OR_DEFAULT:
					return new SqlExpression(func.SystemType, "CAST({0} AS {1} DEFAULT {2} ON CONVERSION ERROR)", Precedence.Primary, func.Parameters[2], func.Parameters[0], func.Parameters[3])
					{
						CanBeNull = func.Parameters[2].CanBeNullable(NullabilityContext) || func.Parameters[3].CanBeNullable(NullabilityContext)
					};
			}

			return base.ConvertSqlFunction(func);
		}

	}
}
