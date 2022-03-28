using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LinqToDB.SqlQuery
{
	partial class QueryHelper
	{
		public static SqlParameterValue GetParameterValue(this SqlParameter parameter, IReadOnlyParameterValues? parameterValues)
		{
			if (parameterValues != null && parameterValues.TryGetValue(parameter, out var value))
			{
				return value;
			}
			return new SqlParameterValue(parameter.Value, parameter.Type);
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, EvaluationContext context, out object? result)
		{
			(result, var error) = expr.TryEvaluateExpression(context);
			return error == null;
		}

		public static bool IsMutable(this IQueryElement expr)
		{
			if (expr.CanBeEvaluated(false))
				return false;
			return expr.CanBeEvaluated(true);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, bool withParameters)
		{
			return expr.TryEvaluateExpression(new EvaluationContext(withParameters ? SqlParameterValues.Empty : null), out _, out _);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, EvaluationContext context)
		{
			return expr.TryEvaluateExpression(context, out _, out _);
		}

		internal static (object? value, string? error) TryEvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			if (!context.TryGetValue(expr, out var info))
			{
				if (TryEvaluateExpressionInternal(expr, context, out var result, out var errorMessage))
					context.Register(expr, result);
				else
					context.RegisterError(expr, errorMessage);
				return (result, errorMessage);
			}

			return info.Value;
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, EvaluationContext context, out object? result,
			[NotNullWhen(false)] out string? errorMessage)
		{
			if (!context.TryGetValue(expr, out var info))
			{
				if (TryEvaluateExpressionInternal(expr, context, out result, out errorMessage))
					context.Register(expr, result);
				else
					context.RegisterError(expr, errorMessage);

				return errorMessage == null;
			}

			result = info.Value.value;
			errorMessage = info.Value.error;
			return errorMessage == null;
		}

		static bool TryEvaluateExpressionInternal(this IQueryElement expr, EvaluationContext context, out object? result, [NotNullWhen(false)] out string? errorMessage)
		{
			result = null;
			errorMessage = null;
			switch (expr.ElementType)
			{
				case QueryElementType.SqlValue           : result = ((SqlValue)expr).Value; return true;
				case QueryElementType.SqlParameter       :
				{
					var sqlParameter = (SqlParameter)expr;

					if (context.ParameterValues == null)
					{
						errorMessage = "context.ParameterValues is null";
						return false;
					}

					result = sqlParameter.GetParameterValue(context.ParameterValues).Value;
					return true;
				}
				case QueryElementType.IsNullPredicate:
				{
					var isNullPredicate = (SqlPredicate.IsNull)expr;
					if (!isNullPredicate.Expr1.TryEvaluateExpression(context, out var value, out errorMessage))
						return false;
					result = isNullPredicate.IsNot == (value != null);
					return true;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var exprExpr = (SqlPredicate.ExprExpr)expr;
					var reduced = exprExpr.Reduce(context);
					if (!ReferenceEquals(reduced, expr))
						return TryEvaluateExpression(reduced, context, out result, out errorMessage);

					if (!exprExpr.Expr1.TryEvaluateExpression(context, out var value1, out errorMessage) ||
					    !exprExpr.Expr2.TryEvaluateExpression(context, out var value2, out errorMessage))
						return false;

					switch (exprExpr.Operator)
					{
						case SqlPredicate.Operator.Equal:
						{
							if (value1 == null)
							{
								result = value2 == null;
							}
							else
							{
								result = (value2 != null) && value1.Equals(value2);
							}
							break;
						}
						case SqlPredicate.Operator.NotEqual:
						{
							if (value1 == null)
							{
								result = value2 != null;
							}
							else
							{
								result = value2 == null || !value1.Equals(value2);
							}
							break;
						}
						default:
						{
							if (!(value1 is IComparable comp1) || !(value2 is IComparable comp2))
							{
								result = false;
								return true;
							}

							switch (exprExpr.Operator)
							{
								case SqlPredicate.Operator.Greater:
									result = comp1.CompareTo(comp2) > 0;
									break;
								case SqlPredicate.Operator.GreaterOrEqual:
									result = comp1.CompareTo(comp2) >= 0;
									break;
								case SqlPredicate.Operator.NotGreater:
									result = !(comp1.CompareTo(comp2) > 0);
									break;
								case SqlPredicate.Operator.Less:
									result = comp1.CompareTo(comp2) < 0;
									break;
								case SqlPredicate.Operator.LessOrEqual:
									result = comp1.CompareTo(comp2) <= 0;
									break;
								case SqlPredicate.Operator.NotLess:
									result = !(comp1.CompareTo(comp2) < 0);
									break;

								default:
									errorMessage = $"Cannot evaluate operator {exprExpr.Operator}";
									return false;

							}
							break;
						}
					}

					return true;
				}
				case QueryElementType.IsTruePredicate:
				{
					var isTruePredicate = (SqlPredicate.IsTrue)expr;
					if (!isTruePredicate.Expr1.TryEvaluateExpression(context, out var value, out errorMessage))
						return false;

					if (value == null)
					{
						result = false;
						return true;
					}

					if (value is bool boolValue)
					{
						result = boolValue != isTruePredicate.IsNot;
						return true;
					}

					errorMessage = "Cannot evaluate IsTrue predicate";
					return false;
				}
				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)expr;
					if (!binary.Expr1.TryEvaluateExpression(context, out var leftEvaluated, out errorMessage))
						return false;
					if (!binary.Expr2.TryEvaluateExpression(context, out var rightEvaluated, out errorMessage))
						return false;
					dynamic? left  = leftEvaluated;
					dynamic? right = rightEvaluated;
					if (left == null || right == null)
						return true;
					switch (binary.Operation)
					{
						case "+" : result = left + right; break;
						case "-" : result = left - right; break;
						case "*" : result = left * right; break;
						case "/" : result = left / right; break;
						case "%" : result = left % right; break;
						case "^" : result = left ^ right; break;
						case "&" : result = left & right; break;
						case "<" : result = left < right; break;
						case ">" : result = left > right; break;
						case "<=": result = left <= right; break;
						case ">=": result = left >= right; break;
						default:
							errorMessage = $"Unknown binary operation '{binary.Operation}'.";
							return false;
					}

					return true;
				}
				case QueryElementType.SqlFunction        :
				{
					var function = (SqlFunction)expr;

					switch (function.Name)
					{
						case "CASE":
						{
							if (function.Parameters.Length != 3)
							{
								errorMessage = "CASE function expected to have 3 parameters.";
								return false;
							}

							if (!function.Parameters[0]
								.TryEvaluateExpression(context, out var cond, out errorMessage))
								return false;

							if (!(cond is bool))
							{
								errorMessage =
									$"CASE function expected to have boolean condition (was: {cond?.GetType()}).";
								return false;
							}

							if ((bool)cond!)
								return function.Parameters[1]
									.TryEvaluateExpression(context, out result, out errorMessage);
							else
								return function.Parameters[2]
									.TryEvaluateExpression(context, out result, out errorMessage);
						}

						case "Length":
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue, out errorMessage))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.Length;
									return true;
								}
							}

							errorMessage = $"Cannot evaluate '{function.Name}' function.";
							return false;
						}

						case "$ToLower$":
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue, out errorMessage))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.ToLower(CultureInfo.InvariantCulture);
									return true;
								}
							}

							errorMessage = $"Cannot evaluate '{function.Name}' function.";
							return false;
						}

						case "$ToUpper$":
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue, out errorMessage))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.ToUpper(CultureInfo.InvariantCulture);
									return true;
								}
							}

							errorMessage = $"Cannot evaluate '{function.Name}' function.";
							return false;
						}

						default:
							errorMessage = $"Unknown function '{function.Name}'.";
							return false;
					}
				}

				case QueryElementType.SearchCondition    :
				{
					var cond     = (SqlSearchCondition)expr;
					errorMessage = null;

					if (cond.Conditions.Count == 0)
					{
						result = true;
						return true;
					}

					for (var i = 0; i < cond.Conditions.Count; i++)
					{
						var condition = cond.Conditions[i];
						if (condition.TryEvaluateExpression(context, out var evaluated, out errorMessage))
						{
							if (evaluated is bool boolValue)
							{
								if (i == cond.Conditions.Count - 1 || condition.IsOr == boolValue)
								{
									result = boolValue;
									return true;
								}
							}
							else if (!condition.IsOr)
							{
								errorMessage = $"Non-boolean condition value '{evaluated}'.";
								return false;
							}
						}
					}

					errorMessage ??= "Cannot evaluate search condition";
					return false;
				}
				case QueryElementType.ExprPredicate      :
				{
					var predicate = (SqlPredicate.Expr)expr;
					if (!predicate.Expr1.TryEvaluateExpression(context, out var value, out errorMessage))
						return false;

					result = value;
					return true;
				}
				case QueryElementType.Condition          :
				{
					var cond = (SqlCondition)expr;
					if (cond.Predicate.TryEvaluateExpression(context, out var evaluated, out errorMessage))
					{
						if (evaluated is bool boolValue)
						{
							result = cond.IsNot ? !boolValue : boolValue;
							return true;
						}
						else
						{
							errorMessage = $"Non-boolean condition value '{evaluated}'.";
							return false;
						}
					}

					return false;
				}

				default:
				{
					errorMessage = $"Cannot evaluate '{expr.ElementType}' expression.";
					return false;
				}
			}
		}

		public static object? EvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			var (value, error) = expr.TryEvaluateExpression(context);
			if (error != null)
				throw new LinqToDBException(error);

			return value;
		}

		public static bool? EvaluateBoolExpression(this IQueryElement expr, EvaluationContext context, bool? defaultValue = null)
		{
			var evaluated = expr.EvaluateExpression(context);

			if (evaluated is bool boolValue)
				return boolValue;

			return defaultValue;
		}
	}
}
