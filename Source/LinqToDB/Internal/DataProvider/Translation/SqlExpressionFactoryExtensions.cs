using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public static class SqlExpressionFactoryExtensions
	{
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Fragment(this ISqlExpressionFactory factory, string fragmentText, params ISqlExpression[] parameters)
		{
			return new SqlFragment(fragmentText, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Fragment(this ISqlExpressionFactory factory, string fragmentText, int precedence, params ISqlExpression[] parameters)
		{
			return new SqlFragment(fragmentText, precedence, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Expression(this ISqlExpressionFactory factory, DbDataType dataType, string expr, params ISqlExpression[] parameters)
		{
			return factory.Expression(dataType, Precedence.Primary, expr, null, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Expression(this ISqlExpressionFactory factory, DbDataType dataType, string expr, int precedence, params ISqlExpression[] parameters)
		{
			return factory.Expression(dataType, precedence, expr, null, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression NotNullExpression(this ISqlExpressionFactory factory, DbDataType dataType, string expr, params ISqlExpression[] parameters)
		{
			return factory.NotNullExpression(dataType, Precedence.Primary, expr, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression NonPureExpression(this ISqlExpressionFactory factory, DbDataType dataType, string expr, params ISqlExpression[] parameters)
		{
			return new SqlExpression(dataType, expr, Precedence.Primary, SqlFlags.None, ParametersNullabilityType.IfAnyParameterNullable, null, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Expression(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string expr, params ISqlExpression[] parameters)
		{
			return new SqlExpression(dataType, expr, precedence, SqlFlags.None, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Expression(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string expr, bool? canBeNull, params ISqlExpression[] parameters)
		{
			return new SqlExpression(dataType, expr, precedence, SqlFlags.None, canBeNull, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression NotNullExpression(this ISqlExpressionFactory factory, DbDataType dataType, int precedence, string expr, params ISqlExpression[] parameters)
		{
			return new SqlExpression(dataType, expr, precedence, SqlFlags.None, ParametersNullabilityType.NotNullable, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Function(this ISqlExpressionFactory factory, DbDataType type, string functionName, params ISqlExpression[] parameters)
		{
			return new SqlFunction(type, functionName, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Function(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, ParametersNullabilityType parametersNullability, params ISqlExpression[] parameters)
		{
			return new SqlFunction(dataType, functionName, parametersNullability, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static SqlSearchCondition SearchCondition(this ISqlExpressionFactory factory, bool isOr = false)
		{
			return new SqlSearchCondition(isOr);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression NonPureFunction(this ISqlExpressionFactory factory, DbDataType dataType, string functionName, params ISqlExpression[] parameters)
		{
			return new SqlFunction(dataType, functionName, SqlFlags.None, parameters);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Null(this ISqlExpressionFactory factory, DbDataType dataType)
		{
			return new SqlValue(dataType, null);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Value<T>(this ISqlExpressionFactory factory, DbDataType dataType, T value)
		{
			return new SqlValue(dataType, value);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Value<T>(this ISqlExpressionFactory factory, T value)
		{
			return factory.Value(factory.GetDbDataType(typeof(T)), value);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression NullValue(this ISqlExpressionFactory factory, DbDataType dataType)
		{
			return new SqlValue(dataType, null);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression NotNull(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return new SqlNullabilityExpression(expression, false);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Cast(this ISqlExpressionFactory factory, ISqlExpression expression, DbDataType toDbDataType, bool isMandatory = false)
		{
			return new SqlCastExpression(expression, toDbDataType, null, isMandatory);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Cast(this ISqlExpressionFactory factory, ISqlExpression expression, DbDataType toDbDataType, SqlDataType? fromType, bool isMandatory = false)
		{
			return new SqlCastExpression(expression, toDbDataType, fromType, isMandatory);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Div(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "/", y, Precedence.Multiplicative);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Div<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, T value)
			where T : struct
		{
			return factory.Div(dbDataType, x, factory.Value(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Multiply(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "*", y, Precedence.Multiplicative);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Multiply<T>(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, T value)
			where T : struct
		{
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Multiply<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Multiply(dbDataType, x, factory.Value(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Negate(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression v)
		{
			return new SqlBinaryExpression(dbDataType, factory.Value(-1), "*", v, Precedence.Multiplicative);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Sub(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "-", y, Precedence.Subtraction);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Add(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "+", y, Precedence.Additive);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Binary(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, string operation, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, operation, y, Precedence.Additive);
		}

		public static ISqlExpression Concat(this ISqlExpressionFactory factory, ISqlExpression x, ISqlExpression y)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new SqlBinaryExpression(dbDataType, x, "+", y, Precedence.Additive);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
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

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Coalesce(this ISqlExpressionFactory factory, params ISqlExpression[] expressions)
		{
			if (expressions.Length == 0)
				throw new InvalidOperationException("At least one expression must be provided for coalesce.");

			return new SqlCoalesceExpression(expressions);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Condition(this ISqlExpressionFactory factory, ISqlPredicate condition, ISqlExpression trueExpression, ISqlExpression falseExpression)
		{
			return new SqlConditionExpression(condition, trueExpression, falseExpression);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, ISqlExpression y)
		{
			return new SqlBinaryExpression(dbDataType, x, "+", y, Precedence.Concatenate);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Concat(this ISqlExpressionFactory factory, DbDataType dbDataType, ISqlExpression x, string value)
		{
			return factory.Concat(dbDataType, x, factory.Value(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Concat(this ISqlExpressionFactory factory, ISqlExpression x, string value)
		{
			var dbDataType = factory.GetDbDataType(x);
			return new SqlBinaryExpression(dbDataType, x, "+", factory.Value(dbDataType, value), Precedence.Concatenate);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Increment<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Add(dbDataType, x, factory.Value(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Increment(this ISqlExpressionFactory factory, ISqlExpression x)
		{
			return factory.Increment(x, 1);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Decrement<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Sub(dbDataType, x, factory.Value(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Decrement(this ISqlExpressionFactory factory, ISqlExpression x)
		{
			return factory.Decrement(x, 1);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Mod(this ISqlExpressionFactory factory, ISqlExpression x, ISqlExpression value)
		{
			return new SqlBinaryExpression(factory.GetDbDataType(x).SystemType, x, "%", value);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Mod<T>(this ISqlExpressionFactory factory, ISqlExpression x, T value)
			where T : struct
		{
			var dbDataType = factory.GetDbDataType(x);
			return factory.Mod(x, new SqlValue(dbDataType, value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression TypeExpression(this ISqlExpressionFactory factory, DbDataType dbDataType)
		{
			return new SqlDataType(dbDataType);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression EnsureType(this ISqlExpressionFactory factory, ISqlExpression expression, DbDataType dbDataType)
		{
			var expressionType = factory.GetDbDataType(expression);
			if (expressionType.EqualsDbOnly(dbDataType))
				return expression;

			return factory.Cast(expression, dbDataType);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static SqlDataType SqlDataType(this ISqlExpressionFactory factory, DbDataType type)
		{
			return new SqlDataType(type);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static SqlDataType SqlDataType(this ISqlExpressionFactory factory, DataType dataType)
		{
			return new SqlDataType(dataType);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlExpression Function(this ISqlExpressionFactory factory, DbDataType dataType, string functionName,
			SqlFunctionArgument[] arguments,
			bool[] argumentsNullability,
			bool? canBeNull = null,
			IEnumerable<SqlWindowOrderItem>? withinGroup = null,
			IEnumerable<ISqlExpression>? partitionBy = null,
			IEnumerable<SqlWindowOrderItem>? orderBy = null,
			SqlFrameClause? frameClause = null,
			SqlSearchCondition? filter = null,
			bool isAggregate = false,
			bool canBeAffectedByOrderBy = false
		)
		{
			return new SqlExtendedFunction(dataType, functionName, arguments, argumentsNullability,
				canBeNull: canBeNull,
				withinGroup: withinGroup,
				partitionBy: partitionBy,
				orderBy: orderBy,
				filter: filter,
				frameClause: frameClause,
				isAggregate: isAggregate,
				canBeAffectedByOrderBy : canBeAffectedByOrderBy);
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

		public static ISqlExpression Length(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return factory.Function(factory.GetDbDataType(typeof(int)), PseudoFunctions.LENGTH, expression);
		}

		public static ISqlExpression Replace(this ISqlExpressionFactory factory, ISqlExpression expression, ISqlExpression oldSubString, ISqlExpression newSubstring)
		{
			var valueType = factory.GetDbDataType(expression);
			return factory.Function(valueType, PseudoFunctions.REPLACE, expression, factory.EnsureType(oldSubString, valueType), factory.EnsureType(newSubstring, valueType));
		}

		#endregion

		#region Predicates

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlPredicate ExprPredicate(this ISqlExpressionFactory factory, ISqlExpression expression)
		{
			return new SqlPredicate.Expr(expression);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlPredicate IsNullPredicate(this ISqlExpressionFactory factory, ISqlExpression expression, bool isNot = false)
		{
			return new SqlPredicate.IsNull(expression, isNot);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static SqlPredicate.Like LikePredicate(this ISqlExpressionFactory factory, ISqlExpression value, bool isNull, ISqlExpression template, ISqlExpression? escape = null, string? functionName = null)
		{
			return new SqlPredicate.Like(value, isNull, template, escape, functionName);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlPredicate Equal(this ISqlExpressionFactory factory, ISqlExpression expr1, ISqlExpression expr2, bool? unknownValue)
		{
			return new SqlPredicate.ExprExpr(expr1, SqlPredicate.Operator.Equal, expr2, unknownValue);
		}

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

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "factory is an extension point")]
		public static ISqlPredicate IsNull(this ISqlExpressionFactory factory, ISqlExpression expr, bool isNot = false)
		{
			return new SqlPredicate.IsNull(expr, isNot);
		}

		#endregion
	}
}
