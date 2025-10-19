using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using static LinqToDB.Internal.Common.Utils;

namespace LinqToDB.Internal.SqlProvider
{
	public class SqlExpressionOptimizerVisitor : SqlQueryVisitor
	{
		ISimilarityMerger           _similarityMerger   = SimilarityMerger.Instance;
		NullabilityContext          _nullabilityContext = default!;
		ICollection<ISqlPredicate>? _allowOptimizeList;
		ISqlPredicate?              _allowOptimize;
		bool                        _visitQueries;
		bool                        _isInsidePredicate;
		bool                        _reducePredicates;

		protected DataOptions       DataOptions       { get; private set; } = default!;
		protected EvaluationContext EvaluationContext { get; private set; } = default!;
		protected MappingSchema     MappingSchema     { get; private set; } = default!;

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
			bool                        reducePredicates)
		{
			Cleanup();
			EvaluationContext  = evaluationContext;
			DataOptions        = dataOptions;
			MappingSchema      = mappingSchema;
			_visitQueries      = visitQueries;
			_reducePredicates  = reducePredicates;

			SetTransformationInfo(transformationInfo);

			_nullabilityContext = nullabilityContext.WithTransformationInfo(GetTransformationInfo());

			return ProcessElement(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_visitQueries       = default;
			_isInsidePredicate  = default;
			_reducePredicates   = default;
			EvaluationContext   = default!;
			_nullabilityContext = default!;
			DataOptions         = default!;
			MappingSchema       = default!;
			_allowOptimize      = default;
			_allowOptimizeList  = default;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null)
				return element;

			var saveIsInsidePredicate = _isInsidePredicate;

			if (element is not SqlNullabilityExpression and not ISqlPredicate)
			{
				_isInsidePredicate = false;
			}

			var newElement = base.Visit(element);

			_isInsidePredicate = saveIsInsidePredicate;

			return newElement;
		}

		#region Helper functions

		protected bool CanBeEvaluateNoParameters(IQueryElement expr)
		{
			if (expr.HasQueryParameter())
			{
				return false;
			}

			return expr.CanBeEvaluated(EvaluationContext);
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
			if (expr.TryEvaluateExpression(EvaluationContext, out result))
				return true;

			return false;
		}

		#endregion

		protected override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			var saveNullabilityContext = _nullabilityContext;
			_nullabilityContext = _nullabilityContext.WithJoinSource(element.Table.Source);

			var newElement = base.VisitSqlJoinedTable(element);

			_nullabilityContext = saveNullabilityContext;

			return newElement;
		}

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
				if (trueConditional.Condition.Equals(element.Condition, SqlQuery.SqlExtensions.DefaultComparer))
				{
					var newConditionExpression = new SqlConditionExpression(element.Condition, trueConditional.TrueValue, element.FalseValue);
					return Visit(newConditionExpression);
				}
			}

			if (element.FalseValue is SqlConditionExpression falseConditional)
			{
				var newCaseExpression = new SqlCaseExpression(QueryHelper.GetDbDataType(element.TrueValue, MappingSchema),
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
					if (unwrapped.Equals(element.TrueValue, SqlQuery.SqlExtensions.DefaultComparer) && element.FalseValue is SqlValue { Value: null })
					{
						return isNullPredicate.Expr1;
					}
				}
				else if (unwrapped.Equals(element.FalseValue, SqlQuery.SqlExtensions.DefaultComparer) && element.TrueValue is SqlValue { Value: null })
				{
					return isNullPredicate.Expr1;
				}
			}

			SqlConditionExpression? nestedCondition = null;
			if (element.Condition is SqlPredicate.Expr { ElementType: QueryElementType.ExprPredicate, Expr1: SqlConditionExpression nestedCondition1 })
				nestedCondition = nestedCondition1;
			else if (element.Condition is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal, Expr1: SqlConditionExpression nestedCondition2, UnknownAsValue: null, Expr2: SqlValue { Value: true } })
				nestedCondition = nestedCondition2;

			if (nestedCondition != null)
			{
				if (element.TrueValue.Equals(nestedCondition.TrueValue, SqlQuery.SqlExtensions.DefaultComparer)
					&& element.FalseValue.Equals(nestedCondition.FalseValue, SqlQuery.SqlExtensions.DefaultComparer))
				{
					return nestedCondition;
				}

				if (element.TrueValue.Equals(nestedCondition.FalseValue, SqlQuery.SqlExtensions.DefaultComparer)
					&& element.FalseValue.Equals(nestedCondition.TrueValue, SqlQuery.SqlExtensions.DefaultComparer))
				{
					return new SqlConditionExpression(nestedCondition.Condition, element.FalseValue, element.TrueValue);
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

			if (element.Predicates.Count == 0)
				return element;

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
							return new SqlSearchCondition(element.IsOr, canBeUnknown: element.CanReturnUnknown, new SqlPredicate.Expr(new SqlValue(typeof(bool?), null)));
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
						newPredicates!.AddRange(sc.Predicates);
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
					newElement = new SqlSearchCondition(element.IsOr, canBeUnknown: element.CanReturnUnknown, newPredicates);
					NotifyReplaced(newElement, element);

					return newElement;
				}
			}

			// propagade IS [NOT] NULL checks to nullability context to get rid of unnecessary nested null checks
			if (element.Predicates.Count > 1)
			{
				Dictionary<ISqlExpression, bool>? notNullOverrides = null;
				bool[]? duplicates = null;

				for (var i = 0; i < element.Predicates.Count; i++)
				{
					var predicate = element.Predicates[i];

					if (predicate is SqlPredicate.IsNull isNull && isNull.IsNot != element.IsOr)
					{
						var isDuplicate = false;
						isDuplicate = !(notNullOverrides ??= new(ISqlExpressionEqualityComparer.Instance)).TryAdd(isNull.Expr1, false);

						// limited duplicates detection for some IsNull predicates only
						// TODO: for full implementation we need ISqlPredicate comparer
						if (isDuplicate)
							(duplicates ??= new bool[element.Predicates.Count])[i] = true;
					}
				}

				if (notNullOverrides != null && notNullOverrides.Count < element.Predicates.Count)
				{
					List<ISqlPredicate>? newPredicates = null;

					var modify = GetVisitMode(element) == VisitMode.Modify;

					var oldContext      = _nullabilityContext;
					_nullabilityContext = new NullabilityContext(_nullabilityContext, notNullOverrides);

					var modified = false;
					var indexOffset = 0;
					for (var i = 0; i < element.Predicates.Count; i++)
					{
						if (duplicates?[i + indexOffset] == true)
						{
							if (modify)
							{
								element.Predicates.RemoveAt(i);
								i--;
								indexOffset++;
								modified = true;
								continue;
							}
							else
							{
								newPredicates ??= [.. element.Predicates.Take(i)];
								continue;
							}
						}

						var predicate = element.Predicates[i];

						if (predicate is SqlPredicate.IsNull isNull && isNull.IsNot != element.IsOr)
						{
							newPredicates?.Add(predicate);
							continue;
						}

						var newPredicate = (ISqlPredicate)Visit(predicate);

						if (!ReferenceEquals(newPredicate, predicate))
						{
							if (modify)
							{
								element.Predicates[i] = newPredicate;
								modified = true;
							}
							else
							{
								newPredicates ??= [.. element.Predicates.Take(i)];
							}
						}

						if (newPredicates != null)
						{
							newPredicates.Add(newPredicate);
						}
					}

					_nullabilityContext = oldContext;

					if (!modify && newPredicates != null)
						return Visit(new SqlSearchCondition(element.IsOr, canBeUnknown: element.CanReturnUnknown, newPredicates));
					else if (modified)
						return Visit(element);
				}
			}

			// Optimizations: PREDICATE vs PREDICATE:
			// 1. A IS NOT NULL AND A = B => A = B, when B is not nullable
			// 2. A OR B OR A => A OR B
			// 3. A AND B AND A => A AND B
			// 4. A AND !A => false
			// 4. A OR !A => true
			newElement = OptimizeSimilarFlat(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			/* TODO: it is buggy.

			// Optimizations: PREDICATE vs (GROUP)
			// 1. A OR (A AND B) => A
			// 2. A AND (A OR B) => A
			// 3. A OR (!A AND B) => A OR B
			// 4. A AND (!A OR B) => A AND B
			newElement = OptimizeSimilarForSinglePredicate(element);
			if (!ReferenceEquals(newElement, element))
				return Visit(newElement);

			*/

			return element;
		}

		IQueryElement OptimizeSimilarFlat(SqlSearchCondition element)
		{
			if (element.Predicates.Count <= 1)
				return element;

			// We should not slowdown translation with dynamically generate conditions.
			if (element.Predicates.Count > 100)
				return element;

			var predicatesToCompare = element.Predicates
				.SelectMany(p => _similarityMerger.GetSimilarityCodes(p).Select(code => (predicate : p, code)))
				.GroupBy(x => x.code)
				.Select(g => g.Select(x => x.predicate).Distinct().ToList())
				.Where(p => p.Count > 1)
				.ToList();

			var isOptimized = false;

			if (predicatesToCompare.Count > 0)
			{
				List<ISqlPredicate>? newPredicates     = null;
				var                  visitedPredicates = new HashSet<ISqlPredicate>(ObjectReferenceEqualityComparer<ISqlPredicate>.Default);

				for (var i = 0; i < predicatesToCompare.Count; i++)
				{
					var group = predicatesToCompare[i];
					for (var j = 0; j < group.Count; j++)
					{
						var predicate1 = group[j];
						if (visitedPredicates.Contains(predicate1))
							continue;

						for (var k = j + 1; k < group.Count; k++)
						{
							var predicate2 = group[k];
							if (visitedPredicates.Contains(predicate2))
								continue;

							if (_similarityMerger.TryMerge(_nullabilityContext, _isInsidePredicate, predicate1, predicate2, element.IsOr, out var mergedPredicate) || 
							    _similarityMerger.TryMerge(_nullabilityContext, _isInsidePredicate, predicate2, predicate1, element.IsOr, out mergedPredicate))
							{
								var predicatesList = element.Predicates;

								if (GetVisitMode(element) == VisitMode.Transform)
								{
									newPredicates  ??= [..element.Predicates];
									predicatesList =   newPredicates;
								}

								group.RemoveAt(k);
								group.RemoveAt(j);

								var idx1 = predicatesList.IndexOf(predicate1);
								var idx2 = predicatesList.IndexOf(predicate2);

								visitedPredicates.Add(predicate1);
								visitedPredicates.Add(predicate2);

								predicatesList.Remove(predicate1);
								predicatesList.Remove(predicate2);

								if (mergedPredicate != null)
								{
									var insertIndex = idx1;
									if (insertIndex < 0 || insertIndex > idx2)
										insertIndex = idx2;

									if (insertIndex < 0)
										insertIndex = 0;

									predicatesList.Insert(insertIndex, mergedPredicate);
									group.Insert(j, mergedPredicate);
								}

								isOptimized = true;

								j--;

								break;
							}
						}
					}
				}

				if (newPredicates != null)
				{
					var newSearchCondition = new SqlSearchCondition(element.IsOr, canBeUnknown: element.CanReturnUnknown, newPredicates);
					return NotifyReplaced(newSearchCondition, element);
				}
					
				if (isOptimized)
				{
					return Visit(element);
				}
			}

			return element;
		}

		public IQueryElement OptimizeSimilarForSinglePredicate(SqlSearchCondition element)
		{
			if (element.Predicates.Count < 2)
				return element;

			// We should not slowdown translation with dynamically generate conditions.
			if (element.Predicates.Count > 100)
				return element;

			for (var i = 0; i < element.Predicates.Count - 1; i++)
			{
				for (var j = i + 1; j < element.Predicates.Count; j++)
				{
					if (element.Predicates[i] is SqlSearchCondition search)
					{
						if (OptimizeSimilarForSearch(element.Predicates[j], search, out var newCondition, out var newPredicate))
						{
							return Optimize(element, newCondition, newPredicate, false);
						}
					}
					else if (element.Predicates[j] is SqlSearchCondition search2)
					{
						if (OptimizeSimilarForSearch(element.Predicates[i], search2, out var newCondition, out var newPredicate))
						{
							return Optimize(element, newCondition, newPredicate, true);
						}
					}
				}
			}

			return element;

			IQueryElement Optimize(SqlSearchCondition element, ISqlPredicate newCondition, ISqlPredicate? newPredicate, bool reverse)
			{
				if (newPredicate == null)
					return newCondition;

				IEnumerable<ISqlPredicate> newPredicates = reverse ? [newPredicate, newCondition] : [newCondition, newPredicate];

				if (GetVisitMode(element) == VisitMode.Transform)
				{
					var newElement = new SqlSearchCondition(element.IsOr, canBeUnknown: element.CanReturnUnknown, newPredicates);
					NotifyReplaced(newElement, element);
					return newElement;
				}

				element.Predicates.Clear();
				element.Predicates.AddRange(newPredicates);
				return Visit(element);
			}
		}

		public bool OptimizeSimilarForSearch(ISqlPredicate predicate, SqlSearchCondition searchCondition, out ISqlPredicate newCondition, out ISqlPredicate? newPredicate)
		{
			newCondition = searchCondition;
			newPredicate = predicate;

			// We should not slowdown translation with dynamically generate conditions.
			if (searchCondition.Predicates.Count > 100)
				return false;

			var predicateCodes = _similarityMerger.GetSimilarityCodes(predicate).ToArray();

			if (predicateCodes.Length == 0)
				return false;

			var predicatesToCompare = searchCondition.Predicates
				.Where(p => _similarityMerger.GetSimilarityCodes(p).Any(code => predicateCodes.Contains(code)))
				.ToList();

			if (predicatesToCompare.Count == 0)
				return false;

			List<ISqlPredicate>? newPredicates = null;
			var visitedPredicates = new HashSet<ISqlPredicate>(ObjectReferenceEqualityComparer<ISqlPredicate>.Default);

			var isOptimized = false;

			for (var i = 0; i < predicatesToCompare.Count; i++)
			{
				var conditionPredicate = predicatesToCompare[i];

				if (visitedPredicates.Contains(conditionPredicate))
					continue;

				if (_similarityMerger.TryMerge(_nullabilityContext, _isInsidePredicate, predicate, conditionPredicate, !searchCondition.IsOr, out var mergedSingle, out var mergedConditional))
				{
					isOptimized = true;

					if (!ReferenceEquals(mergedConditional, conditionPredicate))
					{
						var predicatesList = searchCondition.Predicates;
						if (GetVisitMode(searchCondition) == VisitMode.Transform)
						{
							newPredicates  ??= [.. searchCondition.Predicates];
							predicatesList =   newPredicates;
						}

						var ixd = predicatesList.IndexOf(conditionPredicate);

						if (mergedConditional == null)
							predicatesList.RemoveAt(ixd);
						else
							predicatesList[ixd] = mergedConditional;

						visitedPredicates.Add(conditionPredicate);
					}

					// is it even needed?
					if (!ReferenceEquals(mergedSingle, mergedConditional))
					{
						newPredicate = mergedSingle;
						break;
					}
				}
			}

			if (newPredicates != null)
			{
				newCondition = new SqlSearchCondition(searchCondition.IsOr, canBeUnknown: searchCondition.CanReturnUnknown, newPredicates);
				NotifyReplaced(newCondition, searchCondition);
				return true;
			}

			return isOptimized;
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
			var saveNullabilityContext = _nullabilityContext;
			_nullabilityContext = _nullabilityContext.WithQuery(selectQuery);

			var result = base.VisitSqlQuery(selectQuery);

			_nullabilityContext = saveNullabilityContext;

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

			var saveInsidePredicate = _isInsidePredicate;
			var saveAllow     = _allowOptimize;

			_isInsidePredicate    = true;
			_allowOptimize        = predicate.Predicate;
			var newInnerPredicate = (ISqlPredicate)Visit(predicate.Predicate);
			_isInsidePredicate    = saveInsidePredicate;
			_allowOptimize        = saveAllow;

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
				return new SqlValue(QueryHelper.GetDbDataType(element, MappingSchema), evaluatedValue);

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
								var elementType = QueryHelper.GetDbDataType(element, MappingSchema);
								var expr2Type   = QueryHelper.GetDbDataType(element.Expr2, MappingSchema);
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

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, MappingSchema), element.Precedence);
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

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, MappingSchema), element.Precedence);
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
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 + i2, element, MappingSchema);
						if (value1 is string || value2 is string) return QueryHelper.CreateSqlValue(FormattableString.Invariant($"{value1}{value2}"), element, MappingSchema);
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

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, MappingSchema), element.Precedence);
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

										return new SqlBinaryExpression(element.SystemType, be1.Expr1, oper, QueryHelper.CreateSqlValue(value, element, MappingSchema), element.Precedence);
									}
								}

								break;
							}
						}
					}

					if (v2 && TryEvaluateNoParameters(element.Expr1, out var value1))
					{
						if (value1 is int i1 && value2 is int i2) return QueryHelper.CreateSqlValue(i1 - i2, element, MappingSchema);
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
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element, MappingSchema);
							case int i when i == 1 : return element.Expr2;
							case int i when
								element.Expr2    is SqlBinaryExpression be2 &&
								be2.Operation == "*"                   &&
								TryEvaluateNoParameters(be2.Expr1, out var be2v1)  &&
								be2v1 is int bi :
							{
								return new SqlBinaryExpression(be2.SystemType, QueryHelper.CreateSqlValue(i * bi, element, MappingSchema), "*", be2.Expr2);
							}
						}
					}

					var v2 = TryEvaluateNoParameters(element.Expr2, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int i when i == 0 : return QueryHelper.CreateSqlValue(0, element, MappingSchema);
							case int i when i == 1 : return element.Expr1;
						}
					}

					if (v1 && v2)
					{
						switch (value1)
						{
							case int    i1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(i1 * i2, element, MappingSchema);
							case int    i1 when value2 is double d2 : return QueryHelper.CreateSqlValue(i1 * d2, element, MappingSchema);
							case double d1 when value2 is int    i2 : return QueryHelper.CreateSqlValue(d1 * i2, element, MappingSchema);
							case double d1 when value2 is double d2 : return QueryHelper.CreateSqlValue(d1 * d2, element, MappingSchema);
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
				var from = element.FromType?.Type ?? QueryHelper.GetDbDataType(element.Expression, MappingSchema);

				if (element.SystemType == typeof(object) || from.EqualsDbOnly(element.Type))
					return element.Expression;

				if (element.Expression is SqlCastExpression { IsMandatory: false } castOther)
				{
					var dbType = QueryHelper.GetDbDataType(castOther.Expression, MappingSchema);
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
				return QueryHelper.CreateSqlValue(value, QueryHelper.GetDbDataType(element, MappingSchema), element.Parameters);
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
					return new SqlFunction(function.Type, function.Name, func.Parameters[0]);
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

			if (!predicate.Expr1.CanBeNullableOrUnknown(_nullabilityContext, false))
			{
				//TODO: Exception for Row, find time to analyze why it's needed
				if (predicate.Expr1.ElementType != QueryElementType.SqlRow)
					return SqlPredicate.MakeBool(predicate.IsNot);
			}

			if (TryEvaluate(predicate.Expr1, out var value))
			{
				return SqlPredicate.MakeBool((value == null) != predicate.IsNot);
			}

			using (var reducer = ReduceIsNullExpressionVisitor.Pool.Allocate())
			{
				newPredicate = reducer.Value.Reduce(_nullabilityContext, predicate);
			}

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
			var saveInsidePredicate = _isInsidePredicate;
			_isInsidePredicate      = true;
			var newElement          = base.VisitExprExprPredicate(predicate);
			_isInsidePredicate      = saveInsidePredicate;

			if (!ReferenceEquals(newElement, predicate))
				return Visit(newElement);

			if (TryEvaluateNoParameters(predicate, out var value) && value is bool boolValue)
			{
				return SqlPredicate.MakeBool(boolValue);
			}

			if (_reducePredicates)
			{
				var reduced = predicate.Reduce(_nullabilityContext, EvaluationContext, _isInsidePredicate, DataOptions.LinqOptions);

				if (!ReferenceEquals(reduced, predicate))
				{
					return Visit(reduced);
				}
			}

			var expr = predicate;

			if (expr.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				if (expr.UnknownAsValue == null)
				{
					if (expr.Expr2 is ISqlPredicate expr2Predicate)
					{
						var boolValue1 = QueryHelper.GetBoolValue(expr.Expr1, EvaluationContext);
						if (boolValue1 != null)
						{
							var isNot       = boolValue1.Value != (expr.Operator == SqlPredicate.Operator.Equal);
							var transformed = expr2Predicate.MakeNot(isNot);

							return transformed;
						}
						else if (expr.Expr1 is not ISqlPredicate)
						{
							return new SqlPredicate.ExprExpr(new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.Expr(expr.Expr1)), expr.Operator, expr.Expr2, expr.UnknownAsValue);
						}
					}

					if (expr.Expr1 is ISqlPredicate expr1Predicate)
					{
						var boolValue2 = QueryHelper.GetBoolValue(expr.Expr2, EvaluationContext);
						if (boolValue2 != null)
						{
							var isNot       = boolValue2.Value != (expr.Operator == SqlPredicate.Operator.Equal);
							var transformed = expr1Predicate.MakeNot(isNot);
							return transformed;
						}
						else if (expr.Expr2 is not ISqlPredicate)
						{
							return new SqlPredicate.ExprExpr(expr.Expr1, expr.Operator, new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.Expr(expr.Expr2)), expr.UnknownAsValue);
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
					[]       => new SqlSearchCondition(false, canBeUnknown: null),
					[var p0] => new SqlSearchCondition(false, canBeUnknown: null, p0),
					_        => new SqlSearchCondition(false, canBeUnknown: null, element.SearchCondition),
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

			if (EvaluationContext.ParameterValues == null)
			{
				return predicate;
			}

			if (predicate.Values is [SqlParameter valuesParam] && EvaluationContext.ParameterValues!.TryGetValue(valuesParam, out var parameterValue))
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
				var otherEvaluated = TryEvaluateNoParameters(unwrappedValue, out var otherVal);
				var trueEvaluated  = TryEvaluateNoParameters(sqlConditionExpression.TrueValue, out var trueVal);
				var falseEvaluated = TryEvaluateNoParameters(sqlConditionExpression.FalseValue, out var falseVal);

				if (otherEvaluated && trueEvaluated && falseEvaluated
					&& !Equals(otherVal, trueVal) && !Equals(otherVal, falseVal))
				{
					if (op == SqlPredicate.Operator.Equal)
					{
						return SqlPredicate.False;
					}
					else if (op == SqlPredicate.Operator.NotEqual)
					{
						return SqlPredicate.True;
					}
				}

				if (!sqlConditionExpression.Condition.CanBeUnknown(_nullabilityContext, false))
				{
					if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
					{
						if (sqlConditionExpression.TrueValue.Equals(unwrappedValue) && falseEvaluated)
						{
							return sqlConditionExpression.Condition.MakeNot(isNot);
						}

						if (sqlConditionExpression.FalseValue.Equals(unwrappedValue) && trueEvaluated)
						{
							return sqlConditionExpression.Condition.MakeNot(!isNot);
						}
					}

					if (otherEvaluated)
					{
						var convert = false;

						if (trueEvaluated)
						{
							if ((trueVal != null || op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
								&& Equals(otherVal, trueVal))
							{
								if (ReduceOp(op))
								{
									var sc = new SqlSearchCondition(true)
									.Add(sqlConditionExpression.Condition)
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.FalseValue, op, valueExpression, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? op == SqlPredicate.Operator.NotEqual : null));

									return sc;
								}
								else
								{
									var sc = new SqlSearchCondition(false)
									.Add(sqlConditionExpression.Condition.MakeNot())
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.FalseValue, op, valueExpression, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? op == SqlPredicate.Operator.NotEqual : null));

									return sc;
								}
							}

							convert = true;
						}

						if (falseEvaluated)
						{
							if ((falseVal != null || op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
								&& Equals(otherVal, falseVal))
							{
								if (ReduceOp(op))
								{
									var sc = new SqlSearchCondition(true)
									.Add(sqlConditionExpression.Condition.MakeNot())
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.TrueValue, op, valueExpression, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? op == SqlPredicate.Operator.NotEqual : null));

									return sc;
								}
								else
								{
									var sc = new SqlSearchCondition(false)
									.Add(sqlConditionExpression.Condition)
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.TrueValue, op, valueExpression, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? op == SqlPredicate.Operator.NotEqual : null));

									return sc;
								}
							}

							convert = true;
						}

						if (convert)
						{
							var sc = new SqlSearchCondition(true)
							.AddAnd( sub =>
								sub
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.TrueValue, op, valueExpression, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? op == SqlPredicate.Operator.NotEqual : null))
									.Add(sqlConditionExpression.Condition)
							)
							.AddAnd( sub =>
								sub
									.Add(new SqlPredicate.ExprExpr(sqlConditionExpression.FalseValue, op, valueExpression, DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? op == SqlPredicate.Operator.NotEqual : null))
									.Add(sqlConditionExpression.Condition.MakeNot())
								);

							return sc;
						}

						static bool ReduceOp(SqlPredicate.Operator op)
						{
							// return A op A result
							return op switch
							{
								SqlPredicate.Operator.Equal => true,
								SqlPredicate.Operator.GreaterOrEqual => true,
								SqlPredicate.Operator.LessOrEqual => true,
								SqlPredicate.Operator.NotEqual => false,
								SqlPredicate.Operator.Greater => false,
								SqlPredicate.Operator.Less => false,
								_ => throw new InvalidOperationException($"Unexpected binary operator {op}")
							};
						}
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

					if ((sqlCaseExpression.ElseExpression == null || sqlCaseExpression.ElseExpression.TryEvaluateExpression(EvaluationContext, out elseValue))
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
									condition.Add(new SqlSearchCondition(true, canBeUnknown: null, notMatches).MakeNot());

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

							resultCondition.Add(new SqlSearchCondition(true, canBeUnknown: null, notMatches).MakeNot());
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

			if (_reducePredicates)
			{
				var reduced = isTrue.Reduce(_nullabilityContext, _isInsidePredicate);

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

					return ConvertStringCompare(compareTo1, current.Value);
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

					return ConvertStringCompare(compareTo2, current.Value);
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

			ISqlPredicate ConvertStringCompare(SqlCompareToExpression compare, SqlPredicate.Operator @operator)
			{
				var expr1Nullable = _nullabilityContext.CanBeNull(compare.Expression1);
				var expr2Nullable = _nullabilityContext.CanBeNull(compare.Expression2);

				var expr1IsNull = TryEvaluateNoParameters(compare.Expression1, out var result) && result is null;
				var expr2IsNull = TryEvaluateNoParameters(compare.Expression2, out     result) && result is null;

				ISqlPredicate? predicate = null;

				if (expr1IsNull && expr2IsNull)
				{
					return @operator is SqlPredicate.Operator.LessOrEqual or SqlPredicate.Operator.GreaterOrEqual or SqlPredicate.Operator.Equal
						? SqlPredicate.True
						: SqlPredicate.False;
				}
				else if (expr1IsNull)
				{
					if (@operator is SqlPredicate.Operator.Less)
						return SqlPredicate.True;
					if (@operator is SqlPredicate.Operator.GreaterOrEqual)
						return SqlPredicate.False;

					predicate = new SqlPredicate.IsNull(compare.Expression2, @operator is SqlPredicate.Operator.NotEqual or SqlPredicate.Operator.Greater);
				}
				else if (expr2IsNull)
				{
					if (@operator is SqlPredicate.Operator.Less)
						return SqlPredicate.False;
					if (@operator is SqlPredicate.Operator.GreaterOrEqual)
						return SqlPredicate.True;

					predicate = new SqlPredicate.IsNull(compare.Expression1, @operator is SqlPredicate.Operator.NotEqual or SqlPredicate.Operator.Greater);
				}

				if (predicate == null)
				{
					bool? unknownValue =  null;
					if (expr1Nullable && expr2Nullable)
					{
						// corrected by additional checks
						unknownValue = false;
					}
					else if (expr1Nullable)
					{
						unknownValue = @operator is SqlPredicate.Operator.Less or SqlPredicate.Operator.LessOrEqual;
					}
					else if (expr2Nullable)
					{
						unknownValue = @operator is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual;
					}

					predicate = new SqlPredicate.ExprExpr(compare.Expression1, @operator, compare.Expression2, unknownValue);
				}

				if (expr1Nullable && expr2Nullable)
				{
					predicate = @operator switch
					{
						SqlPredicate.Operator.Less           => new SqlSearchCondition(true, true, predicate, new SqlSearchCondition(false, false, new SqlPredicate.IsNull(compare.Expression1, false), new SqlPredicate.IsNull(compare.Expression2, true))),
						SqlPredicate.Operator.Greater        => new SqlSearchCondition(true, true, predicate, new SqlSearchCondition(false, false, new SqlPredicate.IsNull(compare.Expression1, true), new SqlPredicate.IsNull(compare.Expression2, false))),
						SqlPredicate.Operator.LessOrEqual    => new SqlSearchCondition(true, true, predicate, new SqlPredicate.IsNull(compare.Expression1, false)),
						SqlPredicate.Operator.GreaterOrEqual => new SqlSearchCondition(true, true, predicate, new SqlPredicate.IsNull(compare.Expression2, false)),
						_ => predicate
					};
				}

				return predicate;
			}
		}

		#endregion
	}
}
