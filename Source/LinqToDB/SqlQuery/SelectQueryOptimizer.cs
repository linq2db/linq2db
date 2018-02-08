using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;

	class SelectQueryOptimizer
	{
		public SelectQueryOptimizer(SqlProviderFlags flags, SqlStatement statement, SelectQuery selectQuery)
		{
			_flags       = flags;
			_selectQuery = selectQuery;
			_statement   = statement;
		}

		readonly SqlProviderFlags _flags;
		readonly SelectQuery      _selectQuery;
		readonly SqlStatement     _statement;

		public void FinalizeAndValidate(bool isApplySupported, bool optimizeColumns)
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
			FinalizeAndValidateInternal(isApplySupported, optimizeColumns, new List<ISqlTableSource>());
			ResolveFields();

#if DEBUG
			// ReSharper disable once RedundantAssignment
			sqlText = _selectQuery.SqlText;
#endif
		}

		class QueryData
		{
			public          SelectQuery          Query;
			public readonly List<ISqlExpression> Fields  = new List<ISqlExpression>();
			public readonly List<QueryData>      Queries = new List<QueryData>();
		}

		void ResolveFields()
		{
			var root = GetQueryData(_statement, _selectQuery);

			ResolveFields(root);
		}

		static QueryData GetQueryData(SqlStatement statement, SelectQuery selectQuery)
		{
			var data = new QueryData { Query = selectQuery };

			new QueryVisitor().VisitParentFirst(statement as IQueryElement ?? selectQuery, e =>
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

		static SqlTableSource FindField(SqlField field, SqlTableSource table)
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
								if (dic.TryGetValue(expr.Expression, out ex)) expr.Expression = ex;
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
			var isAllUnion = QueryVisitor.Find(_selectQuery,
				ne => ne is SqlUnion nu && nu.IsAll);

			var isNotAllUnion = QueryVisitor.Find(_selectQuery,
				ne => ne is SqlUnion nu && !nu.IsAll);

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
					scol.RawAlias   = ucol.RawAlias;

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

			_selectQuery.Walk(
				false, expr => exprs.TryGetValue(expr, out var e) ? e : expr);
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
				if (e is SelectQuery sql && sql != _selectQuery)
				{
					sql.ParentSelect = _selectQuery;
					new SelectQueryOptimizer(_flags, _statement, sql).FinalizeAndValidateInternal(isApplySupported, optimizeColumns, tables);

					if (sql.IsParameterDependent)
						_selectQuery.IsParameterDependent = true;
				}
			});

			ResolveWeakJoins(tables);
			OptimizeColumns();
			OptimizeApplies   (isApplySupported, optimizeColumns);
			OptimizeSubQueries(isApplySupported, optimizeColumns);
			OptimizeApplies   (isApplySupported, optimizeColumns);

			new QueryVisitor().Visit(_selectQuery, e =>
			{
				if (e is SelectQuery sql && sql != _selectQuery)
					RemoveOrderBy(sql);
			});
		}

		internal static void OptimizeSearchCondition(SqlSearchCondition searchCondition)
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

				if (cond.Predicate is SqlSearchCondition sc)
				{
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

						if (!c1.IsNot && c1.Predicate is SqlPredicate.ExprExpr)
						{
							var ee = (SqlPredicate.ExprExpr)c1.Predicate;
							SqlPredicate.Operator op;

							switch (ee.Operator)
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

							c1.Predicate = new SqlPredicate.ExprExpr(ee.Expr1, op, ee.Expr2);

							searchCondition.Conditions.Clear();
							searchCondition.Conditions.AddRange(sc.Conditions);

							OptimizeSearchCondition(searchCondition);
							return;
						}
					}
				}

				if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SqlPredicate.Expr)cond.Predicate;

					if (expr.Expr1 is SqlValue value)
						if (value.Value is bool b)
							if (cond.IsNot ? !b : b)
								searchCondition.Conditions.Clear();
				}
			}

			for (var i = 0; i < searchCondition.Conditions.Count; i++)
			{
				var cond = searchCondition.Conditions[i];

				if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SqlPredicate.Expr)cond.Predicate;

					if (expr.Expr1 is SqlValue value)
					{
						if (value.Value is bool b)
						{
							if (cond.IsNot ? !b : b)
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
				else if (cond.Predicate is SqlSearchCondition)
				{
					var sc = (SqlSearchCondition)cond.Predicate;
					OptimizeSearchCondition(sc);
					if (sc.Conditions.Count == 0)
					{
						if (cond.IsOr)
						{
							searchCondition.Conditions.Clear();
							break;
						}
						searchCondition.Conditions.RemoveAt(i);
						--i;
					}
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
			bool FindTable(SqlTableSource table)
			{
				if (tables.Contains(table.Source))
					return true;

				foreach (var join in table.Joins)
				{
					if (FindTable(join.Table))
					{
						join.IsWeak = false;
						return true;
					}
				}

				if (table.Source is SelectQuery query)
					foreach (var t in query.From.Tables)
						if (FindTable(t))
							return true;

				return false;
			}

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

							void TableCollector(IQueryElement expr)
							{
								if (expr is SqlField field && !tables.Contains(field.Table))
									tables.Add(field.Table);
							}

							var visitor = new QueryVisitor();

							visitor.VisitAll(_selectQuery.Select,  TableCollector);
							visitor.VisitAll(_selectQuery.Where,   TableCollector);
							visitor.VisitAll(_selectQuery.GroupBy, TableCollector);
							visitor.VisitAll(_selectQuery.Having,  TableCollector);
							visitor.VisitAll(_selectQuery.OrderBy, TableCollector);

							if (_statement != null)
							{
								foreach (var clause in _statement.EnumClauses())
								{
									visitor.VisitAll(clause, TableCollector);
								}
							}

							visitor.VisitAll(_selectQuery.From, expr =>
							{
								if (expr is SqlTable tbl && tbl.TableArguments != null)
								{
									var v = new QueryVisitor();

									foreach (var arg in tbl.TableArguments)
										v.VisitAll(arg, TableCollector);
								}
							});
						}

						if (FindTable(join.Table))
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

		SqlTableSource OptimizeSubQuery(
			SqlTableSource source,
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
					jt.JoinType == JoinType.Inner || jt.JoinType == JoinType.CrossApply,
					false,
					isApplySupported,
					jt.JoinType == JoinType.Inner || jt.JoinType == JoinType.CrossApply,
					optimizeColumns);

				if (table != jt.Table)
				{
					if (jt.Table.Source is SelectQuery sql && sql.OrderBy.Items.Count > 0)
						foreach (var item in sql.OrderBy.Items)
							_selectQuery.OrderBy.Expr(item.Expression, item.IsDescending);

					jt.Table = table;
				}
			}

			if (source.Source is SelectQuery select)
			{
				var canRemove = !CorrectCrossJoinQuery(select);
				if (canRemove)
					return RemoveSubQuery(source, optimizeWhere, allColumns && !isApplySupported, optimizeValues, optimizeColumns);
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

				var sources = new HashSet<ISqlTableSource>(tables.Select(t => t.Source));
				var foundFields = new HashSet<ISqlExpression>();

				QueryHelper.CollectDependencies(query.RootQuery(), sources, foundFields);
				QueryHelper.CollectDependencies(baseTable,         sources, foundFields);

				var toReplace = foundFields.ToDictionary(f => f,
					f => subQuery.Select.Columns[subQuery.Select.Add(f)] as ISqlExpression);

				ISqlExpression TransformFunc(ISqlExpression e)
				{
					return toReplace.TryGetValue(e, out var newValue) ? newValue : e;
				}

				((ISqlExpressionWalkable) query.RootQuery()).Walk(false, TransformFunc);
				foreach (var j in joins)
				{
					((ISqlExpressionWalkable) j).Walk(false, TransformFunc);
				}

				query.Select.From.Tables.Add(baseTable);
			}

			return true;
		}

		static bool CheckColumn(SqlColumn column, ISqlExpression expr, SelectQuery query, bool optimizeValues, bool optimizeColumns)
		{
			if (expr is SqlField || expr is SqlColumn)
				return false;

			if (expr is SqlValue sqlValue)
				return !optimizeValues && 1.Equals(sqlValue.Value);

			if (expr is SqlBinaryExpression e1)
			{
				if (e1.Operation == "*" && e1.Expr1 is SqlValue)
				{
					var value = (SqlValue)e1.Expr1;

					if (value.Value is int i && i == -1)
						return CheckColumn(column, e1.Expr2, query, optimizeValues, optimizeColumns);
				}
			}

			var visitor = new QueryVisitor();

			if (optimizeColumns &&
				QueryVisitor.Find(expr, ex => ex is SelectQuery || IsAggregationFunction(ex)) == null)
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

			var top = _statement ?? (IQueryElement)_selectQuery.RootQuery();

			((ISqlExpressionWalkable)top).Walk(
				false, expr => map.TryGetValue(expr, out var fld) ? fld : expr);

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

			((ISqlExpressionWalkable)top).Walk(false, expr =>
			{
				if (expr is SelectQuery sql)
					if (sql.ParentSelect == query)
						sql.ParentSelect = query.ParentSelect ?? _selectQuery;

				return expr;
			});

			return query.From.Tables[0];
		}

		static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction func)
				return func.IsAggregate;

			if (expr is SqlExpression expression)
				return expression.IsAggregate;

			return false;
		}

		void OptimizeApply(SqlTableSource tableSource, SqlJoinedTable joinTable, bool isApplySupported, bool optimizeColumns)
		{
			var joinSource = joinTable.Table;

			foreach (var join in joinSource.Joins)
				if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
					OptimizeApply(joinSource, join, isApplySupported, optimizeColumns);

			if (isApplySupported && !joinTable.CanConvertApply)
				return;

			bool ContainsTable(ISqlTableSource table, IQueryElement qe)
			{
				return null != QueryVisitor.Find(qe, e =>
					e == table ||
					e.ElementType == QueryElementType.SqlField && table == ((SqlField) e).Table ||
					e.ElementType == QueryElementType.Column   && table == ((SqlColumn)e).Parent);
			}

			if (joinSource.Source.ElementType == QueryElementType.SqlQuery)
			{
				var sql   = (SelectQuery)joinSource.Source;
				var isAgg = sql.Select.Columns.Any(c => IsAggregationFunction(c.Expression));

				if (isApplySupported  && (isAgg || sql.Select.HasModifier || Common.Configuration.Linq.PrefereApply))
					return;

				var tableSources = new HashSet<ISqlTableSource>();

				((ISqlExpressionWalkable)sql.Where.SearchCondition).Walk(false, e =>
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
						}
					}
				}

				if (!ContainsTable(tableSource.Source, sql))
				{
					if (!(joinTable.JoinType == JoinType.CrossApply && searchCondition.Count == 0) // CROSS JOIN
						&& sql.Select.HasModifier)
						throw new LinqToDBException("Database do not support CROSS/OUTER APPLY join required by the query.");

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
						optimizeColumns);

					if (table != joinTable.Table)
					{
						if (joinTable.Table.Source is SelectQuery q && q.OrderBy.Items.Count > 0)
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
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
						OptimizeApply(table, join, isApplySupported, optimizeColumns);
		}

		void OptimizeColumns()
		{
			((ISqlExpressionWalkable)_selectQuery.Select).Walk(false, expr =>
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

					return query.Select.Columns[0].Expression;
				}

				return expr;
			});
		}
	}
}
