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
			return new SqlParameterValue(parameter.Value, parameter.Value, parameter.Type);
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, EvaluationContext context, out object? result)
		{
			(result, var success) = expr.TryEvaluateExpression(context);
			return success;
		}

		public static bool IsMutable(this IQueryElement expr)
		{
			if (expr.CanBeEvaluated(false))
				return false;

			if (expr is ISqlExpression sqlExpression)
			{
				sqlExpression = UnwrapNullablity(sqlExpression);
				if (sqlExpression is SqlParameter)
					return true;
			}

			var isMutable = false;
			expr.VisitParentFirst(a =>
			{
				if (a is SqlBinaryExpression binary)
				{
					var expr1 = UnwrapNullablity(binary.Expr1);
					var expr2 = UnwrapNullablity(binary.Expr2);
					if ((expr1 is SqlParameter || expr1.CanBeEvaluated(false)) && (expr2 is SqlParameter || expr2.CanBeEvaluated(false)))
						isMutable = true;
				}

				return !isMutable;
			});

			return isMutable;
		}

		public static bool CanBeEvaluated(this IQueryElement expr, bool withParameters)
		{
			return expr.TryEvaluateExpression(new EvaluationContext(withParameters ? SqlParameterValues.Empty : null), out _);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, EvaluationContext context)
		{
			return expr.TryEvaluateExpression(context, out _);
		}

		static (object? value, bool success) TryEvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			if (!context.TryGetValue(expr, out var info))
			{
				if (TryEvaluateExpressionInternal(expr, context, out var result))
				{
					context.Register(expr, result);
					return (result, true);
				}
				else
				{
					context.RegisterError(expr);
					return (result, false);
				}
			}

			return info.Value;
		}

		static bool TryEvaluateExpressionInternal(this IQueryElement expr, EvaluationContext context, out object? result)
		{
			result = null;
			switch (expr.ElementType)
			{
				case QueryElementType.SqlValue           :
				{
					var sqlValue = (SqlValue)expr;
					result = sqlValue.Value;
					return true;
				}

				case QueryElementType.SqlParameter       :
				{
					var sqlParameter = (SqlParameter)expr;

					if (context.ParameterValues == null)
					{
						return false;
					}

					var parameterValue = sqlParameter.GetParameterValue(context.ParameterValues);

					if (parameterValue.ClientValue is null && parameterValue.ProviderValue is not null)
						return false;

					result = parameterValue.ClientValue;

					if (parameterValue.ProviderValue is null)
						result = null;
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
					/*
					var reduced = exprExpr.Reduce(context, TODO);
					if (!ReferenceEquals(reduced, expr))
						return TryEvaluateExpression(reduced, context, out result);
						*/

					if (!exprExpr.Expr1.TryEvaluateExpression(context, out var value1) ||
					    !exprExpr.Expr2.TryEvaluateExpression(context, out var value2))
						return false;

					if (value1 != null && value2 != null)
					{
						if (value1.GetType().IsEnum != value2.GetType().IsEnum)
						{
							return false;
						}
					}

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

							try
							{
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
							}
							catch 
							{
								return false;
							}
							break;
						}
					}

					return true;
				}

				case QueryElementType.NotPredicate:
				{
					var notPredicate = (SqlPredicate.Not)expr;
					if (notPredicate.Predicate.TryEvaluateExpression(context, out var value) && value is bool boolValue)
					{
						result = !boolValue;
						return true;
					}

					return false;
				}

				case QueryElementType.TruePredicate:
				{
					result = true;
					return true;
				}

				case QueryElementType.FalsePredicate:
				{
					result = false;
					return true;
				}

				case QueryElementType.IsTruePredicate:
				{
					var isTruePredicate = (SqlPredicate.IsTrue)expr;
					if (!isTruePredicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;

					if (value == null)
					{
						if (isTruePredicate.WithNull != null)
							result = isTruePredicate.WithNull;
						else
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

				case QueryElementType.SqlCast:
				{
					var cast = (SqlCastExpression)expr;
					if (!cast.Expression.TryEvaluateExpression(context, out var value))
						return false;

					result = value;

					if (result != null)
					{
						if (cast.SystemType == typeof(string))
						{
							if (result.GetType().IsNumeric())
							{
								if (result is int intValue)
									result = intValue.ToString(CultureInfo.InvariantCulture);
								else if (result is long longValue)
									result = longValue.ToString(CultureInfo.InvariantCulture);
								else if (result is short shortValue)
									result = shortValue.ToString(CultureInfo.InvariantCulture);
								else if (result is byte byteValue)
									result = byteValue.ToString(CultureInfo.InvariantCulture);
								else if (result is uint uintValue)
									result = uintValue.ToString(CultureInfo.InvariantCulture);
								else if (result is ulong ulongValue)
									result = ulongValue.ToString(CultureInfo.InvariantCulture);
								else if (result is ushort ushortValue)
									result = ushortValue.ToString(CultureInfo.InvariantCulture);
								else if (result is sbyte sbyteValue)
									result = sbyteValue.ToString(CultureInfo.InvariantCulture);
								else if (result is char charValue)
									result = charValue.ToString(CultureInfo.InvariantCulture);
								else if (result is decimal decimalValue)
									result = decimalValue.ToString(CultureInfo.InvariantCulture);
								else if (result is float floatValue)
									result = floatValue.ToString(CultureInfo.InvariantCulture);
								else if (result is double doubleValue)
									result = doubleValue.ToString(CultureInfo.InvariantCulture);
							}
						}
						else
						{
							if (result.GetType() != cast.SystemType)
							{
								try
								{
									result = Convert.ChangeType(result, cast.SystemType, CultureInfo.InvariantCulture);
								}
								catch (InvalidCastException)
								{
									return false;
								}
							}
						}
					}

					return true;
				}

				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)expr;
					if (!binary.Expr1.TryEvaluateExpression(context, out var leftEvaluated))
						return false;
					if (!binary.Expr2.TryEvaluateExpression(context, out var rightEvaluated))
						return false;
					dynamic? left  = leftEvaluated;
					dynamic? right = rightEvaluated;
					if (left == null || right == null)
						return false;

					try
					{
						switch (binary.Operation)
						{
							case "+":  result = left + right; break;
							case "-":  result = left - right; break;
							case "*":  result = left * right; break;
							case "/":  result = left / right; break;
							case "%":  result = left % right; break;
							case "^":  result = left ^ right; break;
							case "&":  result = left & right; break;
							case "<":  result = left < right; break;
							case ">":  result = left > right; break;
							case "<=": result = left <= right; break;
							case ">=": result = left >= right; break;
							default:
								return false;
						}
					}
					catch
					{
						return false;
					}

					return true;
				}

				case QueryElementType.SqlFunction        :
				{
					var function = (SqlFunction)expr;

					switch (function.Name)
					{
						case "Length":
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
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

						case PseudoFunctions.TO_LOWER:
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
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

						case PseudoFunctions.TO_UPPER:
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
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
							return false;
					}
				}

				case QueryElementType.SearchCondition    :
				{
					var cond = (SqlSearchCondition)expr;

					if (cond.Predicates.Count == 0)
					{
						result = true;
						return true;
					}

					for (var i = 0; i < cond.Predicates.Count; i++)
					{
						var predicate = cond.Predicates[i];
						if (predicate.TryEvaluateExpression(context, out var evaluated))
						{
							if (evaluated is bool boolValue)
							{
								if (boolValue)
								{
									if (cond.IsOr)
									{
										result = true;
										return true;
									}
								}
								else
								{
									if (!cond.IsOr)
									{
										result = false;
										return true;
									}
								}
							}
						}
					}

					return false;
				}

				case QueryElementType.SqlCase:
				{
					var caseExpr = (SqlCaseExpression)expr;

					foreach (var caseItem in caseExpr.Cases)
					{
						if (caseItem.Condition.TryEvaluateExpression(context, out var evaluatedCondition) && evaluatedCondition is bool boolValue)
						{
							if (boolValue)
							{
								if (caseItem.ResultExpression.TryEvaluateExpression(context, out var resultValue))
								{
									result = resultValue;
									return true;
								}
							}
						}
						else
						{
							return false;
						}
					}

					if (caseExpr.ElseExpression == null)
					{
						result = null;
						return true;
					}

					if (caseExpr.ElseExpression.TryEvaluateExpression(context, out var elseValue))
					{
						result = elseValue;
						return true;
					}

					return false;
				}

				case QueryElementType.ExprPredicate      :
				{
					var predicate = (SqlPredicate.Expr)expr;
					if (!predicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;

					result = value;
					return true;
				}

				case QueryElementType.SqlNullabilityExpression:
				{
					var nullability = (SqlNullabilityExpression)expr;
					if (nullability.SqlExpression.TryEvaluateExpression(context, out var evaluated))
					{
						result = evaluated;
						return true;
					}

					return false;
				}

				case QueryElementType.SqlExpression:
				{
					var sqlExpression = (SqlExpression)expr;

					if (sqlExpression is { Expr: "{0}", Parameters: [var p] })
					{
						if (p.TryEvaluateExpression(context, out var evaluated))
						{
							result = evaluated;
							return true;
						} 
					}

					return false;
				}

				case QueryElementType.CompareTo:
				{
					var compareTo = (SqlCompareToExpression)expr;
					if (!compareTo.Expression1.TryEvaluateExpression(context, out var value1) || !compareTo.Expression2.TryEvaluateExpression(context, out var value2))
						return false;

					if (value1 == null || value2 == null)
					{
						result = false;
						return true;
					}

					if (value1 is IComparable comp1 && value2 is IComparable comp2)
					{
						result = comp1.CompareTo(comp2);
						return true;
					}

					return false;
				}

				case QueryElementType.SqlCondition:
				{
					var compareTo = (SqlConditionExpression)expr;

					if (compareTo.Condition.TryEvaluateExpression(context, out var conditionValue) && conditionValue is bool boolCondition)
					{
						if (boolCondition)
						{
							if (compareTo.TrueValue.TryEvaluateExpression(context, out var trueValue))
							{
								result = trueValue;
								return true;
							}
						}
						else
						{
							if (compareTo.FalseValue.TryEvaluateExpression(context, out var falseValue))
							{
								result = falseValue;
								return true;
							}
						}
					}

					return false;
				}

				case QueryElementType.IsDistinctPredicate:
				{
					var isDistinct = (SqlPredicate.IsDistinct)expr;
					if (!isDistinct.Expr1.TryEvaluateExpression(context, out var value1) || !isDistinct.Expr2.TryEvaluateExpression(context, out var value2))
						return false;

					if (value1 == null)
					{
						result = value2 != null;
					}
					else if (value2 == null)
					{
						result = true;
					}
					else
					{
						result = !value1.Equals(value2);
					}

					if (isDistinct.IsNot)
						result = !(bool)result;

					return true;
				}

				default:
				{
					return false;
				}
			}
		}

		public static object? EvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			var (value, success) = expr.TryEvaluateExpression(context);
			if (!success)
				throw new LinqToDBException($"Cannot evaluate expression: {expr}");

			return value;
		}

		public static bool? EvaluateBoolExpression(this IQueryElement expr, EvaluationContext context, bool? defaultValue = null)
		{
			var evaluated = expr.EvaluateExpression(context);

			if (evaluated is bool boolValue)
				return boolValue;

			return defaultValue;
		}

		public static void ExtractPredicate(ISqlPredicate predicate, out ISqlPredicate underlying, out bool isNot)
		{
			underlying = predicate;
			isNot      = false;

			if (predicate is SqlPredicate.Not notPredicate)
			{
				underlying = notPredicate.Predicate;
				isNot      = true;
			}
		}
	}
}
