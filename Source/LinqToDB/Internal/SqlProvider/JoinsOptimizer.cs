using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlProvider
{
	sealed partial class JoinsOptimizer
	{
		Dictionary<int,ISqlExpression[][]?>?                                                    _tableKeysCache;
		Dictionary<(SqlTableSource? left,SqlTableSource right),IReadOnlyList<EqualityFields>?>? _equalityPairsCache;

		public SqlStatement Optimize(SqlStatement statement, EvaluationContext evaluationContext)
		{
			RemoveUnusedLeftJoins(statement, evaluationContext);

			statement = RemoveDuplicateJoins(statement);

			return statement;
		}

		/// <summary>
		/// Removes left joins if they doesn't change query cardinality and joined tables not used in query.
		/// </summary>
		void RemoveUnusedLeftJoins(SqlStatement statement, EvaluationContext evaluationContext)
		{
			statement.Visit((this_: this, statement, evaluationContext), static (ctx, e) =>
			{
				if (e is SqlTableSource source)
				{
					// detect unused LEFT joins that doesn't change query cardinality and remove them
					for (var index = source.Joins.Count - 1; index >= 0; index--)
					{
						if (ctx.this_.CanRemoveLeftJoin(ctx.statement, source.Joins[index], ctx.evaluationContext))
							source.Joins.RemoveAt(index);
					}
				}
			});
		}

		/// <summary>
		/// Moves nested joins to upper level when outer join type is compatible with first nested join type.
		/// </summary>
		public static void UnnestJoins(IQueryElement statement)
		{
			statement.Visit(static e =>
			{
				if (e is SqlTableSource source)
				{
					for (var i = 0; i < source.Joins.Count; i++)
					{
						var insertIndex = i + 1;
						var parent      = source.Joins[i];

						// INNER/LEFT join with nested joins
						if (parent.Table.Joins.Count > 0 && parent.JoinType is JoinType.Inner or JoinType.Left)
						{
							var child = parent.Table.Joins[0];

							// check compatibility of outer join with first nested join:
							// INNER + INNER/LEFT/CROSS APPLY/OUTER APPLY
							// LEFT + LEFT
							if ((parent.JoinType == JoinType.Inner && (child.JoinType is JoinType.Inner or JoinType.Left or JoinType.CrossApply or JoinType.OuterApply)) ||
								(parent.JoinType == JoinType.Left && child.JoinType == JoinType.Left))
							{
								// check that join condition doesn't reference child tables
								var sources = new HashSet<int>(parent.Table.Joins.SelectMany(j => j.Table.GetTables().Select(t => t.SourceID)));
								var found = parent.Condition.Find(sources, static (sources, e) =>
								{
									if (e is ISqlExpression expr
										&& GetUnderlyingFieldOrColumn(expr) is ISqlExpression field
										&& sources.Contains(GetFieldSourceID(field)))
									{
										return true;
									}

									return false;
								});

								if (found != null)
									continue;

								// move all nested joins up
								source.Joins.InsertRange(insertIndex, parent.Table.Joins);
								parent.Table.Joins.Clear();
							}
						}
					}
				}
			});
		}

		public static void UndoNestedJoins(IQueryElement statement)
		{
			var correct = false;
			statement.Visit(e =>
			{
				if (e is SqlTableSource source)
				{
					for (var i = 0; i < source.Joins.Count; i++)
					{
						var join = source.Joins[i];

						if (join.Table.Joins.Count > 0)
						{
							var subQueryTableSource = new SqlTableSource(
								join.Table.Source,
								join.Table.Alias,
								join.Table.Joins,
								join.Table.HasUniqueKeys ? join.Table.UniqueKeys : null);

							var subQuery = new SelectQuery();
							subQuery.From.Tables.Add(subQueryTableSource);

							join.Table.Source = subQuery;
							join.Table.Alias = null;
							join.Table.Joins.Clear();
							if (join.Table.HasUniqueKeys)
								join.Table.UniqueKeys.Clear();

							correct = true;
						}
					}
				}
			});

			if (correct)
			{
				var corrector = new SqlQueryColumnNestingCorrector();
				corrector.CorrectColumnNesting(statement);
			}
		}

		#region Helpers
		bool CanRemoveLeftJoin(SqlStatement statement, SqlJoinedTable join, EvaluationContext evaluationContext)
		{
			// left joins only
			if (join.JoinType is not JoinType.Left)
				return false;

			// has nested joins
			if (join.Table.Joins.Count > 0)
				return false;

			// we cannot make assumptions on non-standard joins
			if (join.SqlQueryExtensions?.Count > 0)
				return false;

			// some table extensions also could affect cardinality
			// https://github.com/linq2db/linq2db/pull/4016
			if (join.Table.Source is SqlTable { SqlQueryExtensions.Count: > 0 })
				return false;

			// check wether join used outside join itself
			if (null != statement.FindExcept(join.Table.SourceID, join, static (sourceID, e) =>
				(e is SqlField field && field.Table?.SourceID == sourceID) ||
				(e is SqlColumn column && column.Parent?.SourceID == sourceID)))
				return false;

			if (!IsLeftJoinCardinalityPreserved(join, evaluationContext))
				return false;

			return true;
		}

		bool IsLeftJoinCardinalityPreserved(SqlJoinedTable join, EvaluationContext evaluationContext)
		{
			// Check that join doesn't change rowcount and has 1-0/1 cardinality:
			// - 1-0 cardinality: condition is false constant
			// - 1-1 cardinality: join made by unique key fields (optionally could have extra AND filters)

			// TODO: this currently doesn't work for cases where nullability makes condition false (e.g. "non_nullable_field == null")
			if (join.Condition.TryEvaluateExpression(evaluationContext, out var value) && value is false)
				return true;

			// get fields, used in join condition
			var found = SearchForJoinEqualityFields(null, join);

			// not joined by left table fields
			if (found == null)
				return false;

			// collect unique keys for table
			var uniqueKeys = GetTableKeys(join.Table);
			if (uniqueKeys == null)
				return false;

			var foundFields = new HashSet<ISqlExpression>(found.Select(static f => f.RightField));

			// check if any of keysets used for join
			for (var i = 0; i < uniqueKeys.Length; i++)
				if (uniqueKeys[i].All(foundFields.Contains))
					return true;

			var unwrapped = new HashSet<ISqlExpression>(foundFields.Select(f => f is SqlColumn c ? c.Expression : f));

			for (var i = 0; i < uniqueKeys.Length; i++)
				if (uniqueKeys[i].All(unwrapped.Contains))
					return true;

			return false;
		}

		sealed record EqualityFields(ISqlPredicate Condition, ISqlExpression? LeftField, ISqlExpression RightField);

		/// <summary>
		/// Inspect join condition and return list of field pairs used in equals conditions between <paramref name="leftSource"/> (when specified) and <paramref name="rightJoin"/> tables.
		/// If condition contains top-level OR operator, method returns <c>null</c>.
		/// </summary>
		IReadOnlyList<EqualityFields>? SearchForJoinEqualityFields(SqlTableSource? leftSource, SqlJoinedTable rightJoin)
		{
			var key = (leftSource, rightJoin.Table);

			if (_equalityPairsCache?.TryGetValue(key, out var found) != true)
			{
				List<EqualityFields>? pairs = null;

				if (!rightJoin.Condition.IsOr)
				{
					for (var i1 = 0; i1 < rightJoin.Condition.Predicates.Count; i1++)
					{
						var p = rightJoin.Condition.Predicates[i1];

						// ignore all predicates except "x == y"
						if (p is not SqlPredicate.ExprExpr exprExpr || exprExpr.Operator != SqlPredicate.Operator.Equal)
							continue;

						// try to extract joined tables fields from predicate
						var field1 = GetUnderlyingFieldOrColumn(exprExpr.Expr1);
						var field2 = GetUnderlyingFieldOrColumn(exprExpr.Expr2);

						ISqlExpression? leftField  = null;
						ISqlExpression? rightField = null;

						if (field1 != null)
							DetectField(leftSource, rightJoin.Table, GetNewField(field1), ref leftField, ref rightField);

						if (field2 != null)
							DetectField(leftSource, rightJoin.Table, GetNewField(field2), ref leftField, ref rightField);

						if (rightField != null && (leftSource == null || leftField != null))
							(pairs ??= new()).Add(new(p, leftField, rightField));
					}
				}

				(_equalityPairsCache ??= new()).Add(key, found = pairs);
			}
			
			return found;

			void DetectField(SqlTableSource? leftSource, SqlTableSource rightSource, ISqlExpression field, ref ISqlExpression? leftField, ref ISqlExpression? rightField)
			{
				var sourceID = GetFieldSourceID(field);

				if (rightSource.Source.SourceID == sourceID)
					rightField = field;
				else if (rightSource.Source is SelectQuery select && select.Select.From.Tables.Count == 1 && select.Select.From.Tables[0].SourceID == sourceID)
					rightField = field;
				else if (leftSource?.Source.SourceID == sourceID)
					leftField = field;
				else if (leftSource != null)
					leftField = MapToSource(leftSource, field, leftSource.Source.SourceID, rightSource.SourceID);
			}
		}

		static int GetFieldSourceID(ISqlExpression field)
		{
			return field switch
			{
				SqlField  sqlField  => sqlField .Table? .SourceID,
				SqlColumn sqlColumn => sqlColumn.Parent?.SourceID,
				_ => null
			} ?? -1;
		}

		/// <summary>
		/// Returns table unique keysets or null of no unique keys found.
		/// </summary>
		ISqlExpression[][]? GetTableKeys(SqlTableSource tableSource)
		{
			if (_tableKeysCache?.TryGetValue(tableSource.SourceID, out var keys) != true)
				(_tableKeysCache ??= new()).Add(tableSource.SourceID, keys = GetTableKeysInternal(tableSource));

			return keys;

			static ISqlExpression[][]? GetTableKeysInternal(SqlTableSource tableSource)
			{
				var knownKeys = new List<IList<ISqlExpression>>();

				QueryHelper.CollectUniqueKeys(tableSource, knownKeys);

				if (knownKeys.Count == 0)
					return null;

				// unwrap keyset expressions as field/column
				var result = new ISqlExpression[knownKeys.Count][];

				for (var i = 0; i < knownKeys.Count; i++)
				{
					var keyset = knownKeys[i];
					var fields = result[i] = new ISqlExpression[keyset.Count];

					for (var j = 0; j < keyset.Count; j++)
						fields[j] = GetUnderlyingFieldOrColumn(keyset[j]) ?? throw new InvalidOperationException($"Cannot get field for {keyset[j]}");
				}

				return result;
			}
		}

		/// <summary>
		/// Reduce <paramref name="expr"/> to <see cref="SqlField"/> or <see cref="SqlColumn"/> if possible.
		/// </summary>
		static ISqlExpression? GetUnderlyingFieldOrColumn(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlExpression:
				{
					var sqlExpr = (SqlExpression)expr;
					if (sqlExpr.Expr == "{0}" && sqlExpr.Parameters.Length == 1)
						return GetUnderlyingFieldOrColumn(sqlExpr.Parameters[0]);
					return null;
				}

				case QueryElementType.SqlNullabilityExpression:
					return GetUnderlyingFieldOrColumn(((SqlNullabilityExpression)expr).SqlExpression);

				case QueryElementType.SqlField:
				case QueryElementType.Column:
					return expr;
			}

			return null;
		}
		#endregion
	}
}
