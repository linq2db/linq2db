using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Common;
using LinqToDB.DataProvider;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public class SelectQueryOptimizerVisitor : SqlQueryVisitor
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
		bool             _isInsideNot;
		SelectQuery?     _updateQuery;
		ISqlTableSource? _updateTable;

		readonly SqlQueryColumnNestingCorrector _columnNestingCorrector      = new();
		readonly SqlQueryColumnUsageCollector   _columnUsageCollector        = new();
		readonly SqlQueryOrderByOptimizer       _orderByOptimizer            = new();
		readonly MovingComplexityVisitor        _movingComplexityVisitor     = new();
		readonly SqlExpressionOptimizerVisitor  _expressionOptimizerVisitor  = new(true);
		readonly MovingOuterPredicateVisitor    _movingOuterPredicateVisitor = new();
		readonly RemoveUnusedColumnsVisitor     _removeUnusedColumnsVisitor  = new();

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
			_isInsideNot       = false;
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

					_orderByOptimizer.OptimizeOrderBy(_root, _providerFlags);
					if (!_orderByOptimizer.IsOptimized)
						break;

					if (_orderByOptimizer.NeedsNestingUpdate) 
						CorrectColumnsNesting();

				} while (true);

				if (removeWeakJoins)
				{
					// It means that we fully optimize query
					_columnUsageCollector.CollectUsedColumns(_rootElement);
					_removeUnusedColumnsVisitor.RemoveUnusedColumns(_columnUsageCollector.UsedColumns, _root);

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
			_updateQuery       = default;
			_updateTable       = default;

			_columnNestingCorrector.Cleanup();
			_columnUsageCollector.Cleanup();
			_orderByOptimizer.Cleanup();
			_movingComplexityVisitor.Cleanup();
			_expressionOptimizerVisitor.Cleanup();
			_movingOuterPredicateVisitor.Cleanup();
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			++_version;
			return base.NotifyReplaced(newElement, oldElement);
		}

		protected override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			var saveQuery = _applySelect;

			if (element.JoinType == JoinType.CrossApply || element.JoinType == JoinType.OuterApply)
				_applySelect = element.Table.Source as SelectQuery;
			else
				_applySelect = null;

			var newElement = base.VisitSqlJoinedTable(element);

			_applySelect = saveQuery;

			return newElement;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveSetOperatorCount = selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0;
			var saveParent           = _parentSelect;
			var saveIsInsideNot      = _isInsideNot;

			_parentSelect = selectQuery;
			_isInsideNot = false;

			if (saveParent == null)
			{
#if DEBUG
				var before = selectQuery.ToDebugString();
#endif
				// only once
				_expressionOptimizerVisitor.Optimize(_evaluationContext, NullabilityContext.GetContext(selectQuery), null, _dataOptions, _mappingSchema, selectQuery, visitQueries: true, isInsideNot: false, reduceBinary: false);
			}

			var newQuery = (SelectQuery)base.VisitSqlQuery(selectQuery);

			if (_correcting == null)
			{
				_parentSelect = selectQuery;

				if (saveParent != null)
				{
					OptimizeColumns(selectQuery);
				}

				do
				{
					var currentVersion = _version;
					var isModified     = false;

					if (OptimizeSubQueries(selectQuery))
					{
						isModified = true;
					}
					
					if (MoveOuterJoinsToSubQuery(selectQuery, processMultiColumn: false))
					{
						isModified = true;
					}

					if (OptimizeApplies(selectQuery, _providerFlags.IsApplyJoinSupported))
					{
						isModified = true;
						EnsureReferencesCorrected(selectQuery);
					}

					if (MoveOuterJoinsToSubQuery(selectQuery, processMultiColumn: true))
					{
						isModified = true;
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

					if (CorrectMultiTables(selectQuery))
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

					_expressionOptimizerVisitor.Optimize(_evaluationContext, NullabilityContext.GetContext(selectQuery), null, _dataOptions, _mappingSchema, selectQuery, visitQueries : true, isInsideNot : false, reduceBinary: false);
				}

				if (saveSetOperatorCount != (selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0))
				{
					// Do it again. Appended new SetOperators. For ensuring how it works check CteTests
					//
					newQuery = (SelectQuery)VisitSqlQuery(selectQuery);
				}

				_parentSelect = saveParent;
			}

			_isInsideNot = saveIsInsideNot;

			return newQuery;
		}

		protected override IQueryElement VisitSqlSetOperator(SqlSetOperator element)
		{
			var saveCurrent = _currentSetOperator;

			_currentSetOperator = element;

			var newElement = base.VisitSqlSetOperator(element);

			_currentSetOperator = saveCurrent;

			return newElement;
		}

		protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			var saveCurrent        = _currentSetOperator;

			_currentSetOperator = null;
			
			var newElement = base.VisitSqlTableSource(element);

			_currentSetOperator = saveCurrent;

			return newElement;
		}

		protected override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			var saveInsubquery = _inSubquery;

			_inSubquery = predicate.SubQuery;
			var newNode = base.VisitInSubQueryPredicate(predicate);
			_inSubquery = saveInsubquery;

			return newNode;
		}

		protected override IQueryElement VisitSqlOrderByClause(SqlOrderByClause element)
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

		protected override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			_updateQuery = element.SelectQuery;
			_updateTable = element.Update.Table as ISqlTableSource ?? element.Update.TableSource;
			var result = base.VisitSqlUpdateStatement(element);
			_updateQuery = null;
			_updateTable = null;
			return result;
		}

		protected override IQueryElement VisitSqlNullabilityExpression(SqlNullabilityExpression element)
		{
			var sqlExpression = Visit(element.SqlExpression);
			if (sqlExpression is SelectQuery { GroupBy.IsEmpty: true } selectQuery)
			{
				var nullabilityContext = NullabilityContext.GetContext(selectQuery);
				if (selectQuery.Select.Columns.All(c => QueryHelper.ContainsAggregationFunction(c.Expression) && !c.Expression.CanBeNullable(nullabilityContext)))
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

			if (selectQuery.From.Tables.Count == 1 &&
			    selectQuery.From.Tables[0].Source is SelectQuery { HasSetOperators: true } mainSubquery)
			{
				var isOk = true;

				if (!selectQuery.HasSetOperators)
				{
					isOk = selectQuery.OrderBy.IsEmpty && selectQuery.Where.IsEmpty && selectQuery.GroupBy.IsEmpty && !selectQuery.Select.HasModifier;
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

					if (mainSubquery.SetOperators.All(so => so.Operation == operation))
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

				if (setOperator.SelectQuery.From.Tables.Count == 1 &&
				    setOperator.SelectQuery.From.Tables[0].Source is SelectQuery { HasSetOperators: true } subQuery)
				{
					if (subQuery.SetOperators.All(so => so.Operation == setOperator.Operation))
					{
						var allColumns = setOperator.Operation != SetOperation.UnionAll;

						if (allColumns)
						{
							if (subQuery.Select.Columns.Count != selectQuery.Select.Columns.Count)
								continue;
						}

						var newIndexes =
							new Dictionary<ISqlExpression, int>(Utils.ObjectReferenceEqualityComparer<ISqlExpression>
								.Default);

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
							if (op.SelectQuery.SourceID == 115)
							{

							}

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

		bool OptimizeGroupBy(SelectQuery selectQuery)
		{
			var isModified = false;

			if (!selectQuery.GroupBy.IsEmpty)
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
			}

			return isModified;
		}

		bool CorrectColumns(SelectQuery selectQuery)
		{
			var isModified = false;
			if (!selectQuery.GroupBy.IsEmpty && selectQuery.Select.Columns.Count == 0)
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

			base.Visit(selectQuery);

			_correcting = null;
		}

		bool IsRemovableJoin(SqlJoinedTable join)
		{
			if (join.IsWeak)
				return true;

			if (join.JoinType == JoinType.Left)
			{
				if (join.Condition.IsFalse())
					return true;
			}

			if (join.JoinType is JoinType.Left or JoinType.OuterApply)
			{
				if ((join.Cardinality & SourceCardinality.One) != 0)
					return true;

				if (join.Table.Source is SelectQuery joinQuery)
				{
					if (joinQuery.Where.SearchCondition.IsFalse())
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

					if (join.Table.Source is SelectQuery subQuery && (join.JoinType is JoinType.Left or JoinType.OuterApply))
					{
						var canRemoveEmptyJoin = false;

						if (join.JoinType == JoinType.Left && join.Condition.IsFalse())
							canRemoveEmptyJoin = true;
						else if (join.JoinType == JoinType.OuterApply && subQuery.Where.SearchCondition.IsFalse())
							canRemoveEmptyJoin = true;

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
									if (!type.IsNullableType())
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
			if (query.Select.TakeValue is SqlValue { Value: 1 })
				return true;

			if (query.GroupBy.IsEmpty && query.Select.Columns.Count > 0 && query.Select.Columns.All(c => QueryHelper.ContainsAggregationFunction(c.Expression)))
				return true;

			if (query.From.Tables.Count == 1 && query.From.Tables[0].Source is SelectQuery subQuery)
				return IsLimitedToOneRecord(subQuery);

			return false;
		}

		static bool IsComplexQuery(SelectQuery query)
		{
			var accessibleSources = new HashSet<ISqlTableSource>();

			var complexFound = false;
			foreach (var source in QueryHelper.EnumerateAccessibleSources(query))
			{
				accessibleSources.Add(source);
				if (source is SelectQuery q && (q.From.Tables.Count != 1 || q.GroupBy.IsEmpty && QueryHelper.EnumerateJoins(q).Any()))
				{
					complexFound = true;
					break;
				}
			}

			if (complexFound)
				return true;

			var usedSources = new HashSet<ISqlTableSource>();
			QueryHelper.CollectUsedSources(query, usedSources);

			return usedSources.Count > accessibleSources.Count;
		}

		bool OptimizeDistinct(SelectQuery selectQuery)
		{
			if (!selectQuery.Select.IsDistinct || !selectQuery.Select.OptimizeDistinct)
				return false;

			if (IsComplexQuery(selectQuery))
				return false;

			if (IsLimitedToOneRecord(selectQuery))
			{
				// we can simplify query if we take only one record
				selectQuery.Select.IsDistinct = false;
				return true;
			}

			if (!selectQuery.GroupBy.IsEmpty)
			{
				if (selectQuery.GroupBy.Items.All(gi => selectQuery.Select.Columns.Any(c => c.Expression.Equals(gi))))
				{
					selectQuery.GroupBy.Items.Clear();
					return true;
				}
			}

			var table = selectQuery.From.Tables[0];

			var keys = new List<IList<ISqlExpression>>();

			QueryHelper.CollectUniqueKeys(selectQuery, includeDistinct: false, keys);
			QueryHelper.CollectUniqueKeys(table, keys);
			if (keys.Count == 0)
				return false;

			var expressions = new HashSet<ISqlExpression>(selectQuery.Select.Columns.Select(static c => c.Expression));
			var foundUnique = false;

			foreach (var key in keys)
			{
				foundUnique = true;
				foreach (var expr in key)
				{
					if (!expressions.Contains(expr))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					break;

				foundUnique = true;
				foreach (var expr in key)
				{
					var underlyingField = QueryHelper.GetUnderlyingField(expr);
					if (underlyingField == null || !expressions.Contains(underlyingField))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					break;
			}

			var isModified = false;
			if (foundUnique)
			{
				// We have found that distinct columns has unique key, so we can remove distinct
				selectQuery.Select.IsDistinct = false;
				isModified = true;
			}

			return isModified;
		}

		static void ApplySubsequentOrder(SelectQuery mainQuery, SelectQuery subQuery)
		{
			if (subQuery.OrderBy.Items.Count > 0)
			{
				var filterItems = mainQuery.Select.IsDistinct || !mainQuery.GroupBy.IsEmpty;

				foreach (var item in subQuery.OrderBy.Items)
				{
					if (filterItems)
					{
						var skip = true;
						foreach (var column in mainQuery.Select.Columns)
						{
							if (column.Expression is SqlColumn sc && sc.Expression.Equals(item.Expression))
							{
								skip = false;
								break;
							}
						}

						if (skip)
							continue;
					}

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

		bool OptimizeApply(SqlJoinedTable joinTable, bool isApplySupported)
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
				var isAgg = sql.Select.Columns.Any(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));

				isApplySupported = isApplySupported && (joinTable.JoinType == JoinType.CrossApply ||
				                                        joinTable.JoinType == JoinType.OuterApply);

				if (isApplySupported && sql.Select.HasModifier && _providerFlags.IsSubQueryTakeSupported)
					return optimized;

				if (isApplySupported && isAgg)
					return optimized;

				if (isAgg)
					return optimized;

				var skipValue = sql.Select.SkipValue;
				var takeValue = sql.Select.TakeValue;

				if (sql.Select.TakeHints != null)
				{
					if (isApplySupported)
						return optimized;
					throw new LinqToDBException("SQL query requires TakeHints in CROSS/OUTER query, which are not supported by provider");
				}

				ISqlExpression?       rnExpression = null;
				List<ISqlExpression>? partitionBy  = null;

				if (skipValue != null || takeValue != null)
				{
					if (!_providerFlags.IsWindowFunctionsSupported)
						return optimized;

					var parameters = new List<ISqlExpression>();

					var found   = new HashSet<ISqlExpression>();

					if (sql.Select.IsDistinct)
					{
						found.AddRange(sql.Select.Columns.Select(c => c.Expression));
					}

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

					if (found.Count > 0)
					{
						partitionBy = found.ToList();
					}

					var rnBuilder = new StringBuilder();
					rnBuilder.Append("ROW_NUMBER() OVER (");

					if (partitionBy != null)
					{
						rnBuilder.Append("PARTITION BY ");
						for (int i = 0; i < partitionBy.Count; i++)
						{
							if (i > 0)
								rnBuilder.Append(", ");

							rnBuilder.Append(CultureInfo.InvariantCulture, $"{{{parameters.Count}}}");
							parameters.Add(partitionBy[i]);
						}
					}

					var orderByItems = sql.OrderBy.Items.ToList();

					if (sql.OrderBy.IsEmpty)
					{
						if (partitionBy != null)
							orderByItems.Add(new SqlOrderByItem(partitionBy[0], false, false));
						else if (!_providerFlags.IsRowNumberWithoutOrderBySupported)
						{
							if (sql.Select.Columns.Count == 0)
							{
								throw new InvalidOperationException("OrderBy not specified for limited recordset.");
							}

							orderByItems.Add(new SqlOrderByItem(sql.Select.Columns[0].Expression, false, false));
						}
					}

					if (orderByItems.Count > 0)
					{
						if (partitionBy != null)
							rnBuilder.Append(' ');

						rnBuilder.Append("ORDER BY ");
						for (int i = 0; i < orderByItems.Count; i++)
						{
							if (i > 0)
								rnBuilder.Append(", ");

							var orderItem = orderByItems[i];
							rnBuilder.Append(CultureInfo.InvariantCulture, $"{{{parameters.Count}}}");
							if (orderItem.IsDescending)
								rnBuilder.Append(" DESC");

							parameters.Add(orderItem.Expression);
						}
					}

					rnBuilder.Append(')');

					rnExpression = new SqlExpression(typeof(long), rnBuilder.ToString(), Precedence.Primary,
						SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters.ToArray());
				}

				var whereToIgnore = new List<IQueryElement> { sql.Where, sql.Select };

				// add join conditions
				// Check SelectManyTests.Basic9 for Access
				foreach (var join in sql.From.Tables.SelectMany(t => t.Joins))
				{
					if (join.JoinType == JoinType.Inner && join.Table.Source is SqlTable)
						whereToIgnore.Add(join.Condition);
					else
						break;
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
								if (predicate is not SqlPredicate.ExprExpr expExpr || expExpr.Operator != SqlPredicate.Operator.Equal)
								{
									return optimized;
								}
							}

							if (!sql.GroupBy.IsEmpty)
							{
								// we can only optimize SqlPredicate.ExprExpr
								if (predicate is not SqlPredicate.ExprExpr expExpr)
								{
									return optimized;
								}

								// check that used key in grouping
								if (!sql.GroupBy.Items.Any(gi => QueryHelper.SameWithoutNullablity(gi, expExpr.Expr1) || QueryHelper.SameWithoutNullablity(gi, expExpr.Expr2)))
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

					sql.Select.SkipValue = null;
					sql.Select.TakeValue = null;

					var rnColumn = sql.Select.AddNewColumn(rnExpression);
					rnColumn.RawAlias = "rn";

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

			var allowed = _movingComplexityVisitor.IsAllowedToMove(column, parent : parentQuery,
				nullability,
				_expressionOptimizerVisitor,
				_dataOptions,
				_mappingSchema,
				_evaluationContext,
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

			if (subQuery.From.Tables.Count == 0)
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

			if (!subQuery.GroupBy.IsEmpty)
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

			if (!subQuery.Where.IsEmpty)
			{
				parentQuery.Where.SearchCondition = QueryHelper.MergeConditions(parentQuery.Where.SearchCondition, subQuery.Where.SearchCondition);
			}

			if (!subQuery.Having.IsEmpty)
			{
				parentQuery.Having.SearchCondition = QueryHelper.MergeConditions(parentQuery.Having.SearchCondition, subQuery.Having.SearchCondition);
			}

			

			if (subQuery.Select.IsDistinct)
				parentQuery.Select.IsDistinct = true;

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

			if (subQuery.OrderBy.Items.Count > 0)
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
				if (parentQuery.Select.Columns.All(c => c.Expression is SqlColumn))
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

			if (!parentQuery.GroupBy.IsEmpty)
			{
				if (!subQuery.GroupBy.IsEmpty)
					return false;

				if (parentQuery.Select.Columns.Count == 0)
					return false;

				// Check that all grouping columns are simple
				if (parentQuery.GroupBy.EnumItems().Any(gi =>
				    {
					    if (gi is not SqlColumn sc)
						    return true;

					    if (QueryHelper.UnwrapNullablity(sc.Expression) is not (SqlColumn or SqlField or SqlParameter or SqlValue))
						    return true;

					    return false;
				    }))
				{
					return false;
				}
			}

			var nullability = NullabilityContext.GetContext(parentQuery);

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
					if (subQuery.Select.HasModifier || subQuery.HasSetOperators || !subQuery.GroupBy.IsEmpty)
					{
						// not allowed to move to parent if it has aggregates
						return false;
					}
				}

				if (containsWindowFunction)
				{
					if (subQuery.Select.HasModifier || subQuery.HasSetOperators || (!parentQuery.Where.IsEmpty && !subQuery.Where.IsEmpty) || !subQuery.GroupBy.IsEmpty)
					{
						// not allowed to break window
						return false;
					}
				}

				if (!parentQuery.GroupBy.IsEmpty)
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
					if (!parentQuery.IsSimpleOrSet)
					{
						// not allowed to break query window 
						return false;
					}
				}

				if (QueryHelper.ContainsAggregationFunction(column.Expression))
				{
					if (parentQuery.Having.HasElement(column) || parentQuery.Select.GroupBy.HasElement(column))
					{
						// aggregate moving not allowed
						return false;
					}

					if (!IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, column.Expression, ignoreWhere : true, inGrouping: !subQuery.GroupBy.IsEmpty))
					{
						// Column expression is complex and Column has more than one reference
						return false;
					}
				}
				else
				{
					if (!IsColumnExpressionAllowedToMoveUp(parentQuery, nullability, column, column.Expression, ignoreWhere : false, inGrouping: !subQuery.GroupBy.IsEmpty))
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

			if (!subQuery.GroupBy.IsEmpty && !parentQuery.GroupBy.IsEmpty)
				return false;

			// Check possible moving Where to Having
			//
			{
				if (!parentQuery.Where.IsEmpty)
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
						if (parentQuery.GroupBy.IsEmpty && !subQuery.GroupBy.IsEmpty)
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
			        || parentQuery.From.Tables.Any(static t => t.Joins.Count > 0)))
			{
				return false;
			}

			if (_currentSetOperator?.SelectQuery == parentQuery || parentQuery.HasSetOperators)
			{
				// processing parent query as part of Set operation
				//

				if (subQuery.Select.HasModifier)
					return false;

				if (!subQuery.Select.OrderBy.IsEmpty)
				{
					return false;
				}
			}

			if (parentQuery.Select.IsDistinct)
			{
				// Common check for Distincts

				if (!subQuery.GroupBy.Having.IsEmpty)
					return false;

				if (subQuery.Select.SkipValue    != null || subQuery.Select.TakeValue    != null ||
				    parentQuery.Select.SkipValue != null || parentQuery.Select.TakeValue != null)
				{
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

			if (subQuery.Select.IsDistinct != parentQuery.Select.IsDistinct)
			{
				if (subQuery.Select.IsDistinct)
				{
					// Columns in parent query should match
					//

					if (!(parentQuery.Select.Columns.Count == 0 || subQuery.Select.Columns.All(sc =>
						    parentQuery.Select.Columns.Any(pc => ReferenceEquals(QueryHelper.UnwrapNullablity(pc.Expression), sc)))))
					{
						return false;
					}

					if (parentQuery.Select.Columns.Count > 0 && parentQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
					{
						return false;
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

				if (!parentQuery.Select.OrderBy.IsEmpty)
					return false;

				if (!parentQuery.Select.Where.IsEmpty)
				{
					if (subQuery.Select.TakeValue != null || subQuery.Select.SkipValue != null)
						return false;
				}

				if (parentQuery.Select.Columns.Any(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
				{
					return false;
				}
			}

			if (subQuery.Select.HasModifier || !subQuery.Where.IsEmpty)
			{
				if (tableSource.Joins.Any(j => j.JoinType == JoinType.Right || j.JoinType == JoinType.RightApply ||
				                               j.JoinType == JoinType.Full  || j.JoinType == JoinType.FullApply))
				{
					return false;
				}
			}

			if (!_providerFlags.AcceptsOuterExpressionInAggregate)
			{
				if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType != JoinType.Inner))
				{
					if (subQuery.Select.Columns.Any(c => IsInsideAggregate(parentQuery, c)))
					{
						if (QueryHelper.IsDependsOnOuterSources(subQuery))
							return false;
					}
				}
			}

			if (parentQuery.GroupBy.IsEmpty && !subQuery.GroupBy.IsEmpty)
			{
				if (tableSource.Joins.Count > 0)
					return false;
				if (parentQuery.From.Tables.Count > 1)
					return false;

/*
				throw new NotImplementedException();

				if (selectQuery.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression)))
					return false;
*/
			}

			if (subQuery.Select.TakeHints != null && parentQuery.Select.TakeValue != null)
				return false;

			if (subQuery.HasSetOperators)
			{
				if (parentQuery.HasSetOperators)
					return false;

				if (parentQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
				{
					if (subQuery.SetOperators.Any(so => so.Operation != SetOperation.UnionAll))
						return false;
				}

				if (!parentQuery.Select.Where.IsEmpty || !parentQuery.Select.Having.IsEmpty || parentQuery.Select.HasModifier || !parentQuery.OrderBy.IsEmpty)
					return false;

				var operation = subQuery.SetOperators[0].Operation;

				if (_currentSetOperator != null && _currentSetOperator.Operation != operation)
					return false;

				if (!subQuery.SetOperators.All(so => so.Operation == operation))
					return false;
			}

			// Do not optimize t.Field IN (SELECT x FROM o)
			if (parentQuery == _inSubquery && (subQuery.Select.HasModifier || subQuery.HasSetOperators))
			{
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

			if (!subQuery.GroupBy.IsEmpty)
				return false;

			if (subQuery.Select.HasModifier)
				return false;

			if (subQuery.HasSetOperators)
				return false;

			if (!subQuery.GroupBy.IsEmpty)
				return false;

			// Rare case when LEFT join is empty. We move search condition up. See TestDefaultExpression_22 test.
			if (joinTable.JoinType == JoinType.Left && subQuery.Where.SearchCondition.IsFalse())
			{
				subQuery.Where.SearchCondition.Predicates.Clear();
				joinTable.Condition.Predicates.Clear();
				joinTable.Condition.Predicates.Add(SqlPredicate.False);

				// Continue in next loop
				return true;
			}

			var moveConditionToQuery = joinTable.JoinType == JoinType.Inner || joinTable.JoinType == JoinType.CrossApply;

			if (joinTable.JoinType != JoinType.Inner)
			{
				if (!subQuery.Where.IsEmpty)
				{
					if (joinTable.JoinType == JoinType.OuterApply)
					{
						if (_providerFlags.IsOuterApplyJoinSupportsCondition)
							moveConditionToQuery = false;
						else
							return false;
					}
					else if (joinTable.JoinType == JoinType.CrossApply)
					{
						if (_providerFlags.IsCrossApplyJoinSupportsCondition)
							moveConditionToQuery = false;
					}
					else if (joinTable.JoinType == JoinType.Left)
					{
						if (joinTable.Condition.IsTrue())
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

				if (!subQuery.Select.Columns.All(c =>
					{
						var columnExpression = QueryHelper.UnwrapCastAndNullability(c.Expression);

						if (columnExpression is SqlColumn or SqlField or SqlTable or SqlBinaryExpression)
							return true;
						if (columnExpression is SqlFunction func)
							return !func.IsAggregate;
						return false;
					}))
				{
					return false;
				}
			}

			if (subQuery.Select.Columns.Any(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
				return false;

			// Actual modification starts from this point
			//

			if (!subQuery.Where.IsEmpty)
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
				foreach(var column in subQuery.Select.Columns)
				{
					selectQuery.Select.AddColumn(column.Expression);
				}
			}

			foreach (var column in subQuery.Select.Columns)
			{
				NotifyReplaced(column.Expression, column);
			}

			if (subQuery.OrderBy.Items.Count > 0 && !QueryHelper.IsAggregationQuery(selectQuery))
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

		protected override IQueryElement VisitSqlFromClause(SqlFromClause element)
		{
			element = (SqlFromClause)base.VisitSqlFromClause(element);

			if (_correcting != null)
				return element;

			return element;
		}

		bool OptimizeSubQueries(SelectQuery selectQuery)
		{
			var replaced = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
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
				if (tableSource.Joins.Count == 0 && tableSource.Source is SelectQuery { From.Tables.Count: 0, Where.IsEmpty: true, HasSetOperators: false } subQuery)
				{
					if (selectQuery.From.Tables.Count == 1)
					{
						if (!selectQuery.GroupBy.IsEmpty
						    || !selectQuery.Having.IsEmpty
						    || !selectQuery.OrderBy.IsEmpty)
						{
							continue;
						}
					}

					replaced = true;

					foreach (var c in subQuery.Select.Columns)
					{
						NotifyReplaced(c.Expression, c);
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
					foreach (var join in tableSource.Joins)
					{
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

		SelectQuery MoveMutliTablesToSubquery(SelectQuery selectQuery)
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
					MoveMutliTablesToSubquery(selectQuery);

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
				for(var nextIndex = index + 1; nextIndex < selectQuery.Select.Columns.Count; nextIndex++)
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

		bool OptimizeApplies(SelectQuery selectQuery, bool isApplySupported)
		{
			var optimized = false;

			foreach (var table in selectQuery.From.Tables)
			{
				foreach (var join in table.Joins)
				{
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply || join.JoinType == JoinType.FullApply || join.JoinType == JoinType.RightApply)
					{
						if (OptimizeApply(join, isApplySupported))
						{
							optimized = true;
						}
					}
				}
			}

			return optimized;
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
					if (join.JoinType == JoinType.Inner && join.Condition.IsTrue())
					{
						if (_providerFlags.IsCrossJoinSupported && (table.Joins.Count > 1 || !QueryHelper.IsDependsOnSource(selectQuery.Where, join.Table.Source)))
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

			if (selectQuery.Select.Columns.Count == 1)
			{
				var column = selectQuery.Select.Columns[0];
				if (QueryHelper.IsAggregationFunction(column.Expression) && !QueryHelper.IsWindowFunction(column.Expression))
					return true;

				if (selectQuery.Select.From.Tables.Count == 0)
					return true;
			}

			if (!selectQuery.Where.IsEmpty)
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
					if (keys.Any(kl => kl.All(k => foundEquality.Contains(k))))
						return true;
				}
			}

			return false;
		}

		static int CountUsage(SelectQuery rootQuery, SqlColumn column)
		{
			int counter = 0;

			rootQuery.VisitParentFirstAll(e =>
			{
				// do not search in the same query
				if (e is SelectQuery sq && sq == column.Parent)
					return false;

				if (Equals(e, column))
				{
					++counter;
				}

				return counter < 2;
			});

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

		void MoveDuplicateUsageToSubQuery(SelectQuery query)
		{
			var subQuery = new SelectQuery();

			subQuery.DoNotRemove = true;

			subQuery.From.Tables.AddRange(query.From.Tables);

			query.Select.From.Tables.Clear();
			_ = query.Select.From.Table(subQuery);

			_columnNestingCorrector.CorrectColumnNesting(query);
		}

		bool ProviderOuterCanHandleSeveralColumnsQuery(SelectQuery selectQuery)
		{
			if (_providerFlags.IsApplyJoinSupported)
				return true;

			if (_providerFlags.IsWindowFunctionsSupported)
			{
				if (!selectQuery.GroupBy.IsEmpty)
				{
					return false;
				}

				if (selectQuery.Select.TakeValue != null)
				{
					if (!selectQuery.Where.IsEmpty)
					{
						if (selectQuery.Where.SearchCondition.Predicates.Any(predicate => predicate is not SqlPredicate.ExprExpr expExpr || expExpr.Operator != SqlPredicate.Operator.Equal))
						{
							// OuterApply cannot be converted in this case
							return false;
						}
					}
				}

				if (selectQuery.From.Tables is [{ Source: SelectQuery baseQuery }])
				{
					return ProviderOuterCanHandleSeveralColumnsQuery(baseQuery);
				}

				// provider can handle this query
				return true;
			}

			return false;
		}

		bool MoveOuterJoinsToSubQuery(SelectQuery selectQuery, bool processMultiColumn)
		{
			var currentVersion = _version;

			EvaluationContext? evaluationContext = null;

			var selectQueries = QueryHelper.EnumerateAccessibleSources(selectQuery).OfType<SelectQuery>().ToList();
			foreach (var sq in selectQueries)
			{
				for (var ti = 0; ti < sq.From.Tables.Count; ti++)
				{
					var table = sq.From.Tables[ti];

					for (int j = table.Joins.Count - 1; j >= 0; j--)
					{
						var join            = table.Joins[j];
						var joinQuery       = join.Table.Source as SelectQuery;

						if (join.JoinType == JoinType.OuterApply ||
						    join.JoinType == JoinType.Left       ||
						    join.JoinType == JoinType.CrossApply)
						{
							bool? isSingeRecord = null;

							if (join.JoinType == JoinType.CrossApply)
							{
								if ((join.Cardinality & SourceCardinality.One) != 0)
								{
									if (join.IsSubqueryExpression)
										isSingeRecord = true;
								}

								if (_applySelect is null && isSingeRecord is null)
								{
									continue;
								}
							}

							evaluationContext ??= new EvaluationContext();

							if (joinQuery != null && joinQuery.Select.Columns.Count > 0)
							{
								if (joinQuery.Select.Columns.Count > 1)
								{
									if (!processMultiColumn || ProviderOuterCanHandleSeveralColumnsQuery(joinQuery))
									{
										// provider can handle this query
										continue;
									}
								}

								if (!(isSingeRecord == true || IsLimitedToOneRecord(sq, joinQuery, evaluationContext)))
									continue;

								// do not move to subquery expression if update table in the query.
								if (_updateTable != null && joinQuery.HasElement(_updateTable))
									continue;

								var isNoTableQuery = joinQuery.From.Tables.Count == 0;

								if (!isNoTableQuery)
								{
									if (!SqlProviderHelper.IsValidQuery(joinQuery, parentQuery : sq, fakeJoin : null, columnSubqueryLevel : 0, _providerFlags, out _))
										continue;
								}

								var isValid = true;

								foreach (var testedColumn in joinQuery.Select.Columns)
								{
									// where we can start analyzing that we can move join to subquery

									var usageCount = CountUsage(sq, testedColumn);
									var isUnique   = usageCount <= 1;

									if (!isUnique)
									{
										if (!processMultiColumn || join.JoinType == JoinType.Left)
										{
											isValid = false;
											break;
										};

										if (_providerFlags.IsApplyJoinSupported)
										{
											MoveDuplicateUsageToSubQuery(sq);
											// will be processed in the next step
											ti = -1;
											isValid = false;
											break;
										}	
									}

									if (usageCount == 1 && !IsInSelectPart(sq, testedColumn))
									{
										var moveToSubquery = IsInOrderByPart(sq, testedColumn) && !_providerFlags.IsSubQueryOrderBySupported;
										if (moveToSubquery)
										{
											MoveDuplicateUsageToSubQuery(sq);
											// will be processed in the next step
											ti = -1;

											isValid = false;
											break;
										}
									}

									if (testedColumn.Expression is SqlFunction function)
									{
										if (function.IsAggregate)
										{
											if (!_providerFlags.AcceptsOuterExpressionInAggregate && IsInsideAggregate(sq.Select, testedColumn))
											{
												if (_providerFlags.IsApplyJoinSupported)
												{
													// Well, provider can process this query as OUTER APPLY
													isValid = false;
													break;
												}

												MoveDuplicateUsageToSubQuery(sq);
												// will be processed in the next step
												ti      = -1;
												isValid = false;
												break;
											}

											if (!_providerFlags.IsCountSubQuerySupported)
											{
												isValid = false;
												break;
											}
										}
									}
								}

								if (!isValid)
									continue;

								// moving whole join to subquery

								table.Joins.RemoveAt(j);
								joinQuery.Where.ConcatSearchCondition(join.Condition);

								var isNullable = join.JoinType is JoinType.Left or JoinType.OuterApply;

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

									var replacement = isNullable ? SqlNullabilityExpression.ApplyNullability(queryToReplace, true) : queryToReplace;

									NotifyReplaced(replacement, testedColumn);
								}
							}
						}
					}
				}
			}

			if (_version != currentVersion)
			{
				EnsureReferencesCorrected(selectQuery);

				_columnNestingCorrector.CorrectColumnNesting(selectQuery);

				return true;
			}

			return false;
		}

		protected override IQueryElement VisitCteClause(CteClause element)
		{
			var saveIsInRecursiveCte = _isInRecursiveCte;
			if (element.IsRecursive)
				_isInRecursiveCte = true;

			var saveParent = _parentSelect;
			_parentSelect = null;
			
			var newElement = base.VisitCteClause(element);

			_parentSelect = saveParent;

			_isInRecursiveCte = saveIsInRecursiveCte;

			return newElement;
		}

		protected override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
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

			if (sq.GroupBy.IsEmpty && !sq.HasSetOperators)
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

			return predicate;
		}

		#region Helpers

		sealed class MovingComplexityVisitor : QueryElementVisitor
		{
			ISqlExpression                _expressionToCheck = default!;
			IQueryElement?[]              _ignore            = default!;
			NullabilityContext            _nullability       = default!;
			EvaluationContext             _evaluationContext = default!;
			SqlExpressionOptimizerVisitor _optimizerVisitor  = default!;
			DataOptions                   _dataOptions       = default!;
			MappingSchema                 _mappingSchema     = default!;
			bool                          _isInsideNot;
			int                           _foundCount;
			bool                          _notAllowedScope;
			bool                          _doNotAllow;

			public bool DoNotAllow
			{
				get => _doNotAllow;
				private set => _doNotAllow = value;
			}

			public MovingComplexityVisitor() : base(VisitMode.ReadOnly)
			{
			}

			public void Cleanup()
			{
				_ignore            = default!;
				_expressionToCheck = default!;
				_doNotAllow        = default;
				_nullability       = default!;
				_evaluationContext = default!;
				_optimizerVisitor  = default!;
				_dataOptions       = default!;
				_mappingSchema     = default!;

				_foundCount = 0;
				_isInsideNot       = default;
			}

			public bool IsAllowedToMove(ISqlExpression testExpression, IQueryElement parent, NullabilityContext nullability, SqlExpressionOptimizerVisitor optimizerVisitor, DataOptions dataOptions, MappingSchema mappingSchema,
				EvaluationContext evaluationContext, params IQueryElement?[] ignore)
			{
				_ignore            = ignore;
				_expressionToCheck = testExpression;
				_nullability       = nullability;
				_evaluationContext = evaluationContext;
				_optimizerVisitor  = optimizerVisitor;
				_dataOptions       = dataOptions;
				_mappingSchema     = mappingSchema;
				_doNotAllow        = default;
				_foundCount        = 0;
				_isInsideNot       = default;

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

					++_foundCount;

					if (_foundCount > 1)
						DoNotAllow = true;

					return element;
				}

				return base.Visit(element);
			}

			protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
			{
				IQueryElement reduced = predicate.Reduce(_nullability, _evaluationContext, _isInsideNot, _dataOptions.LinqOptions);
				if (!ReferenceEquals(reduced, predicate))
				{
					reduced = _optimizerVisitor.Optimize(_evaluationContext, _nullability, null, _dataOptions, _mappingSchema, reduced, false, _isInsideNot, true);

					Visit(reduced);
				}
				else
					base.VisitExprExprPredicate(predicate);

				return predicate;
			}

			protected override IQueryElement VisitSqlOrderByItem(SqlOrderByItem element)
			{
				if (element.IsPositioned)
				{
					// do not count complexity for positioned order item
					if (ReferenceEquals(element.Expression, _expressionToCheck))
						return element;
				}

				return base.VisitSqlOrderByItem(element);
			}

			protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
			{
				var saveIsInsideNot = _isInsideNot;
				_isInsideNot = false;
				var newElement =  base.VisitSqlQuery(selectQuery);
				_isInsideNot = saveIsInsideNot;
				return newElement;
			}

			protected override IQueryElement VisitNotPredicate(SqlPredicate.Not predicate)
			{
				var saveValue = _isInsideNot;
				_isInsideNot = true;

				var result = base.VisitNotPredicate(predicate);

				_isInsideNot = saveValue;

				return result;
			}

			protected override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
			{
				using var scope = DoNotAllowScope(predicate.Expr1.ElementType == QueryElementType.SqlObjectExpression);
				return base.VisitInListPredicate(predicate);
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

			public void Cleanup()
			{
				_forQuery       = default!;
				_predicate      = default!;
				_currentSources = default!;
			}

			[return: NotNullIfNotNull(nameof(element))]
			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (ReferenceEquals(element, _predicate))
					return base.Visit(element);

				if (element is ISqlExpression sqlExpr and not SqlSearchCondition)
				{
					if (QueryHelper.IsDependsOnSources(sqlExpr, _currentSources) && !QueryHelper.IsDependsOnOuterSources(sqlExpr, currentSources : _currentSources))
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

			protected override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
			{
				// OuterApplyOptimization test
				return predicate;
			}
		}

		sealed class RemoveUnusedColumnsVisitor : QueryElementVisitor
		{
			private readonly HashSet<SqlSelectClause> _visitedFromCte = [];
			private IReadOnlyCollection<SqlColumn>    _usedColumns    = null!;

			public RemoveUnusedColumnsVisitor() : base(VisitMode.Modify)
			{
			}

			public void RemoveUnusedColumns(IReadOnlyCollection<SqlColumn> usedColumns, IQueryElement element)
			{
				if (usedColumns.Count == 0)
					return;

				_usedColumns = usedColumns;

				Visit(element);
			}

			public void Cleanup()
			{
				_usedColumns = null!;
				_visitedFromCte.Clear();
			}

			protected override IQueryElement VisitCteClause(CteClause element)
			{
				_visitedFromCte.Add(element.Body!.Select.Select);
				ProcessSelectClause(element.Body!.Select.Select, element);

				return base.VisitCteClause(element);
			}

			protected override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
			{
				if (!_visitedFromCte.Contains(element))
					ProcessSelectClause(element, null);

				return base.VisitSqlSelectClause(element);
			}

			private void ProcessSelectClause(SqlSelectClause element, CteClause? cte)
			{
				for (var i = element.Columns.Count - 1; i >= 0; i--)
				{
					var column = element.Columns[i];

					if (!_usedColumns.Contains(column))
					{
						element.Columns.RemoveAt(i);

						// cte with unused columns could be defined with empty Field list
						if (cte?.Fields.Count > 0)
							cte.Fields.RemoveAt(i);
					}
				}

				// add fake 1 column for cases when SELECT * is not valid/undesirable
				if (element.Columns.Count == 0
					&& (
						// see CteTests.TestNoColumns
						// we don't want SQL like
						// "WITH cte (SELECT * ..."
						// to expose all columns implicitly
						cte != null
						// see JoinTests.Issue3311Test3
						// "SELECT *" table-less syntax is not valid
						// in theory it could be lifted for providers with Fake column, but we don't have this
						// information here currently (it's in SqlBuilder)
						|| element.From.Tables.Count == 0
						// we can replace
						// SELECT xxx GROUP BY ...
						// with
						// SELECT * GROUP BY ...
						// only if we know that all columns in source are in group-by, which is not worth of extra logic
						|| !element.GroupBy.IsEmpty
					))
				{
					element.AddNew(new SqlValue(1), alias: cte != null ? "c1" : null);
					cte?.Fields.Add(new SqlField(new DbDataType(typeof(int)), "c1", false));
				}
			}
		}

		#endregion

	}
}
