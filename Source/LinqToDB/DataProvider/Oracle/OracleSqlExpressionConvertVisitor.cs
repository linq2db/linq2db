using System;

namespace LinqToDB.DataProvider.Oracle
{
	using LinqToDB.Extensions;
	using SqlProvider;
	using SqlQuery;

	public class OracleSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public OracleSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		#region LIKE

		protected static string[] OracleLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => OracleLikeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var expr = predicate;

			// Oracle saves empty string as null to database, so we need predicate modification before sending query
			//
			if (expr.WithNull == true &&
			    (expr.Operator == SqlPredicate.Operator.Equal          ||
			     expr.Operator == SqlPredicate.Operator.NotEqual       ||
			     expr.Operator == SqlPredicate.Operator.GreaterOrEqual ||
			     expr.Operator == SqlPredicate.Operator.LessOrEqual))
			{
				if (expr.Expr1.SystemType == typeof(string) &&
				    expr.Expr1.TryEvaluateExpression(EvaluationContext, out var value1) && value1 is string string1)
				{
					if (string1.Length == 0)
					{
						// Add 'AND [col] IS NOT NULL' when checking Not Equal to Empty String,
						// else add 'OR [col] IS NULL'

						if (expr.Operator == SqlPredicate.Operator.NotEqual)
						{
							var sc = new SqlSearchCondition(true,
								new SqlPredicate.ExprExpr(expr.Expr2, SqlPredicate.Operator.NotEqual, expr.Expr1, null),
								new SqlPredicate.IsNull(expr.Expr2, true));

							return sc;
						}
						else
						{
							var sc = new SqlSearchCondition(true,
								new SqlPredicate.ExprExpr(expr.Expr2, expr.Operator, expr.Expr1, null),
								new SqlPredicate.IsNull(expr.Expr2, false));

							return sc;
						}
					}
				}

				if (expr.Expr2.SystemType == typeof(string)                             &&
				    expr.Expr2.TryEvaluateExpression(EvaluationContext, out var value2) && value2 is string string2)
				{
					if (string2.Length == 0)
					{
						// Add 'AND [col] IS NOT NULL' when checking Not Equal to Empty String,
						// else add 'OR [col] IS NULL'

						if (expr.Operator == SqlPredicate.Operator.NotEqual)
						{
							var sc = new SqlSearchCondition(true, 
								new SqlPredicate.ExprExpr(expr.Expr1, SqlPredicate.Operator.NotEqual, expr.Expr2, null),
								new SqlPredicate.IsNull(expr.Expr1, true));

							return sc;
						}
						else
						{
							var sc = new SqlSearchCondition(true,
								new SqlPredicate.ExprExpr(expr.Expr1, expr.Operator, expr.Expr2, null),
								new SqlPredicate.IsNull(expr.Expr1, false));

							return sc;
						}
					}
				}
			}

			return base.ConvertExprExprPredicate(predicate);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%": return new SqlFunction(element.SystemType, "MOD", element.Expr1, element.Expr2);
				case "&": return new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2);
				case "|": // (a + b) - BITAND(a, b)
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2),
						element.SystemType);

				case "^": // (a + b) - BITAND(a, b) * 2
					return Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlFunction(element.SystemType, "BITAND", element.Expr1, element.Expr2), 2),
						element.SystemType);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlExpression(SqlExpression element)
		{
			if (element.Expr.StartsWith("To_Number(To_Char(") && element.Expr.EndsWith(", 'FF'))"))
				return Div(new SqlExpression(element.SystemType, element.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), element.Parameters), 1000);

			return base.ConvertSqlExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "Coalesce":
				{
					if (func.Parameters.Length == 2)
						return ConvertCoalesceToBinaryFunc(func, "Nvl");

					return func;
				}

				case "CharIndex"      :
					return func.Parameters.Length == 2?
						new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0]):
						new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0], func.Parameters[2]);
				case "AVG"            :
					return new SqlFunction(
						func.SystemType,
						"Round",
						new SqlFunction(func.SystemType, "AVG", func.Parameters[0]),
						new SqlValue(27));
			}

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression ConvertConversion(SqlFunction func)
		{
			var ftype = func.SystemType.ToUnderlying();

			var toType   = func.Parameters[0];
			var fromType = func.Parameters[1];
			var argument = func.Parameters[2];

			if (ftype == typeof(bool))
			{
				var ex = AlternativeConvertToBoolean(func, 2);
				if (ex != null)
					return ex;
			}

			if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
				|| ftype == typeof(DateOnly)
#endif
			   )
			{
				if (IsTimeDataType(toType))
				{
					if (argument.SystemType == typeof(string))
						return argument;

					return new SqlFunction(func.SystemType, "To_Char", argument, new SqlValue("HH24:MI:SS"));
				}

				if (IsDateDataType(toType, "Date"))
				{
					if (argument.SystemType!.ToUnderlying() == typeof(DateTime)
						|| argument.SystemType!.ToUnderlying() == typeof(DateTimeOffset))
					{
						return new SqlFunction(func.SystemType, "Trunc", argument, new SqlValue("DD"));
					}

					return new SqlFunction(func.SystemType, "TO_DATE", argument, new SqlValue("YYYY-MM-DD"));
				}
				else if (IsDateDataOffsetType(toType))
				{
					if (ftype == typeof(DateTimeOffset))
						return argument;

					return new SqlFunction(func.SystemType, "TO_TIMESTAMP_TZ", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
				}

				return new SqlFunction(func.SystemType, "TO_TIMESTAMP", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
			}
			else if (ftype == typeof(string))
			{
				var stype = argument.SystemType!.ToUnderlying();

				if (stype == typeof(DateTimeOffset))
				{
					return new SqlFunction(func.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS TZH:TZM"));
				}
				else if (stype == typeof(DateTime))
				{
					return new SqlFunction(func.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD HH24:MI:SS"));
				}
#if NET6_0_OR_GREATER
				else if (stype == typeof(DateOnly))
				{
					return new SqlFunction(func.SystemType, "To_Char", argument, new SqlValue("YYYY-MM-DD"));
				}
#endif
			}

			return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func, argument), toType);
		}
	}
}
