using System;
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
			var info = expr.TryEvaluateExpression(context);
			result = info.Value;
			return info.IsEvaluated;
		}

		public static bool CanBeEvaluated(this IQueryElement expr, bool withParameters)
		{
			return expr.TryEvaluateExpression(new EvaluationContext(withParameters ? SqlParameterValues.Empty : null)).IsEvaluated;
		}

		public static bool CanBeEvaluated(this IQueryElement expr, EvaluationContext context)
		{
			return expr.TryEvaluateExpression(context).IsEvaluated;
		}

		public static EvaluationContext.EvaluationInfo TryEvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			if (!context.TryGetValue(expr, out var info))
			{
				var canBeEvaluated =
					TryEvaluateExpressionInternal(expr, context, out var result, out var errorMessage);
				info = new EvaluationContext.EvaluationInfo(canBeEvaluated, result, errorMessage);
				context.Register(expr, info);
				return info;
			}

			return info!;
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, EvaluationContext context, out object? result,
			out string? errorMessage)
		{
			if (!context.TryGetValue(expr, out var info))
			{
				var canBeEvaluated = TryEvaluateExpressionInternal(expr, context, out result, out errorMessage);
				info = new EvaluationContext.EvaluationInfo(canBeEvaluated, result, errorMessage);
				context.Register(expr, info);

				return info.IsEvaluated;
			}

			result = info!.Value;
			errorMessage = info.ErrorMessage;
			return info.IsEvaluated;
		}

		static bool TryEvaluateExpressionInternal(this IQueryElement expr, EvaluationContext context, out object? result, out string? errorMessage)
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
						return false;

					result = sqlParameter.GetParameterValue(context.ParameterValues).Value;
					return true;
				}
				case QueryElementType.IsNullPredicate:
				{
					var isNullPredicate = (SqlPredicate.IsNull)expr;
					if (!isNullPredicate.Expr1.TryEvaluateExpression(context, out var value))
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

					if (!exprExpr.Expr1.TryEvaluateExpression(context, out var value1) ||
					    !exprExpr.Expr2.TryEvaluateExpression(context, out var value2))
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
					if (!isTruePredicate.Expr1.TryEvaluateExpression(context, out var value))
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

							return false;
						}

						default:
							errorMessage = $"Unknown function '{function.Name}'.";
							return false;
					}
				}

				default:
				{
					return false;
				}
			}
		}

		public static object? EvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			var info = expr.TryEvaluateExpression(context);
			if (!info.IsEvaluated)
			{
				var message = info.ErrorMessage ?? GetEvaluationError(expr);

				throw new LinqToDBException(message);
			}

			return info.Value;
		}

		private static string GetEvaluationError(IQueryElement expr)
		{
			return $"Not implemented evaluation of '{expr.ElementType}': '{expr.ToDebugString()}'.";
		}

		
	}
}
