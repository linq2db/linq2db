using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;
	using SqlProvider;

	class SelectQueryOptimizer
	{
		public SelectQueryOptimizer(SqlProviderFlags flags, IQueryElement rootElement, SelectQuery selectQuery, int level, params IQueryElement[] dependencies)
		{
			_flags        = flags;
			_selectQuery  = selectQuery;
			_rootElement  = rootElement;
			_level        = level;
			_dependencies = dependencies;
		}

		readonly SqlProviderFlags _flags;
		readonly SelectQuery      _selectQuery;
		readonly IQueryElement    _rootElement;
		readonly int              _level;
		readonly IQueryElement[]  _dependencies;

		public void FinalizeAndValidate(bool isApplySupported, bool optimizeColumns, bool inlineParameters)
		{
#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var sqlText = _selectQuery.SqlText;

			var dic = new Dictionary<SelectQuery,SelectQuery>();

			new QueryVisitor().VisitAll(_selectQuery, e =>
			{
				if (e is SelectQuery sql)
				{
					if (dic.ContainsKey(sql))
						throw new InvalidOperationException("SqlQuery circle reference detected.");

					dic.Add(sql, sql);
				}
			});
#endif

			OptimizeUnions();
			FinalizeAndValidateInternal(isApplySupported, optimizeColumns, inlineParameters);
			ResolveFields();

#if DEBUG
			// ReSharper disable once RedundantAssignment
			var newSqlText = _selectQuery.SqlText;
#endif
		}

		class QueryData
		{
			public          SelectQuery          Query   = null!;
			public readonly List<ISqlExpression> Fields  = new List<ISqlExpression>();
			public readonly List<QueryData>      Queries = new List<QueryData>();
		}

		void ResolveFields()
		{
			var root = GetQueryData(_rootElement, _selectQuery);

			ResolveFields(root);
		}

		static QueryData GetQueryData(IQueryElement? root, SelectQuery selectQuery)
		{
			var data = new QueryData { Query = selectQuery };

			new QueryVisitor().VisitParentFirst(root ?? selectQuery, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField :
						{
							var field = (SqlField)e;

							if (field.Name.Length != 1 || field.Name[0] != '*')
								data.Fields.Add(field);

							break;
						}

					case QueryElementType.SqlQuery :
						{
							if (e != selectQuery)
							{
								data.Queries.Add(GetQueryData(null, (SelectQuery)e));
								return false;
							}

							break;
						}

					case QueryElementType.Column :
						return ((SqlColumn)e).Parent == selectQuery;

					case QueryElementType.SqlTable :
						return false;

					case QueryElementType.SqlCteTable :
						return false;
				}

				return true;
			});

			return data;
		}

		static SqlTableSource? FindField(SqlField field, SqlTableSource table)
		{
			if (field.Table == table.Source)
				return table;

			foreach (var join in table.Joins)
			{
				var t = FindField(field, join.Table);

				if (t != null)
					return join.Table;
			}

			return null;
		}

		static ISqlExpression? GetColumn(QueryData data, SqlField field)
		{
			foreach (var query in data.Queries)
			{
				var q = query.Query;

				foreach (var table in q.From.Tables)
				{
					var t = FindField(field, table);

					if (t != null)
					{
						var n   = q.Select.Columns.Count;
						var idx = q.Select.Add(field);

						if (n != q.Select.Columns.Count)
							if (!q.GroupBy.IsEmpty || q.Select.Columns.Any(c => QueryHelper.IsAggregationFunction(c.Expression)))
								q.GroupBy.Items.Add(field);

						return q.Select.Columns[idx];
					}
				}
			}

			return null;
		}

		static void ResolveFields(QueryData data)
		{
			if (data.Queries.Count == 0)
				return;

			var dic = new Dictionary<ISqlExpression,ISqlExpression>();

			foreach (var sqlExpression in data.Fields)
			{
				var field = (SqlField)sqlExpression;

				if (dic.ContainsKey(field))
					continue;

				var found = false;

				foreach (var table in data.Query.From.Tables)
				{
					found = FindField(field, table) != null;

					if (found)
						break;
				}

				if (!found)
				{
					var expr = GetColumn(data, field);

					if (expr != null)
						dic.Add(field, expr);
				}
			}

			if (dic.Count > 0)
				new QueryVisitor().VisitParentFirst(data.Query, e =>
				{
					ISqlExpression? ex;

					switch (e.ElementType)
					{
						case QueryElementType.SqlQuery :
							return e == data.Query;

						case QueryElementType.SqlFunction :
							{
								var parms = ((SqlFunction)e).Parameters;

								for (var i = 0; i < parms.Length; i++)
									if (dic.TryGetValue(parms[i], out ex))
										parms[i] = ex;

								break;
							}

						case QueryElementType.SqlExpression :
							{
								var parms = ((SqlExpression)e).Parameters;

								for (var i = 0; i < parms.Length; i++)
									if (dic.TryGetValue(parms[i], out ex))
										parms[i] = ex;

								break;
							}

						case QueryElementType.SqlBinaryExpression :
							{
								var expr = (SqlBinaryExpression)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								break;
							}

						case QueryElementType.ExprPredicate       :
						case QueryElementType.NotExprPredicate    :
						case QueryElementType.IsNullPredicate     :
						case QueryElementType.InSubQueryPredicate :
							{
								var expr = (SqlPredicate.Expr)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								break;
							}

						case QueryElementType.ExprExprPredicate :
							{
								var expr = (SqlPredicate.ExprExpr)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								break;
							}

						case QueryElementType.IsTruePredicate :
							{
								var expr = (SqlPredicate.IsTrue)e;
								if (dic.TryGetValue(expr.Expr1,      out ex)) expr.Expr1      = ex;
								if (dic.TryGetValue(expr.TrueValue,  out ex)) expr.TrueValue  = ex;
								if (dic.TryGetValue(expr.FalseValue, out ex)) expr.FalseValue = ex;
								break;
							}
						
						case QueryElementType.LikePredicate :
							{
								var expr = (SqlPredicate.Like)e;
								if (                       dic.TryGetValue(expr.Expr1,  out ex)) expr.Expr1  = ex;
								if (                       dic.TryGetValue(expr.Expr2,  out ex)) expr.Expr2  = ex;
								if (expr.Escape != null && dic.TryGetValue(expr.Escape, out ex)) expr.Escape = ex;
								break;
							}

						case QueryElementType.BetweenPredicate :
							{
								var expr = (SqlPredicate.Between)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								if (dic.TryGetValue(expr.Expr3, out ex)) expr.Expr3 = ex;
								break;
							}

						case QueryElementType.InListPredicate :
							{
								var expr = (SqlPredicate.InList)e;

								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;

								for (var i = 0; i < expr.Values.Count; i++)
									if (dic.TryGetValue(expr.Values[i], out ex))
										expr.Values[i] = ex;

								break;
							}

						case QueryElementType.Column :
							{
								var expr = (SqlColumn)e;

								if (expr.Parent != data.Query)
									return false;

								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;

								break;
							}

						case QueryElementType.SetExpression :
							{
								var expr = (SqlSetExpression)e;
								if (dic.TryGetValue(expr.Expression!, out ex)) expr.Expression = ex;
								break;
							}

						case QueryElementType.GroupByClause :
							{
								var expr = (SqlGroupByClause)e;

								for (var i = 0; i < expr.Items.Count; i++)
									if (dic.TryGetValue(expr.Items[i], out ex))
										expr.Items[i] = ex;

								break;
							}

						case QueryElementType.OrderByItem :
							{
								var expr = (SqlOrderByItem)e;
								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;
								break;
							}
					}

					return true;
				});

			foreach (var query in data.Queries)
				if (query.Queries.Count > 0)
					ResolveFields(query);
		}

		void OptimizeUnions()
		{
			var isAllUnion = new QueryVisitor().Find(_selectQuery,
				ne => ne is SqlSetOperator nu && nu.Operation == SetOperation.UnionAll);

			var isNotAllUnion = new QueryVisitor().Find(_selectQuery,
				ne => ne is SqlSetOperator nu && nu.Operation != SetOperation.UnionAll);

			if (isNotAllUnion != null && isAllUnion != null)
				return;

			var exprs = new Dictionary<ISqlExpression,ISqlExpression>();

			new QueryVisitor().Visit(_selectQuery, e =>
			{
				if (!(e is SelectQuery sql) || sql.From.Tables.Count != 1 || !sql.IsSimple)
					return;

				var table = sql.From.Tables[0];

				if (table.Joins.Count != 0 || !(table.Source is SelectQuery))
					return;

				var union = (SelectQuery)table.Source;

				if (!union.HasSetOperators || sql.Select.Columns.Count != union.Select.Columns.Count)
					return;

				for (var i = 0; i < sql.Select.Columns.Count; i++)
				{
					var scol = sql.  Select.Columns[i];
					var ucol = union.Select.Columns[i];

					if (scol.Expression != ucol)
						return;
				}

				exprs.Add(union, sql);

				for (var i = 0; i < sql.Select.Columns.Count; i++)
				{
					var scol = sql.  Select.Columns[i];
					var ucol = union.Select.Columns[i];

					scol.Expression = ucol.Expression;
					scol.RawAlias   = ucol.RawAlias;

					if (!exprs.ContainsKey(ucol))
					exprs.Add(ucol, scol);
				}

				for (var i = sql.Select.Columns.Count; i < union.Select.Columns.Count; i++)
					sql.Select.ExprNew(union.Select.Columns[i].Expression);

				sql.From.Tables.Clear();
				sql.From.Tables.AddRange(union.From.Tables);

				sql.Where.  SearchCondition.Conditions.AddRange(union.Where. SearchCondition.Conditions);
				sql.Having. SearchCondition.Conditions.AddRange(union.Having.SearchCondition.Conditions);
				sql.GroupBy.Items.                     AddRange(union.GroupBy.Items);
				sql.OrderBy.Items.                     AddRange(union.OrderBy.Items);
				sql.SetOperators.InsertRange(0, union.SetOperators);
			});

			if (exprs.Count > 0)
			{
				_selectQuery.Walk(
					new WalkOptions { ProcessParent = true },
					expr => exprs.TryGetValue(expr, out var e) ? e : expr);
			}
		}

		void FinalizeAndValidateInternal(bool isApplySupported, bool optimizeColumns, bool inlineParameters)
		{
			new QueryVisitor().Visit(_selectQuery, e =>
			{
				if (e is SelectQuery sql && sql != _selectQuery)
				{
					sql.ParentSelect = _selectQuery;
					new SelectQueryOptimizer(_flags, _rootElement, sql, _level + 1, _dependencies)
						.FinalizeAndValidateInternal(isApplySupported, optimizeColumns, inlineParameters);

					if (sql.IsParameterDependent)
						_selectQuery.IsParameterDependent = true;
				}
			});

			ResolveWeakJoins();
			RemoveEmptyJoins();
			OptimizeColumns();
			OptimizeApplies   (isApplySupported, optimizeColumns);
			OptimizeSubQueries(isApplySupported, optimizeColumns);
			OptimizeApplies   (isApplySupported, optimizeColumns);

			OptimizeDistinct();
			OptimizeDistinctOrderBy();
			OptimizeSkipTake(inlineParameters);

			OptimizeSearchConditions();
		}

		private void OptimizeSkipTake(bool inlineParameters)
		{
			var visitor = new QueryVisitor();
			if (_selectQuery.Select.TakeValue != null)
			{
				var supportsParameter = _flags.GetAcceptsTakeAsParameterFlag(_selectQuery);

				if (!supportsParameter && !(_selectQuery.Select.TakeValue is SqlValue))
					_selectQuery.IsParameterDependent = true;
				else if (_selectQuery.Select.TakeValue is SqlBinaryExpression
					// TODO: is this check safe?
					|| _selectQuery.Select.TakeValue is SqlFunction)
				{
					if (visitor.Find(_selectQuery.Select.TakeValue, e => e is SqlParameter) != null)
						_selectQuery.IsParameterDependent = true;
					else
					{
						var value = _selectQuery.Select.TakeValue.EvaluateExpression()!;

						if (supportsParameter)
							_selectQuery.Select.Take(
								new SqlParameter(new DbDataType(value.GetType()), "take", value)
								{
									IsQueryParameter = !inlineParameters
								}, _selectQuery.Select.TakeHints
							);
						else
							_selectQuery.Select.Take(new SqlValue(value), _selectQuery.Select.TakeHints);
					}
				}
			}
			if (_selectQuery.Select.SkipValue != null)
			{
				var supportsParameter = _flags.AcceptsTakeAsParameter;

				if (!supportsParameter && !(_selectQuery.Select.SkipValue is SqlValue))
					_selectQuery.IsParameterDependent = true;
				else if (_selectQuery.Select.SkipValue is SqlBinaryExpression
					|| _selectQuery.Select.SkipValue is SqlFunction)
				{
					if (visitor.Find(_selectQuery.Select.SkipValue, e => e is SqlParameter) != null)
						_selectQuery.IsParameterDependent = true;
					else
					{
						var value = _selectQuery.Select.SkipValue.EvaluateExpression()!;

						if (supportsParameter)
							_selectQuery.Select.Skip(new SqlParameter(new DbDataType(value.GetType()), "skip", value)
								{ IsQueryParameter = !inlineParameters });
						else
							_selectQuery.Select.Skip(new SqlValue(value));
					}
				}
			}
		}

		private void OptimizeSearchConditions()
		{
			_selectQuery.Walk(new WalkOptions(), expr =>
			{
				if (expr is SqlSearchCondition cond)
					return OptimizeSearchCondition(cond);

				return expr;
			});
		}

		public static bool? GetBoolValue(ISqlExpression expression, bool withParameters)
		{
			if (expression.TryEvaluateExpression(withParameters, out var value))
			{
				if (value is bool b)
					return b;
			}
			else if (expression is SqlSearchCondition searchCondition)
			{
				if (searchCondition.Conditions.Count == 0)
					return true;
				if (searchCondition.Conditions.Count == 1)
				{
					var cond = searchCondition.Conditions[0];
					if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
					{
						var boolValue = GetBoolValue(((SqlPredicate.Expr)cond.Predicate).Expr1, withParameters);
						if (boolValue.HasValue)
							return cond.IsNot ? !boolValue : boolValue;
					}
				}
			}

			return null;
		}

		internal static SqlSearchCondition OptimizeSearchCondition(SqlSearchCondition inputCondition, bool withParameters = false)
		{
			var searchCondition = inputCondition;

			void ClearAll()
			{
				searchCondition = new SqlSearchCondition();
			}

			void EnsureCopy()
			{
				if (!ReferenceEquals(searchCondition, inputCondition))
					return;

				searchCondition = new SqlSearchCondition(inputCondition.Conditions.Select(c => new SqlCondition(c.IsNot, c.Predicate, c.IsOr)));
			}

			for (var i = 0; i < searchCondition.Conditions.Count; i++)
			{
				var cond = searchCondition.Conditions[i];
				var newCond = cond;
				if (cond.Predicate.ElementType == QueryElementType.ExprExprPredicate)
				{
					var exprExpr = (SqlPredicate.ExprExpr)cond.Predicate;

					if (cond.IsNot)
					{
						SqlPredicate.Operator op;

						switch (exprExpr.Operator)
						{
							case SqlPredicate.Operator.Equal          : op = SqlPredicate.Operator.NotEqual;       break;
							case SqlPredicate.Operator.NotEqual       : op = SqlPredicate.Operator.Equal;          break;
							case SqlPredicate.Operator.Greater        : op = SqlPredicate.Operator.LessOrEqual;    break;
							case SqlPredicate.Operator.NotLess        :
							case SqlPredicate.Operator.GreaterOrEqual : op = SqlPredicate.Operator.Less;           break;
							case SqlPredicate.Operator.Less           : op = SqlPredicate.Operator.GreaterOrEqual; break;
							case SqlPredicate.Operator.NotGreater     :
							case SqlPredicate.Operator.LessOrEqual    : op = SqlPredicate.Operator.Greater;        break;
							default: throw new InvalidOperationException();
						}

						exprExpr = new SqlPredicate.ExprExpr(exprExpr.Expr1, op, exprExpr.Expr2);
						newCond  = new SqlCondition(false, exprExpr, newCond.IsOr);
					}

					if ((exprExpr.Operator == SqlPredicate.Operator.Equal ||
					     exprExpr.Operator == SqlPredicate.Operator.NotEqual)
					    && exprExpr.Expr1 is SqlValue value1 && value1.Value != null 
					    && exprExpr.Expr2 is SqlValue value2 && value2.Value != null
					    && value1.GetType() == value2.GetType())
					{
						newCond = new SqlCondition(newCond.IsNot, new SqlPredicate.Expr(new SqlValue(
							(value1.Value.Equals(value2.Value) == (exprExpr.Operator == SqlPredicate.Operator.Equal)))), newCond.IsOr);
					}

					if ((exprExpr.Operator == SqlPredicate.Operator.Equal ||
					     exprExpr.Operator == SqlPredicate.Operator.NotEqual)
					    && exprExpr.Expr1 is SqlParameter p1 && !p1.CanBeNull
					    && exprExpr.Expr2 is SqlParameter p2 && Equals(p1, p2))
					{
						newCond = new SqlCondition(newCond.IsNot, new SqlPredicate.Expr(new SqlValue(true)), newCond.IsOr);
					}
				}

				if (newCond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SqlPredicate.Expr)newCond.Predicate;

					if (cond.IsNot && expr.Expr1 is SqlValue sqlValue && sqlValue.Value is bool b)
					{
						newCond = new SqlCondition(false, new SqlPredicate.Expr(new SqlValue(!b)), newCond.IsOr);
					}
				}
				else if (newCond.Predicate.ElementType == QueryElementType.IsTruePredicate)
				{
					//TODO: This everything is weird, predicates needs full refactoring
					var expr = (SqlPredicate.IsTrue)newCond.Predicate;

					if (expr.Expr1.ElementType == QueryElementType.SqlValue || withParameters && expr.Expr1.ElementType == QueryElementType.SqlParameter)
					{
						var value  = expr.Expr1.EvaluateExpression();
						var result = false;
						if (value == null)
						{
							if (expr.WithNull != true)
								result = false;
						}
						else
						{
							if (expr.IsNot)
								result = value.Equals(expr.FalseValue.EvaluateExpression());
							else
								result = value.Equals(expr.TrueValue.EvaluateExpression());
						}

						newCond = new SqlCondition(false, new SqlPredicate.Expr(new SqlValue(result)), newCond.IsOr);

					}
				}

				if (!ReferenceEquals(cond, newCond))
				{
					EnsureCopy();
					searchCondition.Conditions[i] = newCond;
					cond = newCond;
				}

				if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SqlPredicate.Expr)cond.Predicate;
					var boolValue = GetBoolValue(expr.Expr1, withParameters);

					if (boolValue != null)
					{
						var isTrue = cond.IsNot ? !boolValue.Value : boolValue.Value;
						bool? leftIsOr  = i > 0 ? searchCondition.Conditions[i - 1].IsOr : (bool?)null;
						bool? rightIsOr = i + 1 < searchCondition.Conditions.Count ? cond.IsOr : (bool?)null;

						if (isTrue)
						{
							if ((leftIsOr == true || leftIsOr == null) && (rightIsOr == true || rightIsOr == null))
							{
								ClearAll();
								break;
							}

							EnsureCopy();
							searchCondition.Conditions.RemoveAt(i);
							if (leftIsOr== false && rightIsOr != null)
								searchCondition.Conditions[i - 1].IsOr = rightIsOr.Value;
							--i;
						}
						else
						{
							if (leftIsOr == false)
							{
								EnsureCopy();
								searchCondition.Conditions.RemoveAt(i - 1);
								--i;
							}
							else if (rightIsOr == false)
							{
								EnsureCopy();
								searchCondition.Conditions[i].IsOr = searchCondition.Conditions[i + 1].IsOr;
								searchCondition.Conditions.RemoveAt(i + 1);
								--i;
							}
							else
							{
								if (rightIsOr != null || leftIsOr != null)
								{
									EnsureCopy();
									searchCondition.Conditions.RemoveAt(i);
									if (leftIsOr != null && rightIsOr != null)
										searchCondition.Conditions[i - 1].IsOr = rightIsOr.Value;
									--i;
								}
							}
						}

					}
				}
				else if (cond.Predicate is SqlSearchCondition sc)
				{
					var newSc = OptimizeSearchCondition(sc);
					if (!ReferenceEquals(newSc, sc))
					{
						EnsureCopy();
						searchCondition.Conditions[i] = new SqlCondition(cond.IsNot, newSc, cond.IsOr);
						sc = newSc;
					}

					if (sc.Conditions.Count == 0)
					{
						EnsureCopy();
						var inlinePredicate = new SqlPredicate.Expr(new SqlValue(!cond.IsNot));
						searchCondition.Conditions[i] =
							new SqlCondition(false, inlinePredicate, searchCondition.Conditions[i].IsOr);
						--i;
					}
					else if (sc.Conditions.Count == 1)
					{
						// reduce nesting
						EnsureCopy();

						var isNot = searchCondition.Conditions[i].IsNot;
						if (sc.Conditions[0].IsNot)
							isNot = !isNot;

						var inlineCondition = new SqlCondition(isNot, sc.Conditions[0].Predicate, searchCondition.Conditions[i].IsOr);

						searchCondition.Conditions[i] = inlineCondition;

						--i;
					}
				}
			}

			return searchCondition;
		}

		internal void ResolveWeakJoins()
		{
			_selectQuery.ForEachTable(table =>
			{
				for (var i = table.Joins.Count - 1; i >= 0; i--)
				{
					var join = table.Joins[i];

					if (join.IsWeak)
					{
						var sources = new HashSet<ISqlTableSource>(QueryHelper.EnumerateAccessibleSources(join.Table));
						var ignore  = new HashSet<IQueryElement> { join };
						if (QueryHelper.IsDependsOn(_rootElement, sources, ignore) 
						|| _dependencies.Any(d => QueryHelper.IsDependsOn(d, sources, ignore)))
						{
							join.IsWeak = false;
						}
						else
						{
							table.Joins.RemoveAt(i);
						}
					}
				}
			}, new HashSet<SelectQuery>());
		}


		static bool IsComplexQuery(SelectQuery query)
		{
			var accessibleSources = new HashSet<ISqlTableSource>();
			var complexFound = QueryHelper.EnumerateAccessibleSources(query)
				.Any(source =>
				{
					accessibleSources.Add(source);
					if (source is SelectQuery q)
						return q.From.Tables.Count != 1 || QueryHelper.EnumerateJoins(q).Any();
					return false;
				});

			if (complexFound)
				return true;

			var usedSources = new HashSet<ISqlTableSource>();
			QueryHelper.CollectUsedSources(query, usedSources);

			return usedSources.Count > accessibleSources.Count;
		}

		void OptimizeDistinct()
		{
			if (!_selectQuery.Select.IsDistinct || !_selectQuery.Select.OptimizeDistinct)
				return;

			if (IsComplexQuery(_selectQuery))
				return;

			var table = _selectQuery.From.Tables[0];

			var keys = new List<IList<ISqlExpression>>();

			QueryHelper.CollectUniqueKeys(_selectQuery, includeDistinct: false, keys);
			QueryHelper.CollectUniqueKeys(table, keys);
			if (keys.Count == 0)
				return;

			var expressions = new HashSet<ISqlExpression>(_selectQuery.Select.Columns.Select(c => c.Expression));
			var foundUnique = keys.Any(key =>
			{
				if (key.All(k => expressions.Contains(k)))
					return true;
				if (key.Select(k => QueryHelper.GetUnderlyingField(k)).All(k => k != null && expressions.Contains(k)))
					return true;
				return false;
			});

			if (foundUnique)
			{
				// We have found that distinct columns has unique key, so we can remove distinct
				_selectQuery.Select.IsDistinct = false;
			}
		}

		static void ApplySubsequentOrder(SelectQuery mainQuery, SelectQuery subQuery)
		{
			if (subQuery.OrderBy.Items.Count > 0)
			{
				var orderItems = !mainQuery.Select.IsDistinct && mainQuery.GroupBy.IsEmpty
					? subQuery.OrderBy.Items
					: subQuery.OrderBy.Items.Where(oi =>
						mainQuery.Select.Columns.Any(c => c.Expression.Equals(oi.Expression)));

				foreach (var item in orderItems)
					mainQuery.OrderBy.Expr(item.Expression, item.IsDescending);
			}
		}

		SqlTableSource OptimizeSubQuery(
			SqlTableSource source,
			bool optimizeWhere,
			bool allColumns,
			bool isApplySupported,
			bool optimizeValues,
			bool optimizeColumns,
			JoinType parentJoin)
		{
			foreach (var jt in source.Joins)
			{
				var table = OptimizeSubQuery(
					jt.Table,
					jt.JoinType == JoinType.Inner || jt.JoinType == JoinType.CrossApply,
					false,
					isApplySupported,
					jt.JoinType == JoinType.Inner || jt.JoinType == JoinType.CrossApply,
					optimizeColumns,
					jt.JoinType);

				if (table != jt.Table)
				{
					if (jt.Table.Source is SelectQuery sql)
						ApplySubsequentOrder(_selectQuery, sql);

					jt.Table = table;
				}
			}

			if (source.Source is SelectQuery select)
			{
				var canRemove = !CorrectCrossJoinQuery(select);
				if (canRemove)
				{
					if (source.Joins.Count > 0)
					{
						// We can not remove subquery that is left side for FULL and RIGHT joins and there is filter
						var join = source.Joins[0];
						if ((join.JoinType == JoinType.Full || join.JoinType == JoinType.Right)
							&& !select.Where.IsEmpty)
						{
							canRemove = false;
						}
					}
				}
				if (canRemove)
					return RemoveSubQuery(source, optimizeWhere, allColumns && !isApplySupported, optimizeValues, optimizeColumns, parentJoin);
			}

			return source;
		}

		bool CorrectCrossJoinQuery(SelectQuery query)
		{
			var select = query.Select;
			if (select.From.Tables.Count == 1)
				return false;

			var joins = select.From.Tables.SelectMany(_ => _.Joins).Distinct().ToArray();
			if (joins.Length == 0)
				return false;

			var tables = select.From.Tables.ToArray();
			foreach (var t in tables)
				t.Joins.Clear();

			var baseTable = tables[0];

			if (_flags.IsCrossJoinSupported || _flags.IsInnerJoinAsCrossSupported)
			{
				select.From.Tables.Clear();
				select.From.Tables.Add(baseTable);

				foreach (var t in tables.Skip(1))
				{
					baseTable.Joins.Add(new SqlJoinedTable(JoinType.Inner, t, false));
				}

				foreach (var j in joins)
					baseTable.Joins.Add(j);
			}
			else
			{
				// move to subquery
				var subQuery = new SelectQuery();

				subQuery.Select.From.Tables.AddRange(tables);

				baseTable = new SqlTableSource(subQuery, "cross");
				baseTable.Joins.AddRange(joins);

				query.Select.From.Tables.Clear();

				var sources     = new HashSet<ISqlTableSource>(tables.Select(t => t.Source));
				var foundFields = new HashSet<ISqlExpression>();

				QueryHelper.CollectDependencies(query.RootQuery(), sources, foundFields);
				QueryHelper.CollectDependencies(baseTable,         sources, foundFields);

				var toReplace = foundFields.ToDictionary(f => f,
					f => subQuery.Select.Columns[subQuery.Select.Add(f)] as ISqlExpression);

				ISqlExpression TransformFunc(ISqlExpression e)
				{
					return toReplace.TryGetValue(e, out var newValue) ? newValue : e;
				}

				((ISqlExpressionWalkable)query.RootQuery()).Walk(new WalkOptions(), TransformFunc);
				foreach (var j in joins)
				{
					((ISqlExpressionWalkable) j).Walk(new WalkOptions(), TransformFunc);
				}

				query.Select.From.Tables.Add(baseTable);
			}

			return true;
		}

		static bool CheckColumn(SqlColumn column, ISqlExpression expr, SelectQuery query, bool optimizeValues, bool optimizeColumns)
		{
			if (expr is SqlField || expr is SqlColumn || expr.ElementType == QueryElementType.SqlRawSqlTable)
				return false;

			if (expr is SqlValue sqlValue)
				return !optimizeValues && 1.Equals(sqlValue.Value);

			if (expr is SqlBinaryExpression e1)
			{
				if (e1.Operation == "*" && e1.Expr1 is SqlValue value)
				{
					if (value.Value is int i && i == -1)
						return CheckColumn(column, e1.Expr2, query, optimizeValues, optimizeColumns);
				}
			}

			var visitor = new QueryVisitor();

			if (optimizeColumns &&
				new QueryVisitor().Find(expr, ex => ex is SelectQuery || QueryHelper.IsAggregationFunction(ex)) == null)
			{
				var n = 0;
				var q = query.ParentSelect ?? query;

				visitor.VisitAll(q, e => { if (e == column) n++; });

				return n > 2;
			}

			return true;
		}

		SqlTableSource RemoveSubQuery(
			SqlTableSource childSource,
			bool concatWhere,
			bool allColumns,
			bool optimizeValues,
			bool optimizeColumns,
			JoinType parentJoin)
		{
			var query = (SelectQuery)childSource.Source;

			var isQueryOK = !query.DoNotRemove && query.From.Tables.Count == 1;

			isQueryOK = isQueryOK && (concatWhere || query.Where.IsEmpty && query.Having.IsEmpty);
			isQueryOK = isQueryOK && !query.HasSetOperators && query.GroupBy.IsEmpty && !query.Select.HasModifier;
			//isQueryOK = isQueryOK && (_flags.IsDistinctOrderBySupported || query.Select.IsDistinct );

			if (isQueryOK && parentJoin != JoinType.Inner && query.From.Tables[0].Joins.Count > 0)
			{
				isQueryOK = false;
			}

			if (!isQueryOK)
				return childSource;

			var isColumnsOK =
				(allColumns && !query.Select.Columns.Any(c => QueryHelper.IsAggregationFunction(c.Expression))) ||
				!query.Select.Columns.Any(c => CheckColumn(c, c.Expression, query, optimizeValues, optimizeColumns));

			if (!isColumnsOK)
				return childSource;

			var map = new Dictionary<ISqlExpression,ISqlExpression>(query.Select.Columns.Count);

			foreach (var c in query.Select.Columns)
			{
				if (!map.ContainsKey(c))
				{
					map.Add(c, c.Expression);
					if (c.RawAlias != null && c.Expression is SqlColumn clmn && clmn.RawAlias == null)
						clmn.RawAlias = c.RawAlias;
				}			
			}

			List<ISqlExpression[]>? uniqueKeys = null;
			if (parentJoin == JoinType.Inner && query.HasUniqueKeys)
				uniqueKeys = query.UniqueKeys;

			uniqueKeys = uniqueKeys?
				.Select(k => k.Select(e => map.TryGetValue(e, out var nw) ? nw : e).ToArray())
				.ToList();

			var top = _rootElement ?? (IQueryElement)_selectQuery.RootQuery();

			((ISqlExpressionWalkable)top).Walk(
				new WalkOptions(), expr => map.TryGetValue(expr, out var fld) ? fld : expr);

			new QueryVisitor().Visit(top, expr =>
			{
				if (expr.ElementType == QueryElementType.InListPredicate)
				{
					var p = (SqlPredicate.InList)expr;

					if (p.Expr1 == query)
						p.Expr1 = query.From.Tables[0];
				}
			});

			query.From.Tables[0].Joins.AddRange(childSource.Joins);

			if (query.From.Tables[0].Alias == null)
				query.From.Tables[0].Alias = childSource.Alias;

			if (!query.Where. IsEmpty) ConcatSearchCondition(_selectQuery.Where,  query.Where);
			if (!query.Having.IsEmpty) ConcatSearchCondition(_selectQuery.Having, query.Having);

			((ISqlExpressionWalkable)top).Walk(new WalkOptions(), expr =>
			{
				if (expr is SelectQuery sql)
					if (sql.ParentSelect == query)
						sql.ParentSelect = query.ParentSelect ?? _selectQuery;

				return expr;
			});

			var result = query.From.Tables[0];

			if (uniqueKeys != null)
				result.UniqueKeys.AddRange(uniqueKeys);

			return result;
		}

		void OptimizeApply(HashSet<ISqlTableSource> parentTableSources, SqlTableSource tableSource, SqlJoinedTable joinTable, bool isApplySupported, bool optimizeColumns)
		{
			var joinSource = joinTable.Table;

			foreach (var join in joinSource.Joins)
				if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
					OptimizeApply(parentTableSources, joinSource, join, isApplySupported, optimizeColumns);

			if (isApplySupported && !joinTable.CanConvertApply)
				return;

			bool ContainsTable(ISqlTableSource table, IQueryElement qe)
			{
				return null != new QueryVisitor().Find(qe, e =>
					e == table ||
					e.ElementType == QueryElementType.SqlField && table == ((SqlField) e).Table ||
					e.ElementType == QueryElementType.Column   && table == ((SqlColumn)e).Parent);
			}

			if (joinSource.Source.ElementType == QueryElementType.SqlQuery)
			{
				var sql   = (SelectQuery)joinSource.Source;
				var isAgg = sql.Select.Columns.Any(c => QueryHelper.IsAggregationFunction(c.Expression));

				if (isApplySupported  && (isAgg || sql.Select.HasModifier))
					return;

				var tableSources = new HashSet<ISqlTableSource>();

				((ISqlExpressionWalkable)sql.Where.SearchCondition).Walk(new WalkOptions(), e =>
				{
					if (e is ISqlTableSource ts && !tableSources.Contains(ts))
						tableSources.Add(ts);
					return e;
				});

				var searchCondition = new List<SqlCondition>();

				{
					var conditions = sql.Where.SearchCondition.Conditions;

					if (conditions.Count > 0)
					{
						for (var i = conditions.Count - 1; i >= 0; i--)
						{
							var condition = conditions[i];

							if (!tableSources.Any(ts => ContainsTable(ts, condition)))
							{
								searchCondition.Insert(0, condition);
								conditions.RemoveAt(i);
							}
							else if (parentTableSources.Any(ts => ContainsTable(ts, condition)))
							{
								if (isApplySupported && Common.Configuration.Linq.PreferApply)
									return;

								searchCondition.Insert(0, condition);
								conditions.RemoveAt(i);
							}
						}
					}
				}

				var sources = new HashSet<ISqlTableSource> {tableSource.Source};
				var ignore  = new HashSet<IQueryElement>();
				ignore.AddRange(QueryHelper.EnumerateJoins(sql).Select(j => j.Condition));

				if (!QueryHelper.IsDependsOn(sql, sources, ignore))
				{
					if (!(joinTable.JoinType == JoinType.CrossApply && searchCondition.Count == 0) // CROSS JOIN
						&& sql.Select.HasModifier)
						throw new LinqToDBException("Database do not support CROSS/OUTER APPLY join required by the query.");

					// correct conditions
					if (searchCondition.Count > 0 && sql.Select.Columns.Count > 0)
					{
						var map = sql.Select.Columns.ToLookup(c => c.Expression);
						foreach (var condition in searchCondition)
						{
							var newPredicate = ConvertVisitor.Convert(condition.Predicate, (visitor, e) =>
							{
								if (e is ISqlExpression ex && map.Contains(ex))
								{
									var newExpr = map[ex].First();
									if (visitor.ParentElement is SqlColumn column)
									{
										if (newExpr != column)
											e = newExpr;
									}
									else 
										e = newExpr;
								}

								return e;
							});
							condition.Predicate = newPredicate;
						}
					}

					joinTable.JoinType = joinTable.JoinType == JoinType.CrossApply ? JoinType.Inner : JoinType.Left;
					joinTable.Condition.Conditions.AddRange(searchCondition);
				}
				else
				{
					sql.Where.SearchCondition.Conditions.AddRange(searchCondition);

					var table = OptimizeSubQuery(
						joinTable.Table,
						joinTable.JoinType == JoinType.Inner || joinTable.JoinType == JoinType.CrossApply,
						joinTable.JoinType == JoinType.CrossApply,
						isApplySupported,
						joinTable.JoinType == JoinType.Inner || joinTable.JoinType == JoinType.CrossApply,
						optimizeColumns,
						joinTable.JoinType);

					if (table != joinTable.Table)
					{
						if (joinTable.Table.Source is SelectQuery q && q.OrderBy.Items.Count > 0)
							ApplySubsequentOrder(_selectQuery, q);

						joinTable.Table = table;

						OptimizeApply(parentTableSources, tableSource, joinTable, isApplySupported, optimizeColumns);
					}
				}
			}
			else
			{
				if (!ContainsTable(tableSource.Source, joinSource.Source))
					joinTable.JoinType = joinTable.JoinType == JoinType.CrossApply ? JoinType.Inner : JoinType.Left;
			}
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

		void OptimizeSubQueries(bool isApplySupported, bool optimizeColumns)
		{
			CorrectCrossJoinQuery(_selectQuery);

			for (var i = 0; i < _selectQuery.From.Tables.Count; i++)
			{
				var table = OptimizeSubQuery(_selectQuery.From.Tables[i], true, false, isApplySupported, true, optimizeColumns, JoinType.Inner);

				if (table != _selectQuery.From.Tables[i])
				{
					if (!_selectQuery.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression)))
					{
						if (_selectQuery.From.Tables[i].Source is SelectQuery sql)
							ApplySubsequentOrder(_selectQuery, sql);
					}

					_selectQuery.From.Tables[i] = table;
				}
			}

			// Move up simple subqueries
			//
			/* TODO: Cause Stackoverflow in ConcatUnionTests.UnionWithObjects
			for (int tableIndex = 0; tableIndex < _selectQuery.From.Tables.Count; tableIndex++)
			{
				var table = _selectQuery.From.Tables[tableIndex];
				if (table.Source is SelectQuery subQuery && subQuery.IsSimple)
				{
					_selectQuery.From.Tables.RemoveAt(tableIndex);
					_selectQuery.From.Tables.InsertRange(tableIndex, subQuery.Select.From.Tables);
					if (table.Joins.Count > 0)
					{
						subQuery.Select.From.Tables.Last().Joins.AddRange(table.Joins);
					}

					var root = _selectQuery.ParentSelect ?? _selectQuery;

					root.Walk(new WalkOptions(), e =>
					{
						if (e is SqlColumn column && column.Parent == subQuery)
						{
							return column.Expression;
						}
					
						return e;
					});

				}
			}

			*/


			//TODO: Failed SelectQueryTests.JoinScalarTest
			//Needs optimization refactor for 3.X
			/*
			if (_selectQuery.IsSimple 
			    && _selectQuery.From.Tables.Count == 1 
				&& _selectQuery.From.Tables[0].Joins.Count == 0
			    && _selectQuery.From.Tables[0].Source is SelectQuery selectQuery
				&& selectQuery.IsSimple
			    && selectQuery.From.Tables.Count == 0)
			{
				// we can merge queries without tables
				_selectQuery.Walk(new WalkOptions(), e =>
				{
					if (e is SqlColumn column && column.Parent == selectQuery)
					{
						return column.Expression;
					}

					return e;
				});
				_selectQuery.From.Tables.Clear();
			}
			*/
		}

		void OptimizeApplies(bool isApplySupported, bool optimizeColumns)
		{
			var tableSources = new HashSet<ISqlTableSource>();

			foreach (var table in _selectQuery.From.Tables)
			{
				tableSources.Add(table);

				foreach (var join in table.Joins)
				{
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
						OptimizeApply(tableSources, table, join, isApplySupported, optimizeColumns);

					join.Walk(new WalkOptions(), e =>
					{
						if (e is ISqlTableSource ts && !tableSources.Contains(ts))
							tableSources.Add(ts);
						return e;
					});
				}
			}
		}

		void RemoveEmptyJoins()
		{
			if (_flags.IsCrossJoinSupported)
				return;

			for (var tableIndex = 0; tableIndex < _selectQuery.From.Tables.Count; tableIndex++)
			{
				var table = _selectQuery.From.Tables[tableIndex];
				for (var joinIndex = 0; joinIndex < table.Joins.Count; joinIndex++)
				{
					var join = table.Joins[joinIndex];
					if (join.JoinType == JoinType.Inner && join.Condition.Conditions.Count == 0)
					{
						_selectQuery.From.Tables.Insert(tableIndex + 1, join.Table);
						table.Joins.RemoveAt(joinIndex);
						--joinIndex;
					}
				}
			}
		}

		void OptimizeColumns()
		{
			((ISqlExpressionWalkable)_selectQuery.Select).Walk(new WalkOptions(), expr =>
			{
				if (expr is SelectQuery query    &&
					query.From.Tables.Count == 0 &&
					query.Select.Columns.Count == 1)
				{
					new QueryVisitor().Visit(query.Select.Columns[0].Expression, e =>
					{
						if (e.ElementType == QueryElementType.SqlQuery)
						{
							var q = (SelectQuery)e;

							if (q.ParentSelect == query)
								q.ParentSelect = query.ParentSelect;
						}
					});

					//TODO: Need to check purpose of this method
					if (query.Select.Columns[0].Expression is ISqlTableSource ts)
						return ts;
				}

				return expr;
			});
		}

		void OptimizeDistinctOrderBy()
		{
			// algorythm works with whole Query, so skipping sub optimizations

			if (_level > 0)
				return;

			var information = new QueryInformation(_selectQuery);

			foreach (var query in information.GetQueriesParentFirst())
			{
				// removing duplicate order items
				query.OrderBy.Items.RemoveDuplicates(o => o.Expression, Utils.ObjectReferenceEqualityComparer<ISqlExpression>.Default);

				// removing sorting for subselects
				if (QueryHelper.CanRemoveOrderBy(query, _flags, information))
				{
					query.OrderBy.Items.Clear();
					continue;
				}

				if (query.Select.IsDistinct)
				{
					QueryHelper.TryRemoveDistinct(query, information);
				}

				if (query.Select.IsDistinct && !query.Select.OrderBy.IsEmpty)
				{
					// nothing to do - DISTINCT ORDER BY supported
					if (_flags.IsDistinctOrderBySupported)
						continue;

					if (Common.Configuration.Linq.KeepDistinctOrdered)
					{
						// trying to convert to GROUP BY quivalent
						QueryHelper.TryConvertOrderedDistinctToGroupBy(query, _flags);
					}
					else
					{
						// removing ordering if no select columns
						var projection = new HashSet<ISqlExpression>(query.Select.Columns.Select(c => c.Expression));
						for (var i = query.OrderBy.Items.Count - 1; i >= 0; i--)
						{
							if (!projection.Contains(query.OrderBy.Items[i].Expression))
								query.OrderBy.Items.RemoveAt(i);
						}
					}
				}
			}

		}
	}
}
