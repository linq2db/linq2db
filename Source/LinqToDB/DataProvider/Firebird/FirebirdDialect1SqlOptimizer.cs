using System;

namespace LinqToDB.DataProvider.Firebird
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class FirebirdDialect1SqlOptimizer : FirebirdSqlOptimizer
	{
		public FirebirdDialect1SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Average":
					if (func.Parameters.Length == 1 && func.Parameters[0].SystemType?.IsIntegerType() == true)
						return new SqlFunction(func.SystemType, "Avg", new SqlExpression(typeof(int), "CAST({0} as DOUBLE PRECISION)", Precedence.Primary, func.Parameters[0]));
					break;
				case "$Convert$":
					if (func.SystemType == typeof(string)
						&& func.Parameters.Length == 3
						&& func.Parameters[2] is SqlBinaryExpression binaryExpr
						&& binaryExpr.SystemType.IsIntegerType())
						func = new SqlFunction(func.SystemType, func.Name, func.Parameters[0], func.Parameters[1], new SqlExpression(typeof(int), "CAST({0} as INT)", Precedence.Primary, func.Parameters[2]));
					break;
			}

			return base.ConvertFunction(func);
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, ConvertVisitor visitor,
			EvaluationContext context)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
				#region SqlBinaryExpression
				{
					var be = (SqlBinaryExpression)expr;

					switch (be.Operation)
					{
						case "+":
						{
							if (be.Expr1.SystemType == typeof(string) && be.Expr2.SystemType?.IsInteger32Type() == true)
							{
								var len = be.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr2.SystemType).Type.DataType);

								if (len == null || len <= 0)
									len = 10;

								return new SqlBinaryExpression(
									be.SystemType,
									be.Expr1,
									be.Operation,
									ConvertExpressionImpl(new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), new SqlFunction(typeof(int), "Convert", new SqlDataType(DataType.Int32), be.Expr2)), visitor, context),
									be.Precedence);
							}

							if (be.Expr1.SystemType?.IsInteger32Type() == true && be.Expr2.SystemType == typeof(string))
							{
								var len = be.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr1.SystemType).Type.DataType);

								if (len == null || len <= 0)
									len = 10;

								return new SqlBinaryExpression(
									be.SystemType,
									ConvertExpressionImpl(new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), new SqlFunction(typeof(int), "Convert", new SqlDataType(DataType.Int32), be.Expr1)), visitor, context),
									be.Operation,
									be.Expr2,
									be.Precedence);
							}

							break;
						}
					}

					break;
				}
				#endregion
			}

			expr = base.ConvertExpressionImpl(expr, visitor, context);

			if (expr is SqlExpression sqlExpr
				&& sqlExpr.SystemType?.ToUnderlying() == typeof(DateTime)
				&& sqlExpr.Expr == CASTEXPR
				&& sqlExpr.Parameters.Length == 2
				&& sqlExpr.Parameters[1] is SqlExpression typeExpr)
			{
				if (typeExpr.Expr == "Date")
				{
					return new SqlExpression(sqlExpr.SystemType, "CAST(CAST({0} as VARCHAR(11)) AS DATE)", Precedence.Primary, sqlExpr.Parameters[0]);
				}
				else if (typeExpr.Expr == "Time")
				{
					return sqlExpr.Parameters[0];
				}
			}

			return expr;
		}
	}
}
