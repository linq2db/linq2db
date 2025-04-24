using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlProvider
{
	public class SqlExpressionOptimizerVisitor : SqlQueryVisitor
	{
		EvaluationContext           _evaluationContext  = default!;
		NullabilityContext          _nullabilityContext = default!;
		DataOptions                 _dataOptions        = default!;
		MappingSchema               _mappingSchema      = default!;
		ICollection<ISqlPredicate>? _allowOptimizeList;
		ISqlPredicate?              _allowOptimize;
		bool                        _visitQueries;
		bool                        _isInsideNot;
		bool                        _reduceBinary;

		public SqlExpressionOptimizerVisitor(bool allowModify) : base(allowModify ? VisitMode.Modify : VisitMode.Transform, null)
		{
		}

		public virtual IQueryElement Optimize(
			EvaluationContext           evaluationContext,
			NullabilityContext          nullabilityContext,
			IVisitorTransformationInfo? transformationInfo,
			DataOptions                 dataOptions,
			MappingSchema               mappingSchema,
			IQueryElement               element,
			bool                        visitQueries,
			bool                        isInsideNot,
			bool                        reduceBinary)
		{
			Cleanup();
			_evaluationContext = evaluationContext;
			_dataOptions       = dataOptions;
			_mappingSchema     = mappingSchema;
			_allowOptimize     = default;
			_allowOptimizeList = default;
			_visitQueries      = visitQueries;
			_isInsideNot       = isInsideNot;
			_reduceBinary      = reduceBinary;
			SetTransformationInfo(transformationInfo);

			_nullabilityContext = nullabilityContext.WithTransformationInfo(GetTransformationInfo());

			return ProcessElement(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_visitQueries       = default;
			_isInsideNot        = default;
			_evaluationContext  = default!;
			_nullabilityContext = default!;
			_dataOptions        = default!;
			_mappingSchema      = default!;
			_allowOptimize      = default;
			_allowOptimizeList  = default;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return element;

			var newElement = base.Visit(element);

			return newElement;
		}

		#region Helper functions

		protected bool CanBeEvaluateNoParameters(IQueryElement expr)
		{
			if (expr.HasQueryParameter())
			{
				return false;
			}

			return expr.CanBeEvaluated(_evaluationContext);
		}

		protected bool TryEvaluateNoParameters(IQueryElement expr, out object? result)
		{
			if (expr.HasQueryParameter())
			{
				result = null;
				return false;
			}

			return TryEvaluate(expr, out result);
		}

		protected bool TryEvaluate(IQueryElement expr, out object? result)
		{
			if (expr.TryEvaluateExpression(_evaluationContext, out result))
				return true;

			return false;
		}

		#endregion

		protected override IQueryElement VisitIsTruePredicate(SqlPredicate.IsTrue predicate)
		{
			var newElement = base.VisitIsTruePredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			var optimized = OptimizeIsTruePredicate(predicate);
			if (!ReferenceEquals(optimized, predicate))
				return Visit(optimized);

			return predicate;
		}

		protected override IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			var saveAllowOptimize = _allowOptimize;
			_allowOptimize = element.Condition;

			var newExpr = base.VisitSqlConditionExpression(element);

			_allowOptimize = saveAllowOptimize;

			if (!ReferenceEquals(newExpr, element))
				return Visit(newExpr);

			if (TryEvaluate(element.Condition, out var value) && value is bool boolValue)
			{
				return boolValue ? element.TrueValue : element.FalseValue;
			}

			if (element.TrueValue is SqlConditionExpression trueConditional)
			{
				if (trueConditional.Condition.Equals(element.Condition, SqlExpression.DefaultComparer))
				{
					var newConditionExpression = new SqlConditionExpression(element.Condition, trueConditional.TrueValue, element.FalseValue);
					return Visit(newConditionExpression);
				}
			}

			if (element.FalseValue is SqlConditionExpression falseConditional)
			{
				var newCaseExpression = new SqlCaseExpression(QueryHelper.GetDbDataType(element.TrueValue, _mappingSchema),
					new SqlCaseExpression.CaseItem[]
					{
						new(element.Condition, element.TrueValue),
						new(falseConditional.Condition, falseConditional.TrueValue),
					}, falseConditional.FalseValue);

				return Visit(newCaseExpression);
			}

			if (element.FalseValue is SqlCaseExpression falseCase)
			{
				var caseItems = new List<SqlCaseExpression.CaseItem>(falseCase.Cases.Count + 1)
				{
					new(element.Condition, element.TrueValue),
				};

				caseItems.AddRange(falseCase.Cases);

				var newCaseExpression = new SqlCaseExpression(falseCase.Type, caseItems, falseCase.ElseExpression);

				return Visit(newCaseExpression);
			}

			if (element.Condition is SqlPredicate.IsNull isNullPredicate)
			{
				var unwrapped = QueryHelper.UnwrapNullablity(isNullPredicate.Expr1);

				if (isNullPredicate.IsNot)
				{
					if (unwrapped.Equals(element.TrueValue, SqlExpression.DefaultComparer) && element.FalseValue is SqlValue { Value: null })
					{
						return isNullPredicate.Expr1;
					}
				}
				else if (unwrapped.Equals(element.FalseValue, SqlExpression.DefaultComparer) && element.TrueValue is SqlValue { Value: null })
				{
					return isNullPredicate.Expr1;
				}

			}

			return element;
		}

		protected override SqlCaseExpression.CaseItem VisitCaseItem(SqlCaseExpression.CaseItem element)
		{
			var newElement =  base.VisitCaseItem(element);

			if (TryEvaluate(newElement.Condition, out var result) && result is bool boolValue)
			{
				return new SqlCaseExpression.CaseItem(SqlPredicate.MakeBool(boolValue), newElement.ResultExpression);
			}

			return newElement;
		}

		protected override IQueryElement VisitSqlCaseExpression(SqlCaseExpression element)
		{
			var newExpr = base.VisitSqlCaseExpression(element);

			if (!ReferenceEquals(newExpr, element))
				return Visit(newExpr);

			if (GetVisitMode(element) == VisitMode.Modify)
			{
				for (int i = 0; i < element._cases.Count; i++)
				{
					var caseItem = element._cases[i];
					if (caseItem.Condition == SqlPredicate.True)
					{
						element._cases.RemoveRange(i, element._cases.Count - i);
						element.Modify(caseItem.ResultExpression);
						break;
					}

					if (caseItem.Condition == SqlPredicate.False)
					{
						element._cases.RemoveAt(i);
						--i;
					}
				}
			}
			else
			{
				for (int i = 0; i < element._cases.Count; i++)
				{
					var caseItem = element._cases[i];
					if (caseItem.Condition == SqlPredicate.True)
					{
						var newCases = new List<SqlCaseExpression.CaseItem>(element._cases.Count - i);
						newCases.AddRange(element._cases.Take(i));

						var newCaseExpression = new SqlCaseExpression(element.Type, newCases, caseItem.ResultExpression);
						NotifyReplaced(newCaseExpression, element);

						return Visit(newCaseExpression);
					}

					if (caseItem.Condition == SqlPredicate.False)
					{
						var newCases = new List<SqlCaseExpression.CaseItem>(element._cases.Count);
						newCases.AddRange(element._cases);

						newCases.RemoveAt(i);

						var newCaseExpression = new SqlCaseExpression(element.Type, newCases, element.ElseExpression);
						NotifyReplaced(newCaseExpression, element);

						return Visit(newCaseExpression);
					}
				}
			}

			if (element.Cases.Count == 1)
			{
				var conditionExpression = new SqlConditionExpression(element.Cases[0].Condition, element.Cases[0].ResultExpression, element.ElseExpression ?? new SqlValue(element.Type, null));
				return Visit(conditionExpression);
			}

			if (element.Cases.Count == 0)
			{
				return element.ElseExpression ?? new SqlValue(element.Type, null);
			}

			return element;
		}

		bool IsAllowedToOptimizePredicate(ISqlPredicate testPredicate, ISqlPredicate replacement)
		{
			if (replacement is SqlSearchCondition)
				return true;

			if (_allowOptimize == testPredicate)
				return true;
			if (_allowOptimizeList?.Contains(testPredicate) == true)
				return true;

			return false;
		}

		protected override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			var saveAllowToOptimize = _allowOptimizeList;
			_allowOptimizeList = element.Predicates;

			var newElement = base.VisitSqlSearchCondition(element);

			_allowOptimizeList = saveAllowToOptimize;

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.Predicates.Count == 1)
			{
				if (IsAllowedToOptimizePredicate(element, element.Predicates[0]))
				{
					return element.Predicates[0];
				}
			}

			if (GetVisitMode(element) == VisitMode.Modify)
			{
				for (var i = 0; i < element.Predicates.Count; i++)
				{
					var predicate = element.Predicates[i];
					// unnesting search conditions
					//
					if (predicate is SqlSearchCondition sc && (sc.IsOr == element.IsOr || sc.Predicates.Count <= 1))
					{
						element.Predicates.RemoveAt(i);
						element.Predicates.InsertRange(i, sc.Predicates);
						--i;
						continue;
					}

					if (TryEvaluate(predicate, out var value))
					{
						if (value is true)
						{
							if (element.IsAnd)
							{
								// ignore
								if (element.Predicates.Count == 1 && predicate is SqlPredicate.TruePredicate)
									break;

								element.Predicates.RemoveAt(i);

								if (element.Predicates.Count == 0)
									element.Predicates.Add(SqlPredicate.True);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not SqlPredicate.TruePredicate)
							{
								element.Predicates.Clear();
								element.Predicates.Add(SqlPredicate.True);
								break;
							}
						}
						else if (value is false)
						{
							if (element.IsOr)
							{
								// ignore
								if (element.Predicates.Count == 1 && predicate is SqlPredicate.FalsePredicate)
									break;

								element.Predicates.RemoveAt(i);

								if (element.Predicates.Count == 0)
									element.Predicates.Add(SqlPredicate.False);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not SqlPredicate.FalsePredicate)
							{
								element.Predicates.Clear();
								element.Predicates.Add(SqlPredicate.False);
								break;
							}
						}
						else if (value is null)
						{
							return new SqlSearchCondition(element.IsOr, new SqlPredicate.Expr(new SqlValue(typeof(bool?), null)));
						}
					}
				}
			}
			else
			{
				List<ISqlPredicate>? newPredicates = null;

				void EnsureCopied(int count)
				{
					if (newPredicates == null)
					{
						newPredicates = new List<ISqlPredicate>(element.Predicates.Count);
						newPredicates.AddRange(element.Predicates.Take(count));
					}
				}

				void EnsureCleared()
				{
					if (newPredicates == null)
					{
						newPredicates = new List<ISqlPredicate>();
					}
					else
					{
						newPredicates.Clear();
					}
				}

				for (var i = 0; i < element.Predicates.Count; i++)
				{
					var predicate = element.Predicates[i];
					// unnesting search conditions
					//
					if (predicate is SqlSearchCondition sc && (sc.IsOr == element.IsOr || sc.Predicates.Count <= 1))
					{
						EnsureCopied(i);
						newPredicates!.InsertRange(i, sc.Predicates);
						continue;
					}

					if (TryEvaluate(predicate, out var value) &&
					    value is bool boolValue)
					{
						if (boolValue)
						{
							if (element.IsAnd)
							{
								if (element.Predicates.Count == 1 && predicate is SqlPredicate.TruePredicate)
									continue;

								// ignore
								EnsureCopied(i);

								if (element.Predicates.Count == 1)
									newPredicates!.Add(SqlPredicate.True);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not SqlPredicate.TruePredicate)
							{
								EnsureCleared();
								newPredicates!.Add(SqlPredicate.True);
								break;
							}
						}
						else
						{
							if (element.IsOr)
							{
								if (element.Predicates.Count == 1 && predicate is SqlPredicate.FalsePredicate)
									continue;

								// ignore
								EnsureCopied(i);

								if (element.Predicates.Count == 1)
									newPredicates!.Add(SqlPredicate.False);

								continue;
							}

							if (element.Predicates.Count > 1 || predicate is not SqlPredicate.FalsePredicate)
							{
								EnsureCleared();
								newPredicates!.Add(SqlPredicate.False);
								break;
							}
						}
					}

					newPredicates?.Add(predicate);
				}

				if (newPredicates != null)
				{
					newElement = new SqlSearchCondition(element.IsOr, newPredicates);
					NotifyReplaced(newElement, element);

					return newElement;
				}

			}

			return element;
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
			if (TryEvaluate(predicate.Expr1, out var value1) && value1 == null)
			{
				return new SqlPredicate.IsNull(predicate.Expr2, !predicate.IsNot);
			}

			if (TryEvaluate(predicate.Expr2, out var value2) && value2 == null)
			{
				return new SqlPredicate.IsNull(predicate.Expr1, !predicate.IsNot);
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveInsideNot = _isInsideNot;
			_isInsideNot = false;

			var result = base.VisitSqlQuery(selectQuery);

			_isInsideNot = saveInsideNot;

			return result;
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			if (!_visitQueries)
				return element;

			return base.VisitSqlTableSource(element);
		}

		protected override IQueryElement VisitNotPredicate(SqlPredicate.Not predicate)
		{
			if (predicate.Predicate.CanInvert(_nullabilityContext))
			{
				return Visit(predicate.Predicate.Invert(_nullabilityContext));
			}

			var saveInsideNot = _isInsideNot;
			var saveAllow     = _allowOptimize;

			_isInsideNot     = true;
			_allowOptimize = predicate.Predicate;
			var newInnerPredicate = (ISqlPredicate)Visit(predicate.Predicate);
			_isInsideNot     = saveInsideNot;
			_allowOptimize = saveAllow;

			if (newInnerPredicate.CanInvert(_nullabilityContext))
			{
				return Visit(newInnerPredicate.Invert(_nullabilityContext));
			}

			if (!ReferenceEquals(newInnerPredicate, predicate.Predicate))
			{
				if (GetVisitMode(predicate) == VisitMode.Transform)
				{
					return new SqlPredicate.Not(newInnerPredicate);
				}

				predicate.Modify(newInnerPredicate);
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			var newElement = base.VisitSqlBinaryExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			newElement = element switch
			{
				(var e, "+", SqlBinaryExpression { Operation: "*", Expr1: SqlValue { Value: -1 } } binary) => SqlBinaryExpressionHelper.CreateWithTypeInferred(element.SystemType!, e, "-", binary.Expr2, Precedence.Subtraction),
				(var e, "+", SqlBinaryExpression { Operation: "*", Expr2: SqlValue { Value: -1 } } binary) => SqlBinaryExpressionHelper.CreateWithTypeInferred(e.SystemType!, e, "-", binary.Expr1, Precedence.Subtraction),
				(var e, "-", SqlBinaryExpression { Operation: "*", Expr1: SqlValue { Value: -1 } } binary) => SqlBinaryExpressionHelper.CreateWithTypeInferred(element.SystemType!, e, "+", binary.Expr2, Precedence.Subtraction),
				(var e, "-", SqlBinaryExpression { Operation: "*", Expr2: SqlValue { Value: -1 } } binary) => SqlBinaryExpressionHelper.CreateWithTypeInferred(e.SystemType!, e, "+", binary.Expr1, Precedence.Subtraction),

				_ => element
			};

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (TryEvaluateNoParameters(element, out var evaluatedValue))
				return new SqlValue(QueryHelper.GetDbDataType(element, _mappingSchema), evaluatedValue);

			switch (element.Operation)
			{
				case "+":
				{
					var v1 = TryEvaluateNoParameters(element.Expr1, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case short   h when h == 0  :
							case int     i when i == 0  :
							case long    l when l == 0  :
							case decimal d when d == 0  :
							case string  s when s.Length == 0:
							{
								var elementType = QueryHelper.GetDbDataType(element, _mappingSchema);
								var expr2Type   = QueryHelper.GetDbDataType(element.Expr2, _mappingSchema);
								if (!elementType.Equals(expr2Type))
									return new SqlCastExpression(element.Expr2, elementType, null);
								return element.Expr2;
							}
						}
					}

					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return element.Expr1;
							case int vi when
								element.Expr1    is SqlBinaryExpression be1                             &&
								TryEvaluateNoParameters(be1.Expr2, out var be1v2) &&
								                        be1v2 is int be1v2i :
							{
								switch (be1.Operation)
								{
									case "+":
									{
										var value = be1v2i + vi;

										if (value == 0) return be1.Expr1;

										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "-";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, _mappingSchema), element.Precedence);
									}

									case "-":
									{
										var value = be1v2i - vi;

										if (value == 0) return be1.Expr1;

										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "+";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, _mappingSchema), element.Precedence);
									}
								}

								break;
							}

							case string vs when vs.Length == 0 : return element.Expr1;
							case string vs when
								element.Expr1    is SqlBinaryExpression be1 &&
								//be1.Operation == "+"                   &&
								TryEvaluateNoParameters(be1.Expr2, out var be1v2) &&
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
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 + i2, element, _mappingSchema);
						if (value1 is string || value2 is string) return QueryHelper.CreateSqlValue(FormattableString.Invariant($"{value1}{value2}"), element, _mappingSchema);
					}

					break;
				}

				case "-":
				{
					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return element.Expr1;
							case int vi when
								element.Expr1 is SqlBinaryExpression be1 &&
								TryEvaluateNoParameters(be1.Expr2, out var be1v2) &&
								be1v2 is int be1v2i :
							{
								switch (be1.Operation)
								{
									case "+":
									{
										var value = be1v2i - vi;

										if (value == 0) return be1.Expr1;

										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "-";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, _mappingSchema), element.Precedence);
									}

									case "-":
									{
										var value = be1v2i + vi;

										if (value == 0) return be1.Expr1;

										var oper  = be1.Operation;

										if (value < 0)
										{
											value = -value;
											oper  = "+";
										}

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, _mappingSchema), element.Precedence);
									}
								}

								break;
							}
						}
					}

					if (v2 && TryEvaluateNoParameters(element.Expr1, out var value1))
					{
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 - i2, element, _mappingSchema);
					}

					break;
				}

				case "*":
				{
					var v1 = TryEvaluateNoParameters(element.Expr1, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element, _mappingSchema);
							case int i when i == 1 : return element.Expr2;
							case int i when
								element.Expr2    is SqlBinaryExpression be2 &&
								be2.Operation == "*"                   &&
								TryEvaluateNoParameters(be2.Expr1, out var be2v1)  &&
								be2v1 is int bi :
							{
								return new SqlBinaryExpression(be2.SystemType, QueryHelper.CreateSqlValue(i * bi, element, _mappingSchema), "*", be2.Expr2);
							}
						}
					}

					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element, _mappingSchema);
							case int i when i == 1 : return element.Expr1;
						}
					}

					if (v1 && v2)
					{
						switch (value1)
						{
							case int    i1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(i1 * i2, element, _mappingSchema);
							case int    i1 when value2 is double d2 : return QueryHelper.CreateSqlValue(i1 * d2, element, _mappingSchema);
							case double d1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(d1 * i2, element, _mappingSchema);
							case double d1 when value2 is double d2 : return QueryHelper.CreateSqlValue(d1 * d2, element, _mappingSchema);
						}
					}

					break;
				}
			}

			return element;
		}

		protected override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			if (!element.IsMandatory)
			{
				var from = element.FromType?.Type ?? QueryHelper.GetDbDataType(element.Expression, _mappingSchema);

				if (element.SystemType == typeof(object) || from.EqualsDbOnly(element.Type))
					return element.Expression;

				if (element.Expression is SqlCastExpression { IsMandatory: false } castOther)
				{
					var dbType = QueryHelper.GetDbDataType(castOther.Expression, _mappingSchema);
					if (element.Type.EqualsDbOnly(dbType))
						return castOther.Expression;
				}
			}

			if (element.Expression is SelectQuery selectQuery && selectQuery.Select.Columns.Count == 1)
			{
				if (GetVisitMode(selectQuery) == VisitMode.Modify)
				{
					var columnExpression = selectQuery.Select.Columns[0].Expression;
					selectQuery.Select.Columns[0].Expression = (ISqlExpression)Visit(new SqlCastExpression(columnExpression, element.ToType, element.FromType, isMandatory: element.IsMandatory));

					return selectQuery;
				}
			}

			return base.VisitSqlCastExpression(element);
		}

		protected override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			var optmimized = base.VisitExistsPredicate(predicate);

			if (!ReferenceEquals(optmimized, predicate))
				return Visit(optmimized);

			var query = predicate.SubQuery;

			if (query.Select.Columns.Count > 0)
			{
				if (query.GroupBy.IsEmpty)
				{
					if (QueryHelper.IsAggregationQuery(query))
						return SqlPredicate.True;
				}
			}

			if (query.Where.SearchCondition.Predicates is [SqlPredicate.FalsePredicate firstPredicate])
			{
				return firstPredicate.MakeNot(predicate.IsNot);
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			var newElement = base.VisitSqlFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			if (element.DoNotOptimize)
				return element;

			if (TryEvaluate(element, out var value))
			{
				return QueryHelper.CreateSqlValue(value, QueryHelper.GetDbDataType(element, _mappingSchema), element.Parameters);
			}

			newElement = OptimizeFunction(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			return element;
		}

		protected virtual IQueryElement OptimizeFunction(SqlFunction function)
		{
			if (function.Parameters.Length == 1 && function.Name is PseudoFunctions.TO_LOWER or PseudoFunctions.TO_UPPER)
			{
				if (function.Parameters[0] is SqlFunction { Parameters.Length: 1, Name: PseudoFunctions.TO_LOWER or PseudoFunctions.TO_UPPER } func)
				{
					return new SqlFunction(function.SystemType, function.Name, func.Parameters[0]);
				}
			}

			return function;
		}

		protected override IQueryElement VisitSqlCoalesceExpression(SqlCoalesceExpression element)
		{
			var newElement = base.VisitSqlCoalesceExpression(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			List<ISqlExpression>? newExpressions = null;

			for (var i = 0; i < element.Expressions.Length; i++)
			{
				var expr = element.Expressions[i];

				if (QueryHelper.UnwrapNullablity(expr) is SqlCoalesceExpression inner)
				{
					if (newExpressions == null)
					{
						newExpressions = new List<ISqlExpression>(element.Expressions.Length + inner.Expressions.Length - 1);
						newExpressions.AddRange(element.Expressions.Take(i));
					}

					newExpressions.AddRange(inner.Expressions);
				}
				else if (!_nullabilityContext.IsEmpty && !expr.CanBeNullable(_nullabilityContext) && i != element.Expressions.Length - 1)
				{
					if (newExpressions == null)
					{
						newExpressions = new List<ISqlExpression>(element.Expressions.Length);
						newExpressions.AddRange(element.Expressions.Take(i));
					}

					newExpressions.Add(expr);

					// chain terminated early
					break;
				}
				else 
				{
					newExpressions?.Add(expr);
				}
			}

			if (newExpressions != null)
			{
				if (newExpressions.Count == 1)
					return newExpressions[0];
				if (newExpressions.Count > 0)
					return Visit(new SqlCoalesceExpression(newExpressions.ToArray()));
			}

			return element;
		}

		protected override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
		{
			var newPredicate = base.VisitIsNullPredicate(predicate);

			if (!ReferenceEquals(newPredicate, predicate))
				return Visit(newPredicate);

			if (_nullabilityContext.IsEmpty)
				return predicate;

			if (!predicate.Expr1.CanBeNullableOrUnknown(_nullabilityContext))
			{
				//TODO: Exception for Row, find time to analyze why it's needed
				if (predicate.Expr1.ElementType != QueryElementType.SqlRow)
					return SqlPredicate.MakeBool(predicate.IsNot);
			}

			if (TryEvaluate(predicate.Expr1, out var value))
			{
				return SqlPredicate.MakeBool((value == null) != predicate.IsNot);
			}

			var unwrapped = QueryHelper.UnwrapNullablity(predicate.Expr1);
			if (unwrapped is SqlBinaryExpression binaryExpression)
			{
				ISqlPredicate? result = null;

				if (binaryExpression.Operation is "+" or "-" or "*" or "/" or "%" or "&")
				{
					if (binaryExpression.Expr1.CanBeNullable(_nullabilityContext) && !binaryExpression.Expr2.CanBeNullable(_nullabilityContext))
					{
						result = new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(binaryExpression.Expr1, true), predicate.IsNot);
					}
					else if (binaryExpression.Expr2.CanBeNullable(_nullabilityContext) && !binaryExpression.Expr1.CanBeNullable(_nullabilityContext))
					{
						result = new SqlPredicate.IsNull(SqlNullabilityExpression.ApplyNullability(binaryExpression.Expr2, true), predicate.IsNot);
					}
				}

				if (result != null)
					return Visit(result);
			}

			if (ReferenceEquals(unwrapped, predicate.Expr1) || predicate.Expr1 is SqlNullabilityExpression sqlNullabilityExpression &&
			    sqlNullabilityExpression.CanBeNullable(_nullabilityContext) == unwrapped.CanBeNullable(_nullabilityContext))
			{
				if (unwrapped is SqlConditionExpression condition)
				{
					if (condition.TrueValue.IsNullValue())
					{
						var sc = new SqlSearchCondition(true);
						sc.Add(condition.Condition);
						sc.AddIsNull(condition.FalseValue);
						return Visit(sc.MakeNot(predicate.IsNot));
					}

					if (condition.FalseValue.IsNullValue())
					{
						var sc = new SqlSearchCondition(true);
						sc.Add(condition.Condition.MakeNot());
						sc.AddIsNull(condition.TrueValue);
						return Visit(sc.MakeNot(predicate.IsNot));
					}
				}
				else if (unwrapped is SqlCastExpression cast)
				{
					var newIsNull = new SqlPredicate.IsNull(cast.Expression, predicate.IsNot);
					return Visit(newIsNull);
				}
				else if (unwrapped is SqlFunction func)
				{
					// We can extend to more parameters, but it's not clear if it's needed
					if (func is { IsAggregate: false, IsPure: true })
					{
						if (func.NullabilityType == ParametersNullabilityType.IfAnyParameterNullable)
						{
							var sc = new SqlSearchCondition(true);
							sc.AddRange(func.Parameters.Select(p => new SqlPredicate.IsNull(p, false)));
							return Visit(sc.MakeNot(predicate.IsNot));
						}

						if (func.NullabilityType == ParametersNullabilityType.IfAllParametersNullable)
						{
							var sc = new SqlSearchCondition(false);
							sc.AddRange(func.Parameters.Select(p => new SqlPredicate.IsNull(p, false)));
							return Visit(sc.MakeNot(predicate.IsNot));
						}

						if (func.NullabilityType == ParametersNullabilityType.SameAsFirstParameter)
						{
							var newIsNull = new SqlPredicate.IsNull(func.Parameters[0], predicate.IsNot);
							return Visit(newIsNull);
						}

						if (func.NullabilityType == ParametersNullabilityType.SameAsSecondParameter)
						{
							var newIsNull = new SqlPredicate.IsNull(func.Parameters[1], predicate.IsNot);
							return Visit(newIsNull);
						}

						if (func.NullabilityType == ParametersNullabilityType.SameAsThirdParameter)
						{
							var newIsNull = new SqlPredicate.IsNull(func.Parameters[2], predicate.IsNot);
							return Visit(newIsNull);
						}

						if (func.NullabilityType == ParametersNullabilityType.SameAsLastParameter)
						{
							var newIsNull = new SqlPredicate.IsNull(func.Parameters[^1], predicate.IsNot);
							return Visit(newIsNull);
						}
					}
				}
			}

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

			var inner = element.SqlExpression;
			while (inner is SqlCastExpression cast)
			{
				inner = cast.Expression;
			}

			if (inner is SqlValue or SqlParameter or SqlSearchCondition)
				return element.SqlExpression;

			return element;
		}

		protected override IQueryElement VisitExprPredicate(SqlPredicate.Expr predicate)
		{
			var result = base.VisitExprPredicate(predicate);

			if (!ReferenceEquals(result, predicate))
				return Visit(result);

			if (predicate.Expr1 is ISqlPredicate inner)
				return inner;

			return predicate;
		}

		protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var newElement = base.VisitExprExprPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (TryEvaluateNoParameters(predicate, out var value) && value is bool boolValue)
			{
				return SqlPredicate.MakeBool(boolValue);
			}

			if (_reduceBinary)
			{
				var reduced = predicate.Reduce(_nullabilityContext, _evaluationContext, _isInsideNot, _dataOptions.LinqOptions);

				if (!ReferenceEquals(reduced, predicate))
				{
					return Visit(reduced);
				}
			}

			var expr = predicate;

			if (expr.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				if (expr.WithNull == null)
				{
					if (expr.Expr2 is ISqlPredicate expr2Predicate)
					{
						var boolValue1 = QueryHelper.GetBoolValue(expr.Expr1, _evaluationContext);
						if (boolValue1 != null)
						{
							var isNot       = boolValue1.Value != (expr.Operator == SqlPredicate.Operator.Equal);
							var transformed = expr2Predicate.MakeNot(isNot);

							return transformed;
						}
					}

					if (expr.Expr1 is ISqlPredicate expr1Predicate)
					{
						var boolValue2 = QueryHelper.GetBoolValue(expr.Expr2, _evaluationContext);
						if (boolValue2 != null)
						{
							var isNot       = boolValue2.Value != (expr.Operator == SqlPredicate.Operator.Equal);
							var transformed = expr1Predicate.MakeNot(isNot);
							return transformed;
						}
					}
				}

				if (QueryHelper.UnwrapNullablity(predicate.Expr1) is SqlValue { Value: null })
				{
					return Visit(new SqlPredicate.IsNull(predicate.Expr2, expr.Operator == SqlPredicate.Operator.NotEqual));
				}

				if (QueryHelper.UnwrapNullablity(predicate.Expr2) is SqlValue { Value: null })
				{
					return Visit(new SqlPredicate.IsNull(predicate.Expr1, expr.Operator == SqlPredicate.Operator.NotEqual));
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
				{
					var newPredicate = OptimizeExpExprPredicate(expr);
					if (!ReferenceEquals(newPredicate, expr))
						return Visit(newPredicate);

					break;
				}
			}

			return predicate;
		}

		protected override IQueryElement VisitSqlWhereClause(SqlWhereClause element)
		{
			var newElement = base.VisitSqlWhereClause(element);

			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			// Ensure top level is always AND

			if (element.SearchCondition.IsOr)
			{
				SqlSearchCondition newSearchCondition = element.SearchCondition.Predicates switch
				{
					[]       => new SqlSearchCondition(false),
					[var p0] => new SqlSearchCondition(false, p0),
					_        => new SqlSearchCondition(false, element.SearchCondition),
				};

				if (GetVisitMode(element) == VisitMode.Modify)
				{
					element.SearchCondition = newSearchCondition;
				}
				else
				{
					return new SqlWhereClause(newSearchCondition);
				}
			}

			return element;
		}

		protected override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			var optmimized = base.VisitInSubQueryPredicate(predicate);

			if (!ReferenceEquals(optmimized, predicate))
				return Visit(optmimized);

			if (predicate.SubQuery.Where.SearchCondition.Predicates is [SqlPredicate.FalsePredicate firstPredicate])
			{
				return firstPredicate.MakeNot(predicate.IsNot);
			}

			return predicate;
		}

		protected override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var newElement = base.VisitInListPredicate(predicate);

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (_evaluationContext.ParameterValues == null)
			{
				return predicate;
			}

			if (predicate.Values is [SqlParameter valuesParam] && _evaluationContext.ParameterValues!.TryGetValue(valuesParam, out var parameterValue))
			{
				switch (parameterValue.ProviderValue)
				{
					case null:
						return SqlPredicate.MakeBool(predicate.IsNot);

					// Be careful that string is IEnumerable, we don't want to handle x.In(string) here
					case string:
						break;
					case IEnumerable items:
					{
						if (predicate.Expr1 is not ISqlTableSource)
						{
							bool noValues = !items.Cast<object?>().Any();
							if (noValues)
								return SqlPredicate.MakeBool(predicate.IsNot);
						}

						break;
					}
				}
			}

			return predicate;
		}

		#region OptimizeExpExprPredicate

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

		static bool Compare(object? value1, object? value2, SqlPredicate.Operator op, out bool result)
		{
			if (op is SqlPredicate.Operator.NotEqual)
			{
				if (value1 is null && value2 is not null)
				{
					result = false;
					return true;
				}

				if (value2 is null && value1 is not null)
				{
					result = false;
					return true;
				}
			}

			if (value1 is IComparable comparable1 && value1.GetType() == value2?.GetType())
			{
				switch (op)
				{
					case SqlPredicate.Operator.Equal:          result = comparable1.CompareTo(value2) == 0; break;
					case SqlPredicate.Operator.NotEqual:       result = comparable1.CompareTo(value2) != 0; break;
					case SqlPredicate.Operator.Greater:        result = comparable1.CompareTo(value2) >  0; break;
					case SqlPredicate.Operator.GreaterOrEqual: result = comparable1.CompareTo(value2) >= 0; break;
					case SqlPredicate.Operator.Less:           result = comparable1.CompareTo(value2) <  0; break;
					case SqlPredicate.Operator.LessOrEqual:    result = comparable1.CompareTo(value2) <= 0; break;

					default:
					{
						result = false;
						return false;
					}
				}

				return true;
			}

			result = false;
			return false;
		}

		static void CombineOperator(ref SqlPredicate.Operator? current, SqlPredicate.Operator additional)
		{
			if (current == null)
			{
				current = additional;
				return;
			}

			if (current == additional)
				return;

			if (current == SqlPredicate.Operator.Equal && additional == SqlPredicate.Operator.Greater)
				current = SqlPredicate.Operator.GreaterOrEqual;
			else if (current == SqlPredicate.Operator.Equal && additional == SqlPredicate.Operator.Less)
				current = SqlPredicate.Operator.LessOrEqual;
			else if (current == SqlPredicate.Operator.Greater && additional == SqlPredicate.Operator.Equal)
				current = SqlPredicate.Operator.GreaterOrEqual;
			else if (current == SqlPredicate.Operator.Less && additional == SqlPredicate.Operator.Equal)
				current = SqlPredicate.Operator.LessOrEqual;
			else if (current == SqlPredicate.Operator.Greater && additional == SqlPredicate.Operator.Less)
				current = SqlPredicate.Operator.NotEqual;
			else if (current == SqlPredicate.Operator.Less && additional == SqlPredicate.Operator.Greater)
				current = SqlPredicate.Operator.NotEqual;
			else
				throw new NotImplementedException();
		}

		static SqlPredicate.Operator SwapOperator(SqlPredicate.Operator op)
		{
			return op switch
			{
				SqlPredicate.Operator.Equal => op,
				SqlPredicate.Operator.NotEqual => op,
				SqlPredicate.Operator.Greater => SqlPredicate.Operator.Less,
				SqlPredicate.Operator.NotLess => SqlPredicate.Operator.NotGreater,
				SqlPredicate.Operator.GreaterOrEqual => SqlPredicate.Operator.LessOrEqual,
				SqlPredicate.Operator.Less => SqlPredicate.Operator.Greater,
				SqlPredicate.Operator.NotGreater => SqlPredicate.Operator.NotLess,
				SqlPredicate.Operator.LessOrEqual => SqlPredicate.Operator.GreaterOrEqual,
				_ => throw new InvalidOperationException()
			};
		}

		ISqlPredicate? ProcessComparisonWithCase(ISqlExpression other, ISqlExpression valueExpression, SqlPredicate.Operator op)
		{
			var unwrappedOther = QueryHelper.UnwrapNullablity(other);
			var unwrappedValue = QueryHelper.UnwrapNullablity(valueExpression);

			var isNot = op == SqlPredicate.Operator.NotEqual;

			if (unwrappedOther is SqlConditionExpression sqlConditionExpression)
			{
				if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
				{
					if (sqlConditionExpression.TrueValue.Equals(unwrappedValue) && TryEvaluateNoParameters(sqlConditionExpression.FalseValue, out _))
					{
						return sqlConditionExpression.Condition.MakeNot(isNot);
					}

					if (sqlConditionExpression.FalseValue.Equals(unwrappedValue) && TryEvaluateNoParameters(sqlConditionExpression.TrueValue, out _))
					{
						return sqlConditionExpression.Condition.MakeNot(!isNot);
					}
				}

				if (TryEvaluateNoParameters(unwrappedValue, out _))
				{
					if (TryEvaluateNoParameters(sqlConditionExpression.TrueValue, out _) || TryEvaluateNoParameters(sqlConditionExpression.FalseValue, out _))
					{
						var sc = new SqlSearchCondition(true)
							.AddAnd( sub =>
								sub
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.TrueValue, op, valueExpression, _dataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null))
									.Add(sqlConditionExpression.Condition)
							)
							.AddAnd( sub =>
								sub
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.FalseValue, op, valueExpression, _dataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null))
									.Add(sqlConditionExpression.Condition.MakeNot())
								);

						return sc;
					}
				}
			}
			else if (unwrappedOther is SqlCaseExpression sqlCaseExpression)
			{
				// Try comparing by values

				if (TryEvaluateNoParameters(unwrappedValue, out var value))
				{
					var caseMatch      = new bool [sqlCaseExpression._cases.Count];
					var allEvaluatable = true;
					var elseMatch      = false;

					for (var index = 0; index < sqlCaseExpression._cases.Count; index++)
					{
						var caseItem = sqlCaseExpression._cases[index];
						if (TryEvaluateNoParameters(caseItem.ResultExpression, out var caseItemValue)
						    && Compare(caseItemValue, value, op, out var result))
						{
							caseMatch[index] = result;
						}
						else
						{
							allEvaluatable = false;
							break;
						}
					}

					object? elseValue = null;

					if ((sqlCaseExpression.ElseExpression == null || sqlCaseExpression.ElseExpression.TryEvaluateExpression(_evaluationContext, out elseValue))
					    && Compare(elseValue, value, op, out var compareResult))
					{
						elseMatch = compareResult;
					}
					else
						allEvaluatable = false;

					if (allEvaluatable)
					{
						if (caseMatch.All(c => !c) && !elseMatch)
							return SqlPredicate.False;

						var resultCondition = new SqlSearchCondition(true);

						var notMatches = new List<ISqlPredicate>();
						for (int index = 0; index < caseMatch.Length; index++)
						{
							if (caseMatch[index])
							{
								var condition = new SqlSearchCondition(false)
									.Add(sqlCaseExpression._cases[index].Condition);

								if (notMatches.Count > 0)
									condition.Add(new SqlSearchCondition(true, notMatches).MakeNot());

								resultCondition.Add(condition);
							}
							else
							{
								notMatches.Add(sqlCaseExpression._cases[index].Condition);
							}
						}

						if (elseMatch)
						{
							if (notMatches.Count == 0)
								return SqlPredicate.True;

							resultCondition.Add(new SqlSearchCondition(true, notMatches).MakeNot());
						}

						return resultCondition;
					}
				}

			}

			return null;
		}

		ISqlPredicate OptimizeIsTruePredicate(SqlPredicate.IsTrue isTrue)
		{
			if (TryEvaluateNoParameters(isTrue.Expr1, out var result) && result is bool boolValue)
			{
				return SqlPredicate.MakeBool(boolValue != isTrue.IsNot);
			}

			if (isTrue.Expr1 is SqlConditionExpression or SqlCaseExpression)
			{
				if (!isTrue.IsNot)
				{
					var predicate = ProcessComparisonWithCase(isTrue.Expr1, isTrue.TrueValue, SqlPredicate.Operator.Equal);
					if (predicate != null)
						return predicate.MakeNot(isTrue.IsNot);
				}
			}

			if (_reduceBinary)
			{
				var reduced = isTrue.Reduce(_nullabilityContext, _isInsideNot);

				if (!ReferenceEquals(reduced, isTrue))
				{
					return (ISqlPredicate)Visit(reduced);
				}
			}

			return isTrue;
		}

		ISqlPredicate OptimizeExpExprPredicate(SqlPredicate.ExprExpr exprExpr)
		{
			var unwrapped1 = QueryHelper.UnwrapNullablity(exprExpr.Expr1);

			if (unwrapped1 is SqlCompareToExpression compareTo1)
			{
				if (TryEvaluateNoParameters(exprExpr.Expr2, out var result) && result is int intValue)
				{
					SqlPredicate.Operator? current = null;

					if (Compare(1, intValue, exprExpr.Operator))
						CombineOperator(ref current, SqlPredicate.Operator.Greater);

					if (Compare(0, intValue, exprExpr.Operator))
						CombineOperator(ref current, SqlPredicate.Operator.Equal);

					if (Compare(-1, intValue, exprExpr.Operator))
						CombineOperator(ref current, SqlPredicate.Operator.Less);

					if (current == null)
						return SqlPredicate.False;

					return new SqlPredicate.ExprExpr(compareTo1.Expression1, current.Value, compareTo1.Expression2, _dataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null);
				}
			}

			var unwrapped2 = QueryHelper.UnwrapNullablity(exprExpr.Expr2);

			if (unwrapped2 is SqlCompareToExpression compareTo2)
			{
				if (TryEvaluateNoParameters(exprExpr.Expr1, out var result) && result is int intValue)
				{
					SqlPredicate.Operator? current = null;

					if (Compare(1, intValue, exprExpr.Operator))
						CombineOperator(ref current, SqlPredicate.Operator.Less);

					if (Compare(0, intValue, exprExpr.Operator))
						CombineOperator(ref current, SqlPredicate.Operator.Equal);

					if (Compare(-1, intValue, exprExpr.Operator))
						CombineOperator(ref current, SqlPredicate.Operator.Greater);

					if (current == null)
						return SqlPredicate.False;

					return new SqlPredicate.ExprExpr(compareTo2.Expression1, current.Value, compareTo2.Expression2, _dataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null);
				}
			}

			var processed = ProcessComparisonWithCase(exprExpr.Expr1, exprExpr.Expr2, exprExpr.Operator)
			                ?? ProcessComparisonWithCase(exprExpr.Expr2, exprExpr.Expr1, SwapOperator(exprExpr.Operator));

			if (processed != null)
				return processed;

			var left  = QueryHelper.UnwrapNullablity(exprExpr.Expr1);
			var right = QueryHelper.UnwrapNullablity(exprExpr.Expr2);

			if (!exprExpr.Expr1.CanBeNullable(_nullabilityContext) && left.Equals(right))
			{
				if (exprExpr.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.GreaterOrEqual or SqlPredicate.Operator.LessOrEqual or SqlPredicate.Operator.NotGreater or SqlPredicate.Operator.NotLess)
				{
					return SqlPredicate.True;
				}

				if (exprExpr.Operator is SqlPredicate.Operator.NotEqual or SqlPredicate.Operator.Greater or SqlPredicate.Operator.Less)
				{
					return SqlPredicate.False;
				}
			}

			if (!_nullabilityContext.IsEmpty                       &&
			    !exprExpr.Expr1.CanBeNullable(_nullabilityContext) &&
			    !exprExpr.Expr2.CanBeNullable(_nullabilityContext) &&
			    exprExpr.Expr1.SystemType.IsSignedType()           &&
			    exprExpr.Expr2.SystemType.IsSignedType())
			{
				var unwrapped = (left, exprExpr.Operator, right);

				var newExpr = unwrapped switch
				{
					(SqlBinaryExpression binary, var op, var v) when CanBeEvaluateNoParameters(v) =>

						// binary < v
						binary switch
						{
							// e + some < v ===> some < v - e
							(var e, "+", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "-", e), null),
							// e - some < v ===>  e - v < some
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, e, "-", v), op, some, null),

							// some + e < v ===> some < v - e
							(var some, "+", var e) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "-", e), null),
							// some - e < v ===> some < v + e
							(var some, "-", var e) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "+", e), null),

							_ => null
						},

					(SqlBinaryExpression binary, var op, var v) when CanBeEvaluateNoParameters(v) =>

						// binary < v
						binary switch
						{
							// e + some < v ===> some < v - e
							(var e, "+", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "-", e), null),
							// e - some < v ===>  e - v < some
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, e, "-", v), op, some, null),

							// some + e < v ===> some < v - e
							(var some, "+", var e) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "-", e), null),
							// some - e < v ===> some < v + e
							(var some, "-", var e) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "+", e), null),

							_ => null
						},

					(var v, var op, SqlBinaryExpression binary) when CanBeEvaluateNoParameters(v) =>

						// v < binary
						binary switch
						{
							// v < e + some ===> v - e < some
							(var e, "+", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "-", e), op, some, null),
							// v < e - some ===> some < e - v
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(some, op, SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, e, "-", v), null),

							// v < some + e ===> v - e < some
							(var some, "+", var e) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "-", e), op, some, null),
							// v < some - e ===> v + e < some
							(var e, "-", var some) when CanBeEvaluateNoParameters(e) => new SqlPredicate.ExprExpr(SqlBinaryExpressionHelper.CreateWithTypeInferred(v.SystemType!, v, "+", e), op, some, null),

							_ => null
						},

					_ => null
				};

				exprExpr = newExpr ?? exprExpr;
			}

			return exprExpr;
		}

		#endregion

	}
}
