using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.SqlProvider
{
	using Common.Internal;
	using Extensions;
	using SqlQuery;
	using SqlQuery.Visitors;

	public class SqlExpressionOptimizerVisitor : SqlQueryVisitor
	{
		EvaluationContext  _evaluationContext  = default!;
		NullabilityContext _nullabilityContext = default!;
		DataOptions        _dataOptions        = default!;
		SqlProviderFlags?  _sqlProviderFlags;

		public SqlExpressionOptimizerVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform)
		{
		}

		public virtual IQueryElement Optimize(EvaluationContext evaluationContext, NullabilityContext nullabilityContext, SqlProviderFlags? sqlProviderFlags, DataOptions dataOptions, IQueryElement element)
		{
			Cleanup();
			_evaluationContext  = evaluationContext;
			_nullabilityContext = nullabilityContext;
			_sqlProviderFlags   = sqlProviderFlags;
			_dataOptions        = dataOptions;
			IsModified          = false;

			return ProcessElement(element);
		}

		public bool IsModified { get; private set; }

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return element;

			var newElement = base.Visit(element);

			if (!ReferenceEquals(newElement, element))
				MarkModified();

			return newElement;
		}

		protected void MarkModified()
		{
			IsModified = true;
		}

		protected override IQueryElement VisitIsTruePredicate(SqlPredicate.IsTrue predicate)
		{
			var newElement = base.VisitIsTruePredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			//TODO: refactor CASE optimization

			if ((predicate.WithNull == null || predicate.WithNull == false) && predicate.Expr1 is SqlFunction func && func.Name == "CASE")
			{
				if (func.Parameters.Length == 3)
				{
					// It handles one specific case for OData
					if (func.Parameters[0] is SqlSearchCondition                                 &&
					    func.Parameters[2] is SqlSearchCondition sc                              &&
					    func.Parameters[1].TryEvaluateExpression(_evaluationContext, out var v1) && v1 is null)
					{
						if (predicate.IsNot)
							return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);
						return sc;
					}
				}
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			var newElement = base.VisitSqlSearchCondition(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			element = OptimizationHelper.OptimizeSearchCondition(element, _evaluationContext);

			return element;
		}

		protected override IQueryElement VisitNotExprPredicate(SqlPredicate.NotExpr predicate)
		{
			var newElement = base.VisitNotExprPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.IsNot)
			{
				if (predicate.Expr1 is SqlSearchCondition sc)
				{
					if (sc.Conditions.Count == 1)
					{
						var cond = sc.Conditions[0];

						if (cond.IsNot)
							return cond.Predicate;

						if (cond.Predicate is SqlPredicate.ExprExpr ee)
						{
							if (ee.Operator == SqlPredicate.Operator.Equal)
								return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.NotEqual, ee.Expr2,
									_dataOptions.LinqOptions.CompareNullsAsValues ? true : null);

							if (ee.Operator == SqlPredicate.Operator.NotEqual)
								return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.Equal, ee.Expr2,
									_dataOptions.LinqOptions.CompareNullsAsValues ? true : null);
						}
					}
				}
				else if (predicate.Expr1 is IInvertibleElement invertible && invertible.CanInvert())
				{
					return invertible.Invert();
				}
			}

			return predicate;
		}

		protected override IQueryElement VisitIsDistinctPredicate(SqlPredicate.IsDistinct predicate)
		{
			var newElement = base.VisitIsDistinctPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (_nullabilityContext.IsEmpty)
				return predicate;

			// Here, several optimisations would already have occured:
			// - If both expressions could be evaluated, Sql.IsDistinct would have been evaluated client-side.
			// - If both expressions could not be null, an Equals expression would have been used instead.

			// The only remaining case that we'd like to simplify is when one expression is the constant null.
			if (predicate.Expr1.TryEvaluateExpression(_evaluationContext, out var value1) && value1 == null)
			{
				return predicate.Expr2.CanBeNullable(_nullabilityContext)
					? new SqlPredicate.IsNull(predicate.Expr2, !predicate.IsNot)
					: new SqlPredicate.Expr(new SqlValue(!predicate.IsNot));
			}

			if (predicate.Expr2.TryEvaluateExpression(_evaluationContext, out var value2) && value2 == null)
			{
				return predicate.Expr1.CanBeNullable(_nullabilityContext)
					? new SqlPredicate.IsNull(predicate.Expr1, !predicate.IsNot)
					: new SqlPredicate.Expr(new SqlValue(!predicate.IsNot));
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlCondition(SqlCondition element)
		{
			var newElement = base.VisitSqlCondition(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			var current = element;
			do
			{
				var optimizedCondition = OptimizationHelper.OptimizeCondition(current);

				if (optimizedCondition.Predicate.TryEvaluateExpression(_evaluationContext, out var value) && value != null)
				{
					return new SqlCondition(optimizedCondition.IsNot, new SqlPredicate.Expr(new SqlValue(value)), optimizedCondition.IsOr);
				}

				if (ReferenceEquals(optimizedCondition, current))
				{
					break;
				}

				current = optimizedCondition;
			} while (true);

			if (!ReferenceEquals(current, element))
				return Visit(current);

			return element;
		}

		protected override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			var newElement = base.VisitSqlBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			switch (element.Operation)
			{
				case "+":
				{
					var v1 = element.Expr1.TryEvaluateExpression(_evaluationContext, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case short   h when h == 0  :
							case int     i when i == 0  :
							case long    l when l == 0  :
							case decimal d when d == 0  :
							case string  s when s.Length == 0: return element.Expr2;
						}
					}

					var v2 = element.Expr2.TryEvaluateExpression(_evaluationContext, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return element.Expr1;
							case int vi when
								element.Expr1    is SqlBinaryExpression be1                             &&
								be1.Expr2.TryEvaluateExpression(_evaluationContext, out var be1v2) &&
								be1v2 is int be1v2i :
							{
								switch (be1.Operation)
								{
									case "+":
									{
										var value = be1v2i + vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "-";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element), element.Precedence);
									}

									case "-":
									{
										var value = be1v2i - vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "+";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element), element.Precedence);
									}
								}

								break;
							}

							case string vs when vs.Length == 0 : return element.Expr1;
							case string vs when
								element.Expr1    is SqlBinaryExpression be1 &&
								//be1.Operation == "+"                   &&
								be1.Expr2.TryEvaluateExpression(_evaluationContext, out var be1v2) &&
								be1v2 is string be1v2s :
							{
								return new SqlBinaryExpression(
									be1.SystemType,
									be1.Expr1,
									be1.Operation,
									new SqlValue(string.Concat(be1v2s, vs)));
							}
						}
					}

					if (v1 && v2)
					{
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 + i2, element);
						if (value1 is string || value2 is string) return QueryHelper.CreateSqlValue(value1?.ToString() + value2, element);
					}

					break;
				}

				case "-":
				{
					var v2 = element.Expr2.TryEvaluateExpression(_evaluationContext, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return element.Expr1;
							case int vi when
								element.Expr1 is SqlBinaryExpression be1 &&
								be1.Expr2.TryEvaluateExpression(_evaluationContext, out var be1v2) &&
								be1v2 is int be1v2i :
							{
								switch (be1.Operation)
								{
									case "+":
									{
										var value = be1v2i - vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "-";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element), element.Precedence);
									}

									case "-":
									{
										var value = be1v2i + vi;
										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "+";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element), element.Precedence);
									}
								}

								break;
							}
						}
					}

					if (v2 && element.Expr1.TryEvaluateExpression(_evaluationContext, out var value1))
					{
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 - i2, element);
					}

					break;
				}

				case "*":
				{
					var v1 = element.Expr1.TryEvaluateExpression(_evaluationContext, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element);
							case int i when i == 1 : return element.Expr2;
							case int i when
								element.Expr2    is SqlBinaryExpression be2 &&
								be2.Operation == "*"                   &&
								be2.Expr1.TryEvaluateExpression(_evaluationContext, out var be2v1)  &&
								be2v1 is int bi :
							{
								return new SqlBinaryExpression(be2.SystemType, QueryHelper.CreateSqlValue(i * bi, element), "*", be2.Expr2);
							}
						}
					}

					var v2 = element.Expr2.TryEvaluateExpression(_evaluationContext, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element);
							case int i when i == 1 : return element.Expr1;
						}
					}

					if (v1 && v2)
					{
						switch (value1)
						{
							case int    i1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(i1 * i2, element);
							case int    i1 when value2 is double d2 : return QueryHelper.CreateSqlValue(i1 * d2, element);
							case double d1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(d1 * i2, element);
							case double d1 when value2 is double d2 : return QueryHelper.CreateSqlValue(d1 * d2, element);
						}
					}

					break;
				}
			}

			return element;
		}

		protected override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var newElement = base.VisitSqlFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.DoNotOptimize)
				return element;

			if (element.TryEvaluateExpression(_evaluationContext, out var value))
			{
				return QueryHelper.CreateSqlValue(value, element.GetExpressionType(), element.Parameters);
			}

			switch (element.Name)
			{
				case PseudoFunctions.COALESCE:
				{
					var parms = element.Parameters;
					if (parms.Length == 2)
					{
						if (parms[0] is SqlValue val1 && parms[1] is not SqlValue)
							return new SqlFunction(element.SystemType, element.Name, element.IsAggregate, element.Precedence, QueryHelper.CreateSqlValue(val1.Value, parms[1].GetExpressionType(), parms[0]), parms[1])
							{
								DoNotOptimize = true,
								CanBeNull     = element.CanBeNull
							};
						else if (parms[1] is SqlValue val2 && parms[0] is not SqlValue)
							return new SqlFunction(element.SystemType, element.Name, element.IsAggregate, element.Precedence, parms[0], QueryHelper.CreateSqlValue(val2.Value, parms[0].GetExpressionType(), parms[1]))
							{
								DoNotOptimize = true,
								CanBeNull     = element.CanBeNull
							};
					}

					break;
				}
				case "CASE":
				{
					var parms = element.Parameters;
					var len   = parms.Length;

					for (var i = 0; i < parms.Length - 1; i += 2)
					{
						var boolValue = QueryHelper.GetBoolValue(parms[i], _evaluationContext);
						if (boolValue != null)
						{
							if (boolValue == false)
							{
								var newParms = new ISqlExpression[parms.Length - 2];

								if (i != 0)
									Array.Copy(parms, 0, newParms, 0, i);

								Array.Copy(parms, i + 2, newParms, i, parms.Length - i - 2);

								parms = newParms;
								i -= 2;
							}
							else
							{
								var newParms = new ISqlExpression[i + 1];

								if (i != 0)
									Array.Copy(parms, 0, newParms, 0, i);

								newParms[i] = parms[i + 1];

								parms = newParms;
								break;
							}
						}
					}

					if (parms.Length == 1)
						return parms[0];

					if (parms.Length != len)
						return new SqlFunction(element.SystemType, element.Name, element.IsAggregate, element.Precedence, parms);

					if (!element.DoNotOptimize && parms.Length == 3
						&& !parms[0].ShouldCheckForNull(_nullabilityContext)
						&& (parms[0].ElementType == QueryElementType.SqlFunction || parms[0].ElementType == QueryElementType.SearchCondition))
					{
						var boolValue1 = QueryHelper.GetBoolValue(parms[1], _evaluationContext);
						var boolValue2 = QueryHelper.GetBoolValue(parms[2], _evaluationContext);

						if (boolValue1 != null && boolValue2 != null)
						{
							if (boolValue1 == boolValue2)
								return new SqlValue(true);

							if (!boolValue1.Value)
								return new SqlSearchCondition(new SqlCondition(true, new SqlPredicate.Expr(parms[0], parms[0].Precedence)));

							return parms[0];
						}

						// TODO: is it correct?
						// type constant by value from other branch
						if (parms[1] is SqlValue val1 && parms[2] is not SqlValue)
							return new SqlFunction(element.SystemType, element.Name, element.IsAggregate, element.Precedence, parms[0], QueryHelper.CreateSqlValue(val1.Value, parms[2].GetExpressionType(), parms[1]), parms[2])
							{
								DoNotOptimize = true
							};
						else if (parms[2] is SqlValue val2 && parms[1] is not SqlValue)
							return new SqlFunction(element.SystemType, element.Name, element.IsAggregate, element.Precedence, parms[0], parms[1], QueryHelper.CreateSqlValue(val2.Value, parms[1].GetExpressionType(), parms[2]))
							{
								DoNotOptimize = true
							};
					}
				}

				break;

				case "EXISTS":
				{
					if (element.Parameters.Length == 1 && element.Parameters[0] is SelectQuery query && query.Select.Columns.Count > 0)
					{
						var isAggregateQuery =
									query.Select.Columns.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));

						if (isAggregateQuery)
							return new SqlValue(true);
					}

					break;
				}

				case PseudoFunctions.CONVERT:
				{
					var typef = element.SystemType.ToUnderlying();

					if (!element.DoNotOptimize)
					{
						if (element.Parameters[1] is SqlDataType from && element.Parameters[0] is SqlDataType to)
						{
							if (to.Type.SystemType == typeof(object) || from.Type.EqualsDbOnly(to.Type))
								return element.Parameters[2];
						}

						if (element.Parameters[2] is SqlFunction paramFunc && paramFunc.Name == PseudoFunctions.CONVERT && paramFunc.Parameters[1].SystemType!.ToUnderlying() == typef)
							return paramFunc.Parameters[2];
					}

					break;
				}
			}

			return element;
		}

		protected override IQueryElement VisitSqlExpression(SqlExpression element)
		{
			var newElement = base.VisitSqlExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Expr      == "{0}" && element.Parameters.Length == 1 &&
			    element.CanBeNull == element.Parameters[0].CanBeNullable(_nullabilityContext))
			{
				return SqlNullabilityExpression.ApplyNullability(element.Parameters[0], element.CanBeNull);
			}

			return element;
		}

		protected override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
		{
			var newPredicate = base.VisitIsNullPredicate(predicate);
			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			if (!predicate.Expr1.CanBeNullable(_nullabilityContext))
			{
				return new SqlPredicate.Expr(new SqlValue(predicate.IsNot));
			}

			if (predicate.Expr1.TryEvaluateExpression(_evaluationContext, out var value))
			{
				return new SqlPredicate.Expr(new SqlValue(typeof(bool), (value == null) != predicate.IsNot));
			}

			newPredicate = OptimizeCase(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			return predicate;
		}

		protected override IQueryElement VisitSqlNullabilityExpression(SqlNullabilityExpression element)
		{
			var newNode = base.VisitSqlNullabilityExpression(element);

			if (!ReferenceEquals(newNode, element))
				return Visit(newNode);

			if (element.SqlExpression is SqlNullabilityExpression nullabilityExpression)
			{
				return SqlNullabilityExpression.ApplyNullability(nullabilityExpression.SqlExpression,
					element.CanBeNullable(_nullabilityContext));
			}

			if (element.SqlExpression is SqlSearchCondition)
			{
				return element.SqlExpression;
			}

			return element;
		}

		protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var newElement = base.VisitExprExprPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (predicate.TryEvaluateExpression(_evaluationContext, out var value) && value is bool)
			{
				return new SqlPredicate.Expr(new SqlValue(value));
			}

			/*// Avoiding infinite recursion
			//
			if (predicate.Expr1.ElementType == QueryElementType.SqlValue)
				return predicate;*/

			var expr = predicate;

			if (expr.WithNull == null && (expr.Operator == SqlPredicate.Operator.Equal || expr.Operator == SqlPredicate.Operator.NotEqual))
			{
				if (expr.Expr2 is ISqlPredicate)
				{
					var boolValue1 = QueryHelper.GetBoolValue(expr.Expr1, _evaluationContext);
					if (boolValue1 != null)
					{
						ISqlPredicate transformed = new SqlPredicate.Expr(expr.Expr2);
						var           isNot       = boolValue1.Value != (expr.Operator == SqlPredicate.Operator.Equal);
						if (isNot)
						{
							transformed =
								new SqlPredicate.NotExpr(expr.Expr2, true, Precedence.LogicalNegation);
						}

						return transformed;
					}
				}

				if (expr.Expr1 is ISqlPredicate)
				{
					var boolValue2 = QueryHelper.GetBoolValue(expr.Expr2, _evaluationContext);
					if (boolValue2 != null)
					{
						ISqlPredicate transformed = new SqlPredicate.Expr(expr.Expr1);
						var           isNot       = boolValue2.Value != (expr.Operator == SqlPredicate.Operator.Equal);
						if (isNot)
						{
							transformed =
								new SqlPredicate.NotExpr(expr.Expr1, true, Precedence.LogicalNegation);
						}

						return transformed;
					}
				}
			}

			switch (expr.Operator)
			{
				case SqlPredicate.Operator.Equal          :
				case SqlPredicate.Operator.NotEqual       :
				case SqlPredicate.Operator.Greater        :
				case SqlPredicate.Operator.GreaterOrEqual :
				case SqlPredicate.Operator.Less           :
				case SqlPredicate.Operator.LessOrEqual    :
					return OptimizeCase(expr);
			}

			return predicate;
		}

		#region OptimizeCase

		static SqlPredicate.Operator InvertOperator(SqlPredicate.Operator op, bool preserveEqual)
		{
			switch (op)
			{
				case SqlPredicate.Operator.Equal          : return preserveEqual ? op : SqlPredicate.Operator.NotEqual;
				case SqlPredicate.Operator.NotEqual       : return preserveEqual ? op : SqlPredicate.Operator.Equal;
				case SqlPredicate.Operator.Greater        : return SqlPredicate.Operator.LessOrEqual;
				case SqlPredicate.Operator.NotLess        :
				case SqlPredicate.Operator.GreaterOrEqual : return preserveEqual ? SqlPredicate.Operator.LessOrEqual : SqlPredicate.Operator.Less;
				case SqlPredicate.Operator.Less           : return SqlPredicate.Operator.GreaterOrEqual;
				case SqlPredicate.Operator.NotGreater     :
				case SqlPredicate.Operator.LessOrEqual    : return preserveEqual ? SqlPredicate.Operator.GreaterOrEqual : SqlPredicate.Operator.Greater;
				default: throw new InvalidOperationException();
			}
		}

		static bool Compare(int v1, int v2, SqlPredicate.Operator op)
		{
			switch (op)
			{
				case SqlPredicate.Operator.Equal:          return v1 == v2;
				case SqlPredicate.Operator.NotEqual:       return v1 != v2;
				case SqlPredicate.Operator.Greater:        return v1 >  v2;
				case SqlPredicate.Operator.NotLess:
				case SqlPredicate.Operator.GreaterOrEqual: return v1 >= v2;
				case SqlPredicate.Operator.Less:           return v1 <  v2;
				case SqlPredicate.Operator.NotGreater:
				case SqlPredicate.Operator.LessOrEqual:    return v1 <= v2;
			}

			throw new InvalidOperationException();
		}

		ISqlPredicate OptimizeCase(SqlPredicate.IsNull isNull)
		{
			if (QueryHelper.UnwrapNullablity(isNull.Expr1) is SqlFunction func && func.Name == "CASE")
			{
				var sc = new SqlSearchCondition();

				//TODO: I still do not understand why we do not created QueryElement for CASE function
				if (func.Parameters.Length == 3)
				{
					var trueParam  = func.Parameters[1];
					var falseParam = func.Parameters[2];

					sc.Add(new SqlCondition(false, new SqlPredicate.IsNull(trueParam, isNull.IsNot), true));
					sc.Add(new SqlCondition(false, new SqlPredicate.IsNull(falseParam, isNull.IsNot), true));
				}
				else if (func.Parameters.Length == 5)
				{
					var trueParam    = func.Parameters[1];
					var falseParam   = func.Parameters[3];
					var defaultParam = func.Parameters[4];

					sc.Add(new SqlCondition(false, new SqlPredicate.IsNull(trueParam, isNull.IsNot), true));
					sc.Add(new SqlCondition(false, new SqlPredicate.IsNull(falseParam, isNull.IsNot), true));
					sc.Add(new SqlCondition(false, new SqlPredicate.IsNull(defaultParam, isNull.IsNot), true));
				}
				else
					return isNull;

				return sc;
			}

			return isNull;
		}

		ISqlPredicate OptimizeCase(SqlPredicate.ExprExpr expr)
		{
			SqlFunction? func;

			var valueFirst = expr.Expr1.TryEvaluateExpression(_evaluationContext, out var value);
			var isValue    = valueFirst;

			if (valueFirst)
				func = QueryHelper.UnwrapNullablity(expr.Expr2) as SqlFunction;
			else
			{
				func    = QueryHelper.UnwrapNullablity(expr.Expr1) as SqlFunction;
				isValue = expr.Expr2.TryEvaluateExpression(_evaluationContext, out value);
			}

			if (isValue && func != null && func.Name == "CASE")
			{
				if (value is int n && func.Parameters.Length == 5)
				{
					if (func.Parameters[0] is SqlSearchCondition c1 && c1.Conditions.Count == 1 &&
					    func.Parameters[1].TryEvaluateExpression(_evaluationContext, out var value1) && value1 is int i1 &&
					    func.Parameters[2] is SqlSearchCondition c2 && c2.Conditions.Count == 1 &&
					    func.Parameters[3].TryEvaluateExpression(_evaluationContext, out var value2) && value2 is int i2 &&
					    func.Parameters[4].TryEvaluateExpression(_evaluationContext, out var value3) && value3 is int i3)
					{
						if (c1.Conditions[0].Predicate is SqlPredicate.ExprExpr ee1 &&
						    c2.Conditions[0].Predicate is SqlPredicate.ExprExpr ee2 &&
						    ee1.Expr1.Equals(ee2.Expr1) && ee1.Expr2.Equals(ee2.Expr2))
						{
							int e = 0, g = 0, l = 0;

							if (ee1.Operator == SqlPredicate.Operator.Equal   || ee2.Operator == SqlPredicate.Operator.Equal)   e = 1;
							if (ee1.Operator == SqlPredicate.Operator.Greater || ee2.Operator == SqlPredicate.Operator.Greater) g = 1;
							if (ee1.Operator == SqlPredicate.Operator.Less    || ee2.Operator == SqlPredicate.Operator.Less)    l = 1;

							if (e + g + l == 2)
							{
								var n1 = Compare(valueFirst ? n : i1, valueFirst ? i1 : n, expr.Operator) ? 1 : 0;
								var n2 = Compare(valueFirst ? n : i2, valueFirst ? i2 : n, expr.Operator) ? 1 : 0;
								var n3 = Compare(valueFirst ? n : i3, valueFirst ? i3 : n, expr.Operator) ? 1 : 0;

								if (n1 + n2 + n3 == 1)
								{
									if (n1 == 1) return ee1;
									if (n2 == 1) return ee2;

									return
										new SqlPredicate.ExprExpr(
											ee1.Expr1,
											e == 0 ? SqlPredicate.Operator.Equal :
											g == 0 ? SqlPredicate.Operator.Greater :
													 SqlPredicate.Operator.Less,
											ee1.Expr2, null);
								}

								//	CASE
								//		WHEN [p].[FirstName] > 'John'
								//			THEN 1
								//		WHEN [p].[FirstName] = 'John'
								//			THEN 0
								//		ELSE -1
								//	END <= 0
								if (ee1.Operator == SqlPredicate.Operator.Greater && i1 == 1 &&
									ee2.Operator == SqlPredicate.Operator.Equal   && i2 == 0 &&
									i3 == -1 && n == 0)
								{
									return new SqlPredicate.ExprExpr(
											ee1.Expr1,
											valueFirst ? InvertOperator(expr.Operator, true) : expr.Operator,
											ee1.Expr2, null);
								}
							}
						}
					}
				}
				else if (value is bool bv && func.Parameters.Length == 3)
				{
					if (func.Parameters[0] is SqlSearchCondition c1 && c1.Conditions.Count == 1 &&
					    func.Parameters[1].TryEvaluateExpression(_evaluationContext, out var v1) && v1 is bool bv1  &&
					    func.Parameters[2].TryEvaluateExpression(_evaluationContext, out var v2) && v2 is bool bv2)
					{
						if (bv == bv1 && expr.Operator == SqlPredicate.Operator.Equal ||
							bv != bv1 && expr.Operator == SqlPredicate.Operator.NotEqual)
						{
							return c1;
						}

						if (bv == bv2 && expr.Operator == SqlPredicate.Operator.NotEqual ||
							bv != bv1 && expr.Operator == SqlPredicate.Operator.Equal)
						{
							if (c1.Conditions[0].Predicate is SqlPredicate.ExprExpr ee)
							{
								return (ISqlPredicate)ee.Invert();
							}

							var sc = new SqlSearchCondition();

							sc.Conditions.Add(new SqlCondition(true, c1));

							return sc;
						}
					}
				}
				else if (expr.Operator == SqlPredicate.Operator.Equal && func.Parameters.Length == 3)
				{
					if (func.Parameters[0] is SqlSearchCondition sc &&
					    func.Parameters[1].TryEvaluateExpression(_evaluationContext, out var v1) &&
					    func.Parameters[2].TryEvaluateExpression(_evaluationContext, out var v2))
					{
						if (Equals(value, v1))
							return sc;

						if (Equals(value, v2) && !sc.CanBeNull)
							return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);
					}
				}
			}

			if (!_nullabilityContext.IsEmpty                   &&
			    !expr.Expr1.CanBeNullable(_nullabilityContext) &&
			    !expr.Expr2.CanBeNullable(_nullabilityContext) &&
			    expr.Expr1.SystemType.IsSignedType()           &&
			    expr.Expr2.SystemType.IsSignedType())
			{
				var newExpr = expr switch
				{
					(SqlBinaryExpression binary, var op, var v, _) when v.CanBeEvaluated(_evaluationContext) =>

						// binary < v
						binary switch
						{
							// e + some < v ===> some < v - e
							(var e, "+", var some) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, v, "-", e), null),
							// e - some < v ===>  e - v < some
							(var e, "-", var some) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, e, "-", v), op, some, null),

							// some + e < v ===> some < v - e
							(var some, "+", var e) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, v, "-", e), null),
							// some - e < v ===> some < v + e
							(var some, "-", var e) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, v, "+", e), null),

							_ => null
						},

					(var v, var op, SqlBinaryExpression binary, _) when v.CanBeEvaluated(_evaluationContext) =>

						// v < binary
						binary switch
						{
							// v < e + some ===> v - e < some
							(var e, "+", var some) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, v, "-", e), op, some, null),
							// v < e - some ===> some < e - v
							(var e, "-", var some) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, e, "-", v), null),

							// v < some + e ===> v - e < some
							(var some, "+", var e) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, v, "-", e), op, some, null),
							// v < some - e ===> v + e < some
							(var e, "-", var some) when e.CanBeEvaluated(_evaluationContext) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, v, "+", e), op, some, null),

							_ => null
						},

					_ => null
				};

				expr = newExpr ?? expr;
			}

			return expr;
		}

		#endregion

	}
}
