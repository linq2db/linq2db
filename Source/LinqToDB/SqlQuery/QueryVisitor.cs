using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class QueryVisitor
	{
		#region Visit

		readonly Dictionary<IQueryElement,IQueryElement> _visitedElements = new Dictionary<IQueryElement, IQueryElement>();
		public   Dictionary<IQueryElement,IQueryElement>  VisitedElements => _visitedElements;

		bool                     _all;
		Func<IQueryElement,bool> _action1;
		Action<IQueryElement>    _action2;

		public void VisitParentFirst(IQueryElement element, Func<IQueryElement,bool> action)
		{
			_visitedElements.Clear();
			_action1 = action;
			Visit1(element);
		}

		void Visit1(IQueryElement element)
		{
			if (element == null || _visitedElements.ContainsKey(element))
				return;

			_visitedElements.Add(element, element);

			if (!_action1(element))
				return;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction:
					{
						Visit1X((SqlFunction)element);
						break;
					}

				case QueryElementType.SqlExpression:
				{
					Visit1X((SqlExpression)element);
					break;
				}

				case QueryElementType.SqlBinaryExpression:
					{
						Visit1(((SqlBinaryExpression)element).Expr1);
						Visit1(((SqlBinaryExpression)element).Expr2);
						break;
					}

				case QueryElementType.SqlTable:
					{
						Visit1X((SqlTable)element);
						break;
					}

				case QueryElementType.SqlCteTable:
					{
						Visit1X((SqlCteTable)element);
						break;
					}

				case QueryElementType.Column:
					{
						Visit1(((SqlColumn)element).Expression);
						break;
					}

				case QueryElementType.TableSource:
					{
						Visit1X((SqlTableSource)element);
						break;
					}

				case QueryElementType.JoinedTable:
					{
						Visit1(((SqlJoinedTable)element).Table);
						Visit1(((SqlJoinedTable)element).Condition);
						break;
					}

				case QueryElementType.SearchCondition:
					{
						Visit1X((SqlSearchCondition)element);
						break;
					}

				case QueryElementType.Condition:
					{
						Visit1(((SqlCondition)element).Predicate);
						break;
					}

				case QueryElementType.ExprPredicate:
					{
						Visit1(((SqlPredicate.Expr)element).Expr1);
						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						Visit1(((SqlPredicate.NotExpr)element).Expr1);
						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						Visit1(((SqlPredicate.ExprExpr)element).Expr1);
						Visit1(((SqlPredicate.ExprExpr)element).Expr2);
						break;
					}

				case QueryElementType.LikePredicate:
					{
						Visit1(((SqlPredicate.Like)element).Expr1);
						Visit1(((SqlPredicate.Like)element).Expr2);
						Visit1(((SqlPredicate.Like)element).Escape);
						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						Visit1(((SqlPredicate.Between)element).Expr1);
						Visit1(((SqlPredicate.Between)element).Expr2);
						Visit1(((SqlPredicate.Between)element).Expr3);
						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						Visit1(((SqlPredicate.IsNull)element).Expr1);
						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						Visit1(((SqlPredicate.InSubQuery)element).Expr1);
						Visit1(((SqlPredicate.InSubQuery)element).SubQuery);
						break;
					}

				case QueryElementType.InListPredicate:
					{
						Visit1X((SqlPredicate.InList)element);
						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						Visit1(((SqlPredicate.FuncLike)element).Function);
						break;
					}

				case QueryElementType.SetExpression:
					{
						Visit1(((SqlSetExpression)element).Column);
						Visit1(((SqlSetExpression)element).Expression);
						break;
					}

				case QueryElementType.InsertClause:
					{
						Visit1X((SqlInsertClause)element);
						break;
					}

				case QueryElementType.UpdateClause:
					{
						Visit1X((SqlUpdateClause)element);
						break;
					}

				case QueryElementType.CteClause:
					{
						Visit1X((CteClause)element);
						break;
					}

				case QueryElementType.WithClause:
					{
						Visit1X(((SqlWithClause)element));
						break;
					}

				case QueryElementType.SelectStatement:
					{
						Visit1(((SqlSelectStatement)element).With);
						Visit1(((SqlSelectStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.InsertStatement:
					{
						Visit1(((SqlInsertStatement)element).With);
						Visit1(((SqlInsertStatement)element).Insert);
						Visit1(((SqlInsertStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.UpdateStatement:
					{
						Visit1(((SqlUpdateStatement)element).With);
						Visit1(((SqlUpdateStatement)element).Update);
						Visit1(((SqlUpdateStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						Visit1(((SqlInsertOrUpdateStatement)element).With);
						Visit1(((SqlInsertOrUpdateStatement)element).Insert);
						Visit1(((SqlInsertOrUpdateStatement)element).Update);
						Visit1(((SqlInsertOrUpdateStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.DeleteStatement:
					{
						Visit1(((SqlDeleteStatement)element).With);
						Visit1(((SqlDeleteStatement)element).Table);
						Visit1(((SqlDeleteStatement)element).Top);
						Visit1(((SqlDeleteStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.CreateTableStatement:
					{
						if (((SqlCreateTableStatement)element).Table != null)
							Visit1(((SqlCreateTableStatement)element).Table);
						break;
					}

				case QueryElementType.DropTableStatement:
					{
						if (((SqlDropTableStatement)element).Table != null)
							Visit1(((SqlDropTableStatement)element).Table);
						break;
					}

				case QueryElementType.TruncateTableStatement:
					{
						if (((SqlTruncateTableStatement)element).Table != null)
							Visit1(((SqlTruncateTableStatement)element).Table);
						break;
					}

				case QueryElementType.SelectClause:
					{
						Visit1X((SqlSelectClause)element);
						break;
					}

				case QueryElementType.FromClause:
					{
						Visit1X((SqlFromClause)element);
						break;
					}

				case QueryElementType.WhereClause:
					{
						Visit1(((SqlWhereClause)element).SearchCondition);
						break;
					}

				case QueryElementType.GroupByClause:
					{
						Visit1X((SqlGroupByClause)element);
						break;
					}

				case QueryElementType.OrderByClause:
					{
						Visit1X((SqlOrderByClause)element);
						break;
					}

				case QueryElementType.OrderByItem:
					{
						Visit1(((SqlOrderByItem)element).Expression);
						break;
					}

				case QueryElementType.Union:
					{
						Visit1(((SqlUnion)element).SelectQuery);
						break;
					}

				case QueryElementType.SqlQuery:
					{
						if (_all)
						{
							if (_visitedElements.ContainsKey(element))
								return;
							_visitedElements.Add(element, element);
						}

						Visit1X((SelectQuery)element);
						break;
					}
			}
		}

		void Visit1X<T>(IEnumerable<T> elements)
			where T : IQueryElement
		{
			if (elements == null)
				return;
			foreach (var element in elements)
				_action1(element);
		}

		void Visit1X(SelectQuery q)
		{
					Visit1(q.Select);
			Visit1(q.From);
			Visit1(q.Where);
			Visit1(q.GroupBy);
			Visit1(q.Having);
			Visit1(q.OrderBy);

			if (q.HasUnion)
			{
				foreach (var i in q.Unions)
				{
					if (i.SelectQuery == q)
						throw new InvalidOperationException();

					Visit1(i);
				}
			}
		}

		void Visit1X(SqlOrderByClause element)
		{
			foreach (var i in element.Items) Visit1(i);
		}

		void Visit1X(SqlGroupByClause element)
		{
			foreach (var i in element.Items) Visit1(i);
		}

		void Visit1X(SqlFromClause element)
		{
			foreach (var t in element.Tables) Visit1(t);
		}

		void Visit1X(SqlSelectClause sc)
		{
			Visit1(sc.TakeValue);
			Visit1(sc.SkipValue);

			foreach (var c in sc.Columns.ToArray()) Visit1(c);
		}

		void Visit1X(SqlUpdateClause sc)
		{
			if (sc.Table != null)
				Visit1(sc.Table);

			foreach (var c in sc.Items.ToArray()) Visit1(c);
			foreach (var c in sc.Keys. ToArray()) Visit1(c);
		}

		void Visit1X(CteClause sc)
		{
			foreach (var c in sc.Fields) Visit1(c);
			Visit1(sc.Body);
		}

		void Visit1X(SqlInsertClause sc)
		{
			if (sc.Into != null)
				Visit1(sc.Into);

			foreach (var c in sc.Items.ToArray()) Visit1(c);
		}

		void Visit1X(SqlPredicate.InList p)
		{
			Visit1(p.Expr1);
			foreach (var value in p.Values) Visit1(value);
		}

		void Visit1X(SqlSearchCondition element)
		{
			foreach (var c in element.Conditions) Visit1(c);
		}

		void Visit1X(SqlTableSource table)
		{
			Visit1(table.Source);
			foreach (var j in table.Joins) Visit1(j);
		}

		void Visit1X(SqlTable table)
		{
			Visit1(table.All);
			foreach (var field in table.Fields.Values) Visit1(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit1(a);
		}

		void Visit1X(SqlWithClause element)
		{
			foreach (var clause in element.Clauses) Visit1(clause);
		}

		void Visit1X(SqlCteTable table)
		{
			Visit1(table.All);
			foreach (var field in table.Fields.Values) Visit1(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit1(a);

//			Visit1(table.CTE);
		}

		void Visit1X(SqlExpression element)
		{
			foreach (var v in element.Parameters) Visit1(v);
		}

		void Visit1X(SqlFunction element)
		{
			foreach (var p in element.Parameters) Visit1(p);
		}

		public void Visit(IQueryElement element, Action<IQueryElement> action)
		{
			_visitedElements.Clear();
			_all     = false;
			_action2 = action;
			Visit2(element);
		}

		public void VisitAll(IQueryElement element, Action<IQueryElement> action)
		{
			_visitedElements.Clear();
			_all     = true;
			_action2 = action;
			Visit2(element);
		}

		void Visit2(IQueryElement element)
		{
			if (element == null || !_all && _visitedElements.ContainsKey(element))
				return;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction:
					{
						Visit2X((SqlFunction)element);
						break;
					}

				case QueryElementType.SqlExpression:
					{
						Visit2X((SqlExpression)element);
						break;
					}

				case QueryElementType.SqlBinaryExpression:
					{
						Visit2(((SqlBinaryExpression)element).Expr1);
						Visit2(((SqlBinaryExpression)element).Expr2);
						break;
					}

				case QueryElementType.SqlTable:
					{
						Visit2X((SqlTable)element);
						break;
					}

				case QueryElementType.SqlCteTable:
					{
						if (_visitedElements.ContainsKey(element))
							return;
						_visitedElements.Add(element, element);

						Visit2X((SqlCteTable)element);
						break;
					}

				case QueryElementType.Column:
					{
						Visit2(((SqlColumn)element).Expression);
						break;
					}

				case QueryElementType.TableSource:
					{
						Visit2X((SqlTableSource)element);
						break;
					}

				case QueryElementType.JoinedTable:
					{
						Visit2(((SqlJoinedTable)element).Table);
						Visit2(((SqlJoinedTable)element).Condition);
						break;
					}

				case QueryElementType.SearchCondition:
					{
						Visit2X((SqlSearchCondition)element);
						break;
					}

				case QueryElementType.Condition:
					{
						Visit2(((SqlCondition)element).Predicate);
						break;
					}

				case QueryElementType.ExprPredicate:
					{
						Visit2(((SqlPredicate.Expr)element).Expr1);
						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						Visit2(((SqlPredicate.NotExpr)element).Expr1);
						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						Visit2(((SqlPredicate.ExprExpr)element).Expr1);
						Visit2(((SqlPredicate.ExprExpr)element).Expr2);
						break;
					}

				case QueryElementType.LikePredicate:
					{
						Visit2(((SqlPredicate.Like)element).Expr1);
						Visit2(((SqlPredicate.Like)element).Expr2);
						Visit2(((SqlPredicate.Like)element).Escape);
						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						Visit2(((SqlPredicate.Between)element).Expr1);
						Visit2(((SqlPredicate.Between)element).Expr2);
						Visit2(((SqlPredicate.Between)element).Expr3);
						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						Visit2(((SqlPredicate.IsNull)element).Expr1);
						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						Visit2(((SqlPredicate.InSubQuery)element).Expr1);
						Visit2(((SqlPredicate.InSubQuery)element).SubQuery);
						break;
					}

				case QueryElementType.InListPredicate:
					{
						Visit2X((SqlPredicate.InList)element);
						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						Visit2(((SqlPredicate.FuncLike)element).Function);
						break;
					}

				case QueryElementType.SetExpression:
					{
						Visit2(((SqlSetExpression)element).Column);
						Visit2(((SqlSetExpression)element).Expression);
						break;
					}

				case QueryElementType.InsertClause:
					{
						Visit2X((SqlInsertClause)element);
						break;
					}

				case QueryElementType.UpdateClause:
					{
						Visit2X((SqlUpdateClause)element);
						break;
					}

				case QueryElementType.CteClause:
					{
						Visit2X((CteClause)element);
						break;
					}

				case QueryElementType.WithClause:
					{
						Visit2X((SqlWithClause)element);
						break;
					}

				case QueryElementType.SelectStatement:
					{
						Visit2(((SqlSelectStatement)element).With);
						Visit2(((SqlSelectStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.InsertStatement:
					{
						Visit2(((SqlInsertStatement)element).With);
						Visit2(((SqlInsertStatement)element).Insert);
						Visit2(((SqlInsertStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.UpdateStatement:
					{
						Visit2(((SqlUpdateStatement)element).With);
						Visit2(((SqlUpdateStatement)element).Update);
						Visit2(((SqlUpdateStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						Visit2(((SqlInsertOrUpdateStatement)element).With);
						Visit2(((SqlInsertOrUpdateStatement)element).Insert);
						Visit2(((SqlInsertOrUpdateStatement)element).Update);
						Visit2(((SqlInsertOrUpdateStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.DeleteStatement:
					{
						Visit2(((SqlDeleteStatement)element).With);
						Visit2(((SqlDeleteStatement)element).Table);
						Visit2(((SqlDeleteStatement)element).Top);
						Visit2(((SqlDeleteStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.CreateTableStatement:
					{
						if (((SqlCreateTableStatement)element).Table != null)
							Visit2(((SqlCreateTableStatement)element).Table);
						break;
					}

				case QueryElementType.DropTableStatement:
					{
						if (((SqlDropTableStatement)element).Table != null)
							Visit2(((SqlDropTableStatement)element).Table);
						break;
					}

				case QueryElementType.TruncateTableStatement:
					{
						if (((SqlTruncateTableStatement)element).Table != null)
							Visit2(((SqlTruncateTableStatement)element).Table);
						break;
					}

				case QueryElementType.SelectClause:
					{
						if (_visitedElements.ContainsKey(element))
							return;
						_visitedElements.Add(element, element);

						Visit2X((SqlSelectClause)element);
						break;
					}

				case QueryElementType.FromClause:
					{
						Visit2X((SqlFromClause)element);
						break;
					}

				case QueryElementType.WhereClause:
					{
						Visit2(((SqlWhereClause)element).SearchCondition);
						break;
					}

				case QueryElementType.GroupByClause:
					{
						Visit2X((SqlGroupByClause)element);
						break;
					}

				case QueryElementType.OrderByClause:
					{
						Visit2X((SqlOrderByClause)element);
						break;
					}

				case QueryElementType.OrderByItem:
					{
						Visit2(((SqlOrderByItem)element).Expression);
						break;
					}

				case QueryElementType.Union:
					Visit2(((SqlUnion)element).SelectQuery);
					break;

				case QueryElementType.SqlQuery:
					{
						if (_all)
						{
							if (_visitedElements.ContainsKey(element))
								return;
							_visitedElements.Add(element, element);
						}

						Visit2X((SelectQuery)element);

						break;
					}
			}

			_action2(element);

			if (!_all && !_visitedElements.ContainsKey(element))
				_visitedElements.Add(element, element);
		}

		void Visit2X<T>(IEnumerable<T> elements)
			where T : IQueryElement
		{
			if (elements == null)
				return;
			foreach (var element in elements)
				_action2(element);
		}


		void Visit2X(SelectQuery q)
		{
			Visit2(q.Select);

			// Visit2(q.From);
			//
			if (q.From != null && (_all || !_visitedElements.ContainsKey(q.From)))
			{
				foreach (var t in q.From.Tables)
				{
					//Visit2(t);
					//
					if (t != null && (_all || !_visitedElements.ContainsKey(t)))
					{
						Visit2(t.Source);

						foreach (var j in t.Joins)
							Visit2(j);

						_action2(t);
						if (!_all && !_visitedElements.ContainsKey(t))
							_visitedElements.Add(t, t);
					}
				}

				_action2(q.From);
				if (!_all && !_visitedElements.ContainsKey(q.From))
					_visitedElements.Add(q.From, q.From);
			}

			Visit2(q.Where);
			Visit2(q.GroupBy);
			Visit2(q.Having);
			Visit2(q.OrderBy);

			if (q.HasUnion)
			{
				foreach (var i in q.Unions)
				{
					if (i.SelectQuery == q)
						throw new InvalidOperationException();

					Visit2(i);
				}
			}
		}

		void Visit2X(SqlOrderByClause element)
		{
			foreach (var i in element.Items) Visit2(i);
		}

		void Visit2X(SqlGroupByClause element)
		{
			foreach (var i in element.Items) Visit2(i);
		}

		void Visit2X(SqlFromClause element)
		{
			foreach (var t in element.Tables) Visit2(t);
		}

		void Visit2X(SqlSelectClause sc)
		{
			Visit2(sc.TakeValue);
			Visit2(sc.SkipValue);

			foreach (var c in sc.Columns.ToArray()) Visit2(c);
		}

		void Visit2X(SqlUpdateClause sc)
		{
			if (sc.Table != null)
				Visit2(sc.Table);

			foreach (var c in sc.Items.ToArray()) Visit2(c);
			foreach (var c in sc.Keys. ToArray()) Visit2(c);
		}

		void Visit2X(CteClause sc)
		{
			foreach (var c in sc.Fields) Visit2(c);
			Visit2(sc.Body);
		}

		void Visit2X(SqlInsertClause sc)
		{
			if (sc.Into != null)
				Visit2(sc.Into);

			foreach (var c in sc.Items.ToArray()) Visit2(c);
		}

		void Visit2X(SqlPredicate.InList p)
		{
			Visit2(p.Expr1);
			foreach (var value in p.Values) Visit2(value);
		}

		void Visit2X(SqlSearchCondition element)
		{
			foreach (var c in element.Conditions) Visit2(c);
		}

		void Visit2X(SqlTableSource table)
		{
			Visit2(table.Source);
			foreach (var j in table.Joins) Visit2(j);
		}

		void Visit2X(SqlTable table)
		{
			Visit2(table.All);
			foreach (var field in table.Fields.Values) Visit2(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit2(a);
		}

		void Visit2X(SqlWithClause element)
		{
			foreach (var t in element.Clauses) Visit2(t);
		}

		void Visit2X(SqlCteTable table)
		{
			Visit2(table.All);
			foreach (var field in table.Fields.Values) Visit2(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit2(a);

			// do not visit it may fail by stack overflow
			//Visit2(table.CTE);
		}

		void Visit2X(SqlExpression element)
		{
			foreach (var v in element.Parameters) Visit2(v);
		}

		void Visit2X(SqlFunction element)
		{
			foreach (var p in element.Parameters) Visit2(p);
		}

		#endregion

		#region Find

		static IQueryElement Find<T>(IEnumerable<T> arr, Func<IQueryElement,bool> find)
			where T : class, IQueryElement
		{
			if (arr == null)
				return null;

			foreach (var item in arr)
			{
				var e = Find(item, find);
				if (e != null)
					return e;
			}

			return null;
		}

		static IQueryElement FindX(SqlSearchCondition sc, Func<IQueryElement,bool> find)
		{
			if (sc.Conditions == null)
				return null;

			foreach (var item in sc.Conditions)
			{
				var e = Find(item.Predicate, find);
				if (e != null)
					return e;
			}

			return null;
		}

		public static IQueryElement Find(IQueryElement element, Func<IQueryElement,bool> find)
		{
			if (element == null || find(element))
				return element;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction       : return Find(((SqlFunction)          element).Parameters,      find);
				case QueryElementType.SqlExpression     : return Find(((SqlExpression)        element).Parameters,      find);
				case QueryElementType.Column            : return Find(((SqlColumn)            element).Expression,      find);
				case QueryElementType.SearchCondition   : return FindX((SqlSearchCondition)   element,                  find);
				case QueryElementType.Condition         : return Find(((SqlCondition)         element).Predicate,       find);
				case QueryElementType.ExprPredicate     : return Find(((SqlPredicate.Expr)    element).Expr1,           find);
				case QueryElementType.NotExprPredicate  : return Find(((SqlPredicate.NotExpr) element).Expr1,           find);
				case QueryElementType.IsNullPredicate   : return Find(((SqlPredicate.IsNull)  element).Expr1,           find);
				case QueryElementType.FromClause        : return Find(((SqlFromClause)        element).Tables,          find);
				case QueryElementType.WhereClause       : return Find(((SqlWhereClause)       element).SearchCondition, find);
				case QueryElementType.GroupByClause     : return Find(((SqlGroupByClause)     element).Items,           find);
				case QueryElementType.OrderByClause     : return Find(((SqlOrderByClause)     element).Items,           find);
				case QueryElementType.OrderByItem       : return Find(((SqlOrderByItem)       element).Expression,      find);
				case QueryElementType.Union             : return Find(((SqlUnion)             element).SelectQuery,     find);
				case QueryElementType.FuncLikePredicate : return Find(((SqlPredicate.FuncLike)element).Function,        find);

				case QueryElementType.SqlBinaryExpression:
					{
						return
							Find(((SqlBinaryExpression)element).Expr1, find) ??
							Find(((SqlBinaryExpression)element).Expr2, find);
					}

				case QueryElementType.SqlTable:
					{
						return
							Find(((SqlTable)element).All,            find) ??
							Find(((SqlTable)element).Fields.Values,  find) ??
							Find(((SqlTable)element).TableArguments, find);
					}

				case QueryElementType.SqlCteTable:
					{
						return
							Find(((SqlCteTable)element).All,            find) ??
							Find(((SqlCteTable)element).Fields.Values,  find) ??
							Find(((SqlCteTable)element).TableArguments, find) ??
							Find(((SqlCteTable)element).Cte, find);
					}

				case QueryElementType.TableSource:
					{
						return
							Find(((SqlTableSource)element).Source, find) ??
							Find(((SqlTableSource)element).Joins,  find);
					}

				case QueryElementType.JoinedTable:
					{
						return
							Find(((SqlJoinedTable)element).Table,     find) ??
							Find(((SqlJoinedTable)element).Condition, find);
					}

				case QueryElementType.ExprExprPredicate:
					{
						return
							Find(((SqlPredicate.ExprExpr)element).Expr1, find) ??
							Find(((SqlPredicate.ExprExpr)element).Expr2, find);
					}

				case QueryElementType.LikePredicate:
					{
						return
							Find(((SqlPredicate.Like)element).Expr1,  find) ??
							Find(((SqlPredicate.Like)element).Expr2,  find) ??
							Find(((SqlPredicate.Like)element).Escape, find);
					}

				case QueryElementType.BetweenPredicate:
					{
						return
							Find(((SqlPredicate.Between)element).Expr1, find) ??
							Find(((SqlPredicate.Between)element).Expr2, find) ??
							Find(((SqlPredicate.Between)element).Expr3, find);
					}

				case QueryElementType.InSubQueryPredicate:
					{
						return
							Find(((SqlPredicate.InSubQuery)element).Expr1,    find) ??
							Find(((SqlPredicate.InSubQuery)element).SubQuery, find);
					}

				case QueryElementType.InListPredicate:
					{
						return
							Find(((SqlPredicate.InList)element).Expr1,  find) ??
							Find(((SqlPredicate.InList)element).Values, find);
					}

				case QueryElementType.SetExpression:
					{
						return
							Find(((SqlSetExpression)element).Column,     find) ??
							Find(((SqlSetExpression)element).Expression, find);
					}

				case QueryElementType.InsertClause:
					{
						return
							Find(((SqlInsertClause)element).Into,  find) ??
							Find(((SqlInsertClause)element).Items, find);
					}

				case QueryElementType.UpdateClause:
					{
						return
							Find(((SqlUpdateClause)element).Table, find) ??
							Find(((SqlUpdateClause)element).Items, find) ??
							Find(((SqlUpdateClause)element).Keys,  find);
					}

				case QueryElementType.DeleteStatement:
					{
						return
							Find(((SqlDeleteStatement)element).Table, find) ??
							Find(((SqlDeleteStatement)element).Top,   find) ??
							Find(((SqlDeleteStatement)element).SelectQuery, find);
					}

				case QueryElementType.CreateTableStatement:
					{
						return
							Find(((SqlCreateTableStatement)element).Table, find);
					}

				case QueryElementType.DropTableStatement:
					{
						return
							Find(((SqlCreateTableStatement)element).Table, find);
					}

				case QueryElementType.SelectClause:
					{
						return
							Find(((SqlSelectClause)element).TakeValue, find) ??
							Find(((SqlSelectClause)element).SkipValue, find) ??
							Find(((SqlSelectClause)element).Columns,   find);
					}

				case QueryElementType.SqlQuery:
					{
						return
							Find(((SelectQuery)element).Select,  find) ??
							Find(((SelectQuery)element).From,    find) ??
							Find(((SelectQuery)element).Where,   find) ??
							Find(((SelectQuery)element).GroupBy, find) ??
							Find(((SelectQuery)element).Having,  find) ??
							Find(((SelectQuery)element).OrderBy, find) ??
							(((SelectQuery)element).HasUnion ? Find(((SelectQuery)element).Unions, find) : null);
					}
			}

			return null;
		}

		#endregion

		#region Convert

		public T Convert<T>(T element, Func<IQueryElement,IQueryElement> action)
			where T : class, IQueryElement
		{
			_visitedElements.Clear();
			return (T)ConvertInternal(element, action) ?? element;
		}

		IQueryElement ConvertInternal(IQueryElement element, Func<IQueryElement,IQueryElement> action)
		{
			if (element == null)
				return null;

			IQueryElement parent;

			if (_visitedElements.TryGetValue(element, out var newElement))
				return newElement;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction:
					{
						var func  = (SqlFunction)element;
						var parms = Convert(func.Parameters, action);

						if (parms != null && !ReferenceEquals(parms, func.Parameters))
							newElement = new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, parms);

						break;
					}

				case QueryElementType.SqlExpression:
					{
						var expr      = (SqlExpression)element;
						var parameter = Convert(expr.Parameters, action);

						if (parameter != null && !ReferenceEquals(parameter, expr.Parameters))
							newElement = new SqlExpression(expr.SystemType, expr.Expr, expr.Precedence, parameter);

						break;
					}

				case QueryElementType.SqlBinaryExpression:
					{
						var bexpr = (SqlBinaryExpression)element;
						var expr1 = (ISqlExpression)ConvertInternal(bexpr.Expr1, action);
						var expr2 = (ISqlExpression)ConvertInternal(bexpr.Expr2, action);

						if (expr1 != null && !ReferenceEquals(expr1, bexpr.Expr1) ||
							expr2 != null && !ReferenceEquals(expr2, bexpr.Expr2))
							newElement = new SqlBinaryExpression(bexpr.SystemType, expr1 ?? bexpr.Expr1, bexpr.Operation, expr2 ?? bexpr.Expr2, bexpr.Precedence);

						break;
					}

				case QueryElementType.SqlTable:
					{
						var table   = (SqlTable)element;
						var fields1 = ToArray(table.Fields);
						var fields2 = Convert(fields1, action, f => new SqlField(f));
						var targs   = table.TableArguments == null || table.TableArguments.Length == 0 ?
							null : Convert(table.TableArguments, action);

						var fe = fields2 != null && !ReferenceEquals(fields1, fields2);
						var ta = targs   != null && !ReferenceEquals(table.TableArguments, targs);

						if (fe || ta)
						{
							if (!fe)
							{
								fields2 = fields1;

								for (var i = 0; i < fields2.Length; i++)
								{
									var field = fields2[i];

									fields2[i] = new SqlField(field);

									_visitedElements[field] = fields2[i];
								}
							}

							newElement = new SqlTable(table, fields2, targs ?? table.TableArguments);

							_visitedElements[((SqlTable)newElement).All] = table.All;
						}

						break;
					}

				case QueryElementType.SqlCteTable:
					{
						var table   = (SqlCteTable)element;
						var fields1 = ToArray(table.Fields);
						var fields2 = Convert(fields1,     action, f => new SqlField(f));
						var targs   = table.TableArguments == null || table.TableArguments.Length == 0 ?
							null : Convert(table.TableArguments, action);
						var cte     = Convert(table.Cte, action);

						var fe = fields2 != null && !ReferenceEquals(fields1, fields2);
						var ta = targs   != null && !ReferenceEquals(table.TableArguments, targs);
						var ce = cte     != null && !ReferenceEquals(table.Cte, cte);

						if (fe || ta || ce)
						{
							if (!fe)
							{
								fields2 = fields1;

								for (var i = 0; i < fields2.Length; i++)
								{
									var field = fields2[i];

									fields2[i] = new SqlField(field);

									_visitedElements[field] = fields2[i];
								}
							}

							newElement = new SqlCteTable(table, fields2, cte);

							_visitedElements[((SqlCteTable)newElement).All] = table.All;
						}

						break;
					}

				case QueryElementType.Column:
					{
						var col  = (SqlColumn)element;
						var expr = (ISqlExpression)ConvertInternal(col.Expression, action);

						_visitedElements.TryGetValue(col.Parent, out parent);

						if (parent != null || expr != null && !ReferenceEquals(expr, col.Expression))
							newElement = new SqlColumn(parent == null ? col.Parent : (SelectQuery)parent, expr ?? col.Expression, col.RawAlias);

						break;
					}

				case QueryElementType.TableSource:
					{
						var table  = (SqlTableSource)element;
						var source = (ISqlTableSource)ConvertInternal(table.Source, action);
						var joins  = Convert(table.Joins, action);

						if (source != null && !ReferenceEquals(source, table.Source) ||
							joins  != null && !ReferenceEquals(table.Joins, joins))
							newElement = new SqlTableSource(source ?? table.Source, table._alias, joins ?? table.Joins);

						break;
					}

				case QueryElementType.JoinedTable:
					{
						var join  = (SqlJoinedTable)element;
						var table = (SqlTableSource)    ConvertInternal(join.Table,     action);
						var cond  = (SqlSearchCondition)ConvertInternal(join.Condition, action);

						if (table != null && !ReferenceEquals(table, join.Table) ||
							cond  != null && !ReferenceEquals(cond,  join.Condition))
							newElement = new SqlJoinedTable(join.JoinType, table ?? join.Table, join.IsWeak, cond ?? join.Condition);

						break;
					}

				case QueryElementType.SearchCondition:
					{
						var sc    = (SqlSearchCondition)element;
						var conds = Convert(sc.Conditions, action);

						if (conds != null && !ReferenceEquals(sc.Conditions, conds))
							newElement = new SqlSearchCondition(conds);

						break;
					}

				case QueryElementType.Condition:
					{
						var c = (SqlCondition)element;
						var p = (ISqlPredicate)ConvertInternal(c.Predicate, action);

						if (p != null && !ReferenceEquals(c.Predicate, p))
							newElement = new SqlCondition(c.IsNot, p, c.IsOr);

						break;
					}

				case QueryElementType.ExprPredicate:
					{
						var p = (SqlPredicate.Expr)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1, action);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlPredicate.Expr(e, p.Precedence);

						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						var p = (SqlPredicate.NotExpr)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1, action);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlPredicate.NotExpr(e, p.IsNot, p.Precedence);

						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						var p  = (SqlPredicate.ExprExpr)element;
						var e1 = (ISqlExpression)ConvertInternal(p.Expr1, action);
						var e2 = (ISqlExpression)ConvertInternal(p.Expr2, action);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) || e2 != null && !ReferenceEquals(p.Expr2, e2))
							newElement = new SqlPredicate.ExprExpr(e1 ?? p.Expr1, p.Operator, e2 ?? p.Expr2);

						break;
					}

				case QueryElementType.LikePredicate:
					{
						var p  = (SqlPredicate.Like)element;
						var e1 = (ISqlExpression)ConvertInternal(p.Expr1,  action);
						var e2 = (ISqlExpression)ConvertInternal(p.Expr2,  action);
						var es = (ISqlExpression)ConvertInternal(p.Escape, action);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							es != null && !ReferenceEquals(p.Escape, es))
							newElement = new SqlPredicate.Like(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, es ?? p.Escape);

						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						var p = (SqlPredicate.Between)element;
						var e1 = (ISqlExpression)ConvertInternal(p.Expr1, action);
						var e2 = (ISqlExpression)ConvertInternal(p.Expr2, action);
						var e3 = (ISqlExpression)ConvertInternal(p.Expr3, action);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							e3 != null && !ReferenceEquals(p.Expr3, e3))
							newElement = new SqlPredicate.Between(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, e3 ?? p.Expr3);

						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						var p = (SqlPredicate.IsNull)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1, action);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlPredicate.IsNull(e, p.IsNot);

						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						var p = (SqlPredicate.InSubQuery)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1,    action);
						var q = (SelectQuery)ConvertInternal(p.SubQuery, action);

						if (e != null && !ReferenceEquals(p.Expr1, e) || q != null && !ReferenceEquals(p.SubQuery, q))
							newElement = new SqlPredicate.InSubQuery(e ?? p.Expr1, p.IsNot, q ?? p.SubQuery);

						break;
					}

				case QueryElementType.InListPredicate:
					{
						var p = (SqlPredicate.InList)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1,    action);
						var v = Convert(p.Values, action);

						if (e != null && !ReferenceEquals(p.Expr1, e) || v != null && !ReferenceEquals(p.Values, v))
							newElement = new SqlPredicate.InList(e ?? p.Expr1, p.IsNot, v ?? p.Values);

						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						var p = (SqlPredicate.FuncLike)element;
						var f = (SqlFunction)ConvertInternal(p.Function, action);

						if (f != null && !ReferenceEquals(p.Function, f))
							newElement = new SqlPredicate.FuncLike(f);

						break;
					}

				case QueryElementType.SetExpression:
					{
						var s = (SqlSetExpression)element;
						var c = (ISqlExpression)ConvertInternal(s.Column,     action);
						var e = (ISqlExpression)ConvertInternal(s.Expression, action);

						if (c != null && !ReferenceEquals(s.Column, c) || e != null && !ReferenceEquals(s.Expression, e))
							newElement = new SqlSetExpression(c ?? s.Column, e ?? s.Expression);

						break;
					}

				case QueryElementType.InsertClause:
					{
						var s = (SqlInsertClause)element;
						var t = s.Into != null ? (SqlTable)ConvertInternal(s.Into, action) : null;
						var i = Convert(s.Items, action);

						if (t != null && !ReferenceEquals(s.Into, t) || i != null && !ReferenceEquals(s.Items, i))
						{
							var sc = new SqlInsertClause { Into = t ?? s.Into };

							sc.Items.AddRange(i ?? s.Items);
							sc.WithIdentity = s.WithIdentity;

							newElement = sc;
						}

						break;
					}

				case QueryElementType.UpdateClause:
					{
						var s = (SqlUpdateClause)element;
						var t = s.Table != null ? (SqlTable)ConvertInternal(s.Table, action) : null;
						var i = Convert(s.Items, action);
						var k = Convert(s.Keys,  action);

						if (t != null && !ReferenceEquals(s.Table, t) ||
							i != null && !ReferenceEquals(s.Items, i) ||
							k != null && !ReferenceEquals(s.Keys,  k))
						{
							var sc = new SqlUpdateClause { Table = t ?? s.Table };

							sc.Items.AddRange(i ?? s.Items);
							sc.Keys. AddRange(k ?? s.Keys);

							newElement = sc;
						}

						break;
					}

				case QueryElementType.SelectStatement:
					{
						var s = (SqlSelectStatement)element;
						var selectQuery = s.SelectQuery != null ? (SelectQuery) ConvertInternal(s.SelectQuery, action) : null;
						var ps          = ConvertSafe(s.Parameters, action);

						if (ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery))
					{
							newElement = new SqlSelectStatement(selectQuery ?? s.SelectQuery);
							if (ps != null)
								((SqlSelectStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlSelectStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.InsertStatement:
					{
						var s = (SqlInsertStatement)element;
						var selectQuery = s.SelectQuery != null ? (SelectQuery)    ConvertInternal(s.SelectQuery, action) : null;
						var insert      = s.Insert      != null ? (SqlInsertClause)ConvertInternal(s.Insert,      action) : null;
						var ps          = ConvertSafe(s.Parameters, action);

						if (insert      != null && !ReferenceEquals(s.Insert,      insert)       ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery))
					{
							newElement = new SqlInsertStatement(selectQuery ?? s.SelectQuery) { Insert = insert ?? s.Insert };
							if (ps != null)
								((SqlInsertStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlInsertStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.UpdateStatement:
					{
						var s = (SqlUpdateStatement)element;
						var update      = s.Update      != null ? (SqlUpdateClause)ConvertInternal(s.Update, action) : null;
						var selectQuery = s.SelectQuery != null ? (SelectQuery)    ConvertInternal(s.SelectQuery, action) : null;
						var ps          = ConvertSafe(s.Parameters, action);

						if (update      != null && !ReferenceEquals(s.Update,      update)       ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery))
						{
							newElement = new SqlUpdateStatement(selectQuery ?? s.SelectQuery) { Update = update ?? s.Update };
							if (ps != null)
								((SqlUpdateStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlUpdateStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						var s = (SqlInsertOrUpdateStatement)element;

						var insert      = s.Insert      != null ? (SqlInsertClause)ConvertInternal(s.Insert, action) : null;
						var update      = s.Update      != null ? (SqlUpdateClause)ConvertInternal(s.Update, action) : null;
						var selectQuery = s.SelectQuery != null ? (SelectQuery)    ConvertInternal(s.SelectQuery, action) : null;
						var ps          = ConvertSafe(s.Parameters, action);

						if (insert      != null && !ReferenceEquals(s.Insert,      insert)       ||
							update      != null && !ReferenceEquals(s.Update,      update)       ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery))
						{
							newElement = new SqlInsertOrUpdateStatement(selectQuery ?? s.SelectQuery) { Insert = insert ?? s.Insert, Update = update ?? s.Update };
							if (ps != null)
								((SqlInsertOrUpdateStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlInsertOrUpdateStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.DeleteStatement:
					{
						var s = (SqlDeleteStatement)element;
						var table       = s.Table != null ? (SqlTable)         ConvertInternal(s.Table, action) : null;
						var top         = s.Top   != null ? (ISqlExpression)   ConvertInternal(s.Top,  action) : null;
						var selectQuery = s.SelectQuery != null ? (SelectQuery)ConvertInternal(s.SelectQuery, action) : null;
						var ps          = ConvertSafe(s.Parameters, action);

						if (table       != null && !ReferenceEquals(s.Table,       table)       ||
							top         != null && !ReferenceEquals(s.Top,         top)         ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)          ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery))
						{
							newElement = new SqlDeleteStatement
							{
								Table                = table       ?? s.Table,
								SelectQuery          = selectQuery ?? s.SelectQuery,
								Top                  = top         ?? s.Top,
								IsParameterDependent = s.IsParameterDependent
							};
							if (ps != null)
								((SqlDeleteStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlDeleteStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.CreateTableStatement:
					{
						var s  = (SqlCreateTableStatement)element;
						var t  = s.Table != null ? (SqlTable)ConvertInternal(s.Table, action) : null;
						var ps = ConvertSafe(s.Parameters, action);

						if (t  != null && !ReferenceEquals(s.Table, t) ||
							ps != null && !ReferenceEquals(s.Parameters,  ps))
						{
							newElement = new SqlCreateTableStatement { Table = t ?? s.Table };
							if (ps != null)
								((SqlCreateTableStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlCreateTableStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.DropTableStatement:
					{
						var s  = (SqlCreateTableStatement)element;
						var t  = s.Table != null ? (SqlTable)ConvertInternal(s.Table, action) : null;
						var ps = ConvertSafe(s.Parameters, action);

						if (t  != null && !ReferenceEquals(s.Table, t) ||
							ps != null && !ReferenceEquals(s.Parameters,  ps))
						{
							newElement = new SqlDropTableStatement { Table = t ?? s.Table };
							if (ps != null)
								((SqlDropTableStatement)newElement).Parameters.AddRange(ps);
							else
								((SqlDropTableStatement)newElement).Parameters.AddRange(s.Parameters);
						}

						break;
					}

				case QueryElementType.SelectClause:
					{
						var sc   = (SqlSelectClause)element;
						var cols = Convert(sc.Columns, action);
						var take = (ISqlExpression)ConvertInternal(sc.TakeValue, action);
						var skip = (ISqlExpression)ConvertInternal(sc.SkipValue, action);

						_visitedElements.TryGetValue(sc.SelectQuery, out parent);

						if (parent != null ||
							cols != null && !ReferenceEquals(sc.Columns,   cols) ||
							take != null && !ReferenceEquals(sc.TakeValue, take) ||
							skip != null && !ReferenceEquals(sc.SkipValue, skip))
						{
							newElement = new SqlSelectClause(sc.IsDistinct, take ?? sc.TakeValue, skip ?? sc.SkipValue, cols ?? sc.Columns);
							((SqlSelectClause)newElement).SetSqlQuery((SelectQuery)parent);
						}

						break;
					}

				case QueryElementType.FromClause:
					{
						var fc   = (SqlFromClause)element;
						var ts = Convert(fc.Tables, action);

						_visitedElements.TryGetValue(fc.SelectQuery, out parent);

						if (parent != null || ts != null && !ReferenceEquals(fc.Tables, ts))
						{
							newElement = new SqlFromClause(ts ?? fc.Tables);
							((SqlFromClause)newElement).SetSqlQuery((SelectQuery)parent);
						}

						break;
					}

				case QueryElementType.WhereClause:
					{
						var wc   = (SqlWhereClause)element;
						var cond = (SqlSearchCondition)ConvertInternal(wc.SearchCondition, action);

						_visitedElements.TryGetValue(wc.SelectQuery, out parent);

						if (parent != null || cond != null && !ReferenceEquals(wc.SearchCondition, cond))
						{
							newElement = new SqlWhereClause(cond ?? wc.SearchCondition);
							((SqlWhereClause)newElement).SetSqlQuery((SelectQuery)parent);
						}

						break;
					}

				case QueryElementType.GroupByClause:
					{
						var gc = (SqlGroupByClause)element;
						var es = Convert(gc.Items, action);

						_visitedElements.TryGetValue(gc.SelectQuery, out parent);

						if (parent != null || es != null && !ReferenceEquals(gc.Items, es))
						{
							newElement = new SqlGroupByClause(es ?? gc.Items);
							((SqlGroupByClause)newElement).SetSqlQuery((SelectQuery)parent);
						}

						break;
					}

				case QueryElementType.OrderByClause:
					{
						var oc = (SqlOrderByClause)element;
						var es = Convert(oc.Items, action);

						_visitedElements.TryGetValue(oc.SelectQuery, out parent);

						if (parent != null || es != null && !ReferenceEquals(oc.Items, es))
						{
							newElement = new SqlOrderByClause(es ?? oc.Items);
							((SqlOrderByClause)newElement).SetSqlQuery((SelectQuery)parent);
						}

						break;
					}

				case QueryElementType.OrderByItem:
					{
						var i = (SqlOrderByItem)element;
						var e = (ISqlExpression)ConvertInternal(i.Expression, action);

						if (e != null && !ReferenceEquals(i.Expression, e))
							newElement = new SqlOrderByItem(e, i.IsDescending);

						break;
					}

				case QueryElementType.Union:
					{
						var u = (SqlUnion)element;
						var q = (SelectQuery)ConvertInternal(u.SelectQuery, action);

						if (q != null && !ReferenceEquals(u.SelectQuery, q))
							newElement = new SqlUnion(q, u.IsAll);

						break;
					}

				case QueryElementType.SqlQuery:
					{
						var q = (SelectQuery)element;

						parent = null;

						var doConvert = false;

						if (q.ParentSelect != null)
						{
							if (!_visitedElements.TryGetValue(q.ParentSelect, out parent))
							{
								doConvert = true;
								parent    = q.ParentSelect; // TODO why not ConvertInternal(q.ParentSelect, action)??
							}
							else
								doConvert = !ReferenceEquals(q.ParentSelect, parent);
						}

						if (!doConvert)
						{
							doConvert = null != Find(q, e =>
							{
								if (_visitedElements.TryGetValue(e, out var ve) && ve != null && ve != e)
									return true;

								var ret = action(e);

								if (ret != null && !ReferenceEquals(e, ret))
								{
									_visitedElements.Add(e, ret);
									return true;
								}

								return false;
							});
						}

						if (!doConvert)
							break;

						var nq = new SelectQuery();

						_visitedElements.Add(q,     nq);
						_visitedElements.Add(q.All, nq.All);

						var fc = (SqlFromClause)   ConvertInternal(q.From,    action) ?? q.From;
						var sc = (SqlSelectClause) ConvertInternal(q.Select,  action) ?? q.Select;
						var wc = (SqlWhereClause)  ConvertInternal(q.Where,   action) ?? q.Where;
						var gc = (SqlGroupByClause)ConvertInternal(q.GroupBy, action) ?? q.GroupBy;
						var hc = (SqlWhereClause)  ConvertInternal(q.Having,  action) ?? q.Having;
						var oc = (SqlOrderByClause)ConvertInternal(q.OrderBy, action) ?? q.OrderBy;
						var us = q.HasUnion ? Convert(q.Unions, action) : q.Unions;

						nq.Init(sc, fc, wc, gc, hc, oc, us,
							(SelectQuery)parent,
							q.IsParameterDependent);

						_visitedElements[q] = action(nq) ?? nq;

						return nq;
					}
			}

			newElement = newElement == null ? action(element) : (action(newElement) ?? newElement);

			_visitedElements.Add(element, newElement);

			return newElement;
		}

		static TE[] ToArray<TK,TE>(IDictionary<TK,TE> dic)
		{
			var es = new TE[dic.Count];
			var i  = 0;

			foreach (var e in dic.Values)
				es[i++] = e;

			return es;
		}

		delegate T Clone<T>(T obj);

		T[] Convert<T>(T[] arr, Func<IQueryElement, IQueryElement> action)
			where T : class, IQueryElement
		{
			return Convert(arr, action, null);
		}

		T[] Convert<T>(T[] arr1, Func<IQueryElement, IQueryElement> action, Clone<T> clone)
			where T : class, IQueryElement
		{
			T[] arr2 = null;

			for (var i = 0; i < arr1.Length; i++)
			{
				var elem1 = arr1[i];
				var elem2 = (T)ConvertInternal(elem1, action);

				if (elem2 != null && !ReferenceEquals(elem1, elem2))
				{
					if (arr2 == null)
					{
						arr2 = new T[arr1.Length];

						for (var j = 0; j < i; j++)
							arr2[j] = clone == null ? arr1[j] : clone(arr1[j]);
					}

					arr2[i] = elem2;
				}
				else if (arr2 != null)
					arr2[i] = clone == null ? elem1 : clone(elem1);
			}

			return arr2;
		}

		List<T> ConvertSafe<T>(List<T> list, Func<IQueryElement, IQueryElement> action)
			where T : class, IQueryElement
		{
			return ConvertSafe(list, action, null);
		}

		List<T> ConvertSafe<T>(List<T> list1, Func<IQueryElement, IQueryElement> action, Clone<T> clone)
			where T : class, IQueryElement
		{
			List<T> list2 = null;

			for (var i = 0; i < list1.Count; i++)
			{
				var elem1 = list1[i];
				var elem2 = ConvertInternal(elem1, action) as T;

				if (elem2 != null && !ReferenceEquals(elem1, elem2))
				{
					if (list2 == null)
					{
						list2 = new List<T>(list1.Count);

						for (var j = 0; j < i; j++)
							list2.Add(clone == null ? list1[j] : clone(list1[j]));
					}

					list2.Add(elem2);
				}
				else
					list2?.Add(clone == null ? elem1 : clone(elem1));
			}

			return list2;
		}

		List<T> Convert<T>(List<T> list, Func<IQueryElement, IQueryElement> action)
			where T : class, IQueryElement
		{
			return Convert(list, action, null);
		}

		List<T> Convert<T>(List<T> list1, Func<IQueryElement, IQueryElement> action, Clone<T> clone)
			where T : class, IQueryElement
		{
			List<T> list2 = null;

			for (var i = 0; i < list1.Count; i++)
			{
				var elem1 = list1[i];
				var elem2 = (T)ConvertInternal(elem1, action);

				if (elem2 != null && !ReferenceEquals(elem1, elem2))
				{
					if (list2 == null)
					{
						list2 = new List<T>(list1.Count);

						for (var j = 0; j < i; j++)
							list2.Add(clone == null ? list1[j] : clone(list1[j]));
					}

					list2.Add(elem2);
				}
				else
					list2?.Add(clone == null ? elem1 : clone(elem1));
			}

			return list2;
		}

		#endregion
	}
}
