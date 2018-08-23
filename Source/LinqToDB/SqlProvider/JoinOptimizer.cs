using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using JetBrains.Annotations;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class JoinOptimizer
	{
		Dictionary<SqlSearchCondition,SqlSearchCondition>                    _additionalFilter;
		Dictionary<VirtualField,HashSet<Tuple<int,VirtualField>>>            _equalityMap;
		Dictionary<Tuple<SqlTableSource,SqlTableSource>,List<FoundEquality>> _fieldPairCache;
		Dictionary<int,List<List<string>>>                                   _keysCache;
		HashSet<int>                                                         _removedSources;
		Dictionary<VirtualField,VirtualField>                                _replaceMap;
		SelectQuery                                                          _selectQuery;
		SqlStatement                                                         _statement;

		void FlattenJoins(SqlTableSource table)
		{
			for (var i = 0; i < table.Joins.Count; i++)
			{
				var j = table.Joins[i];
				FlattenJoins(j.Table);

				if (j.JoinType == JoinType.Inner)
					for (var si = 0; si < j.Table.Joins.Count; si++)
					{
						var sj = j.Table.Joins[si];
						if ((sj.JoinType == JoinType.Inner || sj.JoinType == JoinType.Left)
							&& table != j.Table && !HasDependencyWithParent(j, sj))
						{
							table.Joins.Insert(i + 1, sj);
							j.Table.Joins.RemoveAt(si);
							--si;
						}
					}
			}
		}

		bool IsDependedBetweenJoins(SqlTableSource table,
			SqlJoinedTable testedJoin)
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

		bool IsDepended(SqlJoinedTable join, SqlJoinedTable toIgnore)
		{
			var testedSources = new HashSet<int>(join.Table.GetTables().Select(t => t.SourceID));
			if (toIgnore != null)
				foreach (var sourceId in toIgnore.Table.GetTables().Select(t => t.SourceID))
					testedSources.Add(sourceId);

			var dependent = false;

			new QueryVisitor().VisitParentFirst(_statement, e =>
			{
				if (dependent)
					return false;

				// ignore non searchable parts
				if (  e.ElementType == QueryElementType.SelectClause
				   || e.ElementType == QueryElementType.GroupByClause
				   || e.ElementType == QueryElementType.OrderByClause)
					return false;

				if (e.ElementType == QueryElementType.JoinedTable)
					if (testedSources.Contains(((SqlJoinedTable) e).Table.SourceID))
						return false;

				var expression = e as ISqlExpression;

				if (expression != null)
				{
					var field = GetUnderlayingField(expression);
					if (field != null)
					{
						var newField = GetNewField(field);
						var local = testedSources.Contains(newField.SourceID);
						if (local)
							dependent = !CanWeReplaceField(null, newField, testedSources, -1);
					}
				}

				return !dependent;
			});

			return dependent;
		}

		bool IsDependedExcludeJoins(SqlJoinedTable join)
		{
			var testedSources = new HashSet<int>(join.Table.GetTables().Select(t => t.SourceID));
			return IsDependedExcludeJoins(testedSources);
		}

		bool IsDependedExcludeJoins(HashSet<int> testedSources)
		{
			var dependent = false;

			bool CheckDependency(IQueryElement e)
			{
				if (dependent)
					return false;

				if (e.ElementType == QueryElementType.JoinedTable)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);

					if (field != null)
					{
						var newField = GetNewField(field);
						var local = testedSources.Contains(newField.SourceID);
						if (local)
							dependent = !CanWeReplaceField(null, newField, testedSources, -1);
					}
				}

				return !dependent;
			}

			//TODO: review dependency checking
			new QueryVisitor().VisitParentFirst(_selectQuery, CheckDependency);
			if (!dependent && _selectQuery.ParentSelect == null)
				new QueryVisitor().VisitParentFirst(_statement, CheckDependency);

			return dependent;
		}

		bool HasDependencyWithParent(SqlJoinedTable parent,
			SqlJoinedTable child)
		{
			var sources   = new HashSet<int>(child.Table.GetTables().Select(t => t.SourceID));
			var dependent = false;

			// check that parent has dependency on child
			new QueryVisitor().VisitParentFirst(parent, e =>
			{
				if (dependent)
					return false;

				if (e == child)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);
					if (field != null)
						dependent = sources.Contains(field.SourceID);
				}

				return !dependent;
			});

			return dependent;
		}

		bool IsDependedOnJoin(SqlTableSource table, SqlJoinedTable testedJoin, HashSet<int> testedSources)
		{
			var dependent       = false;
			var currentSourceId = testedJoin.Table.SourceID;

			// check everyting that can be dependent on specific table
			new QueryVisitor().VisitParentFirst(testedJoin, e =>
			{
				if (dependent)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlayingField(expression);

					if (field != null)
					{
						var newField = GetNewField(field);
						var local    = testedSources.Contains(newField.SourceID);

						if (local)
							dependent = !CanWeReplaceField(table, newField, testedSources, currentSourceId);
					}
				}

				return !dependent;
			});

			return dependent;
		}

		bool CanWeReplaceFieldInternal(
			SqlTableSource table, VirtualField field, HashSet<int> excludeSourceIds, int testedSourceIndex, HashSet<VirtualField> visited)
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

		bool CanWeReplaceField(SqlTableSource table, VirtualField field, HashSet<int> excludeSourceId, int testedSourceId)
		{
			var visited = new HashSet<VirtualField>();

			return CanWeReplaceFieldInternal(table, field, excludeSourceId, GetSourceIndex(table, testedSourceId), visited);
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

		VirtualField MapToSourceInternal(SqlTableSource fromTable, VirtualField field, int sourceId, HashSet<VirtualField> visited)
		{
			if (visited.Contains(field))
				return null;

			if (field.SourceID == sourceId)
				return field;

			visited.Add(field);

			if (_equalityMap == null)
				return null;

			var sourceIndex = GetSourceIndex(fromTable, sourceId);

			HashSet<Tuple<int, VirtualField>> sameFields;

			if (_equalityMap.TryGetValue(field, out sameFields))
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

		VirtualField MapToSource(SqlTableSource table, VirtualField field, int sourceId)
		{
			var visited = new HashSet<VirtualField>();

			return MapToSourceInternal(table, field, sourceId, visited);
		}

		void RemoveSource(SqlTableSource fromTable, SqlJoinedTable join)
		{
			if (_removedSources == null)
				_removedSources = new HashSet<int>();

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

			ResetFieldSearchCache(join.Table);
		}

		bool IsSourceRemoved(int sourceId)
		{
			return _removedSources != null && _removedSources.Contains(sourceId);
		}

		void ReplaceField(VirtualField oldField, VirtualField newField)
		{
			if (_replaceMap == null)
				_replaceMap = new Dictionary<VirtualField, VirtualField>();

			_replaceMap.Remove(oldField);
			_replaceMap.Add   (oldField, newField);
		}

		void AddEqualFields(VirtualField field1, VirtualField field2, int levelSourceId)
		{
			if (_equalityMap == null)
				_equalityMap = new Dictionary<VirtualField, HashSet<Tuple<int, VirtualField>>>();

			HashSet<Tuple<int, VirtualField>> set;

			if (!_equalityMap.TryGetValue(field1, out set))
			{
				set = new HashSet<Tuple<int, VirtualField>>();
				_equalityMap.Add(field1, set);
			}

			set.Add(Tuple.Create(levelSourceId, field2));
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
				case QueryElementType.Column:
				{
					return CompareExpressions(((SqlColumn) expr1).Expression, ((SqlColumn) expr2).Expression);
				}

				case QueryElementType.SqlField:
				{
					var field1 = GetNewField(new VirtualField((SqlField) expr1));
					var field2 = GetNewField(new VirtualField((SqlField) expr2));

					return field1.Equals(field2);
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

		void AddSearchCondition(SqlSearchCondition search, SqlCondition condition)
		{
			AddSearchConditions(search, new[] {condition});
		}

		void AddSearchConditions(SqlSearchCondition search, IEnumerable<SqlCondition> conditions)
		{
			if (_additionalFilter == null)
				_additionalFilter = new Dictionary<SqlSearchCondition, SqlSearchCondition>();

			if (!_additionalFilter.TryGetValue(search, out var value))
			{
				if (search.Conditions.Count > 0 && search.Precedence < Precedence.LogicalConjunction)
				{
					    value = new SqlSearchCondition();
					var prev  = new SqlSearchCondition();

					prev.  Conditions.AddRange(search.Conditions);
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

		Dictionary<string, VirtualField> GetFields(ISqlTableSource source)
		{
			var res = new Dictionary<string, VirtualField>();

			if (source is SqlTable table)
				foreach (var pair in table.Fields)
					res.Add(pair.Key, new VirtualField(pair.Value));

			return res;
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

		void CorrectMappings()
		{
			if (_replaceMap != null && _replaceMap.Count > 0 || _removedSources != null)
			{
				((ISqlExpressionWalkable)_statement)
					.Walk(false, element =>
					{
						if (element is SqlField field)
							return GetNewField(new VirtualField(field)).Element;

						if (element is SqlColumn column)
							return GetNewField(new VirtualField(column)).Element;

						return element;
					});
			}
		}

		int GetSourceIndex(SqlTableSource table, int sourceId)
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

		void CollectEqualFields(SqlJoinedTable join)
		{
			if (join.JoinType != JoinType.Inner)
				return;

			if (join.Condition.Conditions.Any(c => c.IsOr))
				return;

			for (var i1 = 0; i1 < join.Condition.Conditions.Count; i1++)
			{
				var c = join.Condition.Conditions[i1];

				if (   c.ElementType                                  != QueryElementType.Condition
					|| c.Predicate.ElementType                        != QueryElementType.ExprExprPredicate
					|| ((SqlPredicate.ExprExpr) c.Predicate).Operator != SqlPredicate.Operator.Equal)
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

		List<List<string>> GetKeysInternal(ISqlTableSource tableSource)
		{
			//TODO: needed mechanism to define unique indexes. Currently only primary key is used

			// only from tables we can get keys
			if (!(tableSource is SqlTable))
				return null;

			var keys = tableSource.GetKeys(false);

			if (keys == null || keys.Count == 0)
				return null;

			var fields = keys.Select(GetUnderlayingField)
				.Where(f => f != null)
				.Select(f => f.Name).ToList();

			if (fields.Count != keys.Count)
				return null;

			var knownKeys = new List<List<string>> { fields };


			return knownKeys;
		}

		List<List<string>> GetKeys(ISqlTableSource tableSource)
		{
			if (_keysCache == null || !_keysCache.TryGetValue(tableSource.SourceID, out var keys))
			{
				keys = GetKeysInternal(tableSource);

				if (_keysCache == null)
					_keysCache = new Dictionary<int, List<List<string>>>();

				_keysCache.Add(tableSource.SourceID, keys);
			}

			return keys;
		}

		public void OptimizeJoins(SqlStatement statement, SelectQuery selectQuery)
		{
			_selectQuery = selectQuery;
			_statement   = statement;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var fromTable = selectQuery.From.Tables[i];

				FlattenJoins(fromTable);

				for (var i1 = 0; i1 < fromTable.Joins.Count; i1++)
				{
					var j1 = fromTable.Joins[i1];

					CollectEqualFields(j1);

					// supported only INNER and LEFT joins
					if (j1.JoinType != JoinType.Inner && j1.JoinType != JoinType.Left)
						continue;

					// trying to remove join that is equal to FROM table
					if (QueryHelper.IsEqualTables(fromTable.Source as SqlTable, j1.Table.Source as SqlTable))
					{
						var keys = GetKeys(j1.Table.Source);
						if (keys != null && TryMergeWithTable(fromTable, j1, keys))
						{
							fromTable.Joins.RemoveAt(i1);
							--i1;
							continue;
						}
					}

					for (var i2 = i1 + 1; i2 < fromTable.Joins.Count; i2++)
					{
						var j2 = fromTable.Joins[i2];

						// we can merge LEFT and INNER joins together
						if (j2.JoinType != JoinType.Inner && j2.JoinType != JoinType.Left)
							continue;

						if (!QueryHelper.IsEqualTables(j1.Table.Source as SqlTable, j2.Table.Source as SqlTable))
							continue;

						var keys = GetKeys(j2.Table.Source);

						if (keys != null)
						{
							// try merge if joins are the same
							var merged = TryMergeJoins(fromTable, fromTable, j1, j2, keys);

							if (!merged)
								for (var im = 0; im < i2; im++)
									if (fromTable.Joins[im].JoinType == JoinType.Inner || j2.JoinType != JoinType.Left)
									{
										merged = TryMergeJoins(fromTable, fromTable.Joins[im].Table, j1, j2, keys);
										if (merged)
											break;
									}

							if (merged)
							{
								fromTable.Joins.RemoveAt(i2);
								--i2;
							}
						}
					}
				}

				// trying to remove joins that are not in projection
				for (var i1 = 0; i1 < fromTable.Joins.Count; i1++)
				{
					var j1 = fromTable.Joins[i1];

					if (j1.JoinType == JoinType.Left || j1.JoinType == JoinType.Inner)
					{
						var keys = GetKeys(j1.Table.Source);

						if (keys != null && !IsDependedBetweenJoins(fromTable, j1))
						{
							// try merge if joins are the same
							var removed = TryToRemoveIndependent(fromTable, fromTable, j1, keys);
							if (!removed)
								for (var im = 0; im < i1; im++)
								{
									var jm = fromTable.Joins[im];
									if (jm.JoinType == JoinType.Inner || jm.JoinType != JoinType.Left)
									{
										removed = TryToRemoveIndependent(fromTable, jm.Table, j1, keys);
										if (removed)
											break;
									}
								}
							if (removed)
							{
								fromTable.Joins.RemoveAt(i1);
								--i1;
							}
						}
					}
				} // independent joins loop
			} // table loop


			OptimizeFilters();
			CorrectMappings();

		}

		static VirtualField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField:
					return new VirtualField((SqlField) expr);
				case QueryElementType.Column:
					return new VirtualField((SqlColumn)expr);
			}

			return null;
		}

		void DetectField(SqlTableSource manySource, SqlTableSource oneSource, VirtualField field, FoundEquality equality)
		{
			field = GetNewField(field);

			if (oneSource.Source.SourceID == field.SourceID)
				equality.OneField = field;
			else if (manySource.Source.SourceID == field.SourceID)
				equality.ManyField = field;
			else
				equality.ManyField = MapToSource(manySource, field, manySource.Source.SourceID);
		}

		bool MatchFields(SqlTableSource manySource, SqlTableSource oneSource, VirtualField field1, VirtualField field2, FoundEquality equality)
		{
			if (field1 == null || field2 == null)
				return false;

			DetectField(manySource, oneSource, field1, equality);
			DetectField(manySource, oneSource, field2, equality);

			return equality.OneField != null && equality.ManyField != null;
		}

		void ResetFieldSearchCache(SqlTableSource table)
		{
			if (_fieldPairCache == null)
				return;

			var keys = _fieldPairCache.Keys.Where(k => k.Item2 == table || k.Item1 == table).ToArray();

			foreach (var key in keys)
				_fieldPairCache.Remove(key);
		}

		List<FoundEquality> SearchForFields(SqlTableSource manySource, SqlJoinedTable join)
		{
			var                 key   = Tuple.Create(manySource, join.Table);
			List<FoundEquality> found = null;

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

				if (   c.ElementType                                           != QueryElementType.Condition
					|| c.Predicate.ElementType                                 != QueryElementType.ExprExprPredicate
					|| ((SqlPredicate.ExprExpr) c.Predicate).Operator != SqlPredicate.Operator.Equal)
					continue;

				var predicate = (SqlPredicate.ExprExpr) c.Predicate;
				var equality  = new FoundEquality();

				if (!MatchFields(manySource, join.Table,
					GetUnderlayingField(predicate.Expr1),
					GetUnderlayingField(predicate.Expr2),
					equality))
					continue;

				equality.OneCondition = c;

				if (found == null)
					found = new List<FoundEquality>();

				found.Add(equality);
			}

			if (_fieldPairCache == null)
				_fieldPairCache = new Dictionary<Tuple<SqlTableSource, SqlTableSource>, List<FoundEquality>>();

			_fieldPairCache.Add(key, found);

			return found;
		}

		bool TryMergeWithTable(SqlTableSource fromTable, SqlJoinedTable join, List<List<string>> uniqueKeys)
		{
			if (join.Table.Joins.Count != 0)
				return false;

			// do not allow merging if table used in statement
			if (join.Table.Source is SqlTable t && _statement.IsDependedOn(t))
				return false;

			var hasLeftJoin = join.JoinType == JoinType.Left;
			var found       = SearchForFields(fromTable, join);

			if (found == null)
				return false;

			// for removing join with same table fields should be equal
			found = found.Where(f => f.OneField.Name == f.ManyField.Name).ToList();

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

			HashSet<string> foundFields  = new HashSet<string>(found.Select(f => f.OneField.Name));
			HashSet<string> uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Count; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(k => foundFields.Contains(k)))
				{
					if (uniqueFields == null)
						uniqueFields = new HashSet<string>();

					foreach (var key in keys)
						uniqueFields.Add(key);
				}
			}

			if (uniqueFields != null)
			{
				foreach (var item in found)
					if (uniqueFields.Contains(item.OneField.Name))
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
					if (item.ManyField.CanBeNull)
					{
						var newField = MapToSource(fromTable, item.ManyField, fromTable.SourceID);
						AddSearchCondition(_selectQuery.Where.SearchCondition,
							new SqlCondition(false, new SqlPredicate.IsNull(newField.Element, true)));
					}

				// add mapping to new source
				ReplaceSource(fromTable, join, fromTable);

				return true;
			}

			return false;
		}

		bool TryMergeJoins(SqlTableSource fromTable,
			SqlTableSource manySource,
			SqlJoinedTable join1, SqlJoinedTable join2,
			List<List<string>> uniqueKeys)
		{
			// do not allow merging if table used in statement
			if (join2.Table.Source is SqlTable t && _statement.IsDependedOn(t))
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

			List<FoundEquality> found = null;

			for (var i1 = 0; i1 < found1.Count; i1++)
			{
				var f1 = found1[i1];

				for (var i2 = 0; i2 < found2.Count; i2++)
				{
					var f2 = found2[i2];

					if (f1.ManyField.Name == f2.ManyField.Name && f1.OneField.Name == f2.OneField.Name)
					{
						if (found == null)
							found = new List<FoundEquality>();

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

			HashSet<string> foundFields  = new HashSet<string>(found.Select(f => f.OneField.Name));
			HashSet<string> uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Count; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(k => foundFields.Contains(k)))
				{
					if (uniqueFields == null)
						uniqueFields = new HashSet<string>();

					foreach (var key in keys)
						uniqueFields.Add(key);
				}
			}

			if (uniqueFields != null)
			{
				foreach (var item in found)
					if (uniqueFields.Contains(item.OneField.Name))
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

		// here we can deal with LEFT JOIN and INNER JOIN
		bool TryToRemoveIndependent(
			SqlTableSource fromTable, SqlTableSource manySource, SqlJoinedTable join, List<List<string>> uniqueKeys)
		{
			if (join.JoinType == JoinType.Inner)
				return false;

			if (join.Table.Source is SqlTable table)
			{
				// do not allow to remove JOIN if table used in statement
				if (_statement.IsDependedOn(table))
					return false;
			}

			var found = SearchForFields(manySource, join);

			if (found == null)
				return false;

			HashSet<string> foundFields  = new HashSet<string>(found.Select(f => f.OneField.Name));
			HashSet<string> uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Count; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(k => foundFields.Contains(k)))
				{
					if (uniqueFields == null)
						uniqueFields = new HashSet<string>();
					foreach (var key in keys)
						uniqueFields.Add(key);
				}
			}

			if (uniqueFields != null)
			{
				if (join.JoinType == JoinType.Inner)
				{
					foreach (var item in found)
						if (uniqueFields.Contains(item.OneField.Name))
						{
							// remove from second
							join.Condition.Conditions.Remove(item.OneCondition);
							AddEqualFields(item.ManyField, item.OneField, fromTable.SourceID);
						}

					// move rest conditions to Where
					if (join.Condition.Conditions.Count > 0)
					{
						AddSearchConditions(_selectQuery.Where.SearchCondition, join.Condition.Conditions);
						join.Condition.Conditions.Clear();
					}

					// add filer for nullable fileds because after INNER JOIN records with nulls disappear
					foreach (var item in found)
						if (item.ManyField.CanBeNull)
							AddSearchCondition(_selectQuery.Where.SearchCondition,
								new SqlCondition(false, new SqlPredicate.IsNull(item.ManyField.Element, true)));
				}

				RemoveSource(fromTable, join);

				return true;
			}

			return false;
		}

		[DebuggerDisplay("{ManyField.DisplayString()} -> {OneField.DisplayString()}")]
		class FoundEquality
		{
			public VirtualField ManyField;
			public SqlCondition OneCondition;
			public VirtualField OneField;
		}

		[DebuggerDisplay("{DisplayString()}")]
		class VirtualField
		{
			public VirtualField([NotNull] SqlField field)
			{
				Field = field ?? throw new ArgumentNullException(nameof(field));
			}

			public VirtualField([NotNull] SqlColumn column)
			{
				Column = column ?? throw new ArgumentNullException(nameof(column));
			}

			public SqlField  Field  { get; }
			public SqlColumn Column { get; }

			public string Name      => Field == null ? Column.Alias : Field.Name;
			public int    SourceID  => Field == null ? Column.Parent.SourceID : Field.Table?.SourceID ?? -1;
			public bool   CanBeNull => Field?.CanBeNull ?? Column.CanBeNull;

			public ISqlExpression Element
			{
				get
				{
					if (Field != null)
						return Field;

					return Column;
				}
			}

			protected bool Equals(VirtualField other)
			{
				return Equals(Field, other.Field) && Equals(Column, other.Column);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;

				if (ReferenceEquals(this, obj)) return true;

				if (obj.GetType() != GetType()) return false;

				return Equals((VirtualField) obj);
			}

			string GetSourceString(ISqlTableSource source)
			{
				if (source is SqlTable table)
				{
					var res = $"({source.SourceID}).{table.Name}";
					if (table.Alias != table.Name && !string.IsNullOrEmpty(table.Alias))
						res = res + "(" + table.Alias + ")";

					return res;
				}

				return $"({source.SourceID}).{source}";
			}

			public string DisplayString()
			{
				if (Field != null)
					return $"F: '{GetSourceString(Field.Table)}.{Name}'";

				return $"C: '{GetSourceString(Column.Parent)}.{Name}'";
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return ((Field != null ? Field.GetHashCode() : 0) * 397) ^ (Column != null ? Column.GetHashCode() : 0);
				}
			}
		}
	}
}
