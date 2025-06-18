using System;
using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public static class SqlExpressionFactoryExtensions
	{
		public static ISqlExpression Fragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params ISqlExpression[] arguments)
		{
			return factory.Fragment(dataType, Precedence.Primary, fragmentText, null, arguments);
		}

		public static ISqlExpression NotNullFragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params ISqlExpression[] arguments)
		{
			return factory.NotNullFragment(dataType, Precedence.Primary, fragmentText, arguments);
		}

		public static ISqlExpression NonPureFragment(this ISqlExpressionFactory factory, DbDataType dataType, string fragmentText, params ISqlExpression[] arguments)
		{
			return new SqlExpression(dataType.SystemType, fragmentText, Precedence.Primary, SqlFlags.None, ParametersNullabilityType.IfAnyParameterNullable, null, arguments);
		}

		public static ISqlExpression Fragment(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string fragmentText, params ISqlExpression[] arguments)
		{
			return new SqlExpression(dataType.SystemType, fragmentText, precedence, SqlFlags.None, ParametersNullabilityType.Undefined, null, arguments);
		}

		public static ISqlExpression Fragment(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string fragmentText, bool? canBeNull, params ISqlExpression[] arguments)
		{
			return new SqlExpression(dataType.SystemType, fragmentText, precedence, SqlFlags.None, ParametersNullabilityType.Undefined, canBeNull, arguments);
		}

		public static ISqlExpression NotNullFragment(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string fragmentText, params ISqlExpression[] arguments)
		{
			return new SqlExpression(dataType.SystemType, fragmentText, precedence, SqlFlags.None, ParametersNullabilityType.NotNullable, null, arguments);
		}

		public static ISqlExpression Function(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params ISqlExpression[] parameters)
		{
			return new SqlFunction(dataType, functionName, parameters);
		}

		public static ISqlExpression Function(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, ParametersNullabilityType parametersNullability, params ISqlExpression[] parameters)
		{
			return new SqlFunction(dataType, functionName, parametersNullability, parameters);
		}

		public static ISqlExpression WindowFunction(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, 
			SqlFunctionArgument[]                                              arguments,
			bool[]                                                             argumentsNullability,
			IEnumerable<SqlWindowOrderItem>?                                   withinGroup = null,
			IEnumerable<ISqlExpression>?                                       partitionBy = null,
			IEnumerable<SqlWindowOrderItem>?                                   orderBy     = null,
			SqlFrameClause?                                                    frameClause = null,
			SqlSearchCondition?                                                filter      = null,
			bool                                                               isAggregate = false
		)
		{
			return new SqlWindowFunction(dataType, functionName, arguments, argumentsNullability, 
				withinGroup : withinGroup, 
				partitionBy : partitionBy, 
				orderBy : orderBy,
				filter : filter,
				frameClause : frameClause, 
				isAggregate : isAggregate);
		}

		public static ISqlPredicate ExprPredicate(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return new SqlPredicate.Expr(expression);
		}

		public static ISqlPredicate IsNullPredicate(this ISqlExpressionFactory factory, ISqlExpression expression, bool isNot = false)
		{
			return new SqlPredicate.IsNull(expression, isNot);
		}

		public static SqlSearchCondition SearchCondition(this ISqlExpressionFactory factory, bool isOr = false)
		{
			return new SqlSearchCondition(isOr);
		}

		public static ISqlExpression NonPureFunction(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params ISqlExpression[] parameters)
		{
			return new SqlFunction(dataType, functionName, false, false, parameters);
		}

		public static ISqlExpression Value<T>(this ISqlExpressionFactory factory, DbDataType dataType, T value)
		{
			return new SqlValue(dataType, value);
		}

		public static ISqlExpression Value<T>(this ISqlExpressionFactory factory, T value)
		{
			return factory.Value(factory.GetDbDataType(typeof(T)), value);
		}

		public static ISqlExpression NotNull(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return new SqlNullabilityExpression(expression, false);
		}

		public static ISqlExpression Cast(this ISqlExpressionFactory factory, ISqlExpression expression, DbDataType toDbDataType, bool isMandatory = false)
		{
			return new SqlCastExpression(expression, toDbDataType, null, isMandatory);
		}

		public static ISqlExpression Div(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "/", y, Precedence.Multiplicative);
		}

		public static ISqlExpression Div<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, T value)
			where T : struct
		{
			return factory.Div(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static ISqlExpression Multiply(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "*", y, Precedence.Multiplicative);
		}

		public static ISqlExpression Multiply<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, T value)
			where T : struct
		{
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static ISqlExpression Multiply<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static ISqlExpression Negate(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression v)
		{
			return new SqlBinaryExpression(dbDataType, factory.Value(-1), "*", v, Precedence.Multiplicative);
		}

		public static ISqlExpression Sub(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "-", y, Precedence.Subtraction);
		}

		public static ISqlExpression Add(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "+", y, Precedence.Additive);
		}

		public static ISqlExpression Binary(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, string operation, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, operation, y, Precedence.Additive);
		}

		public static ISqlExpression Concat(this ISqlExpressionFactory factory, ISqlExpression x, ISqlExpression y)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new SqlBinaryExpression(dbDataType, x, "+", y, Precedence.Additive);
		}

		public static ISqlExpression Concat(this ISqlExpressionFactory factory, params ISqlExpression[] expressions)
		{
			if (expressions.Length == 0)
				throw new InvalidOperationException("At least one expression must be provided for concatenation.");

			var result     = expressions[0];
			var dbDataType = factory.GetDbDataType(result);

			for (var i = 1; i < expressions.Length; i++)
			{
				result = factory.Concat(dbDataType, result, expressions[i]);
			}
			
			return result;
		}

		public static ISqlExpression Coalesce(this ISqlExpressionFactory factory, params ISqlExpression[] expressions)
		{
			if (expressions.Length == 0)
				throw new InvalidOperationException("At least one expression must be provided for coalesce.");

			return new SqlCoalesceExpression(expressions);
		}

		public static ISqlExpression Condition(this ISqlExpressionFactory factory, ISqlPredicate condition, ISqlExpression trueExpression, ISqlExpression falseExpression)
		{
			return new SqlConditionExpression(condition, trueExpression, falseExpression);
		}

		public static ISqlExpression Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "+", y, Precedence.Concatenate);
		}

		public static ISqlExpression Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, string value)
		{
			return factory.Concat(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static ISqlExpression Concat(this ISqlExpressionFactory factory, ISqlExpression x, string value)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new SqlBinaryExpression(dbDataType, x, "+", factory.Value(dbDataType, value), Precedence.Concatenate);
		}

		public static ISqlExpression Increment<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Add(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static ISqlExpression Increment(this ISqlExpressionFactory factory, ISqlExpression x)
		{
			return factory.Increment(x, 1);
		}

		public static ISqlExpression Decrement<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Sub(dbDataType, x, factory.Value(dbDataType, value));
		}

		public static ISqlExpression Decrement(this ISqlExpressionFactory factory, ISqlExpression x)
		{
			return factory.Decrement(x, 1);
		}

		public static ISqlExpression Mod(this ISqlExpressionFactory factory, ISqlExpression x, ISqlExpression value)
		{
			return new SqlBinaryExpression(factory.GetDbDataType(x).SystemType, x, "%", value);
		}

		public static ISqlExpression Mod<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Mod(x, new SqlValue(dbDataType, value));
		}

		public static ISqlExpression TypeExpression(this ISqlExpressionFactory factory, DbDataType dbDataType)
		{
			return new SqlDataType(dbDataType);
		}

		public static ISqlExpression EnsureType(this ISqlExpressionFactory factory, ISqlExpression expression, DbDataType dbDataType)
		{
			var expressionType = factory.GetDbDataType(expression);
			if (expressionType.Equals(dbDataType))
				return expression;

			return factory.Cast(expression, dbDataType);
		}

		#region String functions

		public static ISqlExpression ToLower(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return factory.Function(factory.GetDbDataType(expression), PseudoFunctions.TO_LOWER, expression);
		}

		public static ISqlExpression ToUpper(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return factory.Function(factory.GetDbDataType(expression), PseudoFunctions.TO_UPPER, expression);
		}

		#endregion

		#region Predicates

		public static ISqlPredicate Equal(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Equal, expr2, factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null);
		}

		public static ISqlPredicate NotEqual(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.NotEqual, expr2, factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null);
		}

		public static ISqlPredicate Greater(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Greater, expr2, factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null);
		}

		public static ISqlPredicate GreaterOrEqual(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.GreaterOrEqual, expr2, factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null);
		}

		public static ISqlPredicate Less(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Less, expr2, factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? false : null);
		}

		public static ISqlPredicate LessOrEqual(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.LessOrEqual, expr2, factory.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null);
		}

		#endregion
	}
}
