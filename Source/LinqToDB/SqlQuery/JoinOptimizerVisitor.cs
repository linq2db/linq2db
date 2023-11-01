﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Linq.Builder;
	using SqlProvider;
	using Visitors;

	// TODO: currently it just a new place for JoinOptimizer code
	// need to refactor/simplify and probably merge with select optimizer
	public sealed class JoinOptimizerVisitor : SqlQueryVisitor
	{
		EvaluationContext _evaluationContext = default!;
		SqlStatement _statement    = default!;
		NullabilityContext                                                      _nullablility = default!;

		Dictionary<VirtualField,HashSet<Tuple<int,VirtualField>>>?  _equalityMap;
		Dictionary<int,List<VirtualField[]>?>? _keysCache;
		HashSet<int>? _removedSources;
		Dictionary<Tuple<SqlTableSource?,SqlTableSource>,List<FoundEquality>?>? _fieldPairCache;
		Dictionary<VirtualField,VirtualField>? _replaceMap;
		Dictionary<SqlSearchCondition,SqlSearchCondition>?                      _additionalFilter;
		SelectQuery                                                             _selectQuery  = null!;
		bool _correntMappings;
		
		public JoinOptimizerVisitor() : base(VisitMode.Modify)
		{
		}

		public IQueryElement OptimizeJoins(SqlStatement root, EvaluationContext evaluationContext)
		{
			_evaluationContext = evaluationContext;
			_statement = root;
			var statement = (SqlStatement)ProcessElement(root);

			OptimizeFilters();
			return CorrectMappings(statement);
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_evaluationContext = default!;
			_statement = default!;
			_nullablility = default!;
			_selectQuery = null!;

			_equalityMap = null;
			_keysCache = null;
			_removedSources = null;
			_fieldPairCache = null;
			_replaceMap = null;
			_additionalFilter = null;
			_correntMappings = false;
		}

		public override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			if (_correntMappings)
				return base.VisitSqlQuery(selectQuery);

			var old = _selectQuery;
			var oldNullability = _nullablility;
			_selectQuery = selectQuery;
			_nullablility = new NullabilityContext(selectQuery);
			var result = base.VisitSqlQuery(selectQuery);
			_selectQuery = old;
			_nullablility = oldNullability;
			return result;
		}

		public override IQueryElement VisitSqlTableSource(SqlTableSource element)
		{
			element = (SqlTableSource)base.VisitSqlTableSource(element);

			if (_correntMappings)
				return element;

			FlattenJoins(element);

			// trying to remove joins that are not in projection
			for (var i1 = element.Joins.Count - 1; i1 >= 0; i1--)
			{
				var j1 = element.Joins[i1];

				if (j1.JoinType == JoinType.Left || j1.JoinType == JoinType.Inner)
				{
					var keys = GetKeys(j1.Table);

					if (keys != null && !IsDependedBetweenJoins(element, j1))
					{
						// try merge if joins are the same
						var removed = TryToRemoveIndependentLeftJoin(element, j1, keys);

						if (removed)
						{
							element.Joins.RemoveAt(i1);
						}
					}
				}
			} // independent joins loop

			return element;
		}

		public override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			if (_correntMappings)
				return GetNewField(new VirtualField(element)).Element;
			return base.VisitSqlFieldReference(element);
		}

		public override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			if (_correntMappings)
				return GetNewField(new VirtualField(element)).Element;
			return base.VisitSqlColumnReference(element);
		}

		SqlStatement CorrectMappings(SqlStatement statement)
		{
			if (_replaceMap != null && _replaceMap.Count > 0 || _removedSources != null)
			{
				_correntMappings = true;
				return (SqlStatement)ProcessElement(statement);
			}

			return statement;
		}

		void OptimizeSearchCondition(SqlSearchCondition searchCondition)
		{
			var items = searchCondition.Conditions;

			if (items.Any(c => c.IsOr))
				return;

			for (var i1 = 0; i1 < items.Count; i1++)
			{
				var c1 = items[i1];
				var cmp = EvaluateLogical(c1);

				if (cmp != null)
					if (cmp.Value)
					{
						items.RemoveAt(i1);
						--i1;
						continue;
					}

				switch (c1.ElementType)
				{
					case QueryElementType.Condition:
					case QueryElementType.SearchCondition:
					{
						if (c1.Predicate is SqlSearchCondition search)
						{
							OptimizeSearchCondition(search);
							if (search.Conditions.Count == 0)
							{
								items.RemoveAt(i1);
								--i1;
								continue;
							}
						}
						break;
					}
				}

				for (var i2 = i1 + 1; i2 < items.Count; i2++)
				{
					var c2 = items[i2];
					if (CompareConditions(c2, c1))
					{
						searchCondition.Conditions.RemoveAt(i2);
						--i2;
					}
				}
			}
		}

		bool? EvaluateLogical(SqlCondition condition)
		{
			switch (condition.ElementType)
			{
				case QueryElementType.Condition:
				{
					if (condition.Predicate is SqlPredicate.ExprExpr expr && expr.Operator == SqlPredicate.Operator.Equal)
						return CompareExpressions(expr.Expr1, expr.Expr2);
					break;
				}
			}

			return null;
		}

		bool CompareExpressions(SqlPredicate.ExprExpr expr1, SqlPredicate.ExprExpr expr2)
		{
			if (expr1.Operator != expr2.Operator)
				return false;

			if (expr1.ElementType != expr2.ElementType)
				return false;

			switch (expr1.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					return CompareExpressions(expr1.Expr1, expr2.Expr1) == true
						&& CompareExpressions(expr1.Expr2, expr2.Expr2) == true
						|| CompareExpressions(expr1.Expr1, expr2.Expr2) == true
						&& CompareExpressions(expr1.Expr2, expr2.Expr1) == true;
				}
			}

			return false;
		}

		bool? CompareExpressions(ISqlExpression expr1, ISqlExpression expr2)
		{
			if (expr1.ElementType != expr2.ElementType)
				return null;

			switch (expr1.ElementType)
			{
				case QueryElementType.SqlNullabilityExpression:
				{
					return CompareExpressions(QueryHelper.UnwrapNullablity(expr1), QueryHelper.UnwrapNullablity(expr2));
				}

				case QueryElementType.Column:
				{
					return CompareExpressions(((SqlColumn)expr1).Expression, ((SqlColumn)expr2).Expression);
				}

				case QueryElementType.SqlField:
				{
					var field1 = GetNewField(new VirtualField((SqlField) expr1));
					var field2 = GetNewField(new VirtualField((SqlField) expr2));

					if (field1.Equals(field2))
						return true;

					break;
				}
			}

			return null;
		}

		bool CompareConditions(SqlCondition cond1, SqlCondition cond2)
		{
			if (cond1.ElementType != cond2.ElementType)
				return false;

			if (cond1.Predicate.ElementType != cond2.Predicate.ElementType)
				return false;

			switch (cond1.Predicate.ElementType)
			{
				case QueryElementType.IsNullPredicate:
				{
					var isNull1 = (SqlPredicate.IsNull) cond1.Predicate;
					var isNull2 = (SqlPredicate.IsNull) cond2.Predicate;

					return isNull1.IsNot == isNull2.IsNot && CompareExpressions(isNull1.Expr1, isNull2.Expr1) == true;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var expr1 = (SqlPredicate.ExprExpr) cond1.Predicate;
					var expr2 = (SqlPredicate.ExprExpr) cond2.Predicate;

					return CompareExpressions(expr1, expr2);
				}
			}
			return false;
		}

		void OptimizeFilters()
		{
			if (_additionalFilter == null)
				return;

			foreach (var pair in _additionalFilter)
			{
				OptimizeSearchCondition(pair.Value);

				if (!ReferenceEquals(pair.Key, pair.Value) && pair.Value.Conditions.Count == 1)
				{
					// conditions can be optimized so we have to remove empty SearchCondition

					if (pair.Value.Conditions[0].Predicate is SqlSearchCondition searchCondition &&
						searchCondition.Conditions.Count == 0)
						pair.Key.Conditions.Remove(pair.Value.Conditions[0]);
				}
			}
		}

		bool TryToRemoveIndependentLeftJoin(
			SqlTableSource fromTable, SqlJoinedTable join, List<VirtualField[]> uniqueKeys)
		{
			if (join.JoinType != JoinType.Left)
				return false;

			if (join.Table.Source is SqlTable { SqlQueryExtensions.Count: > 0 })
				return false;

			var found = SearchForFields(null, join);

			if (found == null)
				return false;

			HashSet<VirtualField>  foundFields  = new HashSet<VirtualField>(found.Select(f => f.OneField!));
			HashSet<VirtualField>? uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Count; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(foundFields.Contains))
				{
					uniqueFields ??= new HashSet<VirtualField>();
					foreach (var key in keys)
						uniqueFields.Add(key);
				}
			}

			if (uniqueFields != null)
			{
				RemoveSource(fromTable, join);

				return true;
			}

			return false;
		}

		bool IsDependedBetweenJoins(SqlTableSource table, SqlJoinedTable testedJoin)
		{
			var testedSources = new HashSet<int>(testedJoin.Table.GetTables().Select(t => t.SourceID));

			foreach (var tableJoin in table.Joins)
			{
				if (testedSources.Contains(tableJoin.Table.SourceID))
					continue;

				if (IsDependedOnJoin(table, tableJoin, testedSources))
					return true;
			}

			return IsDependedExcludeJoins(testedSources);
		}

		bool IsDependedOnJoin(SqlTableSource table, SqlJoinedTable testedJoin, HashSet<int> testedSources)
		{
			var ctx = new IsDependedOnJoinContext(table, testedSources, testedJoin.Table.SourceID);

			// check everything that can be dependent on specific table
			testedJoin.VisitParentFirst(ctx, (context, e) =>
			{
				if (context.Dependent)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);

					if (field != null)
					{
						var newField = GetNewField(field);
						var local    = context.TestedSources.Contains(newField.SourceID);

						if (local)
							context.Dependent = !CanWeReplaceField(context.Table, newField, context.TestedSources, context.CurrentSourceId);
					}
				}

				return !context.Dependent;
			});

			return ctx.Dependent;
		}

		private sealed class IsDependedOnJoinContext
		{
			public IsDependedOnJoinContext(SqlTableSource table, HashSet<int> testedSources, int currentSourceId)
			{
				Table = table;
				TestedSources = testedSources;
				CurrentSourceId = currentSourceId;
			}

			public bool Dependent;

			public readonly SqlTableSource Table;
			public readonly HashSet<int>   TestedSources;
			public readonly int            CurrentSourceId;

		}
		private void FlattenJoins(SqlTableSource element)
		{
			// move join's nested join to same level (sibling)
			for (var i = 0; i < element.Joins.Count; i++)
			{
				var join = element.Joins[i];

				if (join.JoinType is JoinType.Inner or JoinType.Left)
				{
					for (var si = 0; si < join.Table.Joins.Count; si++)
					{
						var nestedJoin = join.Table.Joins[si];

						// TODO: why only those combinations?
						var canFlatten =
							(join.JoinType == JoinType.Inner && (nestedJoin.JoinType is JoinType.Inner or JoinType.Left or JoinType.CrossApply or JoinType.OuterApply))
							||
							(join.JoinType == JoinType.Left && nestedJoin.JoinType == JoinType.Left);

						// TODO: why those conditions
						if (canFlatten && element != join.Table && !HasDependencyWithParent(join, nestedJoin))
						{
							element.Joins.Insert(i + 1, nestedJoin);
							join.Table.Joins.RemoveAt(si);
							--si;
						}
					}
				}

				if (join.JoinType == JoinType.Inner)
					CollectEqualFields(join);

				if (join.JoinType is JoinType.Inner or JoinType.Left)
				{
					// trying to remove join that is equal to FROM table
					if (QueryHelper.IsEqualTables(element.Source as SqlTable, join.Table.Source as SqlTable))
					{
						var keys = GetKeys(join.Table);
						if (keys != null && TryMergeWithTable(element, join, keys))
						{
							element.Joins.RemoveAt(i);
							--i;
							continue;
						}
					}

					for (var i2 = i + 1; i2 < element.Joins.Count; i2++)
					{
						var j2 = element.Joins[i2];

						// we can merge LEFT and INNER joins together
						if (j2.JoinType != JoinType.Inner && j2.JoinType != JoinType.Left)
							continue;

						if (!QueryHelper.IsEqualTables(join.Table.Source as SqlTable, j2.Table.Source as SqlTable))
							continue;

						var keys = GetKeys(j2.Table);

						if (keys != null)
						{
							// try merge if joins are the same
							var merged = TryMergeJoins(element, element, join, j2, keys);

							if (!merged)
								for (var im = 0; im < i2; im++)
									if (element.Joins[im].JoinType == JoinType.Inner || j2.JoinType != JoinType.Left)
									{
										merged = TryMergeJoins(element, element.Joins[im].Table, join, j2, keys);
										if (merged)
											break;
									}

							if (merged)
							{
								element.Joins.RemoveAt(i2);
								--i2;
							}
						}
					}
				}
			}
		}

		void DetectField(SqlTableSource? manySource, SqlTableSource oneSource, VirtualField field, FoundEquality equality)
		{
			field = GetNewField(field);

			if (oneSource.Source.SourceID == field.SourceID)
				equality.OneField = field;
			else if (oneSource.Source is SelectQuery select && select.Select.From.Tables.Count == 1 && select.Select.From.Tables[0].SourceID == field.SourceID)
				equality.OneField = field;
			else if (manySource?.Source.SourceID == field.SourceID)
				equality.ManyField = field;
			else if (manySource != null)
				equality.ManyField = MapToSource(manySource, field, manySource.Source.SourceID)!;
		}

		bool MatchFields(SqlTableSource? manySource, SqlTableSource oneSource, VirtualField? field1, VirtualField? field2, FoundEquality equality)
		{
			if (field1 != null)
				DetectField(manySource, oneSource, field1, equality);
			if (field2 != null)
				DetectField(manySource, oneSource, field2, equality);

			return equality.OneField != null && (manySource == null || equality.ManyField != null);
		}

		List<FoundEquality>? SearchForFields(SqlTableSource? manySource, SqlJoinedTable join)
		{
			var                  key   = Tuple.Create(manySource, join.Table);
			List<FoundEquality>? found = null;

			if (_fieldPairCache != null && _fieldPairCache.TryGetValue(key, out found))
				return found;

			for (var i1 = 0; i1 < join.Condition.Conditions.Count; i1++)
			{
				var c = join.Condition.Conditions[i1];

				if (c.IsOr)
				{
					found = null;
					break;
				}

				if (c.Predicate is SqlSearchCondition search && search.Conditions.Count == 1)
				{
					c = search.Conditions[0];

					if (c.IsOr)
					{
						found = null;
						break;
					}
				}

				if (c.ElementType != QueryElementType.Condition
					|| c.Predicate.ElementType != QueryElementType.ExprExprPredicate
					|| ((SqlPredicate.ExprExpr)c.Predicate).Operator != SqlPredicate.Operator.Equal)
					continue;

				var predicate = (SqlPredicate.ExprExpr) c.Predicate;
				var equality  = new FoundEquality();

				if (!MatchFields(manySource, join.Table,
					GetUnderlayingField(predicate.Expr1),
					GetUnderlayingField(predicate.Expr2),
					equality))
					continue;

				equality.OneCondition = c;

				found ??= new List<FoundEquality>();

				found.Add(equality);
			}

			_fieldPairCache ??= new Dictionary<Tuple<SqlTableSource?, SqlTableSource>, List<FoundEquality>?>();

			_fieldPairCache.Add(key, found);

			return found;
		}

		bool TryMergeWithTable(SqlTableSource fromTable, SqlJoinedTable join, List<VirtualField[]> uniqueKeys)
		{
			if (join.Table.Joins.Count != 0)
				return false;

			if (!(join.Table.Source is SqlTable t && t.SqlTableType == SqlTableType.Table))
				return false;

			// do not allow merging if table used in statement
			if (_statement.IsDependedOn(t))
				return false;

			var hasLeftJoin = join.JoinType == JoinType.Left;
			var found       = SearchForFields(fromTable, join);

			if (found == null)
				return false;

			// for removing join with same table fields should be equal
			found = found.Where(f => IsSimilarFields(f.OneField, f.ManyField)).ToList();

			if (found.Count == 0)
				return false;

			if (hasLeftJoin)
			{
				if (join.Condition.Conditions.Count != found.Count)
					return false;

				// currently no dependencies in search condition allowed for left join
				if (IsDependedExcludeJoins(join))
					return false;
			}

			HashSet<VirtualField>  foundFields  = new HashSet<VirtualField>(found.Select(f => f.OneField));
			HashSet<VirtualField>? uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Count; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(foundFields.Contains))
				{
					uniqueFields ??= new HashSet<VirtualField>();

					foreach (var key in keys)
						uniqueFields.Add(key);
				}
			}

			if (uniqueFields != null)
			{
				foreach (var item in found)
					if (uniqueFields.Contains(item.OneField))
					{
						// remove unique key conditions
						join.Condition.Conditions.Remove(item.OneCondition);
						AddEqualFields(item.ManyField, item.OneField, fromTable.SourceID);
					}

				// move rest conditions to the Where section
				if (join.Condition.Conditions.Count > 0)
				{
					AddSearchConditions(_selectQuery.Where.SearchCondition, join.Condition.Conditions);
					join.Condition.Conditions.Clear();
				}

				// add check that previously joined fields is not null
				foreach (var item in found)
					if (item.ManyField.CanBeNullable(_nullablility))
					{
						var newField = MapToSource(fromTable, item.ManyField, fromTable.SourceID);
						AddSearchCondition(_selectQuery.Where.SearchCondition,
							new SqlCondition(false, new SqlPredicate.IsNull(newField!.Element, true)));
					}

				// add mapping to new source
				ReplaceSource(fromTable, join, fromTable);

				return true;
			}

			return false;
		}

		void AddSearchCondition(SqlSearchCondition search, SqlCondition condition)
		{
			AddSearchConditions(search, new[] { condition });
		}

		private sealed class IsDependedExcludeJoinsContext
		{
			public IsDependedExcludeJoinsContext(HashSet<int> testedSources)
			{
				TestedSources = testedSources;
			}

			public bool Dependent;

			public readonly HashSet<int>  TestedSources;
		}

		bool IsDependedExcludeJoins(SqlJoinedTable join)
		{
			var testedSources = new HashSet<int>(join.Table.GetTables().Select(t => t.SourceID));
			return IsDependedExcludeJoins(testedSources);
		}

		bool IsDependedExcludeJoins(HashSet<int> testedSources)
		{
			bool CheckDependency(IsDependedExcludeJoinsContext context, IQueryElement e)
			{
				if (context.Dependent)
					return false;

				if (e.ElementType == QueryElementType.JoinedTable)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);

					if (field != null)
					{
						var newField = GetNewField(field);
						var local = context.TestedSources.Contains(newField.SourceID);
						if (local)
							context.Dependent = !CanWeReplaceField(null, newField, context.TestedSources, -1);
					}
				}

				return !context.Dependent;
			}

			var ctx = new IsDependedExcludeJoinsContext(testedSources);

			//TODO: review dependency checking
			_selectQuery.VisitParentFirst(ctx, CheckDependency);
			if (!ctx.Dependent)
				_statement.VisitParentFirst(ctx, CheckDependency);

			return ctx.Dependent;
		}

		bool TryMergeJoins(SqlTableSource fromTable,
			SqlTableSource manySource,
			SqlJoinedTable join1, SqlJoinedTable join2,
			List<VirtualField[]> uniqueKeys)
		{
			if (!(join2.Table.Source is SqlTable t && t.SqlTableType == SqlTableType.Table))
				return false;

			// do not allow merging if table used in statement
			if (_statement.IsDependedOn(t))
				return false;

			var found1 = SearchForFields(manySource, join1);

			if (found1 == null)
				return false;

			var found2 = SearchForFields(manySource, join2);

			if (found2 == null)
				return false;

			var hasLeftJoin = join1.JoinType == JoinType.Left || join2.JoinType == JoinType.Left;

			// left join should match exactly
			if (hasLeftJoin)
			{
				if (join1.Condition.Conditions.Count != join2.Condition.Conditions.Count)
					return false;

				if (found1.Count != found2.Count)
					return false;

				if (join1.Table.Joins.Count != 0 || join2.Table.Joins.Count != 0)
					return false;
			}

			List<FoundEquality>? found = null;

			for (var i1 = 0; i1 < found1.Count; i1++)
			{
				var f1 = found1[i1];

				for (var i2 = 0; i2 < found2.Count; i2++)
				{
					var f2 = found2[i2];

					if (IsSimilarFields(f1.ManyField, f2.ManyField) && IsSimilarFields(f1.OneField, f2.OneField))
					{
						found ??= new List<FoundEquality>();

						found.Add(f2);
					}
				}
			}

			if (found == null)
				return false;

			if (hasLeftJoin)
			{
				// for left join each expression should be used
				if (found.Count != join1.Condition.Conditions.Count)
					return false;

				// currently no dependencies in search condition allowed for left join
				if (IsDepended(join1, join2))
					return false;
			}

			HashSet<VirtualField>  foundFields  = new HashSet<VirtualField>(found.Select(f => f.OneField));
			HashSet<VirtualField>? uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Count; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(foundFields.Contains))
				{
					uniqueFields ??= new HashSet<VirtualField>();

					foreach (var key in keys)
						uniqueFields.Add(key);
				}
			}

			if (uniqueFields != null)
			{
				foreach (var item in found)
					if (uniqueFields.Contains(item.OneField))
					{
						// remove from second
						join2.Condition.Conditions.Remove(item.OneCondition);

						AddEqualFields(item.ManyField, item.OneField, fromTable.SourceID);
					}

				// move rest conditions to first
				if (join2.Condition.Conditions.Count > 0)
				{
					AddSearchConditions(join1.Condition, join2.Condition.Conditions);
					join2.Condition.Conditions.Clear();
				}

				join1.Table.Joins.AddRange(join2.Table.Joins);

				// add mapping to new source
				ReplaceSource(fromTable, join2, join1.Table);

				return true;
			}

			return false;
		}

		static bool IsSimilarFields(VirtualField field1, VirtualField field2)
		{
			if (field1.Element is SqlField sqlField1)
			{
				if (field2.Element is SqlField sqlField2)
					return sqlField1.PhysicalName == sqlField2.PhysicalName;
				return false;
			}

			return ReferenceEquals(field1.Element, field2.Element);
		}

		private sealed class IsDependedContext
		{
			public IsDependedContext(HashSet<int> testedSources)
			{
				TestedSources = testedSources;
			}

			public bool Dependent;

			public readonly HashSet<int>  TestedSources;
		}

		bool IsDepended(SqlJoinedTable join, SqlJoinedTable toIgnore)
		{
			var testedSources = new HashSet<int>(join.Table.GetTables().Select(t => t.SourceID));
			if (toIgnore != null)
				foreach (var sourceId in toIgnore.Table.GetTables().Select(t => t.SourceID))
					testedSources.Add(sourceId);

			var ctx = new IsDependedContext(testedSources);

			_statement.VisitParentFirst(ctx, (context, e) =>
			{
				if (context.Dependent)
					return false;

				// ignore non searchable parts
				if (e.ElementType == QueryElementType.SelectClause
				   || e.ElementType == QueryElementType.GroupByClause
				   || e.ElementType == QueryElementType.OrderByClause)
					return false;

				if (e.ElementType == QueryElementType.JoinedTable)
					if (context.TestedSources.Contains(((SqlJoinedTable)e).Table.SourceID))
						return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);
					if (field != null)
					{
						var newField = GetNewField(field);
						var local = context.TestedSources.Contains(newField.SourceID);
						if (local)
							context.Dependent = !CanWeReplaceField(null, newField, context.TestedSources, -1);
					}
				}

				return !context.Dependent;
			});

			return ctx.Dependent;
		}

		bool IsSourceRemoved(int sourceId)
		{
			return _removedSources != null && _removedSources.Contains(sourceId);
		}

		bool CanWeReplaceFieldInternal(
			SqlTableSource? table, VirtualField field, HashSet<int> excludeSourceIds, int testedSourceIndex, HashSet<VirtualField> visited)
		{
			if (visited.Contains(field))
				return false;

			if (!excludeSourceIds.Contains(field.SourceID) && !IsSourceRemoved(field.SourceID))
				return true;

			visited.Add(field);

			if (_equalityMap == null)
				return false;

			if (testedSourceIndex < 0)
				return false;

			if (_equalityMap.TryGetValue(field, out var sameFields))
				foreach (var pair in sameFields)
					if ((testedSourceIndex == 0 || GetSourceIndex(table, pair.Item1) > testedSourceIndex)
						&& CanWeReplaceFieldInternal(table, pair.Item2, excludeSourceIds, testedSourceIndex, visited))
						return true;

			return false;
		}

		bool CanWeReplaceField(SqlTableSource? table, VirtualField field, HashSet<int> excludeSourceId, int testedSourceId)
		{
			var visited = new HashSet<VirtualField>();

			return CanWeReplaceFieldInternal(table, field, excludeSourceId, GetSourceIndex(table, testedSourceId), visited);
		}

		void AddSearchConditions(SqlSearchCondition search, IEnumerable<SqlCondition> conditions)
		{
			_additionalFilter ??= new Dictionary<SqlSearchCondition, SqlSearchCondition>();

			if (!_additionalFilter.TryGetValue(search, out var value))
			{
				if (search.Conditions.Count > 0 && search.Precedence < Precedence.LogicalConjunction)
				{
					value = new SqlSearchCondition();
					var prev  = new SqlSearchCondition();

					prev.Conditions.AddRange(search.Conditions);
					search.Conditions.Clear();

					search.Conditions.Add(new SqlCondition(false, value, false));
					search.Conditions.Add(new SqlCondition(false, prev, false));
				}
				else
				{
					value = search;
				}

				_additionalFilter.Add(search, value);
			}

			value.Conditions.AddRange(conditions);
		}

		void ReplaceSource(SqlTableSource fromTable, SqlJoinedTable oldSource, SqlTableSource newSource)
		{
			var oldFields = GetFields(oldSource.Table.Source);
			var newFields = GetFields(newSource.Source);

			foreach (var old in oldFields)
			{
				var newField = newFields[old.Key];

				ReplaceField(old.Value, newField);
			}

			RemoveSource(fromTable, oldSource);
		}

		static Dictionary<string, VirtualField> GetFields(ISqlTableSource source)
		{
			var res = new Dictionary<string, VirtualField>();

			if (source is SqlTable table)
			{
				foreach (var field in table.Fields)
					res.Add(field.Name, new VirtualField(field));

				res.Add(source.All.Name, new VirtualField(source.All));
			}

			return res;
		}

		void RemoveSource(SqlTableSource fromTable, SqlJoinedTable join)
		{
			_removedSources ??= new HashSet<int>();

			_removedSources.Add(join.Table.SourceID);

			if (_equalityMap != null)
			{
				var keys = _equalityMap.Keys.Where(k => k.SourceID == join.Table.SourceID).ToArray();

				foreach (var key in keys)
				{
					var newField = MapToSource(fromTable, key, fromTable.SourceID);

					if (newField != null)
						ReplaceField(key, newField);

					_equalityMap.Remove(key);
				}
			}

			//TODO: investigate another ways when we can propagate keys up
			if (join.JoinType == JoinType.Inner && join.Table.HasUniqueKeys)
			{
				var newFields = join.Table.UniqueKeys
					.Select(uk => uk.Select(k => GetNewField(new VirtualField(k)).Element).ToArray());
				fromTable.UniqueKeys.AddRange(newFields);
			}

			ResetFieldSearchCache(join.Table);
		}

		VirtualField? MapToSource(SqlTableSource table, VirtualField field, int sourceId)
		{
			var visited = new HashSet<VirtualField>();

			return MapToSourceInternal(table, field, sourceId, visited);
		}

		VirtualField? MapToSourceInternal(SqlTableSource fromTable, VirtualField field, int sourceId, HashSet<VirtualField> visited)
		{
			if (visited.Contains(field))
				return null;

			if (field.SourceID == sourceId)
				return field;

			visited.Add(field);

			if (_equalityMap == null)
				return null;

			var sourceIndex = GetSourceIndex(fromTable, sourceId);

			if (_equalityMap.TryGetValue(field, out var sameFields))
				foreach (var pair in sameFields)
				{
					var itemIndex = GetSourceIndex(fromTable, pair.Item1);

					if (itemIndex >= 0 && (sourceIndex == 0 || itemIndex < sourceIndex))
					{
						var newField = MapToSourceInternal(fromTable, pair.Item2, sourceId, visited);

						if (newField != null)
							return newField;
					}
				}

			return null;
		}

		static int GetSourceIndex(SqlTableSource? table, int sourceId)
		{
			if (table == null || table.SourceID == sourceId || sourceId == -1)
				return 0;

			var i = 0;

			while (i < table.Joins.Count)
			{
				if (table.Joins[i].Table.SourceID == sourceId)
					return i + 1;

				++i;
			}

			return -1;
		}

		void ReplaceField(VirtualField oldField, VirtualField newField)
		{
			_replaceMap ??= new Dictionary<VirtualField, VirtualField>();

			_replaceMap.Remove(oldField);
			_replaceMap.Add(oldField, newField);
		}

		VirtualField GetNewField(VirtualField field)
		{
			if (_replaceMap == null)
				return field;

			if (_replaceMap.TryGetValue(field, out var newField))
			{
				while (_replaceMap.TryGetValue(newField, out var fieldOther))
					newField = fieldOther;
			}
			else
			{
				newField = field;
			}

			return newField;
		}

		void ResetFieldSearchCache(SqlTableSource table)
		{
			if (_fieldPairCache == null)
				return;

			var keys = _fieldPairCache.Keys.Where(k => k.Item2 == table || k.Item1 == table).ToArray();

			foreach (var key in keys)
				_fieldPairCache.Remove(key);
		}

		/// <summary>
		/// Collects unique keys from different sources.
		/// </summary>
		/// <param name="tableSource"></param>
		/// <returns>List of unique keys</returns>
		static List<VirtualField[]>? GetKeysInternal(SqlTableSource tableSource)
		{
			var knownKeys = new List<IList<ISqlExpression>>();
			QueryHelper.CollectUniqueKeys(tableSource, knownKeys);
			if (knownKeys.Count == 0)
				return null;

			var result = new List<VirtualField[]>();

			foreach (var v in knownKeys)
			{
				var fields = new VirtualField[v.Count];
				for (var i = 0; i < v.Count; i++)
					fields[i] = GetUnderlayingField(v[i]) ?? throw new InvalidOperationException($"Cannot get field for {v[i]}");
				result.Add(fields);
			}

			return result.Count > 0 ? result : null;
		}

		List<VirtualField[]>? GetKeys(SqlTableSource tableSource)
		{
			if (_keysCache == null || !_keysCache.TryGetValue(tableSource.SourceID, out var keys))
			{
				keys = GetKeysInternal(tableSource);

				_keysCache ??= new Dictionary<int, List<VirtualField[]>?>();

				_keysCache.Add(tableSource.SourceID, keys);
			}

			return keys;
		}

		void CollectEqualFields(SqlJoinedTable join)
		{
			if (join.Condition.Conditions.Any(c => c.IsOr))
				return;

			for (var i1 = 0; i1 < join.Condition.Conditions.Count; i1++)
			{
				var c = join.Condition.Conditions[i1];

				if (c.ElementType != QueryElementType.Condition
					|| c.Predicate.ElementType != QueryElementType.ExprExprPredicate
					|| ((SqlPredicate.ExprExpr)c.Predicate).Operator != SqlPredicate.Operator.Equal)
					continue;

				var predicate = (SqlPredicate.ExprExpr) c.Predicate;

				var field1 = GetUnderlayingField(predicate.Expr1);

				if (field1 == null)
					continue;

				var field2 = GetUnderlayingField(predicate.Expr2);

				if (field2 == null)
					continue;

				if (field1.Equals(field2))
					continue;

				AddEqualFields(field1, field2, join.Table.SourceID);
				AddEqualFields(field2, field1, join.Table.SourceID);
			}
		}

		void AddEqualFields(VirtualField field1, VirtualField field2, int levelSourceId)
		{
			_equalityMap ??= new Dictionary<VirtualField, HashSet<Tuple<int, VirtualField>>>();

			if (!_equalityMap.TryGetValue(field1, out var set))
			{
				set = new HashSet<Tuple<int, VirtualField>>();
				_equalityMap.Add(field1, set);
			}

			set.Add(Tuple.Create(levelSourceId, field2));
		}

		public override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
		{
			var condition = base.VisitSqlSearchCondition(element);

			if (_correntMappings)
				return condition;

			if (condition is SqlSearchCondition cond && cond.Conditions.Count > 0)
				return SelectQueryOptimizerVisitor.OptimizeSearchCondition(cond, _evaluationContext);

			return condition;
		}

		private sealed class HasDependencyWithParentContext
		{
			public HasDependencyWithParentContext(SqlJoinedTable child, HashSet<int> sources)
			{
				Child = child;
				Sources = sources;
			}

			public readonly SqlJoinedTable Child;
			public readonly HashSet<int>   Sources;

			public bool Dependent;
		}

		static bool HasDependencyWithParent(SqlJoinedTable parent, SqlJoinedTable child)
		{
			var sources = new HashSet<int>(child.Table.GetTables().Select(t => t.SourceID));
			var ctx     = new HasDependencyWithParentContext(child, sources);

			// check that parent has dependency on child
			parent.VisitParentFirst(ctx, static (context, e) =>
			{
				if (context.Dependent)
					return false;

				if (e == context.Child)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);
					if (field != null)
						context.Dependent = context.Sources.Contains(field.SourceID);
				}

				return !context.Dependent;
			});

			return ctx.Dependent;
		}

		static VirtualField? GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlExpression:
				{
					var sqlExpr = (SqlExpression) expr;
					if (sqlExpr.Expr == "{0}" && sqlExpr.Parameters.Length == 1)
						return GetUnderlayingField(sqlExpr.Parameters[0]);
					break;
				}
				case QueryElementType.SqlNullabilityExpression:
					return GetUnderlayingField(((SqlNullabilityExpression)expr).SqlExpression);
				case QueryElementType.SqlField:
					return new VirtualField((SqlField)expr);
				case QueryElementType.Column:
					return new VirtualField((SqlColumn)expr);
			}

			return null;
		}

		[DebuggerDisplay("{ManyField.DisplayString()} -> {OneField.DisplayString()}")]
		sealed class FoundEquality
		{
			public VirtualField ManyField = null!;
			public SqlCondition OneCondition = null!;
			public VirtualField OneField = null!;
		}

		//TODO: investigate do we still needs this class over ISqlExpression
		[DebuggerDisplay("{DisplayString()}")]
		sealed class VirtualField
		{
			public VirtualField(ISqlExpression expression)
			{
				if (expression == null) throw new ArgumentNullException(nameof(expression));

				if (expression is not SqlField && expression is not SqlColumn)
				{
					throw new ArgumentException($"Expression '{expression}' is not a Field or Column.",
						nameof(expression));
				}

				_expression = expression;
			}

			public VirtualField(SqlField field) : this((ISqlExpression)field)
			{
			}

			public VirtualField(SqlColumn column) : this((ISqlExpression)column)
			{
			}

			public int SourceID
			{
				get
				{
					var sourceId = _expression switch
					{
						SqlField sqlField   => sqlField.Table?.SourceID,
						SqlColumn sqlColumn => sqlColumn.Parent?.SourceID,
						_                   => null
					};

					return sourceId ?? -1;
				}
			}

			public bool CanBeNullable(NullabilityContext nullability) => Element.CanBeNullable(nullability);

			private ISqlExpression _expression;
			private ISqlExpression Expression => _expression;

			public ISqlExpression Element => _expression;

			bool Equals(VirtualField other)
			{
				return Equals(Expression, other.Expression);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;

				if (ReferenceEquals(this, obj)) return true;

				if (obj.GetType() != GetType()) return false;

				return Equals((VirtualField)obj);
			}

			static string GetSourceString(ISqlTableSource? source)
			{
				if (source == null)
					return "(unknown)";

				if (source is SqlTable table)
				{
					var res = $"({source.SourceID}).{table.NameForLogging}";
					if (table.Alias != table.NameForLogging && !string.IsNullOrEmpty(table.Alias))
						res = res + "(" + table.Alias + ")";

					return res;
				}

				var writer = new QueryElementTextWriter();
				source.ToString(writer);

				return $"({source.SourceID}).{writer}";
			}

			public string DisplayString()
			{
				if (_expression is SqlField sqlField)
					return $"F: '{GetSourceString(sqlField.Table!)}.{sqlField.Name}'";

				if (_expression is SqlColumn sqlColumn)
					return $"C: '{GetSourceString(sqlColumn.Parent)}.{sqlColumn.Alias}'";

				return _expression.ToDebugString();
			}

			public override int GetHashCode()
			{
				return Expression?.GetHashCode() ?? 0;
			}
		}
	}
}
