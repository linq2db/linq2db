using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using static LinqToDB.Internal.Reflection.Methods.LinqToDB;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public sealed class SelectQueryOptimizerVisitor : SqlQueryVisitor
	{
		SqlProviderFlags  _providerFlags     = default!;
		DataOptions       _dataOptions       = default!;
		EvaluationContext _evaluationContext = default!;
		IQueryElement     _root              = default!;
		IQueryElement     _rootElement       = default!;
		IQueryElement[]   _dependencies      = default!;
		MappingSchema     _mappingSchema     = default!;
		SelectQuery?      _correcting;
		int               _version;
		bool              _removeWeakJoins;

		SelectQuery?     _parentSelect;
		SqlSetOperator?  _currentSetOperator;
		SelectQuery?     _applySelect;
		SelectQuery?     _inSubquery;
		bool             _isInRecursiveCte;
		CteClause?       _currentCteClause;
		SelectQuery?     _updateQuery;
		ISqlTableSource? _updateTable;

		readonly SqlQueryColumnNestingCorrector _columnNestingCorrector      = new();
		readonly SqlQueryOrderByOptimizer       _orderByOptimizer            = new();
		readonly MovingComplexityVisitor        _movingComplexityVisitor     = new();
		readonly SqlExpressionOptimizerVisitor  _expressionOptimizerVisitor  = new(true);
		readonly MovingOuterPredicateVisitor    _movingOuterPredicateVisitor = new();
		readonly SqlQueryColumnOptimizerVisitor _columnOptimizerVisitor      = new();

		public SelectQueryOptimizerVisitor() : base(VisitMode.Modify, null)
		{
		}

		public IQueryElement Optimize(
			IQueryElement          root,
			IQueryElement          rootElement,
			SqlProviderFlags       providerFlags,
			bool                   removeWeakJoins,
			DataOptions            dataOptions,
			MappingSchema          mappingSchema,
			EvaluationContext      evaluationContext,
			params IQueryElement[] dependencies)
		{
#if DEBUG
			if (root.ElementType == QueryElementType.SelectStatement)
			{

			}
#endif

			_providerFlags     = providerFlags;
			_removeWeakJoins   = removeWeakJoins;
			_dataOptions       = dataOptions;
			_mappingSchema     = mappingSchema;
			_evaluationContext = evaluationContext;
			_root              = root;
			_rootElement       = rootElement;
			_dependencies      = dependencies;
			_parentSelect      = default!;
			_applySelect       = default!;
			_inSubquery        = default!;
			_updateQuery       = default!;
			_updateTable       = default!;

			// OUTER APPLY Queries usually may have wrong nesting in WHERE clause.
			// Making it consistent in LINQ Translator is bad for performance and it is hard to implement task.
			// Function also detects that optimizations is needed
			//
			if (CorrectColumnsNesting())
			{
				do
				{
					ProcessElement(_root);

					_orderByOptimizer.OptimizeOrderBy(_root, _providerFlags, _columnNestingCorrector);
					if (!_orderByOptimizer.IsOptimized)
						break;

				} while (true);

				if (removeWeakJoins)
				{
					// It means that we fully optimize query
					_root = _columnOptimizerVisitor.OptimizeColumns(_root);

					// do it always, ignore dataOptions.LinqOptions.OptimizeJoins
					JoinsOptimizer.UnnestJoins(_root);

					// convert remaining nested joins to subqueries
					if (!_providerFlags.IsNestedJoinsSupported)
						JoinsOptimizer.UndoNestedJoins(_root);
				}
			}

			return _root;
		}

		bool CorrectColumnsNesting()
		{
			_columnNestingCorrector.CorrectColumnNesting(_root);

			return _columnNestingCorrector.HasSelectQuery;
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_providerFlags     = default!;
			_dataOptions       = default!;
			_mappingSchema     = default!;
			_evaluationContext = default!;
			_root              = default!;
			_rootElement       = default!;
			_dependencies      = default!;
			_parentSelect      = default!;
			_applySelect       = default!;
			_version           = default;
			_isInRecursiveCte  = false;
			_currentCteClause  = null;
			_updateQuery       = default;
			_updateTable       = default;

			_columnNestingCorrector.Cleanup();
			_orderByOptimizer.Cleanup();
			_columnOptimizerVisitor.Cleanup();
			_movingComplexityVisitor.Cleanup();
			_expressionOptimizerVisitor.Cleanup();
			_movingOuterPredicateVisitor.Cleanup();
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			++_version;
			return base.NotifyReplaced(newElement, oldElement);
		}

		protected internal override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			var saveQuery = _applySelect;

			_applySelect = element.JoinType switch
			{
				JoinType.CrossApply or JoinType.OuterApply => element.Table.Source as SelectQuery,
				_ => null,
			};

			var newElement = base.VisitSqlJoinedTable(element);

			_applySelect = saveQuery;

			return newElement;
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveSetOperatorCount  = selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0;
			var saveParent            = _parentSelect;

			_parentSelect      = selectQuery;

			if (saveParent == null)
			{
#if DEBUG
				var before = selectQuery.ToDebugString();
#endif
				// only once
				_expressionOptimizerVisitor.Optimize(_evaluationContext, NullabilityContext.GetContext(selectQuery), null, _dataOptions, _mappingSchema, selectQuery, visitQueries: true, reducePredicates: false);
			}

			var newQuery = (SelectQuery)base.VisitSqlQuery(selectQuery);

			if (_correcting == null)
			{
				_parentSelect = selectQuery;

				if (saveParent != null)
				{
					OptimizeColumns(selectQuery);
				}

				List<SelectQuery>? doNotRemoveQueries = null;

				do
				{
					var currentVersion = _version;
					var isModified     = false;

					if (OptimizeSubQueries(selectQuery, doNotRemoveQueries))
					{
						isModified = true;
					}

					if (OptimizeJoins(selectQuery, ref doNotRemoveQueries))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (ResolveWeakJoins(selectQuery))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (OptimizeJoinSubQueries(selectQuery))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (CorrectRecursiveCteJoins(selectQuery))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (CorrectLeftJoins(selectQuery))
					{
						isModified = true;
					}

					if (CorrectMultiTables(selectQuery))
					{
						isModified = true;
					}

					if (!_providerFlags.IsComplexJoinConditionSupported && OptimizeJoinConditions(selectQuery))
					{
						isModified = true;
					}

					if (FinalizeAndValidateInternal(selectQuery))
					{
						isModified = true;
					}

					if (currentVersion != _version)
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (!isModified)
					{
						break;
					}

				} while (true);

				if (saveParent == null)
				{
					// do expression optimization again
#if DEBUG
					var before = selectQuery.ToDebugString();
#endif
					CorrectEmptyInnerJoinsRecursive(selectQuery);

					_expressionOptimizerVisitor.Optimize(
						_evaluationContext,
						NullabilityContext.GetContext(selectQuery),
						transformationInfo: null,
						_dataOptions,
						_mappingSchema,
						selectQuery,
						visitQueries: true, 
						reducePredicates: false
					);
				}

				if (saveSetOperatorCount != (selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0))
				{
					// Do it again. Appended new SetOperators. For ensuring how it works check CteTests
					//
					newQuery = (SelectQuery)VisitSqlQuery(selectQuery);
				}

				_parentSelect = saveParent;
			}

			return newQuery;
		}

		protected internal override IQueryElement VisitSqlSetOperator(SqlSetOperator element)
		{
			var saveCurrent = _currentSetOperator;

			_currentSetOperator = element;

			var newElement = base.VisitSqlSetOperator(element);

			_currentSetOperator = saveCurrent;

			return newElement;
		}

		protected internal override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			var saveCurrent        = _currentSetOperator;

			_currentSetOperator = null;

			var newElement = base.VisitSqlTableSource(element);

			_currentSetOperator = saveCurrent;

			return newElement;
		}

		protected internal override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			var saveInsubquery = _inSubquery;

			_inSubquery = predicate.SubQuery;
			var newNode = base.VisitInSubQueryPredicate(predicate);
			_inSubquery = saveInsubquery;

			return newNode;
		}

		protected internal override IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
		{
			var newElement = (SqlOrderByClause)base.VisitSqlOrderByClause(element);

			for (int i = newElement.Items.Count - 1; i >= 0; i--)
			{
				var item = newElement.Items[i];
				if (QueryHelper.IsConstantFast(item.Expression))
					newElement.Items.RemoveAt(i);
			}

			return newElement;
		}

		protected internal override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			_updateQuery = element.SelectQuery;
			_updateTable = element.Update.Table as ISqlTableSource ?? element.Update.TableSource;
			var result = base.VisitSqlUpdateStatement(element);
			_updateQuery = null;
			_updateTable = null;
			return result;
		}

		protected internal override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			_updateQuery = element.SelectQuery;
			_updateTable = element.Update.Table as ISqlTableSource ?? element.Update.TableSource;
			var result = base.VisitSqlInsertOrUpdateStatement(element);
			_updateQuery = null;
			return result;
		}

		protected internal override IQueryElement VisitSqlNullabilityExpression(SqlNullabilityExpression element)
		{
			var sqlExpression = Visit(element.SqlExpression);
			if (sqlExpression is SelectQuery { GroupBy.IsEmpty: true } selectQuery)
			{
				var nullabilityContext = NullabilityContext.GetContext(selectQuery);
				if (selectQuery.Select.Columns.TrueForAll(c => QueryHelper.ContainsAggregationFunction(c.Expression) && !c.Expression.CanBeNullable(nullabilityContext)))
				{
					return sqlExpression;
				}
			}

			element.Modify((ISqlExpression)sqlExpression);

			return element;
		}

		bool OptimizeUnions(SelectQuery selectQuery)
		{
			var isModified = false;

			if (selectQuery.From.Tables is [{ Source: SelectQuery { HasSetOperators: true } mainSubquery }])
			{
				var isOk = true;

				if (!selectQuery.HasSetOperators)
				{
					isOk = !selectQuery.HasOrderBy && !selectQuery.HasWhere && !selectQuery.HasGroupBy && !selectQuery.Select.HasModifier;
					if (isOk)
					{
						if (_currentSetOperator != null)
						{
							isOk = _currentSetOperator.Operation == mainSubquery.SetOperators[0].Operation;
						}
					}
				}

				if (isOk && mainSubquery.Select.Columns.Count == selectQuery.Select.Columns.Count)
				{
					var newIndexes = new Dictionary<ISqlExpression, int>(Utils
						.ObjectReferenceEqualityComparer<ISqlExpression>
						.Default);

					for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
					{
						var scol = selectQuery.Select.Columns[i];

						if (!newIndexes.ContainsKey(scol.Expression))
							newIndexes[scol.Expression] = i;
					}

					var operation = selectQuery.HasSetOperators ? selectQuery.SetOperators[0].Operation : mainSubquery.SetOperators[0].Operation;

					if (mainSubquery.SetOperators.TrueForAll(so => so.Operation == operation))
					{
						if (CheckSetColumns(newIndexes, mainSubquery, operation))
						{
							UpdateSetIndexes(newIndexes, mainSubquery, operation);
							selectQuery.SetOperators.InsertRange(0, mainSubquery.SetOperators);
							mainSubquery.SetOperators.Clear();

							selectQuery.From.Tables[0].Source = mainSubquery;

							for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
							{
								var c = selectQuery.Select.Columns[i];
								c.Expression = mainSubquery.Select.Columns[i];
							}

							isModified = true;
						}
					}
				}
			}

			if (!selectQuery.HasSetOperators)
				return isModified;

			for (var index = 0; index < selectQuery.SetOperators.Count; index++)
			{
				var setOperator = selectQuery.SetOperators[index];

				if (setOperator.SelectQuery.From.Tables is [{ Source: SelectQuery { HasSetOperators: true } subQuery }])
				{
					if (subQuery.SetOperators.TrueForAll(so => so.Operation == setOperator.Operation))
					{
						var allColumns = setOperator.Operation != SetOperation.UnionAll;

						if (allColumns)
						{
							if (subQuery.Select.Columns.Count != selectQuery.Select.Columns.Count)
								continue;
						}

						var newIndexes = new Dictionary<ISqlExpression, int>(Utils.ObjectReferenceEqualityComparer<ISqlExpression>.Default);

						for (var i = 0; i < setOperator.SelectQuery.Select.Columns.Count; i++)
						{
							var scol = setOperator.SelectQuery.Select.Columns[i];

							if (!newIndexes.ContainsKey(scol.Expression))
								newIndexes[scol.Expression] = i;
						}

						if (!CheckSetColumns(newIndexes, subQuery, setOperator.Operation))
							continue;

						UpdateSetIndexes(newIndexes, subQuery, setOperator.Operation);

						setOperator.Modify(subQuery);
						selectQuery.SetOperators.InsertRange(index + 1, subQuery.SetOperators);
						subQuery.SetOperators.Clear();
						--index;

						isModified = true;
					}
				}
			}

			return isModified;
		}

		static void UpdateSetIndexes(Dictionary<ISqlExpression, int> newIndexes, SelectQuery setQuery, SetOperation setOperation)
		{
			if (setOperation == SetOperation.UnionAll)
			{
				for (var index = 0; index < setQuery.Select.Columns.Count; index++)
				{
					var column = setQuery.Select.Columns[index];
					if (!newIndexes.ContainsKey(column))
					{
						setQuery.Select.Columns.RemoveAt(index);

						foreach (var op in setQuery.SetOperators)
						{
							if (index < op.SelectQuery.Select.Columns.Count)
								op.SelectQuery.Select.Columns.RemoveAt(index);
						}

						--index;
					}
				}
			}

			foreach (var pair in newIndexes.OrderBy(x => x.Value))
			{
				var currentIndex = setQuery.Select.Columns.FindIndex(c => ReferenceEquals(c, pair.Key));
				if (currentIndex < 0)
				{
					if (setOperation != SetOperation.UnionAll)
						throw new InvalidOperationException();

					foreach (var op in setQuery.SetOperators)
					{
						op.SelectQuery.Select.Columns.Insert(pair.Value, new SqlColumn(op.SelectQuery, pair.Key));
					}

					continue;
				}

				var newIndex = pair.Value;
				if (currentIndex != newIndex)
				{
					var uc = setQuery.Select.Columns[currentIndex];
					setQuery.Select.Columns.RemoveAt(currentIndex);
					setQuery.Select.Select.Columns.Insert(newIndex, uc);

					// change indexes in SetOperators
					foreach (var op in setQuery.SetOperators)
					{
						var column = op.SelectQuery.Select.Columns[currentIndex];
						op.SelectQuery.Select.Columns.RemoveAt(currentIndex);
						op.SelectQuery.Select.Columns.Insert(newIndex, column);
					}
				}
			}
		}

		static bool CheckSetColumns(Dictionary<ISqlExpression, int> newIndexes, SelectQuery setQuery, SetOperation setOperation)
		{
			foreach (var pair in newIndexes.OrderBy(x => x.Value))
			{
				var currentIndex = setQuery.Select.Columns.FindIndex(c => ReferenceEquals(c, pair.Key));
				if (currentIndex < 0)
				{
					if (setOperation != SetOperation.UnionAll)
						return false;

					if (!QueryHelper.IsConstantFast(pair.Key))
						return false;
				}
			}

			return true;
		}

		bool FinalizeAndValidateInternal(SelectQuery selectQuery)
		{
			var isModified = false;

			if (OptimizeGroupBy(selectQuery))
				isModified = true;

			if (OptimizeUnions(selectQuery))
				isModified = true;

			if (OptimizeDistinct(selectQuery))
				isModified = true;

			if (CorrectColumns(selectQuery))
				isModified = true;

			return isModified;
		}

		static bool IsGroupingQueryCanBeOptimized(SelectQuery selectQuery)
		{
			if (selectQuery.HasHaving)
				return false;

			if (selectQuery.GroupBy.GroupingType != GroupingType.Default)
				return false;

			if (QueryHelper.ContainsAggregationOrWindowFunction(selectQuery.Select))
				return false;

			if (selectQuery.HasWhere && QueryHelper.ContainsAggregationOrWindowFunction(selectQuery.Where.SearchCondition))
				return false;

			if (selectQuery.HasOrderBy && QueryHelper.ContainsAggregationOrWindowFunction(selectQuery.OrderBy))
				return false;

			if (selectQuery.GroupBy.Items.Exists(i => i is SqlGroupingSet))
				return false;

			return true;
		}

		static bool ContainsUniqueKey(IEnumerable<ISqlExpression> expressions, List<IList<ISqlExpression>> keys)
		{
			if (keys.Count == 0)
				return false;

			var expressionsSet = new HashSet<ISqlExpression>(expressions);

			foreach (var key in keys)
			{
				var foundUnique = true;
				foreach (var expr in key)
				{
					if (!expressionsSet.Contains(expr))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					return true;

				foundUnique = true;
				foreach (var expr in key)
				{
					var underlyingField = QueryHelper.GetUnderlyingField(expr);
					if (underlyingField == null || !expressionsSet.Contains(underlyingField))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					return true;
			}

			return false;
		}

		bool OptimizeGroupBy(SelectQuery selectQuery)
		{
			var isModified = false;

			if (selectQuery.HasGroupBy)
			{
				// Remove constants.
				//
				for (int i = selectQuery.GroupBy.Items.Count - 1; i >= 0; i--)
				{
					var groupByItem = selectQuery.GroupBy.Items[i];
					if (QueryHelper.IsConstantFast(groupByItem))
					{
						selectQuery.GroupBy.Items.RemoveAt(i);
						isModified = true;
					}
				}

				selectQuery.GroupBy.Items.RemoveDuplicates(item => item);
			}

			if (selectQuery.HasGroupBy)
			{
				// Check if we can remove GROUP BY entirely when there are no aggregations
				if (IsGroupingQueryCanBeOptimized(selectQuery))
				{
					// Check if query is limited to one record
					if (IsLimitedToOneRecord(selectQuery))
					{
						selectQuery.GroupBy.Items.Clear();
						isModified = true;
					}
					else if (!IsComplexQuery(selectQuery, true) && selectQuery.From.Tables.Count > 0)
					{
						// Check if we're grouping by unique keys
						var keys = new List<IList<ISqlExpression>>();

						QueryHelper.CollectUniqueKeys(selectQuery, includeDistinctAndGrouping: false, keys);
						var table = selectQuery.From.Tables[0];
						QueryHelper.CollectUniqueKeys(table, keys);

						if (ContainsUniqueKey(selectQuery.GroupBy.Items, keys))
						{
							// We have found that group by contains unique key, so we can remove group by
							selectQuery.GroupBy.Items.Clear();
							isModified = true;
						}
					}

					if (selectQuery.HasGroupBy)
					{
						var transformToDistinct = selectQuery.GroupBy.Items.Count == selectQuery.Select.Columns.Count
							&& selectQuery.GroupBy.Items.TrueForAll(gi => selectQuery.Select.Columns.Exists(c => c.Expression.Equals(gi)));

						if (transformToDistinct)
						{
							// All group by items are already in select columns, we can transform to distinct
							//
							selectQuery.GroupBy.Items.Clear();
							selectQuery.Select.OptimizeDistinct = true;
							selectQuery.Select.IsDistinct       = true;
							isModified                          = true;
						}
					}

				}
			}

			return isModified;
		}

		bool CorrectColumns(SelectQuery selectQuery)
		{
			var isModified = false;
			if (selectQuery.HasGroupBy && selectQuery.Select.Columns.Count == 0)
			{
				isModified = true;
				foreach (var item in selectQuery.GroupBy.Items)
				{
					selectQuery.Select.Add(item);
				}
			}

			return isModified;
		}

		void EnsureReferencesCorrected(SelectQuery selectQuery)
		{
			if (_correcting != null)
				throw new InvalidOperationException();

			_correcting = selectQuery;

			Visit(selectQuery);

			_correcting = null;
		}

		bool IsRemovableJoin(SqlJoinedTable join)
		{
			if (join.IsWeak)
				return true;

			if (join.JoinType == JoinType.Left)
			{
				if (join.Condition.IsFalse)
					return true;
			}

			if (join.JoinType is JoinType.Left or JoinType.OuterApply)
			{
				if ((join.Cardinality & SourceCardinality.One) != 0)
					return true;

				if (join.Table.Source is SelectQuery joinQuery)
				{
					if (joinQuery.Where.SearchCondition.IsFalse)
						return true;

					if (IsLimitedToOneRecord(joinQuery))
						return true;
				}
			}

			return false;
		}

		internal bool ResolveWeakJoins(SelectQuery selectQuery)
		{
			if (!_removeWeakJoins)
				return false;

			var isModified = false;

			foreach (var table in selectQuery.From.Tables)
			{
				for (var i = table.Joins.Count - 1; i >= 0; i--)
				{
					var join = table.Joins[i];

					if (IsRemovableJoin(join))
					{
						var sources = QueryHelper.EnumerateAccessibleSources(join.Table).ToList();
						var ignore  = new[] { join };
						if (QueryHelper.IsDependsOnSources(_rootElement, sources, ignore))
						{
							join.IsWeak = false;
							continue;
						}

						var moveNext = false;
						foreach (var d in _dependencies)
						{
							if (QueryHelper.IsDependsOnSources(d, sources, ignore))
							{
								join.IsWeak = false;
								moveNext    = true;
								break;
							}
						}

						if (moveNext)
							continue;

						table.Joins.RemoveAt(i);
						isModified = true;
					}
				}

				for (var i = table.Joins.Count - 1; i >= 0; i--)
				{
					var join = table.Joins[i];

					if (join is { Table.Source: SelectQuery subQuery, JoinType: JoinType.Left or JoinType.OuterApply })
					{
						var canRemoveEmptyJoin = join switch
						{
							{ JoinType: JoinType.Left, Condition.IsFalse: true } => true,
							{ JoinType: JoinType.OuterApply } when subQuery.Where.SearchCondition.IsFalse => true,
							_ => false,
						};

						if (canRemoveEmptyJoin)
						{
							// we can substitute all values by null

							foreach (var column in subQuery.Select.Columns)
							{
								var nullValue = column.Expression as SqlValue;
								if (nullValue is not { Value: null })
								{
									var dbType = QueryHelper.GetDbDataType(column.Expression, _mappingSchema);
									var type   = dbType.SystemType;
									if (!type.IsNullableOrReferenceType())
										type = type.AsNullable();
									nullValue = new SqlValue(dbType.WithSystemType(type), null);
								}

								NotifyReplaced(nullValue, column);
							}

							table.Joins.RemoveAt(i);
							isModified = true;
						}
					}
				}
			}

			return isModified;
		}

		static bool IsLimitedToOneRecord(SelectQuery query)
		{
			return query switch
			{
				{ Select.TakeValue: SqlValue { Value: 1 } } => true,

				{ HasGroupBy: false, Select.Columns: { Count: > 0 } columns } when columns.TrueForAll(c => QueryHelper.ContainsAggregationFunction(c.Expression)) =>
					true,

				{ From.Tables: [{ Source: SelectQuery subQuery }] } =>
					IsLimitedToOneRecord(subQuery),

				_ => false,
			};
		}

		static bool IsComplexQuery(SelectQuery query, bool ignoreGroupBy)
		{
			if (query.From.Tables.Count != 1)
			{
				return true;
			}

			if (!ignoreGroupBy && query.HasGroupBy)
			{
				return false;
			}

			var mainTable = query.From.Tables[0];

			if (mainTable.Joins.Count == 0)
			{
				return false;
			}

			foreach (var join in mainTable.Joins)
			{
				if (join.JoinType is not (JoinType.Inner or JoinType.Left) || join.Table.Joins.Count > 0)
				{
					return true;
				}
			}

			var mainKeys = new List<IList<ISqlExpression>>();
			QueryHelper.CollectUniqueKeys(mainTable.Source, includeDistinctAndGrouping: true, mainKeys);

			foreach (var join in mainTable.Joins)
			{
				if (join.Cardinality.HasFlag(SourceCardinality.One))
					continue;

				var joinKeys = new List<IList<ISqlExpression>>();
				QueryHelper.CollectUniqueKeys(join.Table.Source, includeDistinctAndGrouping: true, joinKeys);

				if (!IsUniqueCondition(mainKeys, joinKeys, join.Condition))
					return true;

			}

			return false;
		}

		static bool IsUniqueCondition(List<IList<ISqlExpression>> keys1, List<IList<ISqlExpression>> keys2, SqlSearchCondition searchCondition)
		{
			if (searchCondition.IsOr)
				return false;

			if (keys1.Count == 0 || keys2.Count == 0)
				return false;

			// Collect all equality pairs (expr1 = expr2) from (AND-only) search condition recursively.
			var equalityPairs = new List<(ISqlExpression Left, ISqlExpression Right)>();

			void CollectCondition(SqlSearchCondition condition)
			{
				if (condition.IsOr)
					return; // OR parts cannot produce unique join detection safely

				foreach (var p in condition.Predicates)
				{
					switch (p)
					{
						case SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } ee:
						{
							var left  = QueryHelper.UnwrapNullablity(ee.Expr1);
							var right = QueryHelper.UnwrapNullablity(ee.Expr2);
							if (!ReferenceEquals(left, right))
								equalityPairs.Add((left, right));
							break;
						}
						case SqlSearchCondition nested:
							CollectCondition(nested);
							break;
					}
				}
			}

			CollectCondition(searchCondition);

			if (equalityPairs.Count == 0)
				return false;

			bool SameExpr(ISqlExpression a, ISqlExpression b) =>
				ReferenceEquals(a, b) || QueryHelper.SameWithoutNullablity(a, b);

			// Helper to check that for two keys we have equality predicates covering all columns.
			bool CoversKey(IList<ISqlExpression> keyA, IList<ISqlExpression> keyB)
			{
				if (keyA.Count != keyB.Count)
					return false;

				// Each element from keyA must be paired with exactly one element from keyB via equality
				int matched = 0;

				for (int i = 0; i < keyA.Count; i++)
				{
					var aExpr = keyA[i];
					bool found = false;
					for (int j = 0; j < keyB.Count; j++)
					{
						var bExpr = keyB[j];

						// Search equality pair either direction
						if (equalityPairs.Exists(ep => (SameExpr(ep.Left, aExpr) && SameExpr(ep.Right, bExpr)) ||
													(SameExpr(ep.Left, bExpr) && SameExpr(ep.Right, aExpr))))
						{
							found = true;
							break;
						}
					}

					if (!found)
						return false;

					matched++;
				}

				return matched == keyA.Count;
			}

			// We consider join unique if ANY unique key from side1 fully matches ANY unique key from side2.
			foreach (var k1 in keys1)
			{
				foreach (var k2 in keys2)
				{
					if (CoversKey(k1, k2))
						return true;
				}
			}

			return false;
		}

		bool OptimizeDistinct(SelectQuery selectQuery)
		{
			if (!selectQuery.Select.IsDistinct || !selectQuery.Select.OptimizeDistinct)
				return false;

			if (IsComplexQuery(selectQuery, false))
				return false;

			if (IsLimitedToOneRecord(selectQuery))
			{
				// we can simplify query if we take only one record
				selectQuery.Select.IsDistinct = false;
				return true;
			}

			if (selectQuery.HasGroupBy)
			{
				if (selectQuery.GroupBy.Items.TrueForAll(gi => selectQuery.Select.Columns.Exists(c => c.Expression.Equals(gi))))
				{
					selectQuery.GroupBy.Items.Clear();
					return true;
				}
			}

			var table = selectQuery.From.Tables[0];

			var keys = new List<IList<ISqlExpression>>();

			QueryHelper.CollectUniqueKeys(selectQuery, includeDistinctAndGrouping: false, keys);
			QueryHelper.CollectUniqueKeys(table,       keys);

			if (ContainsUniqueKey(selectQuery.Select.Columns.Select(static c => c.Expression), keys))
			{
				// We have found that distinct columns has unique key, so we can remove distinct
				selectQuery.Select.IsDistinct = false;
				return true;
			}

			return false;
	}

		static void ApplySubsequentOrder(SelectQuery mainQuery, SelectQuery subQuery)
		{
			if (subQuery.HasOrderBy)
			{
				foreach (var item in subQuery.OrderBy.Items)
				{
					mainQuery.OrderBy.Expr(item.Expression, item.IsDescending, item.IsPositioned);
				}
			}
		}

		static void ApplySubQueryExtensions(SelectQuery mainQuery, SelectQuery subQuery)
		{
			if (subQuery.SqlQueryExtensions is not null)
				(mainQuery.SqlQueryExtensions ??= new()).AddRange(subQuery.SqlQueryExtensions);
		}

		static JoinType ConvertApplyJoinType(JoinType joinType)
		{
			var newJoinType = joinType switch
			{
				JoinType.CrossApply => JoinType.Inner,
				JoinType.OuterApply => JoinType.Left,
				JoinType.FullApply  => JoinType.Full,
				JoinType.RightApply => JoinType.Right,
				_ => throw new InvalidOperationException($"Invalid APPLY Join: {joinType}"),
			};

			return newJoinType;
		}

		bool OptimizeApplyJoin(SqlJoinedTable joinTable, bool doNotEmulate)
		{
			var joinSource = joinTable.Table;

			var accessible = QueryHelper.EnumerateAccessibleSources(joinTable.Table).ToList();

			var optimized = false;

			if (!QueryHelper.IsDependsOnOuterSources(joinSource.Source))
			{
				var newJoinType = ConvertApplyJoinType(joinTable.JoinType);

				joinTable.JoinType = newJoinType;
				optimized          = true;
				return optimized;
			}

			if (joinSource.Source.ElementType == QueryElementType.SqlQuery)
			{
				var sql   = (SelectQuery)joinSource.Source;

				var isAgg = sql.Select.Columns.Exists(static c => QueryHelper.IsAggregationOrWindowExpression(c.Expression));

				var isApplySupported = _providerFlags.IsApplyJoinSupported;

				if (sql.Select.HasModifier || sql.HasGroupBy)
				{
					if (isApplySupported)
						return optimized;
				}

				if (isApplySupported && isAgg)
					return optimized;

				if (doNotEmulate)
					return optimized;

				var skipValue = sql.Select.SkipValue;
				var takeValue = sql.Select.TakeValue;

				if (sql.Select.TakeHints != null)
				{
					return optimized;
				}

				ISqlExpression?       rnExpression = null;
				List<ISqlExpression>? partitionBy  = null;

				if (skipValue != null || takeValue != null)
				{
					if (!_providerFlags.IsWindowFunctionsSupported)
						return optimized;

					if (doNotEmulate)
						return optimized;

					var found   = new HashSet<ISqlExpression>();

					sql.Where.VisitAll(1, (ctx, e) =>
					{
						if (e is SqlPredicate.ExprExpr exprExpr)
						{
							var expr1 = SequenceHelper.UnwrapNullability(exprExpr.Expr1);
							var expr2 = SequenceHelper.UnwrapNullability(exprExpr.Expr2);

							var depended1 = QueryHelper.IsDependsOnOuterSources(expr1, currentSources : accessible);
							var depended2 = QueryHelper.IsDependsOnOuterSources(expr2, currentSources : accessible);

							if (depended1 && !depended2)
							{
								found.Add(expr2);
							}
							else if (!depended1 && depended2)
							{
								found.Add(expr1);
							}
						}
					});

					if (sql.Select.IsDistinct)
					{
						if (found.Count == 0)
						{
							found.AddRange(sql.Select.Columns.Select(c => c.Expression));
						}
						else
						{
							if (!sql.Select.Columns.TrueForAll(c => found.Contains(c.Expression)))
								return optimized;
						}
					}

					if (found.Count > 0)
					{
						partitionBy = found.ToList();
					}

					var orderByItems = sql.OrderBy.Items.ToList();

					if (!sql.HasOrderBy)
					{
						if (partitionBy != null)
						{
							orderByItems.Add(new SqlOrderByItem(partitionBy[0], false, false));
						}
						else if (!_providerFlags.IsRowNumberWithoutOrderBySupported)
						{
							if (sql.Select.Columns.Count == 0)
							{
								throw new InvalidOperationException("OrderBy not specified for limited recordset.");
							}

							orderByItems.Add(new SqlOrderByItem(sql.Select.Columns[0].Expression, false, false));
						}
					}

					var orderItems = orderByItems.Select(o => new SqlWindowOrderItem(o.Expression, o.IsDescending, Sql.NullsPosition.None));

					var longType = _mappingSchema.GetDbDataType(typeof(long));
					rnExpression = new SqlExtendedFunction(longType, "ROW_NUMBER", [], [], partitionBy: partitionBy, orderBy: orderItems);
				}

				var whereToIgnore = new List<IQueryElement> { sql.Where, sql.Select };

				if (joinTable.JoinType == JoinType.CrossApply)
				{
					// add join conditions
					// Check SelectManyTests.Basic9 for Access
					foreach (var join in sql.From.Tables.SelectMany(t => t.Joins))
					{
						if (join.JoinType == JoinType.Inner && join.Table.Source is SqlTable)
							whereToIgnore.Add(join.Condition);
						else
							break;
					}
				}

				// we cannot optimize apply because reference to parent sources are used inside the query
				if (QueryHelper.IsDependsOnOuterSources(sql, whereToIgnore))
					return optimized;

				var searchCondition = new List<ISqlPredicate>();

				var predicates = sql.Where.SearchCondition.Predicates;

				if (predicates.Count > 0)
				{
					List<ISqlPredicate>? toRemove = null;
					for (var i = predicates.Count - 1; i >= 0; i--)
					{
						var predicate = predicates[i];

						var contains = QueryHelper.IsDependsOnOuterSources(predicate, currentSources: accessible);

						if (contains)
						{
							if (rnExpression != null)
							{
								// we can only optimize equals
								if (predicate is not (SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } or SqlPredicate.IsNull))
								{
									return optimized;
								}
							}

							if (sql.HasGroupBy)
							{
								// we can only optimize SqlPredicate.ExprExpr
								if (predicate is not SqlPredicate.ExprExpr expExpr)
								{
									return optimized;
								}

								// check that used key in grouping
								if (!sql.GroupBy.Items.Exists(gi => QueryHelper.SameWithoutNullablity(gi, expExpr.Expr1) || QueryHelper.SameWithoutNullablity(gi, expExpr.Expr2)))
								{
									return optimized;
								}
							}
							else if (isAgg)
							{
								return optimized;
							}

							if (sql.Select.IsDistinct)
							{
								// we can only optimize SqlPredicate.ExprExpr
								if (predicate is not SqlPredicate.ExprExpr expExpr)
								{
									return optimized;
								}

								// check that used key in distinct
								if (!sql.Select.Columns.Exists(c => QueryHelper.SameWithoutNullablity(c.Expression, expExpr.Expr1) || QueryHelper.SameWithoutNullablity(c.Expression, expExpr.Expr2)))
								{
									return optimized;
								}
							}

							toRemove ??= new List<ISqlPredicate>();
							toRemove.Add(predicate);
						}
					}

					if (toRemove != null)
					{
						foreach (var predicate in toRemove)
						{
							searchCondition.Insert(0, predicate);
							predicates.Remove(predicate);
						}
					}
				}

				if (rnExpression != null)
				{
					// processing ROW_NUMBER

					var isLimitedToOneRecord = sql.IsLimitedToOneRecord();

					sql.Select.SkipValue = null;
					sql.Select.TakeValue = null;

					var rnColumn = sql.Select.AddNewColumn(rnExpression);
					rnColumn.RawAlias = "rn";

					// Remove order by items, they are not needed anymore
					if (isLimitedToOneRecord)
						sql.OrderBy.Items.Clear();

					if (skipValue != null)
					{
						searchCondition.Add(new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.Greater, skipValue, null));

						if (takeValue != null)
						{
							searchCondition.Add(new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.LessOrEqual, new SqlBinaryExpression(skipValue.SystemType!, skipValue, "+", takeValue), null));
						}
					}
					else if (takeValue != null)
					{
						if (isLimitedToOneRecord)
							searchCondition.Add(new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.Equal, takeValue, null));
						else
							searchCondition.Add(new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.LessOrEqual, takeValue, null));
					}
					else if (sql.Select.IsDistinct)
					{
						sql.Select.IsDistinct = false;
						searchCondition.Add(new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.Equal, new SqlValue(1), null));
					}
				}

				var toCheck = QueryHelper.EnumerateAccessibleSources(sql).ToList();

				for (int i = 0; i < searchCondition.Count; i++)
				{
					var predicate = searchCondition[i];

					var newPredicate = _movingOuterPredicateVisitor.CorrectReferences(sql, toCheck, predicate);

					searchCondition[i] = newPredicate;
				}

				var newJoinType = ConvertApplyJoinType(joinTable.JoinType);

				joinTable.JoinType = newJoinType;
				joinTable.Condition.Predicates.AddRange(searchCondition);

				optimized = true;
			}

			return optimized;
		}

		bool IsColumnExpressionAllowedToMoveUp(SelectQuery parentQuery, NullabilityContext nullability, SqlColumn column, ISqlExpression columnExpression, bool ignoreWhere, bool inGrouping)
		{
			if (columnExpression.ElementType is QueryElementType.Column or QueryElementType.SqlRawSqlTable or QueryElementType.SqlField or QueryElementType.SqlValue or QueryElementType.SqlParameter)
			{
				return true;
			}

			var underlying = QueryHelper.UnwrapExpression(columnExpression, false);
			if (!ReferenceEquals(underlying, columnExpression))
			{
				return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, underlying, ignoreWhere, inGrouping);
			}

			if (underlying is SqlBinaryExpression binary)
			{
				if (QueryHelper.IsConstantFast(binary.Expr1))
				{
					return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, binary.Expr2, ignoreWhere, inGrouping);
				}

				if (QueryHelper.IsConstantFast(binary.Expr2))
				{
					return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, binary.Expr1, ignoreWhere, inGrouping);
				}
			}
			else if (underlying is SqlCastExpression castExpression)
			{
				return IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, castExpression.Expression, ignoreWhere, inGrouping);
			}

			var allowed = _movingComplexityVisitor.IsAllowedToMove(_providerFlags, column, parent : parentQuery,
				// Elements which should be ignored while searching for usage
				column.Parent,
				_applySelect == parentQuery ? parentQuery.Where : null,
				!inGrouping && _applySelect == parentQuery ? parentQuery.Select : null,
				ignoreWhere ? parentQuery.Where : null
			);

			return allowed;
		}

		bool MoveSubQueryUp(SelectQuery parentQuery, SqlTableSource tableSource)
		{
			if (tableSource.Source is not SelectQuery subQuery)
				return false;

			if (subQuery.DoNotRemove)
				return false;

			if (subQuery.From.Tables.Count == 0 && !subQuery.HasSetOperators)
			{
				// optimized in level up function
				return false;
			}

			if (!IsMovingUpValid(parentQuery, tableSource, subQuery, out var havingDetected))
			{
				return false;
			}

			// -------------------------------------------
			// Actual modification starts from this point
			//

			if (subQuery.HasSetOperators)
			{
				var newIndexes = new Dictionary<ISqlExpression, int>(Utils.ObjectReferenceEqualityComparer<ISqlExpression>.Default);

				if (parentQuery.Select.Columns.Count == 0)
				{
					for (var i = 0; i < subQuery.Select.Columns.Count; i++)
					{
						var scol = subQuery.Select.Columns[i];
						newIndexes[scol] = i;
					}
				}
				else
				{
					for (var i = 0; i < parentQuery.Select.Columns.Count; i++)
					{
						var scol = parentQuery.Select.Columns[i];

						if (!newIndexes.ContainsKey(scol.Expression))
							newIndexes[scol.Expression] = i;
					}
				}

				var operation = subQuery.SetOperators[0].Operation;

				if (!CheckSetColumns(newIndexes, subQuery, operation))
					return false;

				UpdateSetIndexes(newIndexes, subQuery, operation);

				parentQuery.SetOperators.InsertRange(0, subQuery.SetOperators);
				subQuery.SetOperators.Clear();
			}

			parentQuery.QueryName ??= subQuery.QueryName;

			if (subQuery.HasGroupBy)
			{
				parentQuery.GroupBy.Items.InsertRange(0, subQuery.GroupBy.Items);
				parentQuery.GroupBy.GroupingType = subQuery.GroupBy.GroupingType;
			}

			if (havingDetected?.Count > 0)
			{
				// move Where to Having
				parentQuery.Having.SearchCondition = QueryHelper.MergeConditions(parentQuery.Having.SearchCondition, parentQuery.Where.SearchCondition);
				parentQuery.Where.SearchCondition.Predicates.Clear();
			}

			if (subQuery.HasWhere)
			{
				parentQuery.Where.SearchCondition = QueryHelper.MergeConditions(parentQuery.Where.SearchCondition, subQuery.Where.SearchCondition);
			}

			if (subQuery.HasHaving)
			{
				parentQuery.Having.SearchCondition = QueryHelper.MergeConditions(parentQuery.Having.SearchCondition, subQuery.Having.SearchCondition);
			}

			if (subQuery.Select.IsDistinct)
			{
				parentQuery.Select.OptimizeDistinct = parentQuery.Select.OptimizeDistinct || subQuery.Select.OptimizeDistinct;
				parentQuery.Select.IsDistinct       = true;
			}

			if (subQuery.Select.TakeValue != null)
			{
				parentQuery.Select.Take(subQuery.Select.TakeValue, subQuery.Select.TakeHints);
			}

			if (subQuery.Select.SkipValue != null)
			{
				parentQuery.Select.SkipValue = subQuery.Select.SkipValue;
			}

			foreach (var column in subQuery.Select.Columns)
			{
				// populating aliases
				if (column.RawAlias != null && column.Expression is SqlColumn exprColumn)
				{
					exprColumn.RawAlias = column.RawAlias;
				}

				NotifyReplaced(column.Expression, column);
			}

			if (parentQuery.Select.Columns.Count == 0 && (subQuery.Select.IsDistinct || parentQuery.HasSetOperators))
			{
				foreach (var column in subQuery.Select.Columns)
				{
					parentQuery.Select.AddNew(column.Expression);
				}
			}

			// First table processing
			if (subQuery.From.Tables.Count > 0)
			{
				var subQueryTableSource = subQuery.From.Tables[0];

				NotifyReplaced(subQueryTableSource.All, subQuery.All);

				if (subQueryTableSource.Joins.Count > 0)
					tableSource.Joins.InsertRange(0, subQueryTableSource.Joins);

				tableSource.Source = subQueryTableSource.Source;

				if (subQueryTableSource.HasUniqueKeys)
				{
					tableSource.UniqueKeys.AddRange(subQueryTableSource.UniqueKeys);
				}

				if (subQuery.HasUniqueKeys)
				{
					tableSource.UniqueKeys.AddRange(subQuery.UniqueKeys);
				}
			}

			if (subQuery.From.Tables.Count > 1)
			{
				var idx = parentQuery.From.Tables.IndexOf(tableSource);
				for (var i = subQuery.From.Tables.Count - 1; i >= 1; i--)
				{
					var subQueryTableSource = subQuery.From.Tables[i];
					parentQuery.From.Tables.Insert(idx + 1, subQueryTableSource);
				}
			}

			ApplySubQueryExtensions(parentQuery, subQuery);

			if (subQuery.HasOrderBy)
			{
				ApplySubsequentOrder(parentQuery, subQuery);
			}

			return true;
		}

		bool IsMovingUpValid(SelectQuery parentQuery, SqlTableSource tableSource, SelectQuery subQuery, out HashSet<ISqlPredicate>? havingDetected)
		{
			havingDetected = null;

			if (subQuery.IsSimple && parentQuery.IsSimple)
			{
				if (parentQuery.Select.Columns.TrueForAll(c => c.Expression is SqlColumn))
				{
					// shortcut
					return true;
				}
			}

			if (subQuery.From.Tables.Count > 1)
			{
				if (!_providerFlags.IsMultiTablesSupportsJoins)
				{
					if (QueryHelper.EnumerateJoins(parentQuery).Any())
					{
						// do not allow moving subquery with joins to multitable parent query
						return false;
					}
				}
			}

			// Trying to do not mix query hints
			if (subQuery.SqlQueryExtensions?.Count > 0)
			{
				if (tableSource.Joins.Count > 0 || parentQuery.From.Tables.Count > 1)
					return false;
			}

			if (parentQuery.HasOrderBy && !_providerFlags.IsOrderByAggregateFunctionSupported)
			{
				if (
					parentQuery.OrderBy.Items.Select(o => o.Expression).Any(e =>
					{
						if (QueryHelper.UnwrapNullablity(e) is SqlColumn column)
						{
							if (column.Parent == subQuery)
								return QueryHelper.ContainsAggregationFunction(column.Expression);
						}

						return false;
					})
				)
				{
					// not allowed to move to parent if it has aggregates
					return false;
				}
			}

			if (parentQuery.HasGroupBy)
			{
				if (subQuery.HasGroupBy)
					return false;

				if (parentQuery.Select.Columns.Count == 0)
					return false;

				// Check that all grouping columns are simple
				if (
					parentQuery.GroupBy.EnumItems().Any(gi =>
					{
						if (gi is not SqlColumn sc)
							return true;

						if (QueryHelper.UnwrapNullablity(sc.Expression) is not (SqlColumn or SqlField or SqlParameter or SqlValue))
							return true;

						return false;
					})
				)
				{
					return false;
				}
			}

			var nullability = NullabilityContext.GetContext(parentQuery);

			if (subQuery.HasOrderBy)
			{
				if (parentQuery.HasGroupBy || parentQuery.IsDistinct || QueryHelper.ContainsAggregationOrWindowFunction(parentQuery.Select))
				{
					return false;
				}
			}

			if (subQuery.HasOrderBy)
			{
				if (QueryHelper.IsAggregationQuery(parentQuery, out var needsOrderBy) && needsOrderBy)
					return false;

				if (parentQuery.IsDistinct)
				{
					// Check that all order by columns are in select list
					foreach (var ob in subQuery.OrderBy.Items)
					{
						if (!parentQuery.Select.Columns.Exists(c => QueryHelper.SameWithoutNullablity(c.Expression, ob.Expression)))
						{
							return false;
						}
					}
				}
			}

			// Check columns
			//

			foreach (var parentColumn in parentQuery.Select.Columns)
			{
				var containsAggregateFunction = false;
				var containsWindowFunction    = false;
				var isNotValid = false;

				parentColumn.Expression.VisitAll(e =>
				{
					if (isNotValid)
						return;

					if (e is ISqlExpression sqlExpr)
					{
						var isWindowFunction = QueryHelper.IsWindowFunction(sqlExpr);
						var isAggregationFunction = QueryHelper.IsAggregationFunction(sqlExpr);

						if (isWindowFunction || isAggregationFunction)
						{
							containsWindowFunction    = containsWindowFunction    || isWindowFunction;
							containsAggregateFunction = containsAggregateFunction || isAggregationFunction;

							sqlExpr.VisitAll(se =>
							{
								if (isNotValid)
									return;

								if (se is SqlColumn column && column.Parent == subQuery)
								{
									if (QueryHelper.ContainsAggregationOrWindowFunction(column.Expression))
									{
										isNotValid = true;
									}
								}
							});
						}
					}
				});

				if (isNotValid)
				{
					// not allowed to move complex expressions
					return false;
				}

				if (containsAggregateFunction)
				{
					if (subQuery.Select.HasModifier || subQuery.HasSetOperators || subQuery.HasGroupBy)
					{
						// not allowed to move to parent if it has aggregates
						return false;
					}
				}

				if (containsWindowFunction)
				{
					if (subQuery.Select.HasModifier || subQuery.HasSetOperators || (parentQuery.HasWhere && subQuery.HasWhere) || subQuery.HasGroupBy)
					{
						// not allowed to break window
						return false;
					}
				}

				if (parentQuery.HasGroupBy)
				{
					if (QueryHelper.UnwrapNullablity(parentColumn.Expression) is SqlColumn sc && sc.Parent == subQuery)
					{
						var expr = QueryHelper.UnwrapNullablity(sc.Expression);

						// not allowed to move complex expressions for grouping
						if (expr.ElementType is not (QueryElementType.SqlField or QueryElementType.Column or QueryElementType.SqlValue or QueryElementType.SqlParameter))
						{
							return false;
						}
					}

				}
			}

			List<SqlColumn>? groupingConstants = null;

			foreach (var column in subQuery.Select.Columns)
			{
				if (QueryHelper.ContainsWindowFunction(column.Expression))
				{
					if (parentQuery is not
						{
							Select.HasModifier: false,
							HasWhere: false,
							HasGroupBy: false,
							HasHaving: false,
							From.Tables: [{ Joins.Count: 0 }]
						})
					{
						// not allowed to break query window
						return false;
					}
				}

				if (QueryHelper.ContainsAggregationFunction(column.Expression))
				{
					if (parentQuery.From.Tables.Count != 1)
					{
						return false;
					}

					if (parentQuery.Having.HasElement(column) || parentQuery.Select.GroupBy.HasElement(column))
					{
						// aggregate moving not allowed
						return false;
					}

					if (!IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, column.Expression, ignoreWhere: true, inGrouping: subQuery.HasGroupBy))
					{
						// Column expression is complex and Column has more than one reference
						return false;
					}
				}
				else
				{
					if (!QueryHelper.HasCteClauseReference(subQuery, _currentCteClause)
						&& !IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, column.Expression, ignoreWhere: false, inGrouping: subQuery.HasGroupBy))
					{
						// Column expression is complex and Column has more than one reference
						return false;
					}
				}

				if (QueryHelper.IsConstantFast(column.Expression))
				{
					if (parentQuery.GroupBy.HasElement(column))
					{
						groupingConstants ??= new List<SqlColumn>();
						groupingConstants.Add(column);
					}
				}
			}

			if (groupingConstants != null)
			{
				// All constants in grouping will be optimized to query which produce different query. Optimization will be done in 'OptimizeGroupBy'.
				// See 'GroupByConstantsEmpty' test. It will fail if this check is not performed.
				//
				if (!parentQuery.GroupBy.EnumItems().Except(groupingConstants, Utils.ObjectReferenceEqualityComparer<ISqlExpression>.Default).Any())
				{
					return false;
				}
			}

			HashSet<ISqlExpression>? aggregates = null;

			if (subQuery.HasGroupBy && parentQuery.HasGroupBy)
				return false;

			// Check possible moving Where to Having
			//
			{
				if (parentQuery.HasWhere)
				{
					var searchCondition = parentQuery.Where.SearchCondition;
					if (searchCondition.Predicates is [SqlSearchCondition subCondition])
						searchCondition = subCondition;

					foreach (var subColumn in subQuery.Select.Columns)
					{
						if (QueryHelper.IsAggregationFunction(subColumn.Expression))
						{
							aggregates ??= new(Utils.ObjectReferenceEqualityComparer<ISqlExpression>.Default);
							aggregates.Add(subColumn);

							for (var i = 0; i < searchCondition.Predicates.Count; i++)
							{
								var p = searchCondition.Predicates[i];
								if (p.ElementType == QueryElementType.ExprExprPredicate)
								{
									if (p.HasElement(subColumn))
									{
										havingDetected ??= new(Utils.ObjectReferenceEqualityComparer<ISqlPredicate>.Default);
										havingDetected.Add(p);
									}
								}
								else
								{
									// no optimization allowed
									return false;
								}
							}
						}
					}

					if (havingDetected?.Count != searchCondition.Predicates.Count)
					{
						if (!parentQuery.HasGroupBy && subQuery.HasGroupBy)
						{
							// everything should be moved to having
							return false;
						}

						// do not move to having
						havingDetected = null;
					}
				}
			}

			// named sub-query cannot be removed
			if (subQuery.QueryName != null
				// parent also has name
				&& (parentQuery.QueryName != null
					// parent has other tables/sub-queries
					|| parentQuery.From.Tables.Count > 1
					|| parentQuery.From.Tables.Exists(static t => t.Joins.Count > 0)))
			{
				return false;
			}

			if (_currentSetOperator?.SelectQuery == parentQuery || parentQuery.HasSetOperators)
			{
				// processing parent query as part of Set operation
				//

				if (subQuery.Select.HasModifier)
					return false;

				if (subQuery.HasOrderBy)
				{
					return false;
				}
			}

			if (parentQuery.IsDistinct)
			{
				// Common check for Distincts

				if (subQuery.HasHaving)
					return false;

				if (subQuery.HasOrderBy)
				{
					if (subQuery.IsLimited || parentQuery.IsLimited)
						return false;
				}
				else
				{
					if (subQuery.IsLimited)
						return false;
				}

				// Common column check for Distincts

				foreach (var parentColumn in parentQuery.Select.Columns)
				{
					if (parentColumn.Expression is not SqlColumn column || column.Parent != subQuery || QueryHelper.ContainsAggregationOrWindowFunction(parentColumn.Expression))
					{
						return false;
					}
				}
			}

			if (subQuery.IsDistinct != parentQuery.IsDistinct)
			{
				if (subQuery.IsDistinct)
				{
					// Columns in parent query should match
					//
					if (parentQuery.Select.Columns.Count > 0)
					{
						if (parentQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
						{
						return false;
					}

						if (!subQuery.Select.Columns.TrueForAll(sc => parentQuery.Select.Columns.Exists(pc => ReferenceEquals(QueryHelper.UnwrapNullablity(pc.Expression), sc))))
					{
						return false;
					}
				}
				}
				else
				{
					// handling case when we have two DISTINCT
					// Note, columns already checked above
					//
				}
			}

			if (subQuery.Select.HasModifier)
			{
				// Do not optimize queries for update
				if (_updateQuery == parentQuery
					&& subQuery.Select.HasSomeModifiers(_providerFlags.IsUpdateSkipTakeSupported, _providerFlags.IsUpdateTakeSupported))
					return false;

				if (tableSource.Joins.Count > 0)
					return false;
				if (parentQuery.From.Tables.Count > 1)
					return false;

				if (parentQuery.HasOrderBy)
				{
					if (subQuery.IsLimited)
						return false;
				}

				if (parentQuery.HasWhere)
				{
					if (subQuery.Select.TakeValue != null || subQuery.Select.SkipValue != null)
						return false;
				}

				if (parentQuery.Select.Columns.Exists(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
				{
					return false;
				}
			}

			if (subQuery.Select.HasModifier || subQuery.HasWhere)
			{
				if (tableSource.Joins.Exists(j => j.JoinType is JoinType.Right or JoinType.RightApply or JoinType.Full or JoinType.FullApply))
				{
					return false;
				}
			}

			if (!_providerFlags.AcceptsOuterExpressionInAggregate)
			{
				if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType != JoinType.Inner))
				{
					if (subQuery.Select.Columns.Exists(c => IsInsideAggregate(parentQuery, c)))
					{
						if (QueryHelper.IsDependsOnOuterSources(subQuery))
							return false;
					}
				}
			}

			if (!parentQuery.HasGroupBy && subQuery.HasGroupBy)
			{
				if (tableSource.Joins.Count > 0)
					return false;

				if (parentQuery.From.Tables.Count > 1)
					return false;

				//throw new NotImplementedException();

				//if (selectQuery.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression)))
				//	return false;
			}

			if (subQuery.Select.TakeHints != null && parentQuery.Select.TakeValue != null)
				return false;

			if (subQuery.HasSetOperators)
			{
				if (parentQuery.HasSetOperators)
					return false;

				if (parentQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
				{
					if (subQuery.SetOperators.Exists(so => so.Operation != SetOperation.UnionAll))
						return false;
				}

				if (parentQuery.HasWhere || parentQuery.HasHaving || parentQuery.Select.HasModifier || parentQuery.HasOrderBy)
					return false;

				var operation = subQuery.SetOperators[0].Operation;

				if (_currentSetOperator != null && _currentSetOperator.Operation != operation)
					return false;

				if (!subQuery.SetOperators.TrueForAll(so => so.Operation == operation))
					return false;
			}

			// Do not optimize t.Field IN (SELECT x FROM o)
			if (parentQuery == _inSubquery && (subQuery.Select.HasModifier || subQuery.HasSetOperators))
			{
				if (_dataOptions.LinqOptions.PreferExistsForScalar || _providerFlags.IsExistsPreferableForContains)
					return false;

				if (!_providerFlags.IsTakeWithInAllAnySomeSubquerySupported && (subQuery.Select.TakeValue != null || subQuery.Select.SkipValue != null))
					return false;

				if (!_providerFlags.IsSubQuerySkipSupported && subQuery.Select.SkipValue != null)
					return false;

				if (!_providerFlags.IsSubQueryTakeSupported && subQuery.Select.TakeValue != null)
					return false;
			}

			return true;
		}

		bool JoinMoveSubQueryUp(SelectQuery selectQuery, SqlJoinedTable joinTable)
		{
			if (joinTable.Table.Source is not SelectQuery subQuery)
				return false;

			if (subQuery.DoNotRemove)
				return false;

			if (subQuery.SqlQueryExtensions?.Count > 0)
				return false;

			if (subQuery.From.Tables.Count != 1)
				return false;

			// named sub-query cannot be removed
			if (subQuery.QueryName != null)
				return false;

			if (subQuery.HasGroupBy)
				return false;

			if (subQuery.Select.HasModifier)
				return false;

			if (subQuery.HasSetOperators)
				return false;

			if (subQuery.HasGroupBy)
				return false;

			// Rare case when LEFT join is empty. We move search condition up. See TestDefaultExpression_22 test.
			if (joinTable.JoinType == JoinType.Left && subQuery.Where.SearchCondition.IsFalse)
			{
				subQuery.Where.SearchCondition.Predicates.Clear();
				joinTable.Condition.Predicates.Clear();
				joinTable.Condition.Predicates.Add(SqlPredicate.False);

				// Continue in next loop
				return true;
			}

			var moveConditionToQuery = joinTable.JoinType is JoinType.Inner or JoinType.CrossApply;

			if (joinTable.JoinType != JoinType.Inner)
			{
				if (subQuery.HasWhere)
				{
					if (joinTable.JoinType == JoinType.OuterApply)
					{
						if (!_providerFlags.IsOuterApplyJoinSupportsCondition)
							return false;

						// Should remain LATERAL
						if (QueryHelper.IsDependsOnOuterSources(subQuery, [subQuery.Where]))
							return false;
					}
					else if (joinTable.JoinType == JoinType.CrossApply)
					{
						if (_providerFlags.IsCrossApplyJoinSupportsCondition)
							moveConditionToQuery = false;

						// Should remain LATERAL
						if (QueryHelper.IsDependsOnOuterSources(subQuery, [subQuery.Where]))
							return false;
					}
					else if (joinTable.JoinType == JoinType.Left)
					{
						if (joinTable.Condition.IsTrue)
						{
							// See `PostgreSQLExtensionsTests.GenerateSeries`
							if (subQuery.From.Tables[0].Joins.Count > 0)
							{
								// See 'Issue2199Tests.LeftJoinTests2'
								return false;
							}
						}

						moveConditionToQuery = false;
					}
					else
					{
						return false;
					}
				}

				if (!_providerFlags.IsOuterJoinSupportsInnerJoin)
				{
					// Especially for Access. See ComplexTests.Contains3
					//
					if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType == JoinType.Inner))
						return false;
				}

				// Check that all columns in sub-query are allowed to move up
				NullabilityContext? nullabilityContext = null;
				foreach (var c in subQuery.Select.Columns)
				{
					var columnExpression = QueryHelper.UnwrapCast(c.Expression);
					if (columnExpression is not (SqlField or SqlColumn))
					{
						nullabilityContext ??= NullabilityContext.GetContext(subQuery);
						if (!c.Expression.CanBeNullable(nullabilityContext))
						{
							if (QueryHelper.IsDependsOn(selectQuery, c, [joinTable]))
								return false;
						}
					}
				}
			}

			if (subQuery.Select.Columns.Exists(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
				return false;

			// Actual modification starts from this point
			//

			if (subQuery.HasWhere)
			{
				if (moveConditionToQuery)
				{
					selectQuery.Where.EnsureConjunction().Predicates.AddRange(subQuery.Where.SearchCondition.Predicates);
				}
				else
				{
					joinTable.Condition.Predicates.AddRange(subQuery.Where.SearchCondition.Predicates);
				}
			}

			if (selectQuery.Select.Columns.Count == 0)
			{
				foreach (var column in subQuery.Select.Columns)
				{
					selectQuery.Select.AddColumn(column.Expression);
				}
			}

			foreach (var column in subQuery.Select.Columns)
			{
				NotifyReplaced(column.Expression, column);
			}

			if (subQuery.HasOrderBy && !QueryHelper.IsAggregationQuery(selectQuery))
			{
				ApplySubsequentOrder(selectQuery, subQuery);
			}

			var subQueryTableSource = subQuery.From.Tables[0];
			joinTable.Table.Joins.AddRange(subQueryTableSource.Joins);
			joinTable.Table.Source = subQueryTableSource.Source;

			if (joinTable.Table.RawAlias == null && subQueryTableSource.RawAlias != null)
				joinTable.Table.Alias = subQueryTableSource.RawAlias;

			if (!joinTable.Table.HasUniqueKeys && subQueryTableSource.HasUniqueKeys)
				joinTable.Table.UniqueKeys.AddRange(subQueryTableSource.UniqueKeys);

			return true;
		}

		protected internal override IQueryElement VisitSqlFromClause(SqlFromClause element)
		{
			element = (SqlFromClause)base.VisitSqlFromClause(element);
			return element;
		}

		bool OptimizeSubQueries(SelectQuery selectQuery, List<SelectQuery>? doNotRemoveQueries)
		{
			var replaced = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];

				if (tableSource.Source is SelectQuery innerQuery && doNotRemoveQueries?.Contains(innerQuery) == true)
				{
					continue;
				}

				if (MoveSubQueryUp(selectQuery, tableSource))
				{
					replaced = true;

					EnsureReferencesCorrected(selectQuery);

					--i; // repeat again
				}
			}

			// Removing subqueries which has no tables

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
				if (tableSource.Joins.Count == 0 && tableSource.Source is SelectQuery { From.Tables.Count: 0, HasSetOperators: false } subQuery)
				{
					if (selectQuery.From.Tables.Count == 1)
					{
						if (selectQuery.HasGroupBy
							|| selectQuery.HasHaving
							|| selectQuery.HasOrderBy)
						{
							continue;
						}
					}

					if (subQuery.HasWhere)
					{
						if (!QueryHelper.IsAggregationQuery(selectQuery))
							continue;
					}

					replaced = true;

					foreach (var c in subQuery.Select.Columns)
					{
						NotifyReplaced(c.Expression, c);
					}

					if (subQuery.HasWhere)
					{
						selectQuery.Where.SearchCondition = QueryHelper.MergeConditions(selectQuery.Where.SearchCondition, subQuery.Where.SearchCondition);
					}

					selectQuery.From.Tables.RemoveAt(i);

					--i; // repeat again
				}
			}

			return replaced;
		}

		bool OptimizeJoinSubQueries(SelectQuery selectQuery)
		{
			var replaced = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];

				if (tableSource.Joins.Count > 0)
				{
					for (var j = 0; j < tableSource.Joins.Count; j++)
					{
						var join = tableSource.Joins[j];

						if (JoinMoveSubQueryUp(selectQuery, join))
							replaced = true;
					}
				}
			}

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];

				if (tableSource.Joins.Count > 0)
				{
					for (var index = 0; index < tableSource.Joins.Count; index++)
					{
						var join = tableSource.Joins[index];
						if (join.JoinType == JoinType.Inner && join.Table.Source is SelectQuery joinQuery)
						{
							if (joinQuery.From.Tables.Count == 0)
							{
								replaced = true;

								foreach (var c in joinQuery.Select.Columns)
								{
									NotifyReplaced(c.Expression, c);
								}

								tableSource.Joins.RemoveAt(index);
								--index;
							}
						}
					}
				}
			}

			return replaced;
		}

		private bool OptimizeJoinConditions(SelectQuery selectQuery)
		{
			if (_root is not SqlStatement root)
				return false;

			var modified = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];

				if (tableSource.Joins.Count > 0)
				{
					for (var j = 0; j < tableSource.Joins.Count; j++)
					{
						var join = tableSource.Joins[j];

						if (join.JoinType is (JoinType.Inner or JoinType.Left) && !join.Condition.IsOr && join.Condition.Predicates.Count != 0)
							modified |= MoveJoinConditionsToWhere(root, tableSource, join, selectQuery.Where, NullabilityContext.GetContext(selectQuery));
					}
				}
			}

			return modified;

			bool MoveJoinConditionsToWhere(SqlStatement root, SqlTableSource left, SqlJoinedTable join, SqlWhereClause where, NullabilityContext nullabilityContext)
			{
				var modified                   = false;
				var isLeft                     = join.JoinType == JoinType.Left;
				List<ISqlTableSource>? sources = null;

				SqlSearchCondition? whereCond       = null;
				SqlSearchCondition? nestedWhereCond = null;

				for (var i = 0; i < join.Condition.Predicates.Count; i++)
				{
					var predicate = join.Condition.Predicates[i];

					var move = predicate is not SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } exprExpr
						|| exprExpr.Reduce(nullabilityContext.WithJoinSource(join.Table.Source), _evaluationContext, false, _dataOptions.LinqOptions) is not SqlPredicate.ExprExpr
						|| exprExpr.Expr1 is SqlValue || exprExpr.Expr2 is SqlValue;

					if (move && isLeft)
					{
						sources ??= QueryHelper.EnumerateAccessibleSources(join.Table).ToList();
						move = !QueryHelper.IsDependsOnSources(predicate, sources);

						if (!move && !QueryHelper.IsDependsOnSources(predicate, [left]))
						{
							if (nestedWhereCond == null)
							{
								if (join.Table.Source is SelectQuery sq)
								{
									QueryHelper.WrapQuery(root, sq, true, doNotRemove: true);
									nestedWhereCond = ((SelectQuery)join.Table.Source).Where.EnsureConjunction();
									modified = true;
								}
								else if (join.Table.Source is SqlTable t)
								{
									var subQuery      = new SelectQuery() { DoNotRemove = true };
									join.Table.Source = subQuery;
									nestedWhereCond = subQuery.Where.EnsureConjunction();
									subQuery.From.Table(t);

									var tableSources = new HashSet<ISqlTableSource>() { t };
									var foundFields  = new HashSet<ISqlExpression>();

									QueryHelper.CollectDependencies(_rootElement, tableSources, foundFields, ignore: join.Table.Joins);

									if (foundFields.Count > 0)
									{
										var toReplace = new Dictionary<IQueryElement, IQueryElement>(foundFields.Count);
										foreach (var expr in foundFields)
											toReplace.Add(expr, subQuery.Select.AddColumn(expr));

										_rootElement.Replace(toReplace, subQuery.Select);
									}

									modified = true;
								}
							}

							if (nestedWhereCond != null)
							{
								// update references
								var foundFields = new HashSet<ISqlExpression>();
								QueryHelper.CollectDependencies(predicate, [join.Table.Source], foundFields);

								if (foundFields.Count > 0)
								{
									var toReplace = new Dictionary<IQueryElement, IQueryElement>(foundFields.Count);
									foreach (var expr in foundFields)
										toReplace.Add(expr, ((SqlColumn)expr).Expression);
									predicate.Replace(toReplace);
								}

								nestedWhereCond.Predicates.Add(predicate);
								join.Condition.Predicates.RemoveAt(i);
								i--;
								continue;
							}
						}
					}

					if (move)
					{
						(whereCond ??= where.EnsureConjunction()).Predicates.Add(predicate);
						join.Condition.Predicates.RemoveAt(i);
						i--;
					}
				}

				// this could result in empty condition, but it is fine - user created unsupported query
				return modified;
			}
		}

		bool CorrectRecursiveCteJoins(SelectQuery selectQuery)
		{
			var isModified = false;

			if (!_providerFlags.IsRecursiveCTEJoinWithConditionSupported && _isInRecursiveCte)
			{
				for (int i = 0; i < selectQuery.From.Tables.Count; i++)
				{
					var ts = selectQuery.From.Tables[i];
					if (ts.Joins.Count > 0)
					{
						var join = ts.Joins[0];

						if (join.JoinType != JoinType.Inner)
							break;

						isModified = true;
						selectQuery.From.Tables.Insert(i + 1, join.Table);
						selectQuery.Where.ConcatSearchCondition(join.Condition);
						ts.Joins.RemoveAt(0);
						--i;
					}
				}
			}

			return isModified;
		}

		/// <summary>
		/// Transforms LEFT JOINs to INNER JOINs if possible.
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		bool CorrectLeftJoins(SelectQuery selectQuery)
		{
			var joins = selectQuery.Select.From.Tables.SelectMany(t => t.Joins)
				.Where(j => j.JoinType is JoinType.Left or JoinType.Inner)
				.ToList();

			if (joins.Count == 0)
				return false;

			var isModified  = false;
			var nullability = NullabilityContext.GetContext(selectQuery);

			for (var i = 0; i < joins.Count; i++)
			{
				var join = joins[i];
				if (join.JoinType != JoinType.Left)
					continue;

				var hasStrictCondition = IsStrictCondition(nullability, selectQuery.Where.SearchCondition, join.Table.Source);
				if (!hasStrictCondition)
				{
					for (var j = i + 1; j < joins.Count; j++)
					{
						var nextJoin = joins[j];
						if (nextJoin.JoinType == JoinType.Inner)
						{
							if (IsStrictCondition(nullability, nextJoin.Condition, join.Table.Source))
							{
								hasStrictCondition = true;
								break;
							}
						}
					}
				}

				if (hasStrictCondition)
				{
					join.JoinType = JoinType.Inner;
					join.IsWeak   = false;
					isModified    = true;
					// reset nullability
					nullability = NullabilityContext.GetContext(selectQuery);
				}

			}

			return isModified;
		}

		static bool IsStrictCondition(NullabilityContext nullability, SqlSearchCondition condition, ISqlTableSource testedSource)
		{
			if (condition.IsOr || condition.Predicates.Count > 10)
				return false;

			foreach (var predicate in condition.Predicates)
			{
				switch (predicate)
				{
					case SqlPredicate.IsNull isNullPredicate:
					{
						if (isNullPredicate.IsNot)
						{
							var source = ExtractSource(isNullPredicate.Expr1);
							if (source == testedSource)
							{
								var local = NullabilityContext.GetContext(source as SelectQuery);
								return local.CanBeNull(isNullPredicate.Expr1);
							}
						}

						break;
					}
					case SqlPredicate.ExprExpr exprExPredicate:
					{
						if (exprExPredicate.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.Greater or SqlPredicate.Operator.Less)
						{
							var source = ExtractSource(exprExPredicate.Expr1);
							if (source == testedSource)
							{
								var local = NullabilityContext.GetContext(source as SelectQuery);
								if (!local.CanBeNull(exprExPredicate.Expr1) && IsNotNullable(exprExPredicate.Expr2) == true)
									return true;
							}

							source = ExtractSource(exprExPredicate.Expr2);
							if (source == testedSource)
							{
								var local = NullabilityContext.GetContext(source as SelectQuery);
								if (!local.CanBeNull(exprExPredicate.Expr2) && IsNotNullable(exprExPredicate.Expr1) == true)
									return true;
							}
						}

						break;
					}
				}

				bool? IsNotNullable(ISqlExpression expr)
				{
					if (expr is SqlValue value)
					{
						return value.Value != null;
					}

					var isValid = true;
					expr.VisitAll(e =>
					{
						if (!isValid)
							return;

						var exprSource = ExtractSource(expr);
						if (exprSource == null)
							return;

						if (nullability.CanBeNullSource(exprSource) == null)
							isValid = false;
					});

					if (!isValid)
						return null;

					return !nullability.CanBeNull(expr);
				}
			}

			return false;

			static ISqlTableSource? ExtractSource(ISqlExpression expr)
			{
				return expr switch
				{
					SqlColumn column   => column.Parent,
					SqlField field     => field.Table,
					ISqlTableSource ts => ts,
					_                  => null,
				};
			}
		}

		SelectQuery MoveMultiTablesToSubquery(SelectQuery selectQuery)
		{
			var joins = new List<SqlJoinedTable>(selectQuery.From.Tables.Count);
			foreach (var t in selectQuery.From.Tables)
			{
				joins.AddRange(t.Joins);
				t.Joins.Clear();
			}

			var subQuery = new SelectQuery();

			var tables = selectQuery.From.Tables.ToArray();
			selectQuery.From.Tables.Clear();

			var baseTable = new SqlTableSource(subQuery, "cross");
			baseTable.Joins.AddRange(joins);
			selectQuery.Select.From.Tables.Add(baseTable);
			subQuery.From.Tables.AddRange(tables);

			var sources     = new HashSet<ISqlTableSource>(tables.Select(static t => t.Source));
			var foundFields = new HashSet<ISqlExpression>();

			QueryHelper.CollectDependencies(_rootElement, sources, foundFields);

			var toReplace = new Dictionary<IQueryElement, IQueryElement>(foundFields.Count);
			foreach (var expr in foundFields)
				toReplace.Add(expr, subQuery.Select.AddColumn(expr));

			if (toReplace.Count > 0)
			{
				_rootElement.Replace(toReplace, subQuery.Select);
			}

			subQuery.DoNotRemove = true;

			return subQuery;
		}

		bool CorrectMultiTables(SelectQuery selectQuery)
		{
			if (_providerFlags.IsMultiTablesSupportsJoins)
				return false;

			var isModified = false;

			if (selectQuery.From.Tables.Count > 1)
			{
				if (QueryHelper.EnumerateJoins(selectQuery).Any())
				{
					MoveMultiTablesToSubquery(selectQuery);

					isModified = true;
				}
			}

			return isModified;
		}

		bool OptimizeColumns(SelectQuery selectQuery)
		{
			if (_parentSelect == null)
				return false;

			if (_currentSetOperator != null)
				return false;

			if (selectQuery.HasSetOperators)
				return false;

			var isModified = false;

			for (var index = 0; index < selectQuery.Select.Columns.Count; index++)
			{
				var c = selectQuery.Select.Columns[index];
				for (var nextIndex = index + 1; nextIndex < selectQuery.Select.Columns.Count; nextIndex++)
				{
					var nc = selectQuery.Select.Columns[nextIndex];

					if (ReferenceEquals(c.Expression, nc.Expression))
					{
						selectQuery.Select.Columns.RemoveAt(nextIndex);
						--nextIndex;

						NotifyReplaced(c, nc);

						isModified = true;
					}
				}
			}

			return isModified;
		}

		bool OptimizeJoins(SelectQuery selectQuery, ref List<SelectQuery>? doNotRemoveQueries)
		{
			var isModified = false;

			for (var i = selectQuery.From.Tables.Count - 1; i >= 0; i--)
			{
				var table = selectQuery.From.Tables[i];

				for (var index = 0; index < table.Joins.Count; index++)
				{
					var join = table.Joins[index];

					if (join.JoinType is JoinType.CrossApply or JoinType.OuterApply or JoinType.FullApply or JoinType.RightApply)
					{
						if (OptimizeApplyJoin(join, doNotEmulate: true))
						{
							isModified = true;
						}
					}

					// First run
					if (
						MoveSingleOuterJoinToSubQuery(
							selectQuery,
							join,
							ref doNotRemoveQueries,
							processMultiColumn: false,
							deduplicate: join.JoinType is JoinType.OuterApply && !_providerFlags.IsApplyJoinSupported,
							out var modified
						)
					)
					{
						table.Joins.RemoveAt(index);

						isModified = true;

						--index;
						continue;
					}

					isModified = isModified || modified;

					if (join.JoinType is JoinType.CrossApply or JoinType.OuterApply or JoinType.FullApply or JoinType.RightApply)
					{
						if (OptimizeApplyJoin(join, doNotEmulate: false))
						{
							isModified = true;
						}
					}

					if (!_providerFlags.IsApplyJoinSupported && join.JoinType is JoinType.OuterApply || !_providerFlags.IsSupportsJoinWithoutCondition && join.Condition.IsTrue)
					{
						// last chance to remove apply join before finalizing query.
						if (MoveSingleOuterJoinToSubQuery(selectQuery, join, ref doNotRemoveQueries, processMultiColumn: true, deduplicate: true, out modified))
						{
							table.Joins.RemoveAt(index);
							OptimizeInnerQueries(selectQuery, doNotRemoveQueries);

							isModified = true;

							--index;
							continue;
						}

						isModified = isModified || modified;
					}
				}
			}

			if (isModified)
			{
				EnsureReferencesCorrected(selectQuery);
				_columnNestingCorrector.CorrectColumnNesting(selectQuery);

				OptimizeInnerQueries(selectQuery, doNotRemoveQueries);
			}

			return isModified;
		}

		void RemoveNonUsedOuterJoins(SelectQuery selectQuery)
		{
			for (var i = selectQuery.From.Tables.Count - 1; i >= 0; i--)
			{
				var table = selectQuery.From.Tables[i];

				for (var index = table.Joins.Count - 1; index >= 0; index--)
				{
					var join = table.Joins[index];

					if (join.JoinType is not (JoinType.Left or JoinType.OuterApply))
					{
						continue;
					}

					if (!QueryHelper.IsDependsOnSource(selectQuery, join.Table.Source, [join]) && IsRemovableJoin(join))
					{
						table.Joins.RemoveAt(index);
					}
				}
			}
		}

		void CorrectEmptyInnerJoinsRecursive(SelectQuery selectQuery)
		{
			selectQuery.Visit(e =>
			{
				if (e is SelectQuery sq)
					CorrectEmptyInnerJoinsInQuery(sq);
			});
		}

		bool CorrectEmptyInnerJoinsInQuery(SelectQuery selectQuery)
		{
			var isModified = false;

			for (var queryTableIndex = 0; queryTableIndex < selectQuery.From.Tables.Count; queryTableIndex++)
			{
				var table = selectQuery.From.Tables[queryTableIndex];
				for (var joinIndex = 0; joinIndex < table.Joins.Count; joinIndex++)
				{
					var join = table.Joins[joinIndex];
					if (join is { JoinType: JoinType.Inner, Condition.IsTrue: true })
					{
						if (_providerFlags.IsCrossJoinSupported
							&& (table.Joins.Count > (_providerFlags.IsCrossJoinSyntaxRequired ? 0 : 1)
								|| !QueryHelper.IsDependsOnSource(selectQuery.Where, join.Table.Source)))
						{
							join.JoinType = JoinType.Cross;
							if (join.Table.Joins.Count > 0)
							{
								// move joins to the same level as parent table
								for (var ij = 0; ij < join.Table.Joins.Count; ij++)
								{
									table.Joins.Insert(joinIndex + ij + 1, join.Table.Joins[ij]);
								}

								join.Table.Joins.Clear();
							}

							isModified = true;
						}
						else
						{
							selectQuery.From.Tables.Insert(queryTableIndex + 1, join.Table);
							table.Joins.RemoveAt(joinIndex);

							// move joins INNER JOIN table from parent
							for (var ij = 0; ij < table.Joins.Count; ij++)
							{
								join.Table.Joins.Insert(ij, table.Joins[ij]);
							}

							table.Joins.Clear();

							--joinIndex;
							isModified = true;
						}
					}
				}
			}

			return isModified;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			expression = base.VisitSqlColumnExpression(column, expression);

			expression = QueryHelper.SimplifyColumnExpression(expression);

			return expression;
		}

		static bool IsLimitedToOneRecord(SelectQuery parentQuery, SelectQuery selectQuery, EvaluationContext context)
		{
			if (selectQuery.Select.TakeValue != null &&
				selectQuery.Select.TakeValue.TryEvaluateExpression(context, out var takeValue))
			{
				if (takeValue is int intValue)
				{
					return intValue == 1;
				}
			}

			if (QueryHelper.IsAggregationQuery(selectQuery))
				return true;

			if (selectQuery.Select.Columns.Count == 1)
			{
				if (selectQuery.Select.From.Tables.Count == 0)
					return true;
			}

			if (selectQuery.HasWhere)
			{
				var keys = new List<IList<ISqlExpression>>();
				QueryHelper.CollectUniqueKeys(selectQuery, true, keys);

				if (keys.Count > 0)
				{
					var outerSources = QueryHelper.EnumerateAccessibleSources(parentQuery)
						.Where(s => s != selectQuery)
						.ToList();

					var innerSources = QueryHelper.EnumerateAccessibleSources(selectQuery).ToList();

					var toIgnore = new List<ISqlTableSource>() { selectQuery };

					var foundEquality = new List<ISqlExpression>();
					foreach (var p in selectQuery.Where.SearchCondition.Predicates)
					{
						if (p is SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } equality)
						{
							var left  = QueryHelper.UnwrapNullablity(equality.Expr1);
							var right = QueryHelper.UnwrapNullablity(equality.Expr2);

							if (!left.Equals(right))
							{
								if (QueryHelper.IsDependsOnSources(left, outerSources, toIgnore) && QueryHelper.IsDependsOnSources(right, innerSources))
									foundEquality.Add(right);
								else if (QueryHelper.IsDependsOnSources(right, outerSources, toIgnore) && QueryHelper.IsDependsOnSources(left, innerSources))
									foundEquality.Add(left);
							}
						}
					}

					// all keys should be matched
					if (keys.Exists(kl => kl.All(k => foundEquality.Contains(k))))
						return true;
				}
			}

			return false;
		}

		int CountUsage(SelectQuery rootQuery, SqlColumn column)
		{
			IQueryElement root = rootQuery;
			if (_rootElement is not SqlSelectStatement)
			{
				root = _rootElement;
			}

			var counter = 1;

			if (!_movingComplexityVisitor.IsAllowedToMove(_providerFlags, column, root, [column.Parent]))
			{
				counter = 2;
			}

			return counter;
		}

		static bool IsInSelectPart(SelectQuery rootQuery, SqlColumn column)
		{
			var result = rootQuery.Select.HasElement(column);
			return result;
		}

		static bool IsInOrderByPart(SelectQuery rootQuery, SqlColumn column)
		{
			var result = rootQuery.OrderBy.HasElement(column);
			return result;
		}

		static bool IsInsideAggregate(IQueryElement testedElement, SqlColumn column)
		{
			bool result = false;

			testedElement.VisitParentFirstAll(e =>
			{
				// do not search in the same query
				if (QueryHelper.IsAggregationFunction(e))
				{
					result = result || null != e.Find(1, (_, te) => ReferenceEquals(te, column));
					return false;
				}

				return !result;
			});

			return result;
		}

		void MoveDuplicateUsageToSubQuery(SelectQuery query, ref List<SelectQuery>? doNotRemoveQueries)
		{
			var subQuery = new SelectQuery();

			doNotRemoveQueries ??= new();
			doNotRemoveQueries.Add(subQuery);

			subQuery.From.Tables.AddRange(query.From.Tables);

			query.Select.From.Tables.Clear();
			_ = query.Select.From.Table(subQuery);

			_columnNestingCorrector.CorrectColumnNesting(query);
		}

		void MoveToSubQuery(SelectQuery query)
		{
			var subQuery = MoveTablesToSubQuery(query);
			subQuery.DoNotRemove = true;
		}

		SelectQuery MoveTablesToSubQuery(SelectQuery query)
		{
			var subQuery = new SelectQuery();

			subQuery.From.Tables.AddRange(query.From.Tables);

			query.Select.From.Tables.Clear();
			_ = query.Select.From.Table(subQuery);

			if (query.HasOrderBy)
			{
				subQuery.OrderBy.Items.AddRange(query.OrderBy.Items);
				query.OrderBy.Items.Clear();
			}

			if (query.HasGroupBy)
			{
				subQuery.GroupBy.Items.AddRange(query.GroupBy.Items);
				query.GroupBy.Items.Clear();
			}

			if (query.HasWhere)
			{
				subQuery.Where.SearchCondition.Predicates.AddRange(query.Where.SearchCondition.Predicates);
				subQuery.Where.SearchCondition.IsOr = query.Where.SearchCondition.IsOr;
				query.Where.SearchCondition.Predicates.Clear();
			}

			if (query.HasHaving)
			{
				subQuery.Having.SearchCondition.Predicates.AddRange(query.Having.SearchCondition.Predicates);
				subQuery.Having.SearchCondition.IsOr = query.Having.SearchCondition.IsOr;
				query.Having.SearchCondition.Predicates.Clear();
			}

			if (query.IsDistinct)
			{
				subQuery.Select.IsDistinct = true;
				query.Select.IsDistinct    = false;
			}

			_columnNestingCorrector.CorrectColumnNesting(query);

			return subQuery;
		}

		bool MoveSingleOuterJoinToSubQuery(SelectQuery parentQuery, SqlJoinedTable join, ref List<SelectQuery>? doNotRemoveQueries, bool processMultiColumn, bool deduplicate, out bool isModified)
		{
			isModified = false;

			if (
				join.JoinType is not (
					JoinType.OuterApply or
					JoinType.Left or
					JoinType.CrossApply or
					JoinType.Inner
				)
			)
			{
				return false;
			}

			if (join.Table.Source is not SelectQuery { Select.Columns.Count: > 0 } joinQuery)
			{
				return false;
			}

			bool? isSingleRecord = null;

			if (join.JoinType is JoinType.CrossApply or JoinType.Inner)
			{
				if (!join.IsSubqueryExpression)
				{
					return false;
				}

				isSingleRecord = true;
			}

			var evaluationContext = new EvaluationContext();

			if (joinQuery.Select.Columns.Count > 1)
			{
				if (!processMultiColumn)
					return false;

				if (join.JoinType == JoinType.Left)
				{
					if (_providerFlags.IsSupportsJoinWithoutCondition || join.Condition.Predicates.Count > 0)
						return false;
				}

				if (_providerFlags.IsApplyJoinSupported)
				{
					// provider can handle this query
					return false;
				}
			}

			if (!(isSingleRecord == true || IsLimitedToOneRecord(parentQuery, joinQuery, evaluationContext)))
				return false;

			// do not move to subquery expression if update table in the query.
			if (_updateTable != null && joinQuery.HasElement(_updateTable))
				return false;

			var isNoTableQuery = joinQuery.From.Tables.Count == 0;

			if (!isNoTableQuery)
			{
				if (!SqlProviderHelper.IsValidQuery(joinQuery, parentQuery: parentQuery, fakeJoin: null, columnSubqueryLevel: 0, _providerFlags, out _))
					return false;
			}

			if (joinQuery.Select.Columns.Count > 1 && joinQuery.Select.IsDistinct)
			{
				if (!SqlProviderHelper.IsValidQuery(joinQuery, parentQuery: parentQuery, fakeJoin: null, columnSubqueryLevel: 1, _providerFlags, out _))
					return false;

				MoveToSubQuery(joinQuery);
			}

			foreach (var testedColumn in joinQuery.Select.Columns)
			{
				// where we can start analyzing that we can move join to subquery

				var usageCount = CountUsage(parentQuery, testedColumn);
				var isUnique   = usageCount <= 1;

				if (!isUnique && !deduplicate)
				{
					return false;
				}

				if (!isUnique)
				{
					if (join.JoinType == JoinType.Left)
					{
						return false;
					}

					if (_updateQuery != parentQuery)
					{
						if (SqlProviderHelper.IsValidQuery(joinQuery, parentQuery: parentQuery, fakeJoin: null, columnSubqueryLevel: 0, _providerFlags, out _))
						{
							MoveDuplicateUsageToSubQuery(parentQuery, ref doNotRemoveQueries);

							// will be processed in the next step
							isModified = true;
						}
					}
				}

				if (usageCount == 1 && !IsInSelectPart(parentQuery, testedColumn))
				{
					var moveToSubquery = IsInOrderByPart(parentQuery, testedColumn) && !_providerFlags.IsSubQueryOrderBySupported;
					if (moveToSubquery)
					{
						MoveToSubQuery(parentQuery);
						isModified = true;
					}
				}

				if (QueryHelper.IsAggregationFunction(testedColumn.Expression))
				{
					if (!_providerFlags.AcceptsOuterExpressionInAggregate && IsInsideAggregate(parentQuery.Select, testedColumn))
					{
						if (_providerFlags.IsApplyJoinSupported)
						{
							return false;
						}

						MoveToSubQuery(parentQuery);
						isModified = true;
					}

					if (!_providerFlags.IsCountSubQuerySupported)
					{
						return false;
					}
				}
			}

			// moving whole join to subquery

			joinQuery.Where.ConcatSearchCondition(join.Condition);

			// replacing column with subquery

			for (var index = joinQuery.Select.Columns.Count - 1; index >= 0; index--)
			{
				var queryToReplace = joinQuery;
				var testedColumn   = joinQuery.Select.Columns[index];

				// cloning if there are many columns
				if (index > 0)
				{
					queryToReplace = joinQuery.Clone();
				}

				if (queryToReplace.Select.Columns.Count > 1)
				{
					var sourceColumn = queryToReplace.Select.Columns[index];
					queryToReplace.Select.Columns.Clear();
					queryToReplace.Select.Columns.Add(sourceColumn);
				}

				isModified = true;

				NotifyReplaced(queryToReplace, testedColumn);
			}

			return true;
		}

		void OptimizeInnerQueries(SelectQuery selectQuery, List<SelectQuery>? doNotRemoveQueries)
		{
			var queries = QueryHelper.EnumerateAccessibleSources(selectQuery).OfType<SelectQuery>().ToList();
			foreach (var sq in queries)
			{
				var nullabilityContext = NullabilityContext.GetContext(sq);
				foreach (var column in sq.Select.Columns)
				{
					var optimized = _expressionOptimizerVisitor.Optimize(_evaluationContext, nullabilityContext, null, _dataOptions, _mappingSchema, column.Expression, visitQueries: false, reducePredicates: false);

					if (!ReferenceEquals(optimized, column.Expression))
					{
						column.Expression = (ISqlExpression)optimized;
						doNotRemoveQueries?.Clear();
					}
				}
			}
		}

		protected internal override IQueryElement VisitCteClause(CteClause element)
		{
			var saveIsInRecursiveCte = _isInRecursiveCte;
			var saveCurrentCteClause = _currentCteClause;
			if (element.IsRecursive)
				_isInRecursiveCte = true;

			var saveParent = _parentSelect;
			_parentSelect = null;
			_currentCteClause = element;

			var newElement = base.VisitCteClause(element);

			_parentSelect = saveParent;

			_currentCteClause = saveCurrentCteClause;
			_isInRecursiveCte = saveIsInRecursiveCte;

			return newElement;
		}

		protected internal override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			var result = base.VisitExistsPredicate(predicate);

			if (!ReferenceEquals(result, predicate))
				return Visit(predicate);

			var sq = predicate.SubQuery;

			// We can safely optimize out Distinct
			if (sq.Select.IsDistinct)
			{
				sq.Select.IsDistinct = false;
			}

			if (!sq.HasGroupBy && !sq.HasSetOperators)
			{
				// non aggregation columns can be removed
				for (int i = sq.Select.Columns.Count - 1; i >= 0; i--)
				{
					var colum = sq.Select.Columns[i];
					if (!QueryHelper.ContainsAggregationFunction(colum.Expression))
					{
						sq.Select.Columns.RemoveAt(i);
					}
				}
			}

			RemoveNonUsedOuterJoins(sq);

			return predicate;
		}

		#region Helpers

		sealed class MovingComplexityVisitor : QueryElementVisitor
		{
			ISqlExpression   _expressionToCheck = default!;
			IQueryElement?[] _ignore            = default!;
			SqlProviderFlags _sqlProviderFlags  = default!;
			int              _foundCount;
			int              _multiplier;
			bool             _notAllowedScope;

			public bool DoNotAllow { get; private set; }

			public MovingComplexityVisitor() : base(VisitMode.ReadOnly)
			{
			}

			public override void Cleanup()
			{
				_ignore            = default!;
				_expressionToCheck = default!;
				DoNotAllow         = default;
				_sqlProviderFlags  = default!;
				_multiplier        = 1;

				_foundCount = 0;

				base.Cleanup();
			}

			public bool IsAllowedToMove(SqlProviderFlags sqlProviderFlags, ISqlExpression testExpression, IQueryElement parent, params IQueryElement?[] ignore)
			{
				Cleanup();

				_sqlProviderFlags  = sqlProviderFlags;
				_ignore            = ignore;
				_expressionToCheck = testExpression;

				Visit(parent);

				return !DoNotAllow;
			}

			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (element == null)
					return null;

				if (DoNotAllow)
					return element;

				if (_ignore.Contains(element, Utils.ObjectReferenceEqualityComparer<IQueryElement?>.Default))
					return element;

				if (ReferenceEquals(element, _expressionToCheck))
				{
					if (_notAllowedScope)
					{
						DoNotAllow = true;
						return element;
					}

					_foundCount += _multiplier;

					if (_foundCount > 1)
						DoNotAllow = true;

					return element;
				}

				return base.Visit(element);
			}

			protected internal override IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
			{
				if (element.IsPositioned)
				{
					// do not count complexity for positioned order item
					if (ReferenceEquals(element.Expression, _expressionToCheck))
						return element;
				}

				return base.VisitSqlOrderByItem(element);
			}

			protected internal override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
			{
				using var scope = DoNotAllowScope(predicate.Expr1.ElementType == QueryElementType.SqlObjectExpression);
				return base.VisitInListPredicate(predicate);
			}

			protected internal override IQueryElement VisitSqlCoalesceExpression(SqlCoalesceExpression element)
			{
				if (!_sqlProviderFlags.IsSimpleCoalesceSupported)
					++_multiplier;

				base.VisitSqlCoalesceExpression(element);

				if (!_sqlProviderFlags.IsSimpleCoalesceSupported)
					--_multiplier;

				return element;
			}

			readonly struct DoNotAllowScopeStruct : IDisposable
			{
				readonly MovingComplexityVisitor _visitor;
				readonly bool                    _saveValue;

				public DoNotAllowScopeStruct(MovingComplexityVisitor visitor, bool? doNotAllow)
				{
					_visitor   = visitor;
					_saveValue = visitor._notAllowedScope;
					if (doNotAllow != null)
						visitor._notAllowedScope = doNotAllow.Value;
				}

				public void Dispose()
				{
					_visitor._notAllowedScope = _saveValue;
				}
			}

			DoNotAllowScopeStruct DoNotAllowScope(bool? doNotAllow)
			{
				return new DoNotAllowScopeStruct(this, doNotAllow);
			}
		}

		sealed class MovingOuterPredicateVisitor : QueryElementVisitor
		{
			SelectQuery                          _forQuery       = default!;
			ISqlPredicate                        _predicate      = default!;
			IReadOnlyCollection<ISqlTableSource> _currentSources = default!;

			public MovingOuterPredicateVisitor() : base(VisitMode.Transform)
			{
			}

			public ISqlPredicate CorrectReferences(SelectQuery forQuery, IReadOnlyCollection<ISqlTableSource> currentSources, ISqlPredicate predicate)
			{
				_forQuery       = forQuery;
				_predicate      = predicate;
				_currentSources = currentSources;

				return (ISqlPredicate)Visit(predicate);
			}

			public override void Cleanup()
			{
				_forQuery       = default!;
				_predicate      = default!;
				_currentSources = default!;

				base.Cleanup();
			}

			[return: NotNullIfNotNull(nameof(element))]
			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (ReferenceEquals(element, _predicate))
					return base.Visit(element);

				if (element is ISqlExpression sqlExpr and not SqlSearchCondition)
				{
					if (QueryHelper.IsDependsOnSources(sqlExpr, _currentSources) && !QueryHelper.IsDependsOnOuterSources(sqlExpr, currentSources: _currentSources))
					{
						if (sqlExpr is SqlColumn column && column.Parent == _forQuery)
							return sqlExpr;

						var withoutNullabilityCheck = sqlExpr;

						var nullabilityExpression = sqlExpr as SqlNullabilityExpression;
						if (nullabilityExpression != null)
							withoutNullabilityCheck = nullabilityExpression.SqlExpression;

						var newExpr = (ISqlExpression)_forQuery.Select.AddColumn(withoutNullabilityCheck);

						if (nullabilityExpression != null)
							newExpr = SqlNullabilityExpression.ApplyNullability(newExpr, nullabilityExpression.CanBeNull);

						return newExpr;
					}

					return element;
				}

				return base.Visit(element);
			}

			protected internal override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
			{
				// OuterApplyOptimization test
				return predicate;
			}
		}

		#endregion

	}
}
