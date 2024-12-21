using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	sealed partial class JoinsOptimizer
	{
		Dictionary<IQueryElement,IQueryElement>?                                 _replaceMap;
		HashSet<int>?                                                            _removedSources;
		Dictionary<ISqlExpression,HashSet<(int sourceID,ISqlExpression field)>>? _equalityMap;
		Dictionary<SqlSearchCondition,SqlSearchCondition>?                       _additionalFilter;

		SqlStatement RemoveDuplicateJoins(SqlStatement statement)
		{
			statement.Visit((this_: this, statement), static (ctx, e) =>
			{
				if (e is SelectQuery selectQuery)
				{
					var nullability = new NullabilityContext(selectQuery);

					foreach (var source in selectQuery.From.Tables)
					{
						for (var i = 0; i < source.Joins.Count; i++)
						{
							var join = source.Joins[i];

							if (join.JoinType == JoinType.Inner)
								ctx.this_.CollectEqualFields(join);

							if (ctx.this_.TryMergeSources(source, join, out var keys) && ctx.this_.TryMergeSources2(ctx.statement, selectQuery, nullability, source, null, null, join, keys))
							{
								source.Joins.RemoveAt(i);
								--i;
								continue;
							}

							for (var i2 = i + 1; i2 < source.Joins.Count; i2++)
							{
								var join2 = source.Joins[i2];

								if (!ctx.this_.TryMergeSources(join.Table, join2, out var keys2))
									continue;

								var merged = ctx.this_.TryMergeSources2(ctx.statement, selectQuery, nullability, source, source, join, join2, keys2);

								if (!merged)
								{
									for (var im = 0; im < i2; im++)
									{
										var join3 = source.Joins[im];
										if (join3.JoinType is JoinType.Inner or JoinType.Left)
										{
											merged = ctx.this_.TryMergeSources2(ctx.statement, selectQuery, nullability, source, join3.Table, join, join2, keys2);
											if (merged)
												break;
										}
									}
								}

								if (merged)
								{
									source.Joins.RemoveAt(i2);
									--i2;
								}
							}
						}
					}
				}
			});

			if (_replaceMap != null)
			{
				statement.Replace(_replaceMap);
			}

			return statement;
		}

		bool TryMergeSources(SqlTableSource leftSource, SqlJoinedTable rightJoin, [NotNullWhen(true)] out ISqlExpression[][]? keys)
		{
			keys = null;

			// join type not supported by optimization
			if (rightJoin.JoinType is not JoinType.Inner and not JoinType.Left)
				return false;

			// both sources should use same table for removal
			if (!QueryHelper.IsEqualTables(leftSource.Source as SqlTable, rightJoin.Table.Source as SqlTable))
				return false;

			keys = GetTableKeys(rightJoin.Table);

			return keys != null;
		}

		bool TryMergeSources2(SqlStatement statement, SelectQuery selectQuery, NullabilityContext nullability, SqlTableSource fromTable, SqlTableSource? manySource, SqlJoinedTable? join1, SqlJoinedTable join2, ISqlExpression[][] uniqueKeys)
		{
			if (join2.Table.Joins.Count != 0)
				return false;

			if (!(join2.Table.Source is SqlTable t && t.SqlTableType == SqlTableType.Table))
				return false;

			// TODO: do we have test that depend on it?
			// do not allow merging if table used in statement (currently applied to target table in UPDATE queries)
			if (statement.IsDependedOn(t))
				return false;

			List<EqualityFields> foundPairs;
			if (join1 != null)
			{
				var found1 = SearchForJoinEqualityFields(manySource, join1);
				if (found1 == null)
					return false;

				var found2 = SearchForJoinEqualityFields(manySource, join2)?.ToList();
				if (found2 == null)
					return false;

				var hasLeftJoin = join1.JoinType == JoinType.Left || join2.JoinType == JoinType.Left;

				// left join should match exactly
				if (hasLeftJoin)
				{
					if (join1.Condition.Predicates.Count != join2.Condition.Predicates.Count)
						return false;

					if (found1.Count != found2.Count)
						return false;

					if (join1.Table.Joins.Count != 0 || join2.Table.Joins.Count != 0)
						return false;
				}

				for (var i = 0; i < found2.Count;)
				{
					var f2 = found2[i];

					var found = false;
					for (var j = 0; !found && j < found1.Count; j++)
					{
						var f1 = found1[j];
						found = IsSimilarFields(f1.RightField, f2.RightField) && IsSimilarFields(f1.LeftField!, f2.LeftField!);
					}

					if (!found)
					{
						found2.RemoveAt(i);
						continue;
					}

					i++;
				}

				if (hasLeftJoin)
				{
					// for left join each expression should be used
					if (found2.Count != join1.Condition.Predicates.Count)
						return false;

					// currently no dependencies in search condition allowed for left join
					if (IsDepended(statement, join1, join2))
						return false;
				}

				foundPairs = found2;
			}
			else
			{
				var found = SearchForJoinEqualityFields(fromTable, join2)?.ToList();
				if (found == null)
					return false;

				// for removing join with same table fields should be equal
				for (var i = 0; i < found.Count;)
				{
					if (!IsSimilarFields(found[i].LeftField!, found[i].RightField))
					{
						found.RemoveAt(i);
						continue;
					}

					i++;
				}

				if (found.Count == 0)
					return false;

				if (join2.JoinType == JoinType.Left)
				{
					if (join2.Condition.Predicates.Count != found.Count)
						return false;

					// currently no dependencies in search condition allowed for left join
					if (IsDependedExcludeJoins(statement, selectQuery, join2))
						return false;
				}

				foundPairs = found;
			}

			var                    foundFields  = new HashSet<ISqlExpression>(foundPairs.Select(f => f.RightField));
			HashSet<ISqlExpression>? uniqueFields = null;

			for (var i = 0; i < uniqueKeys.Length; i++)
			{
				var keys = uniqueKeys[i];

				if (keys.All(foundFields.Contains))
					(uniqueFields ??= new()).AddRange(keys);
			}

			if (uniqueFields == null)
				return false;

			foreach (var item in foundPairs)
			{
				if (uniqueFields.Contains(item.RightField))
				{
					// remove unique key conditions
					join2.Condition.Predicates.Remove(item.Condition);
					AddEqualFields(item.LeftField!, item.RightField, fromTable.SourceID);
				}
			}

			if (join2.Condition.Predicates.Count > 0)
			{
				// move rest conditions to first join or Where section
				AddSearchConditions(
					join1 == null ? selectQuery.Where.SearchCondition : join1.Condition,
					join2.Condition.Predicates);

				join2.Condition.Predicates.Clear();
			}

			if (join1 != null)
				join1.Table.Joins.AddRange(join2.Table.Joins);
			else
			{
				// add check that previously joined fields is not null
				foreach (var item in foundPairs)
				{
					if (item.LeftField!.CanBeNullable(nullability))
					{
						var newField = MapToSource(fromTable, item.LeftField, fromTable.SourceID, null)
							?? throw new InvalidOperationException();
						AddSearchConditions(selectQuery.Where.EnsureConjunction(), new[] { new SqlPredicate.IsNull(newField, true) });
					}
				}
			}

			// add mapping to new source
			ReplaceSource(fromTable, join2, join1?.Table ?? fromTable);

			return true;

			bool IsDepended(SqlStatement statement, SqlJoinedTable join, SqlJoinedTable toIgnore)
			{
				var testedSources = new HashSet<int>(join.Table.GetTables().Select(t => t.SourceID));
				if (toIgnore != null)
				{
					foreach (var sourceId in toIgnore.Table.GetTables().Select(t => t.SourceID))
						testedSources.Add(sourceId);
				}

				var ctx = new IsDependedContext(testedSources);

				statement.VisitParentFirst(ctx, (context, e) =>
				{
					if (context.Dependent)
						return false;

					// ignore non searchable parts
					if (e.ElementType is QueryElementType.SelectClause
									  or QueryElementType.GroupByClause
									  or QueryElementType.OrderByClause)
						return false;

					if (e.ElementType == QueryElementType.JoinedTable)
						if (context.TestedSources.Contains(((SqlJoinedTable)e).Table.SourceID))
							return false;

					if (e is ISqlExpression expression)
					{
						var field = GetUnderlyingFieldOrColumn(expression);
						if (field != null)
						{
							var newField = GetNewField(field);
							var local = context.TestedSources.Contains(GetFieldSourceID(newField));
							if (local)
								context.Dependent = !CanWeReplaceField(null, newField, context.TestedSources, -1);
						}
					}

					return !context.Dependent;
				});

				return ctx.Dependent;
			}
		}

		bool IsDependedExcludeJoins(SqlStatement statement, SelectQuery selectQuery, SqlJoinedTable join)
		{
			var testedSources = new HashSet<int>(join.Table.GetTables().Select(t => t.SourceID));
			return IsDependedExcludeJoins(statement, selectQuery, testedSources);
		}

		bool IsDependedExcludeJoins(SqlStatement statement, SelectQuery selectQuery, HashSet<int> testedSources)
		{
			bool CheckDependency(IsDependedExcludeJoinsContext context, IQueryElement e)
			{
				if (context.Dependent)
					return false;

				if (e.ElementType == QueryElementType.JoinedTable)
					return false;

				if (e is ISqlExpression expression)
				{
					var field = GetUnderlyingFieldOrColumn(expression);

					if (field != null)
					{
						var newField = GetNewField(field);
						var local = context.TestedSources.Contains(GetFieldSourceID(newField));
						if (local)
							context.Dependent = !CanWeReplaceField(null, newField, context.TestedSources, -1);
					}
				}

				return !context.Dependent;
			}

			var ctx = new IsDependedExcludeJoinsContext(testedSources);

			//TODO: review dependency checking
			selectQuery.VisitParentFirst(ctx, CheckDependency);
			if (!ctx.Dependent)
				statement.VisitParentFirst(ctx, CheckDependency);

			return ctx.Dependent;
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

		void AddSearchConditions(SqlSearchCondition search, IEnumerable<ISqlPredicate> predicates)
		{
			_additionalFilter ??= new ();

			if (!_additionalFilter.TryGetValue(search, out var value))
			{
				value = search;
				_additionalFilter.Add(search, search);
			}

			value.Predicates.AddRange(predicates);
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

		bool CanWeReplaceField(SqlTableSource? table, ISqlExpression field, HashSet<int> excludeSourceId, int testedSourceId)
		{
			var visited = new HashSet<ISqlExpression>();

			return CanWeReplaceFieldInternal(table, field, excludeSourceId, GetSourceIndex(table, testedSourceId), visited);

			bool CanWeReplaceFieldInternal(
				SqlTableSource? table, ISqlExpression field, HashSet<int> excludeSourceIds, int testedSourceIndex, HashSet<ISqlExpression> visited)
			{
				if (visited.Contains(field))
					return false;

				var sourceId = GetFieldSourceID(field);
				if (!excludeSourceIds.Contains(sourceId) && !IsSourceRemoved(sourceId))
					return true;

				visited.Add(field);

				if (_equalityMap == null)
					return false;

				if (testedSourceIndex < 0)
					return false;

				if (_equalityMap.TryGetValue(field, out var sameFields))
				{
					foreach (var pair in sameFields)
					{
						if ((testedSourceIndex == 0 || GetSourceIndex(table, pair.sourceID) > testedSourceIndex)
							&& CanWeReplaceFieldInternal(table, pair.field, excludeSourceIds, testedSourceIndex, visited))
							return true;
					}
				}

				return false;
			}

			bool IsSourceRemoved(int sourceId)
			{
				return _removedSources != null && _removedSources.Contains(sourceId);
			}
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

		static Dictionary<string, ISqlExpression> GetFields(ISqlTableSource source)
		{
			var res = new Dictionary<string, ISqlExpression>();

			if (source is SqlTable table)
			{
				foreach (var field in table.Fields)
					res.Add(field.Name, field);

				res.Add(source.All.Name, source.All);
			}

			return res;
		}

		void RemoveSource(SqlTableSource fromTable, SqlJoinedTable join)
		{
			_removedSources ??= new HashSet<int>();

			_removedSources.Add(join.Table.SourceID);

			if (_equalityMap != null)
			{
				var keys = _equalityMap.Keys.Where(k => GetFieldSourceID(k) == join.Table.SourceID).ToArray();

				foreach (var key in keys)
				{
					var newField = MapToSource(fromTable, key, fromTable.SourceID, null);

					if (newField != null)
						ReplaceField(key, newField);

					_equalityMap.Remove(key);
				}
			}

			//TODO: investigate another ways when we can propagate keys up
			if (join.JoinType == JoinType.Inner && join.Table.HasUniqueKeys)
			{
				var newFields = join.Table.UniqueKeys.Select(uk => uk.Select(k => GetNewField(k)).ToArray());
				fromTable.UniqueKeys.AddRange(newFields);
			}

			ResetFieldSearchCache(join.Table);
		}

		ISqlExpression? MapToSource(SqlTableSource table, ISqlExpression field, int targetSourceID, int? ignoreSourceID)
		{
			var visited = new HashSet<ISqlExpression>();

			return MapToSourceInternal(table, field, targetSourceID, visited);

			ISqlExpression? MapToSourceInternal(SqlTableSource fromTable, ISqlExpression field, int sourceId, HashSet<ISqlExpression> visited)
			{
				if (visited.Contains(field))
					return null;

				if (GetFieldSourceID(field) == sourceId)
					return field;

				visited.Add(field);

				if (_equalityMap == null)
					return null;

				var sourceIndex = GetSourceIndex(fromTable, sourceId);

				if (_equalityMap.TryGetValue(field, out var sameFields))
				{
					foreach (var pair in sameFields)
					{
						if (ignoreSourceID != null && ignoreSourceID == pair.sourceID)
							continue;

						var itemIndex = GetSourceIndex(fromTable, pair.sourceID);

						if (itemIndex >= 0 && (sourceIndex == 0 || itemIndex < sourceIndex))
						{
							var newField = MapToSourceInternal(fromTable, pair.field, sourceId, visited);

							if (newField != null)
								return newField;
						}
					}
				}

				return null;
			}
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

		void ResetFieldSearchCache(SqlTableSource table)
		{
			if (_equalityPairsCache == null)
				return;

			var keys = _equalityPairsCache.Keys.Where(k => k.left == table || k.right == table).ToArray();

			foreach (var key in keys)
			{
				_equalityPairsCache.Remove(key);
			}
		}

		void ReplaceField(ISqlExpression oldField, ISqlExpression newField)
		{
			_replaceMap ??= new();

			_replaceMap.Remove(oldField);
			_replaceMap.Add(oldField, newField);
		}

		ISqlExpression GetNewField(ISqlExpression field)
		{
			if (_replaceMap == null)
				return field;

			if (_replaceMap.TryGetValue(field, out var newField))
			{
				while (_replaceMap.TryGetValue(newField, out var fieldOther))
					newField = fieldOther;
			}
			else
				newField = field;

			return (ISqlExpression)newField;
		}

		static bool IsSimilarFields(ISqlExpression field1, ISqlExpression field2)
		{
			if (field1 is SqlField sqlField1)
			{
				if (field2 is SqlField sqlField2)
					return sqlField1.PhysicalName == sqlField2.PhysicalName;

				return false;
			}

			return ReferenceEquals(field1, field2);
		}

		void CollectEqualFields(SqlJoinedTable join)
		{
			if (join.Condition.IsOr)
				return;

			for (var i1 = 0; i1 < join.Condition.Predicates.Count; i1++)
			{
				var c = join.Condition.Predicates[i1];

				if (c is not SqlPredicate.ExprExpr { Operator: SqlPredicate.Operator.Equal } predicate)
					continue;

				var field1 = GetUnderlyingFieldOrColumn(predicate.Expr1);

				if (field1 == null)
					continue;

				var field2 = GetUnderlyingFieldOrColumn(predicate.Expr2);

				if (field2 == null)
					continue;

				if (field1.Equals(field2))
					continue;

				AddEqualFields(field1, field2, join.Table.SourceID);
				AddEqualFields(field2, field1, join.Table.SourceID);
			}
		}

		void AddEqualFields(ISqlExpression field1, ISqlExpression field2, int levelSourceId)
		{
			if (_equalityMap?.TryGetValue(field1, out var set) != true)
				(_equalityMap ??= new()).Add(field1, set = new());

			set!.Add((levelSourceId, field2));
		}
	}
}
