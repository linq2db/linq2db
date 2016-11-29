using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;

	class SelectQueryOptimizer
	{
		public SelectQueryOptimizer(SqlProviderFlags flags, SelectQuery selectQuery)
		{
			_flags       = flags;
			_selectQuery = selectQuery;
		}

		readonly SqlProviderFlags _flags;
		readonly SelectQuery      _selectQuery;

		public void FinalizeAndValidate(bool isApplySupported, bool optimizeColumns)
		{
#if DEBUG
			if (_selectQuery.IsUpdate)
			{
			}

			var sqlText = _selectQuery.SqlText;

			var dic = new Dictionary<SelectQuery,SelectQuery>();

			new QueryVisitor().VisitAll(_selectQuery, e =>
			{
				var sql = e as SelectQuery;

				if (sql != null)
				{
					if (dic.ContainsKey(sql))
						throw new InvalidOperationException("SqlQuery circle reference detected.");

					dic.Add(sql, sql);
				}
			});
#endif

			OptimizeUnions();
			FinalizeAndValidateInternal(isApplySupported, optimizeColumns, new List<ISqlTableSource>());
			ResolveFields();
			_selectQuery.SetAliases();

#if DEBUG
			sqlText = _selectQuery.SqlText;
#endif
		}

		class QueryData
		{
			public SelectQuery          Query;
			public List<ISqlExpression> Fields  = new List<ISqlExpression>();
			public List<QueryData>      Queries = new List<QueryData>();
		}

		void ResolveFields()
		{
			var root = GetQueryData(_selectQuery);

			ResolveFields(root);
		}

		static QueryData GetQueryData(SelectQuery selectQuery)
		{
			var data = new QueryData { Query = selectQuery };

			new QueryVisitor().VisitParentFirst(selectQuery, e =>
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
								data.Queries.Add(GetQueryData((SelectQuery)e));
								return false;
							}

							break;
						}

					case QueryElementType.Column :
						return ((SelectQuery.Column)e).Parent == selectQuery;

					case QueryElementType.SqlTable :
						return false;
				}

				return true;
			});

			return data;
		}

		static SelectQuery.TableSource FindField(SqlField field, SelectQuery.TableSource table)
		{
			if (field.Table == table.Source)
				return table;

			foreach (var @join in table.Joins)
			{
				var t = FindField(field, @join.Table);

				if (t != null)
					return @join.Table;
			}

			return null;
		}

		static ISqlExpression GetColumn(QueryData data, SqlField field)
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
							if (!q.GroupBy.IsEmpty || q.Select.Columns.Any(c => IsAggregationFunction(c.Expression)))
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

			foreach (SqlField field in data.Fields)
			{
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
					ISqlExpression ex;

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
								var expr = (SelectQuery.Predicate.Expr)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								break;
							}

						case QueryElementType.ExprExprPredicate :
							{
								var expr = (SelectQuery.Predicate.ExprExpr)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								break;
							}

						case QueryElementType.LikePredicate :
							{
								var expr = (SelectQuery.Predicate.Like)e;
								if (dic.TryGetValue(expr.Expr1,  out ex)) expr.Expr1  = ex;
								if (dic.TryGetValue(expr.Expr2,  out ex)) expr.Expr2  = ex;
								if (dic.TryGetValue(expr.Escape, out ex)) expr.Escape = ex;
								break;
							}

						case QueryElementType.BetweenPredicate :
							{
								var expr = (SelectQuery.Predicate.Between)e;
								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;
								if (dic.TryGetValue(expr.Expr2, out ex)) expr.Expr2 = ex;
								if (dic.TryGetValue(expr.Expr3, out ex)) expr.Expr3 = ex;
								break;
							}

						case QueryElementType.InListPredicate :
							{
								var expr = (SelectQuery.Predicate.InList)e;

								if (dic.TryGetValue(expr.Expr1, out ex)) expr.Expr1 = ex;

								for (var i = 0; i < expr.Values.Count; i++)
									if (dic.TryGetValue(expr.Values[i], out ex))
										expr.Values[i] = ex;

								break;
							}

						case QueryElementType.Column :
							{
								var expr = (SelectQuery.Column)e;

								if (expr.Parent != data.Query)
									return false;

								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;

								break;
							}

						case QueryElementType.SetExpression :
							{
								var expr = (SelectQuery.SetExpression)e;
								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;
								break;
							}

						case QueryElementType.GroupByClause :
							{
								var expr = (SelectQuery.GroupByClause)e;

								for (var i = 0; i < expr.Items.Count; i++)
									if (dic.TryGetValue(expr.Items[i], out ex))
										expr.Items[i] = ex;

								break;
							}

						case QueryElementType.OrderByItem :
							{
								var expr = (SelectQuery.OrderByItem)e;
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
			var exprs = new Dictionary<ISqlExpression,ISqlExpression>();

			new QueryVisitor().Visit(_selectQuery, e =>
			{
				var sql = e as SelectQuery;

				if (sql == null || sql.From.Tables.Count != 1 || !sql.IsSimple || sql.IsInsert || sql.IsUpdate || sql.IsDelete)
					return;

				var table = sql.From.Tables[0];

				if (table.Joins.Count != 0 || !(table.Source is SelectQuery))
					return;

				var union = (SelectQuery)table.Source;

				if (!union.HasUnion)
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
					scol._alias     = ucol._alias;

					exprs.Add(ucol, scol);
				}

				for (var i = sql.Select.Columns.Count; i < union.Select.Columns.Count; i++)
					sql.Select.Expr(union.Select.Columns[i].Expression);

				sql.From.Tables.Clear();
				sql.From.Tables.AddRange(union.From.Tables);

				sql.Where.  SearchCondition.Conditions.AddRange(union.Where. SearchCondition.Conditions);
				sql.Having. SearchCondition.Conditions.AddRange(union.Having.SearchCondition.Conditions);
				sql.GroupBy.Items.                     AddRange(union.GroupBy.Items);
				sql.OrderBy.Items.                     AddRange(union.OrderBy.Items);
				sql.Unions.InsertRange(0, union.Unions);
			});

			((ISqlExpressionWalkable)_selectQuery).Walk(false, expr =>
			{
				ISqlExpression e;

				if (exprs.TryGetValue(expr, out e))
					return e;

				return expr;
			});
		}

		void FinalizeAndValidateInternal(bool isApplySupported, bool optimizeColumns, List<ISqlTableSource> tables)
		{
			OptimizeSearchCondition(_selectQuery.Where. SearchCondition);
			OptimizeSearchCondition(_selectQuery.Having.SearchCondition);

			_selectQuery.ForEachTable(table =>
			{
				foreach (var join in table.Joins)
					OptimizeSearchCondition(join.Condition);
			}, new HashSet<SelectQuery>());

			new QueryVisitor().Visit(_selectQuery, e =>
			{
				var sql = e as SelectQuery;

				if (sql != null && sql != _selectQuery)
				{
					sql.ParentSelect = _selectQuery;
					new SelectQueryOptimizer(_flags, sql).FinalizeAndValidateInternal(isApplySupported, optimizeColumns, tables);

					if (sql.IsParameterDependent)
						_selectQuery.IsParameterDependent = true;
				}
			});

			ResolveWeakJoins(tables);
			OptimizeColumns();
			OptimizeApplies   (isApplySupported, optimizeColumns);
			OptimizeSubQueries(isApplySupported, optimizeColumns);
			OptimizeApplies   (isApplySupported, optimizeColumns);
			OptimizeJoins();

			new QueryVisitor().Visit(_selectQuery, e =>
			{
				var sql = e as SelectQuery;

				if (sql != null && sql != _selectQuery)
					RemoveOrderBy(sql);
			});
		}

		internal static void OptimizeSearchCondition(SelectQuery.SearchCondition searchCondition)
		{
			// This 'if' could be replaced by one simple match:
			//
			// match (searchCondition.Conditions)
			// {
			// | [SearchCondition(true, _) sc] =>
			//     searchCondition.Conditions = sc.Conditions;
			//     OptimizeSearchCondition(searchCodition)
			//
			// | [SearchCondition(false, [SearchCondition(true, [ExprExpr]) sc])] => ...
			//
			// | [Expr(true,  SqlValue(true))]
			// | [Expr(false, SqlValue(false))]
			//     searchCondition.Conditions = []
			// }
			//
			// One day I am going to rewrite all this crap in Nemerle.
			//
			if (searchCondition.Conditions.Count == 1)
			{
				var cond = searchCondition.Conditions[0];

				if (cond.Predicate is SelectQuery.SearchCondition)
				{
					var sc = (SelectQuery.SearchCondition)cond.Predicate;

					if (!cond.IsNot)
					{
						searchCondition.Conditions.Clear();
						searchCondition.Conditions.AddRange(sc.Conditions);

						OptimizeSearchCondition(searchCondition);
						return;
					}

					if (sc.Conditions.Count == 1)
					{
						var c1 = sc.Conditions[0];

						if (!c1.IsNot && c1.Predicate is SelectQuery.Predicate.ExprExpr)
						{
							var ee = (SelectQuery.Predicate.ExprExpr)c1.Predicate;
							SelectQuery.Predicate.Operator op;

							switch (ee.Operator)
							{
								case SelectQuery.Predicate.Operator.Equal          : op = SelectQuery.Predicate.Operator.NotEqual;       break;
								case SelectQuery.Predicate.Operator.NotEqual       : op = SelectQuery.Predicate.Operator.Equal;          break;
								case SelectQuery.Predicate.Operator.Greater        : op = SelectQuery.Predicate.Operator.LessOrEqual;    break;
								case SelectQuery.Predicate.Operator.NotLess        :
								case SelectQuery.Predicate.Operator.GreaterOrEqual : op = SelectQuery.Predicate.Operator.Less;           break;
								case SelectQuery.Predicate.Operator.Less           : op = SelectQuery.Predicate.Operator.GreaterOrEqual; break;
								case SelectQuery.Predicate.Operator.NotGreater     :
								case SelectQuery.Predicate.Operator.LessOrEqual    : op = SelectQuery.Predicate.Operator.Greater;        break;
								default: throw new InvalidOperationException();
							}

							c1.Predicate = new SelectQuery.Predicate.ExprExpr(ee.Expr1, op, ee.Expr2);

							searchCondition.Conditions.Clear();
							searchCondition.Conditions.AddRange(sc.Conditions);

							OptimizeSearchCondition(searchCondition);
							return;
						}
					}
				}

				if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SelectQuery.Predicate.Expr)cond.Predicate;

					if (expr.Expr1 is SqlValue)
					{
						var value = (SqlValue)expr.Expr1;

						if (value.Value is bool)
							if (cond.IsNot ? !(bool)value.Value : (bool)value.Value)
								searchCondition.Conditions.Clear();
					}
				}
			}

			for (var i = 0; i < searchCondition.Conditions.Count; i++)
			{
				var cond = searchCondition.Conditions[i];

				if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SelectQuery.Predicate.Expr)cond.Predicate;

					if (expr.Expr1 is SqlValue)
					{
						var value = (SqlValue)expr.Expr1;

						if (value.Value is bool)
						{
							if (cond.IsNot ? !(bool)value.Value : (bool)value.Value)
							{
								if (i > 0)
								{
									if (searchCondition.Conditions[i-1].IsOr)
									{
										searchCondition.Conditions.RemoveRange(0, i);
										OptimizeSearchCondition(searchCondition);

										break;
									}
								}
							}
						}
					}
				}
				else if (cond.Predicate is SelectQuery.SearchCondition)
				{
					var sc = (SelectQuery.SearchCondition)cond.Predicate;
					OptimizeSearchCondition(sc);
				}
			}
		}

		static void RemoveOrderBy(SelectQuery selectQuery)
		{
			if (selectQuery.OrderBy.Items.Count > 0 && selectQuery.Select.SkipValue == null && selectQuery.Select.TakeValue == null)
				selectQuery.OrderBy.Items.Clear();
		}

		internal void ResolveWeakJoins(List<ISqlTableSource> tables)
		{
			Func<SelectQuery.TableSource,bool> findTable = null; findTable = table =>
			{
				if (tables.Contains(table.Source))
					return true;

				foreach (var join in table.Joins)
				{
					if (findTable(join.Table))
					{
						join.IsWeak = false;
						return true;
					}
				}

				if (table.Source is SelectQuery)
					foreach (var t in ((SelectQuery)table.Source).From.Tables)
						if (findTable(t))
							return true;

				return false;
			};

			var areTablesCollected = false;

			_selectQuery.ForEachTable(table =>
			{
				for (var i = 0; i < table.Joins.Count; i++)
				{
					var join = table.Joins[i];

					if (join.IsWeak)
					{
						if (!areTablesCollected)
						{
							areTablesCollected = true;

							Action<IQueryElement> tableCollector = expr =>
							{
								var field = expr as SqlField;

								if (field != null && !tables.Contains(field.Table))
									tables.Add(field.Table);
							};

							var visitor = new QueryVisitor();

							visitor.VisitAll(_selectQuery.Select,  tableCollector);
							visitor.VisitAll(_selectQuery.Where,   tableCollector);
							visitor.VisitAll(_selectQuery.GroupBy, tableCollector);
							visitor.VisitAll(_selectQuery.Having,  tableCollector);
							visitor.VisitAll(_selectQuery.OrderBy, tableCollector);

							if (_selectQuery.IsInsert)
								visitor.VisitAll(_selectQuery.Insert, tableCollector);

							if (_selectQuery.IsUpdate)
								visitor.VisitAll(_selectQuery.Update, tableCollector);

							if (_selectQuery.IsDelete)
								visitor.VisitAll(_selectQuery.Delete, tableCollector);

							visitor.VisitAll(_selectQuery.From, expr =>
							{
								var tbl = expr as SqlTable;

								if (tbl != null && tbl.TableArguments != null)
								{
									var v = new QueryVisitor();

									foreach (var arg in tbl.TableArguments)
										v.VisitAll(arg, tableCollector);
								}
							});
						}

						if (findTable(join.Table))
						{
							join.IsWeak = false;
						}
						else
						{
							table.Joins.RemoveAt(i);
							i--;
						}
					}
				}
			}, new HashSet<SelectQuery>());
		}

		SelectQuery.TableSource OptimizeSubQuery(
			SelectQuery.TableSource source,
			bool optimizeWhere,
			bool allColumns,
			bool isApplySupported,
			bool optimizeValues,
			bool optimizeColumns)
		{
			foreach (var jt in source.Joins)
			{
				var table = OptimizeSubQuery(
					jt.Table,
					jt.JoinType == SelectQuery.JoinType.Inner || jt.JoinType == SelectQuery.JoinType.CrossApply,
					false,
					isApplySupported,
					jt.JoinType == SelectQuery.JoinType.Inner || jt.JoinType == SelectQuery.JoinType.CrossApply,
					optimizeColumns);

				if (table != jt.Table)
				{
					var sql = jt.Table.Source as SelectQuery;

					if (sql != null && sql.OrderBy.Items.Count > 0)
						foreach (var item in sql.OrderBy.Items)
							_selectQuery.OrderBy.Expr(item.Expression, item.IsDescending);

					jt.Table = table;
				}
			}

			return source.Source is SelectQuery ?
				RemoveSubQuery(source, optimizeWhere, allColumns && !isApplySupported, optimizeValues, optimizeColumns) :
				source;
		}

		static bool CheckColumn(SelectQuery.Column column, ISqlExpression expr, SelectQuery query, bool optimizeValues, bool optimizeColumns)
		{
			if (expr is SqlField || expr is SelectQuery.Column)
				return false;

			if (expr is SqlValue)
				return !optimizeValues && 1.Equals(((SqlValue)expr).Value);

			if (expr is SqlBinaryExpression)
			{
				var e = (SqlBinaryExpression)expr;

				if (e.Operation == "*" && e.Expr1 is SqlValue)
				{
					var value = (SqlValue)e.Expr1;

					if (value.Value is int && (int)value.Value == -1)
						return CheckColumn(column, e.Expr2, query, optimizeValues, optimizeColumns);
				}
			}

			var visitor = new QueryVisitor();

			if (optimizeColumns &&
				visitor.Find(expr, e => e is SelectQuery || IsAggregationFunction(e)) == null)
			{
				var n = 0;
				var q = query.ParentSelect ?? query;

				visitor.VisitAll(q, e => { if (e == column) n++; });

				return n > 2;
			}

			return true;
		}

		SelectQuery.TableSource RemoveSubQuery(
			SelectQuery.TableSource childSource,
			bool concatWhere,
			bool allColumns,
			bool optimizeValues,
			bool optimizeColumns)
		{
			var query = (SelectQuery)childSource. Source;

			var isQueryOK = query.From.Tables.Count == 1;

			isQueryOK = isQueryOK && (concatWhere || query.Where.IsEmpty && query.Having.IsEmpty);
			isQueryOK = isQueryOK && !query.HasUnion && query.GroupBy.IsEmpty && !query.Select.HasModifier;
			//isQueryOK = isQueryOK && (_flags.IsDistinctOrderBySupported || query.Select.IsDistinct );

			if (!isQueryOK)
				return childSource;

			var isColumnsOK =
				(allColumns && !query.Select.Columns.Any(c => IsAggregationFunction(c.Expression))) ||
				!query.Select.Columns.Any(c => CheckColumn(c, c.Expression, query, optimizeValues, optimizeColumns));

			if (!isColumnsOK)
				return childSource;

			var map = new Dictionary<ISqlExpression,ISqlExpression>(query.Select.Columns.Count);

			foreach (var c in query.Select.Columns)
				map.Add(c, c.Expression);

			var top = _selectQuery;

			while (top.ParentSelect != null)
				top = top.ParentSelect;

			((ISqlExpressionWalkable)top).Walk(false, expr =>
			{
				ISqlExpression fld;
				return map.TryGetValue(expr, out fld) ? fld : expr;
			});

			new QueryVisitor().Visit(top, expr =>
			{
				if (expr.ElementType == QueryElementType.InListPredicate)
				{
					var p = (SelectQuery.Predicate.InList)expr;

					if (p.Expr1 == query)
						p.Expr1 = query.From.Tables[0];
				}
			});

			query.From.Tables[0].Joins.AddRange(childSource.Joins);

			if (query.From.Tables[0].Alias == null)
				query.From.Tables[0].Alias = childSource.Alias;

			if (!query.Where. IsEmpty) ConcatSearchCondition(_selectQuery.Where,  query.Where);
			if (!query.Having.IsEmpty) ConcatSearchCondition(_selectQuery.Having, query.Having);

			((ISqlExpressionWalkable)top).Walk(false, expr =>
			{
				if (expr is SelectQuery)
				{
					var sql = (SelectQuery)expr;

					if (sql.ParentSelect == query)
						sql.ParentSelect = query.ParentSelect ?? _selectQuery;
				}

				return expr;
			});

			return query.From.Tables[0];
		}

		static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction)
				switch (((SqlFunction)expr).Name)
				{
					case "Count"   :
					case "Average" :
					case "Min"     :
					case "Max"     :
					case "Sum"     : return true;
				}

			return false;
		}

		void OptimizeApply(SelectQuery.TableSource tableSource, SelectQuery.JoinedTable joinTable, bool isApplySupported, bool optimizeColumns)
		{
			var joinSource = joinTable.Table;

			foreach (var join in joinSource.Joins)
				if (join.JoinType == SelectQuery.JoinType.CrossApply || join.JoinType == SelectQuery.JoinType.OuterApply)
					OptimizeApply(joinSource, join, isApplySupported, optimizeColumns);

			if (isApplySupported && !joinTable.CanConvertApply)
				return;

			if (joinSource.Source.ElementType == QueryElementType.SqlQuery)
			{
				var sql   = (SelectQuery)joinSource.Source;
				var isAgg = sql.Select.Columns.Any(c => IsAggregationFunction(c.Expression));

				if (isApplySupported && (isAgg || sql.Select.TakeValue != null || sql.Select.SkipValue != null))
					return;

				var searchCondition = new List<SelectQuery.Condition>(sql.Where.SearchCondition.Conditions);

				sql.Where.SearchCondition.Conditions.Clear();

				if (!ContainsTable(tableSource.Source, sql))
				{
					joinTable.JoinType = joinTable.JoinType == SelectQuery.JoinType.CrossApply ? SelectQuery.JoinType.Inner : SelectQuery.JoinType.Left;
					joinTable.Condition.Conditions.AddRange(searchCondition);
				}
				else
				{
					sql.Where.SearchCondition.Conditions.AddRange(searchCondition);

					var table = OptimizeSubQuery(
						joinTable.Table,
						joinTable.JoinType == SelectQuery.JoinType.Inner || joinTable.JoinType == SelectQuery.JoinType.CrossApply,
						joinTable.JoinType == SelectQuery.JoinType.CrossApply,
						isApplySupported,
						joinTable.JoinType == SelectQuery.JoinType.Inner || joinTable.JoinType == SelectQuery.JoinType.CrossApply,
						optimizeColumns);

					if (table != joinTable.Table)
					{
						var q = joinTable.Table.Source as SelectQuery;

						if (q != null && q.OrderBy.Items.Count > 0)
							foreach (var item in q.OrderBy.Items)
								_selectQuery.OrderBy.Expr(item.Expression, item.IsDescending);

						joinTable.Table = table;

						OptimizeApply(tableSource, joinTable, isApplySupported, optimizeColumns);
					}
				}
			}
			else
			{
				if (!ContainsTable(tableSource.Source, joinSource.Source))
					joinTable.JoinType = joinTable.JoinType == SelectQuery.JoinType.CrossApply ? SelectQuery.JoinType.Inner : SelectQuery.JoinType.Left;
			}
		}

		static bool ContainsTable(ISqlTableSource table, IQueryElement sql)
		{
			return null != new QueryVisitor().Find(sql, e =>
				e == table ||
				e.ElementType == QueryElementType.SqlField && table == ((SqlField)e).Table ||
				e.ElementType == QueryElementType.Column   && table == ((SelectQuery.Column)  e).Parent);
		}

		static void ConcatSearchCondition(SelectQuery.WhereClause where1, SelectQuery.WhereClause where2)
		{
			if (where1.IsEmpty)
			{
				where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
			else
			{
				if (where1.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc1 = new SelectQuery.SearchCondition();

					sc1.Conditions.AddRange(where1.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Clear();
					where1.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, sc1));
				}

				if (where2.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc2 = new SelectQuery.SearchCondition();

					sc2.Conditions.AddRange(where2.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, sc2));
				}
				else
					where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
		}

		void OptimizeSubQueries(bool isApplySupported, bool optimizeColumns)
		{
			for (var i = 0; i < _selectQuery.From.Tables.Count; i++)
			{
				var table = OptimizeSubQuery(_selectQuery.From.Tables[i], true, false, isApplySupported, true, optimizeColumns);

				if (table != _selectQuery.From.Tables[i])
				{
					var sql = _selectQuery.From.Tables[i].Source as SelectQuery;

					if (!_selectQuery.Select.Columns.All(c => IsAggregationFunction(c.Expression)))
						if (sql != null && sql.OrderBy.Items.Count > 0)
							foreach (var item in sql.OrderBy.Items)
								_selectQuery.OrderBy.Expr(item.Expression, item.IsDescending);

					_selectQuery.From.Tables[i] = table;
				}
			}
		}

		void OptimizeApplies(bool isApplySupported, bool optimizeColumns)
		{
			foreach (var table in _selectQuery.From.Tables)
				foreach (var join in table.Joins)
					if (join.JoinType == SelectQuery.JoinType.CrossApply || join.JoinType == SelectQuery.JoinType.OuterApply)
						OptimizeApply(table, join, isApplySupported, optimizeColumns);
		}


		private bool CanRemoveSecond(SelectQuery.JoinedTable join1,
			SelectQuery.JoinedTable join2)
		{
			var result = join1.Condition.Precedence == join2.Condition.Precedence;

			if (!result)
				return false;

			var table1 = join1.Table.Source as SqlTable;
			var table2 = join2.Table.Source as SqlTable;

			result = IsEqualTables(table1, table2);
			result = result && table1 != null && table1.GetKeys(false).Count > 0; // table has keys
			result = result && IsSameEqualConditions(join1, join2);

			return result;
		}

		private bool IsEqualTables(SqlTable table1, SqlTable table2)
		{
			var result =
				table1 != null && table2 != null
				&& table1.Database == table2.Database
				&& table1.Owner    == table2.Owner
				&& table1.Name     == table2.Name;
			return result;
		}

		private bool IsSameEqualConditions(SelectQuery.JoinedTable join1,
			SelectQuery.JoinedTable join2)
		{
			var c1 = join1.Condition;
			var c2 = join2.Condition;

			Func<SelectQuery.Condition, bool> allowed =
				c =>
				{
					var expr = c.Predicate as SelectQuery.Predicate.ExprExpr;
					return expr != null && expr.Operator == SelectQuery.Predicate.Operator.Equal && !c.IsOr;
				};

			if (!c1.Conditions.All(allowed) || !c2.Conditions.All(allowed))
				return false;

			var result = c1.Conditions.Count == c2.Conditions.Count;

			if (result)
			{
				Func<ISqlExpression, ISqlExpression, HashSet<int>, bool> isEqual = (e1, e2, pks) =>
				{
					var er = e1.ElementType == e2.ElementType && e1.Precedence == e2.Precedence;
					if (er)
					{
						switch (e1.ElementType)
						{
							case QueryElementType.Column:
							{
								var col1 = ((SelectQuery.Column) e1).Expression as SqlField;
								var col2 = ((SelectQuery.Column) e2).Expression as SqlField;
								er = col1 != null && col2 != null
								     && col1.Name == col2.Name
								     && col1.Table.SourceID == col2.Table.SourceID;
								break;
							}

							case QueryElementType.SqlField:
							{
								var col1 = (SqlField) e1;
								var col2 = (SqlField) e2;

								er = col1.Name == col2.Name
								     && IsEqualTables(col1.Table as SqlTable, col2.Table as SqlTable);

								if (er && (col1.Table.SourceID == join2.Table.SourceID || col2.Table.SourceID == join2.Table.SourceID))
//                                if (er && IsEqualTables(col1.Table as SqlTable, join2.Table.Source as SqlTable))
								{
									if (col1.IsPrimaryKey && col2.IsPrimaryKey)
										pks.Add(Math.Max(1, col1.PrimaryKeyOrder));
								}

								break;
							}
							default:
								er = false;
								break;
						}
					}
					return er;
				};

				var c2C = c2.Conditions.ToList();
				var primary = new HashSet<int>();

				for (int i1 = 0; i1 < c1.Conditions.Count; i1++)
				{
					var it1 = c1.Conditions[i1].Predicate as SelectQuery.Predicate.ExprExpr;
					if (it1 == null)
					{
						result = false;
						break;
					}

					var found = false;

					for (int i2 = 0; i2 < c2C.Count; i2++)
					{
						var it2 = c2C[i2].Predicate as SelectQuery.Predicate.ExprExpr;
						if (it2 == null)
						{
							result = false;
							break;
						}

						if ((isEqual(it1.Expr1, it2.Expr1, primary) && isEqual(it1.Expr2, it2.Expr2, primary)) ||
						    (isEqual(it2.Expr1, it1.Expr1, primary) && isEqual(it2.Expr2, it1.Expr2, primary)))
						{
							found = true;
							c2C.RemoveAt(i2);
							break;
						}
					}

					if (!found)
						result = false;

					if (!result)
						break;
				}

				if (c2C.Count > 0)
					result = false;

				// check that all keys used in comparison
				result = result && (join2.Table.Source.GetKeys(false).Count == primary.Count);
			}

			return result;
		}

		bool IsDependent(SelectQuery.JoinedTable joinedTable, SelectQuery.JoinedTable tested)
		{
			var dependent = false;
			var sourceId = tested.Table.Source.SourceID;

			// check everyting that can be dependent on specific table
			new QueryVisitor().VisitParentFirst(joinedTable, e =>
			{
				if (dependent)
					return false;

				// do not check tested join
				if (e == tested)
					return false;

				if (e.ElementType == QueryElementType.SqlField)
				{
					var sqlField = (SqlField) e;

					dependent = sqlField.Table.SourceID == sourceId;
				}

				return !dependent;
			});

			return dependent;
		}

		void FlattenInnerJoins()
		{
			foreach (var table in _selectQuery.From.Tables)
			{
				for (int i = 0; i < table.Joins.Count; i++)
				{
					var j = table.Joins[i];
					if (j.JoinType != SelectQuery.JoinType.Inner)
						continue;
					for (int si = 0; si < j.Table.Joins.Count; si++)
					{
						var sj = j.Table.Joins[si];
						if (sj.JoinType == SelectQuery.JoinType.Inner && !IsDependent(j, sj))
						{
							table.Joins.Add(sj);
							j.Table.Joins.RemoveAt(si);
							--si;
						}
					}
				}
			}
		}

		void OptimizeJoins()
		{
			FlattenInnerJoins();

			foreach (var table in _selectQuery.From.Tables)
			{
				if (table.Joins.Count > 1)
				{
					for (int i1 = 0; i1 < table.Joins.Count; i1++)
					{
						var j1 = table.Joins[i1];
						if (j1.JoinType != SelectQuery.JoinType.Inner)
							continue;

						for (int i2 = i1 + 1; i2 < table.Joins.Count; i2++)
						{
							var j2 = table.Joins[i2];
							if (j2.JoinType != SelectQuery.JoinType.Inner)
								continue;

							var canRemove = CanRemoveSecond(j1, j2);

							if (canRemove)
							{
								table.Joins.RemoveAt(i2);
								j1.Table.Joins.AddRange(j2.Table.Joins);
								--i2;

								new QueryVisitor().Visit(_selectQuery, element =>
								{
									switch (element.ElementType)
									{
										case QueryElementType.SqlField:
										{
											var sqlField = (SqlField) element;
											if (sqlField.Table.SourceID == j2.Table.Source.SourceID)
												sqlField.Table = j1.Table.Source;
											break;
										}
									}
								});
							}
						}
					}
				}
			}
		}

		void OptimizeColumns()
		{
			((ISqlExpressionWalkable)_selectQuery.Select).Walk(false, expr =>
			{
				var query = expr as SelectQuery;
					
				if (query != null && query.From.Tables.Count == 0 && query.Select.Columns.Count == 1)
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

					return query.Select.Columns[0].Expression;
				}

				return expr;
			});
		}
	}
}
