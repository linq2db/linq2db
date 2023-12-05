using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Linq.Builder;

	using SqlProvider;
	using Visitors;
	using DataProvider;

	public class SelectQueryOptimizerVisitor : SqlQueryVisitor
	{
		SqlProviderFlags  _flags             = default!;
		DataOptions       _dataOptions       = default!;
		EvaluationContext _evaluationContext = default!;
		IQueryElement     _rootElement       = default!;
		IQueryElement[]   _dependencies      = default!;
		SelectQuery?      _correcting;
		int               _version;
		bool              _removeWeakJoins;

		SelectQuery?    _parentSelect;
		SqlSetOperator? _currentSetOperator;
		SelectQuery?    _applySelect;
		SelectQuery?    _inSubquery;
		bool            _isInRecursiveCte;

		SqlQueryColumnNestingCorrector _columnNestingCorrector     = new();
		SqlQueryOrderByOptimizer       _orderByOptimizer           = new();
		MovingComplexityVisitor        _movingComplexityVisitor    = new();
		SqlExpressionOptimizerVisitor  _expressionOptimizerVisitor = new(true);

		public SelectQueryOptimizerVisitor() : base(VisitMode.Modify)
		{
		}

		public IQueryElement OptimizeQueries(IQueryElement root, SqlProviderFlags flags, bool removeWeakJoins, DataOptions dataOptions,
			EvaluationContext evaluationContext, IQueryElement rootElement,
			params IQueryElement[] dependencies)
		{
#if DEBUG
			if (root.ElementType == QueryElementType.SelectStatement)
			{

			}
#endif

			_flags                 = flags;
			_removeWeakJoins       = removeWeakJoins;
			_dataOptions           = dataOptions;
			_evaluationContext     = evaluationContext;
			_rootElement           = rootElement;
			_dependencies          = dependencies;
			_parentSelect          = default!;
			_applySelect           = default!;
			_inSubquery            = default!;

			// OUTER APPLY Queries usually may have wrong nesting in WHERE clause.
			// Making it consistent in LINQ Translator is bad for performance and it is hard to implement task.
			//
			var result = _columnNestingCorrector.CorrectColumnNesting(root);

			RemoveNotUsedColumns(_columnNestingCorrector.UsedColumns, result);

			do
			{
				result = ProcessElement(result);

				_orderByOptimizer.OptimizeOrderBy(result);
				if (!_orderByOptimizer.IsOptimized)
					break;

			} while (true);

			return result;
		}

		static void RemoveNotUsedColumns(IReadOnlyList<SqlColumn> usedColumns, IQueryElement element)
		{
			element.Visit(usedColumns, static (uc, e) =>
			{
				if (e is SqlSelectClause select)
				{
					for (var i = select.Columns.Count - 1; i >= 0; i--)
					{
						var column = select.Columns[i];
						if (!uc.Contains(column))
						{
							if (select.Columns.Count > 1 && !QueryHelper.IsAggregationOrWindowFunction(column.Expression))
								select.Columns.RemoveAt(i);
						}
					}
				}
			});
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_flags             = default!;
			_dataOptions       = default!;
			_evaluationContext = default!;
			_rootElement       = default!;
			_dependencies      = default!;
			_parentSelect      = default!;
			_applySelect       = default!;
			_version           = default;
			_isInRecursiveCte  = false;

			_columnNestingCorrector.Cleanup();
			_orderByOptimizer.Cleanup();
			_movingComplexityVisitor.Cleanup();
			_expressionOptimizerVisitor.Cleanup();
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			++_version;
			return base.NotifyReplaced(newElement, oldElement);
		}

		protected override IQueryElement VisitSqlJoinedTable(SqlJoinedTable element)
		{
			var saveQuery          = _applySelect;

			if (element.JoinType == JoinType.CrossApply || element.JoinType == JoinType.OuterApply)
				_applySelect = element.Table.Source as SelectQuery;
			else
				_applySelect = null;

			var newElement = base.VisitSqlJoinedTable(element);

			_applySelect    = saveQuery;

			return newElement;
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveSetOperatorCount = selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0;
			var saveParent       = _parentSelect;

			_parentSelect = selectQuery;

			var newQuery = (SelectQuery)base.VisitSqlQuery(selectQuery);

			if (_correcting == null)
			{
				_parentSelect = saveParent;

				if (saveParent == null)
				{
#if DEBUG
					var before = selectQuery.ToDebugString();
#endif
					// only once
					_expressionOptimizerVisitor.Optimize(_evaluationContext, NullabilityContext.GetContext(selectQuery), _flags, _dataOptions, selectQuery);

					if (_expressionOptimizerVisitor.IsModified)
					{

					}
				}
				else
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
					
					if (MoveOuterJoinsToSubQuery(selectQuery))
					{
						isModified = true;
					}

					if (OptimizeApplies(selectQuery, _flags.IsApplyJoinSupported))
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

					if (CorrectJoins(selectQuery))
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

				if (saveSetOperatorCount != (selectQuery.HasSetOperators ? selectQuery.SetOperators.Count : 0))
				{
					// Do it again. Appended new SetOperators. For ensuring how it works check CteTests
					//
					newQuery = (SelectQuery)VisitSqlQuery(selectQuery);
				}

			}

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
			
			var newElement = base.VisitSqlTableSource(element);;

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

		void OptimizeUnions(SelectQuery selectQuery)
		{
			if (!selectQuery.HasSetOperators)
				return;

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
					}
				}
			}
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

			RemoveEmptyJoins(selectQuery);
			OptimizeGroupBy(selectQuery);

			RemoveEmptyJoins(selectQuery);

			OptimizeUnions(selectQuery);

			OptimizeGroupBy(selectQuery);
			OptimizeDistinct(selectQuery);
			CorrectColumns(selectQuery);

			return isModified;
		}

		void OptimizeGroupBy(SelectQuery selectQuery)
		{
			if (!selectQuery.GroupBy.IsEmpty)
			{
				// Remove constants.
				//
				for (int i = selectQuery.GroupBy.Items.Count - 1; i >= 0; i--)
				{
					var groupByItem = selectQuery.GroupBy.Items[i];
					if (QueryHelper.IsConstantFast(groupByItem))
					{
						if (!selectQuery.Select.Columns.Any(c => ReferenceEquals(c.Expression, groupByItem)))
						{
							selectQuery.GroupBy.Items.RemoveAt(i);
						}
					}
				}
			}
		}

		void CorrectColumns(SelectQuery selectQuery)
		{
			if (!selectQuery.GroupBy.IsEmpty && selectQuery.Select.Columns.Count == 0)
			{
				foreach (var item in selectQuery.GroupBy.Items)
				{
					selectQuery.Select.Add(item);
				}
			}
		}

		void EnsureReferencesCorrected(SelectQuery selectQuery)
		{
			if (_correcting != null)
				throw new InvalidOperationException();

			_correcting = selectQuery;

			base.Visit(selectQuery);

			_correcting = null;
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

					if (join.IsWeak)
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
			}

			return isModified;
		}

		static bool IsLimitedToOneRecord(SelectQuery query)
		{
			if (query.Select.TakeValue is SqlValue value && Equals(value.Value, 1))
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

		void OptimizeDistinct(SelectQuery selectQuery)
		{
			if (!selectQuery.Select.IsDistinct || !selectQuery.Select.OptimizeDistinct)
				return;

			if (IsComplexQuery(selectQuery))
				return;

			if (IsLimitedToOneRecord(selectQuery))
			{
				// we can simplify query if we take only one record
				selectQuery.Select.IsDistinct = false;
				return;
			}

			if (!selectQuery.GroupBy.IsEmpty)
			{
				if (selectQuery.GroupBy.Items.All(gi => selectQuery.Select.Columns.Any(c => c.Expression.Equals(gi))))
				{
					selectQuery.GroupBy.Items.Clear();
					return;
				}
			}

			var table = selectQuery.From.Tables[0];

			var keys = new List<IList<ISqlExpression>>();

			QueryHelper.CollectUniqueKeys(selectQuery, includeDistinct: false, keys);
			QueryHelper.CollectUniqueKeys(table, keys);
			if (keys.Count == 0)
				return;

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

			if (foundUnique)
			{
				// We have found that distinct columns has unique key, so we can remove distinct
				selectQuery.Select.IsDistinct = false;
			}
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

					mainQuery.OrderBy.Expr(item.Expression, item.IsDescending);
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

			if (!joinTable.CanConvertApply)
				return optimized;

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

				if (isApplySupported && sql.Select.HasModifier && _flags.IsSubQueryTakeSupported)
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
					if (!_flags.IsWindowFunctionsSupported)
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

							rnBuilder.Append($"{{{parameters.Count}}}");
							parameters.Add(partitionBy[i]);
						}
					}

					var orderByItems = sql.OrderBy.Items.ToList();

					if (sql.OrderBy.IsEmpty)
					{
						if (partitionBy != null)
							orderByItems.Add(new SqlOrderByItem(partitionBy[0], false));
						else if (!_flags.IsRowNumberWithoutOrderBySupported)
						{
							if (sql.Select.Columns.Count == 0)
							{
								throw new InvalidOperationException("OrderBy not specified for limited recordset.");
							}
							orderByItems.Add(new SqlOrderByItem(sql.Select.Columns[0].Expression, false));
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
							rnBuilder.Append($"{{{parameters.Count}}}");
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
				foreach (var join in sql.From.Tables.SelectMany(t => t.Joins))
				{
					if (join.JoinType == JoinType.Inner)
						whereToIgnore.Add(join.Condition);
					else
						break;
				}

				// we cannot optimize apply because reference to parent sources are used inside the query
				if (QueryHelper.IsDependsOnOuterSources(sql, whereToIgnore))
					return optimized;

				var searchCondition = new List<SqlCondition>();

				var conditions = sql.Where.SearchCondition.Conditions;

				var toIgnore       = new [] { joinTable };
				var currentSources = new[] { joinTable.Table.Source };

				if (conditions.Count > 0)
				{
					for (var i = conditions.Count - 1; i >= 0; i--)
					{
						var condition = conditions[i];

						var contains = QueryHelper.IsDependsOnOuterSources(condition, currentSources: accessible);

						if (contains)
						{
							searchCondition.Insert(0, condition);
							conditions.RemoveAt(i);
						}
					}
				}

				if (rnExpression != null)
				{
					// processing ROW_NUMBER

					var rnColumn = sql.Select.AddNewColumn(rnExpression);
					rnColumn.RawAlias = "rn";

					sql.Select.SkipValue = null;
					sql.Select.TakeValue = null;

					if (skipValue != null)
					{
						searchCondition.Add(new SqlCondition(false,
							new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.Greater, skipValue, null)));

						if (takeValue != null)
						{
							searchCondition.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(rnColumn,
								SqlPredicate.Operator.LessOrEqual, new SqlBinaryExpression(skipValue.SystemType!,
									skipValue, "+", takeValue), null)));
						}
					}
					else if (takeValue != null)
					{
						searchCondition.Add(new SqlCondition(false,
							new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.LessOrEqual, takeValue, null)));

					}
					else if (sql.Select.IsDistinct)
					{
						sql.Select.IsDistinct = false;
						searchCondition.Add(new SqlCondition(false,
							new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.Equal, new SqlValue(1), null)));
					}
				}

				var toCheck = QueryHelper.EnumerateAccessibleSources(sql).ToList();

				for (int i = 0; i < searchCondition.Count; i++)
				{
					var cond = searchCondition[i];
					var newCond = cond.Convert((sql, toCheck, toIgnore, isAgg), static (visitor, e) =>
					{
						if (e.ElementType == QueryElementType.Column || e.ElementType == QueryElementType.SqlField)
						{
							if (QueryHelper.IsDependsOnSources(e, visitor.Context.toCheck))
							{
								if (e is not SqlColumn clm || clm.Parent != visitor.Context.sql)
								{
									var newExpr = visitor.Context.sql.Select.AddColumn((ISqlExpression)e);

									if (visitor.Context.isAgg)
									{
										visitor.Context.sql.Select.GroupBy.Items.Add((ISqlExpression)e);
									}

									return newExpr;
								}
							}
						}

						return e;
					});

					searchCondition[i] = newCond;
				}

				var newJoinType = ConvertApplyJoinType(joinTable.JoinType);

				joinTable.JoinType = newJoinType;
				joinTable.Condition.Conditions.AddRange(searchCondition);

				optimized = true;
			}

			return optimized;
		}

		static void ConcatSearchCondition(SqlWhereClause where1, SqlWhereClause where2)
		{
			if (where1.IsEmpty)
			{
				where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
			else
			{
				if (where1.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc1 = new SqlSearchCondition();

					sc1.Conditions.AddRange(where1.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Clear();
					where1.SearchCondition.Conditions.Add(new SqlCondition(false, sc1));
				}

				if (where2.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc2 = new SqlSearchCondition();

					sc2.Conditions.AddRange(where2.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Add(new SqlCondition(false, sc2));
				}
				else
					where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
		}

		bool IsColumnExpressionValid(SelectQuery parentQuery, NullabilityContext nullability, SelectQuery subQuery, SqlColumn column, ISqlExpression columnExpression)
		{
			if (columnExpression.ElementType == QueryElementType.Column ||
			    columnExpression.ElementType == QueryElementType.SqlRawSqlTable ||
			    columnExpression.ElementType == QueryElementType.SqlField)
			{
				return true;
			}

			var underlying = QueryHelper.UnwrapExpression(columnExpression, false);
			if (!ReferenceEquals(underlying, columnExpression))
			{
				return IsColumnExpressionValid(parentQuery, nullability, subQuery, column, underlying);
			}

			if (underlying is SqlBinaryExpression binary)
			{
				if (QueryHelper.IsConstantFast(binary.Expr1))
				{
					return IsColumnExpressionValid(parentQuery, nullability, subQuery, column, binary.Expr2);
				}

				if (QueryHelper.IsConstantFast(binary.Expr2))
				{
					return IsColumnExpressionValid(parentQuery, nullability, subQuery, column, binary.Expr1);
				}
			}

			// check that column has at least one reference
			//

			if (!parentQuery.GroupBy.IsEmpty)
			{
				if (null != parentQuery.GroupBy.Find(e => ReferenceEquals(e, column)))
				{
					if (QueryHelper.IsConstantFast(columnExpression))
					{
						if (parentQuery.Select.Columns.Count == 0 || null != parentQuery.Select.Find(e => ReferenceEquals(e, column)))
						{
							return false;
						}
					}
					else
					{
						return false;
					}
				}
			}

			if (QueryHelper.IsAggregationOrWindowFunction(columnExpression))
			{
				if (!parentQuery.Where.IsEmpty)
				{
					if (null != parentQuery.Where.Find(e => ReferenceEquals(e, column)))
						return false;
				}

				if (!parentQuery.Having.IsEmpty)
				{
					if (null != parentQuery.Having.Find(e => ReferenceEquals(e, column)))
						return false;
				}
			}

			var allowed = _movingComplexityVisitor.IsAllowedToMove(column, parent: parentQuery,
				nullability,
				_evaluationContext,
				column.Parent,
				_applySelect == parentQuery ? parentQuery.Where  : null,
				_applySelect == parentQuery ? parentQuery.Select : null
				);

			return allowed;
		}

		static bool IsSimpleForNoTablesMove(SelectQuery selectQuery)
		{
			var isSimple = selectQuery.Where.IsEmpty
			               && selectQuery.GroupBy.IsEmpty
			               && selectQuery.Having.IsEmpty
			               && selectQuery.OrderBy.IsEmpty && selectQuery.From.Tables.Count == 1
			               && selectQuery.From.Tables[0].Joins.Count                       == 0;
			return isSimple;
		}

		bool MoveSubQueryUp(SelectQuery selectQuery, SqlTableSource tableSource)
		{
			if (tableSource.Source is not SelectQuery subQuery)
				return false;

			if (subQuery.DoNotRemove)
				return false;

			if (subQuery.From.Tables.Count == 0)
			{
				if (!IsSimpleForNoTablesMove(selectQuery))
					return false;
			}

			if (subQuery.From.Tables.Count > 1)
			{
				if (!_flags.IsMultiTablesSupportsJoins)
				{
					if (QueryHelper.EnumerateJoins(selectQuery).Any())
						return false;
				}
			}

			// // named sub-query cannot be removed
			if (subQuery.QueryName != null
				// parent also has name
				&& (selectQuery.QueryName != null
				// parent has other tables/sub-queries
				|| selectQuery.From.Tables.Count > 1
				|| selectQuery.From.Tables.Any(static t => t.Joins.Count > 0)))
			{
				return false;
			}

			if (_currentSetOperator?.SelectQuery == selectQuery || selectQuery.HasSetOperators)
			{
				// processing parent query as part of Set operation
				//

				if (subQuery.Select.HasModifier)
					return false;

				/*
				if (!subQuery.Select.GroupBy.IsEmpty || !subQuery.Select.Where.IsEmpty)
					return false;
					*/

				if (!subQuery.Select.OrderBy.IsEmpty)
				{
					return false;
				}

				/*
				if (QueryHelper.EnumerateAccessibleSources(subQuery).Skip(1).Take(2).Count() > 1)
					return false;
			*/
			}

			if (!subQuery.GroupBy.IsEmpty && !selectQuery.GroupBy.IsEmpty)
				return false;

			if (selectQuery.Select.IsDistinct)
			{
				// Common check for Distincts

				if (subQuery.Select.SkipValue    != null || subQuery.Select.TakeValue    != null ||
				    selectQuery.Select.SkipValue != null || selectQuery.Select.TakeValue != null)
				{
					return false;
				}

				// Common column check for Distincts

				foreach (var parentColumn in selectQuery.Select.Columns)
				{
					if (parentColumn.Expression is not SqlColumn column || column.Parent != subQuery || QueryHelper.ContainsAggregationOrWindowFunction(parentColumn.Expression))
					{
						return false;
					}
				}
			}

			if (subQuery.Select.IsDistinct != selectQuery.Select.IsDistinct)
			{
				if (subQuery.Select.IsDistinct)
				{
					// Columns in parent query should match
					//

					if (!subQuery.Select.Columns.All(sc =>
						    selectQuery.Select.Columns.Any(pc => ReferenceEquals(pc.Expression, sc))))
					{
						return false;
					}

					// if (subQuery.Select.Columns.Count != selectQuery.Select.Columns.Count)
					// {
					// 	return false;
					// }

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
				if (tableSource.Joins.Count > 0)
					return false;
				if (selectQuery.From.Tables.Count > 1)
					return false;

				if (!selectQuery.Select.OrderBy.IsEmpty)
					return false;

				if (!selectQuery.Select.Where.IsEmpty)
					return false;

				if (selectQuery.Select.Columns.Any(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
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

			if (!_flags.AcceptsOuterExpressionInAggregate)
			{
				if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType != JoinType.Inner))
				{
					if (subQuery.Select.Columns.Any(c => IsInsideAggregate(selectQuery, c)))
						return false;
				}
			}

			var nullability = NullabilityContext.GetContext(selectQuery);

			if (subQuery.Select.Columns.Any(c => !IsColumnExpressionValid(selectQuery, nullability, subQuery, c, c.Expression)))
				return false;

			if (!selectQuery.GroupBy.IsEmpty)
			{
				if (subQuery.Select.Columns.Any(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression) || !IsColumnExpressionValid(selectQuery, nullability, subQuery, c, c.Expression)))
					return false;
			}

			if (!selectQuery.GroupBy.IsEmpty && !subQuery.GroupBy.IsEmpty)
				return false;

			if (selectQuery.GroupBy.IsEmpty && !subQuery.GroupBy.IsEmpty)
			{
				if (tableSource.Joins.Count > 0)
					return false;
				if (selectQuery.From.Tables.Count > 1)
					return false;

				if (selectQuery.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression)))
					return false;
			}

			if (subQuery.Select.TakeHints != null && selectQuery.Select.TakeValue != null)
				return false;

			if (subQuery.HasSetOperators)
			{
				if (selectQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
				{
					if (subQuery.SetOperators.Any(so => so.Operation != SetOperation.UnionAll))
						return false;
				}

				if (!selectQuery.Select.Where.IsEmpty || !selectQuery.Select.Having.IsEmpty || selectQuery.Select.HasModifier || !selectQuery.OrderBy.IsEmpty)
					return false;

				var operation = subQuery.SetOperators[0].Operation;

				if (_currentSetOperator != null && _currentSetOperator.Operation != operation)
					return false;

				if (!subQuery.SetOperators.All(so => so.Operation == operation))
					return false;

				if (selectQuery.HasSetOperators && !selectQuery.SetOperators.All(so => so.Operation == operation))
					return false;
			}

			if (subQuery.Select.Columns.Any(c => QueryHelper.ContainsAggregationOrWindowFunction(c.Expression)))
			{
				if (!selectQuery.IsSimpleOrSet)
					return false;
			}

			if (selectQuery == _inSubquery && subQuery.Select.HasModifier)
			{
				return false;
			}

			// -------------------------------------------
			// Actual modification starts from this point
			//

#pragma warning disable CA1508 // Avoid dead conditional code : analyzer bug
			selectQuery.QueryName ??= subQuery.QueryName;
#pragma warning restore CA1508 // Avoid dead conditional code

			if (subQuery.HasSetOperators)
			{
				var newIndexes =
					new Dictionary<ISqlExpression, int>(Utils.ObjectReferenceEqualityComparer<ISqlExpression>
						.Default);

				for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
				{
					var scol = selectQuery.Select.Columns[i];

					if (!newIndexes.ContainsKey(scol.Expression))
						newIndexes[scol.Expression] = i;
				}

				var operation = subQuery.SetOperators[0].Operation;

				if (!CheckSetColumns(newIndexes, subQuery, operation))
					return false;

				UpdateSetIndexes(newIndexes, subQuery, operation);

				selectQuery.SetOperators.InsertRange(0, subQuery.SetOperators);
				subQuery.SetOperators.Clear();
			}

			if (!subQuery.Where.IsEmpty)
			{
				ConcatSearchCondition(selectQuery.Where, subQuery.Where);
			}

			if (!subQuery.GroupBy.IsEmpty)
			{
				selectQuery.GroupBy.Items.AddRange(subQuery.GroupBy.Items);
				selectQuery.GroupBy.GroupingType = subQuery.GroupBy.GroupingType;
			}

			if (!subQuery.Having.IsEmpty)
			{
				ConcatSearchCondition(selectQuery.Having, subQuery.Having);
			}

			if (subQuery.Select.IsDistinct)
				selectQuery.Select.IsDistinct = true;

			if (subQuery.Select.TakeValue != null)
			{
				selectQuery.Select.Take(subQuery.Select.TakeValue, subQuery.Select.TakeHints);
			}

			if (subQuery.Select.SkipValue != null)
			{
				selectQuery.Select.SkipValue = subQuery.Select.SkipValue;
			}

			/*if (selectQuery.Select.Columns.Count == 0)
			{
				foreach(var column in subQuery.Select.Columns)
				{
					selectQuery.Select.AddColumn(column.Expression);
				}
			}*/

			foreach (var column in subQuery.Select.Columns)
			{
				NotifyReplaced(column.Expression, column);
			}

			// First table processing
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
				var idx = selectQuery.From.Tables.IndexOf(tableSource);
				for (var i = subQuery.From.Tables.Count - 1; i >= 1; i--)
				{
					var subQueryTableSource = subQuery.From.Tables[i];
					selectQuery.From.Tables.Insert(idx + 1, subQueryTableSource);
				}

				// Move joins to last table
				//
				if (tableSource.Joins.Count > 0)
				{
					var lastTableSource = subQuery.From.Tables[^1];
					lastTableSource.Joins.InsertRange(0, tableSource.Joins);
					tableSource.Joins.Clear();
				}
			}

			ApplySubQueryExtensions(selectQuery, subQuery);

			if (subQuery.OrderBy.Items.Count > 0 && !selectQuery.Select.Columns.Any(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
			{
				ApplySubsequentOrder(selectQuery, subQuery);
			}

			return true;
		}

		bool JoinMoveSubQueryUp(SelectQuery selectQuery, SqlJoinedTable joinTable)
		{
			if (joinTable.Table.Source is not SelectQuery subQuery)
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

			var moveConditionToQuery = joinTable.JoinType == JoinType.Inner || joinTable.JoinType == JoinType.CrossApply;

			if (joinTable.JoinType != JoinType.Inner)
			{
				if (!subQuery.Where.IsEmpty)
				{
					if (joinTable.JoinType == JoinType.OuterApply)
					{
						if (_flags.IsApplyJoinSupportsCondition)
							moveConditionToQuery = false;
						else
							return false;
					}
					else if (joinTable.JoinType == JoinType.CrossApply)
					{
						if (_flags.IsApplyJoinSupportsCondition)
							moveConditionToQuery = false;
					}
					else if (joinTable.JoinType == JoinType.Left)
					{
						moveConditionToQuery = false;
					}
					else
					{
						return false;
					}
				}

				if (!_flags.IsOuterJoinSupportsInnerJoin)
				{
					// Especially for Access. See ComplexTests.Contains3
					//
					if (QueryHelper.EnumerateJoins(subQuery).Any(j => j.JoinType == JoinType.Inner))
						return false;
				}

				if (!subQuery.Select.Columns.All(c => c.Expression is SqlColumn or SqlField))
					return false;
			}

			var nullability = NullabilityContext.GetContext(selectQuery);

			if (subQuery.Select.Columns.Any(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression) || !IsColumnExpressionValid(selectQuery, nullability, subQuery, c, c.Expression)))
				return false;

			// Actual modification starts from this point
			//

			if (!subQuery.Where.IsEmpty)
			{
				if (moveConditionToQuery)
				{
					selectQuery.Where.EnsureConjunction().SearchCondition.Conditions.AddRange(subQuery.Where.SearchCondition.Conditions);
				}
				else
				{
					joinTable.Condition.EnsureConjunction().Conditions.AddRange(subQuery.Where.SearchCondition.Conditions);
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

			if (subQuery.OrderBy.Items.Count > 0 && !selectQuery.Select.Columns.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
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

		protected override IQueryElement VisitSqlCondition(SqlCondition element)
		{
			return base.VisitSqlCondition(element);
		}

		protected override IQueryElement VisitNotExprPredicate(SqlPredicate.NotExpr predicate)
		{
			if (predicate is { IsNot: true, Expr1: IInvertibleElement invertible } && invertible.CanInvert())
			{
				var newNode = invertible.Invert();
				return Visit(newNode);
			}

			return base.VisitNotExprPredicate(predicate);
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

					if (tableSource.Source is SelectQuery sc && sc.From.Tables.Count == 0 && IsSimpleForNoTablesMove(selectQuery))
					{
						selectQuery.From.Tables.RemoveAt(i);
					}

					EnsureReferencesCorrected(selectQuery);

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

			return replaced;
		}

		bool CorrectJoins(SelectQuery selectQuery)
		{
			var isModified = false;

			if (!_flags.IsRecursiveCTEJoinWithConditionSupported && _isInRecursiveCte)
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
			if (_flags.IsMultiTablesSupportsJoins)
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
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply|| join.JoinType == JoinType.FullApply|| join.JoinType == JoinType.RightApply)
					{
						if (OptimizeApply(join, isApplySupported))
							optimized = true;
					}
				}
			}

			return optimized;
		}

		void RemoveEmptyJoins(SelectQuery selectQuery)
		{
			if (_flags.IsCrossJoinSupported)
				return;

			for (var tableIndex = 0; tableIndex < selectQuery.From.Tables.Count; tableIndex++)
			{
				var table = selectQuery.From.Tables[tableIndex];
				for (var joinIndex = 0; joinIndex < table.Joins.Count; joinIndex++)
				{
					var join = table.Joins[joinIndex];
					if (join.JoinType == JoinType.Inner && join.Condition.Conditions.Count == 0)
					{
						selectQuery.From.Tables.Insert(tableIndex + 1, join.Table);
						table.Joins.RemoveAt(joinIndex);
						--joinIndex;
					}
				}
			}
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			expression      = base.VisitSqlColumnExpression(column, expression);

			expression = QueryHelper.SimplifyColumnExpression(expression);

			return expression;
		}

		static bool IsLimitedToOneRecord(SelectQuery selectQuery, EvaluationContext context)
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
				if (QueryHelper.IsAggregationFunction(column.Expression))
					return true;

				if (selectQuery.Select.From.Tables.Count == 0)
					return true;
			}

			return false;
		}

		static bool IsUniqueUsage(SelectQuery rootQuery, SqlColumn column)
		{
			int counter = 0;

			rootQuery.VisitParentFirstAll(e =>
			{
				// do not search in the same query
				if (e is SelectQuery sq && sq == column.Parent)
					return false;

				if (e == column)
				{
					++counter;
				}

				return counter < 2;
			});

			return counter <= 1;
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

		bool MoveOuterJoinsToSubQuery(SelectQuery selectQuery)
		{
			if (!_flags.IsSubQueryColumnSupported)
				return false;

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
						var join = table.Joins[j];
						if (join.JoinType == JoinType.OuterApply || join.JoinType == JoinType.CrossApply ||
						    join.JoinType == JoinType.Left)
						{
							evaluationContext ??= new EvaluationContext();

							if (join.Table.Source is SelectQuery tsQuery &&
							    tsQuery.Select.Columns.Count > 0 &&
							    IsLimitedToOneRecord(tsQuery, evaluationContext))
							{
								if (!SqlProviderHelper.IsValidQuery(tsQuery, parentQuery: sq, forColumn: true, _flags))
									continue;

								if (tsQuery.Select.Columns.Count > 1 && (_flags.IsWindowFunctionsSupported || _flags.IsApplyJoinSupported))
								{
									// provider can handle this query
									continue;
								}

								if (!_flags.IsSubqueryWithParentReferenceInJoinConditionSupported)
								{
									// for Oracle we cannot move to subquery
									if (tsQuery.Select.HasModifier)
										continue;
								}

								var isValid = true;

								foreach (var testedColumn in tsQuery.Select.Columns)
								{
									// where we can start analyzing that we can move join to subquery
									
									if (_flags.IsApplyJoinSupported && !IsUniqueUsage(sq, testedColumn))
									{
										QueryHelper.MoveDuplicateUsageToSubQuery(sq);
										// will be processed in the next step
										ti = -1;

										isValid = false;

										break;
									}

									if (testedColumn.Expression is SqlFunction function)
									{
										if (function.IsAggregate)
										{
											if (!_flags.AcceptsOuterExpressionInAggregate && IsInsideAggregate(sq.Select, testedColumn))
											{
												isValid = false;
												break;
											}

											if (!_flags.IsCountSubQuerySupported)
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
								tsQuery.Where.ConcatSearchCondition(join.Condition);

								// replacing column with subquery

								for (var index = tsQuery.Select.Columns.Count - 1; index >= 0; index--)
								{
									var queryToReplace = tsQuery;
									var testedColumn   = tsQuery.Select.Columns[index];

									// cloning if there are many columns
									if (index > 0)
									{
										queryToReplace = tsQuery.Clone();
									}


									if (queryToReplace.Select.Columns.Count > 1)
									{
										var sourceColumn = queryToReplace.Select.Columns[index];
										queryToReplace.Select.Columns.Clear();
										queryToReplace.Select.Columns.Add(sourceColumn);
									}

									NotifyReplaced(queryToReplace, testedColumn);
								}
							}
						}
					}
				}
			}

			if (_version != currentVersion)
			{
				EnsureReferencesCorrected(selectQuery);
				return true;
			}

			return false;
		}

		protected override IQueryElement VisitCteClause(CteClause element)
		{
			var saveIsInRecursiveCte = _isInRecursiveCte;
			if (element.IsRecursive)
				_isInRecursiveCte = true;

			var newElement = base.VisitCteClause(element);

			_isInRecursiveCte = saveIsInRecursiveCte;

			return newElement;
		}

		#region Helpers

		class MovingComplexityVisitor : QueryElementVisitor
		{
			ISqlExpression       _expressionToCheck = default!;
			IQueryElement?[]     _ignore            = default!;
			NullabilityContext   _nullability       = default!;
			EvaluationContext    _evaluationContext = default!;
			int                  _foundCount;
			bool                 _notAllowedScope;
			bool                 _doNotAllow;

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
				_foundCount        = 0;
			}

			public bool IsAllowedToMove(ISqlExpression testExpression, IQueryElement parent, NullabilityContext nullability, 
				EvaluationContext evaluationContext, params IQueryElement?[] ignore)
			{
				_ignore            = ignore;
				_expressionToCheck = testExpression;
				_nullability       = nullability;
				_evaluationContext = evaluationContext;
				_doNotAllow        = default;
				_foundCount        = 0;

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
				var reduced = predicate.Reduce(_nullability, _evaluationContext);
				if (!ReferenceEquals(reduced, predicate))
					Visit(reduced);
				else
					base.VisitExprExprPredicate(predicate);

				return predicate;
			}

			protected override IQueryElement VisitIsTruePredicate(SqlPredicate.IsTrue predicate)
			{
				var reduced = predicate.Reduce(_nullability);
				if (!ReferenceEquals(reduced, predicate))
					Visit(reduced);
				else
					base.VisitIsTruePredicate(predicate);

				return predicate;
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

		#endregion

	}
}
