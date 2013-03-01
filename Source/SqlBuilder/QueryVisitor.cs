using System;
using System.Collections.Generic;

namespace LinqToDB.SqlBuilder
{
	public class QueryVisitor
	{
		#region Visit

		readonly Dictionary<IQueryElement,IQueryElement> _visitedElements = new Dictionary<IQueryElement, IQueryElement>();
		public   Dictionary<IQueryElement,IQueryElement>  VisitedElements
		{
			get { return _visitedElements; }
		}

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
						foreach (var p in ((SqlFunction)element).Parameters) Visit1(p);
						break;
					}

				case QueryElementType.SqlExpression:
					{
						foreach (var v in ((SqlExpression)element).Parameters) Visit1(v);
						break;
					}

				case QueryElementType.SqlBinaryExpression:
					{
						//var bexpr = (SqlBinaryExpression)element;
						Visit1(((SqlBinaryExpression)element).Expr1);
						Visit1(((SqlBinaryExpression)element).Expr2);
						break;
					}

				case QueryElementType.SqlTable:
					{
						var table = (SqlTable)element;

						Visit1(table.All);
						foreach (var field in table.Fields.Values) Visit1(field);

						if (table.TableArguments != null)
							foreach (var a in table.TableArguments) Visit1(a);

						break;
					}

				case QueryElementType.Column:
					{
						Visit1(((SqlQuery.Column)element).Expression);
						break;
					}

				case QueryElementType.TableSource:
					{
						//var table = ((SqlQuery.TableSource)element);

						Visit1(((SqlQuery.TableSource)element).Source);
						foreach (var j in ((SqlQuery.TableSource)element).Joins) Visit1(j);
						break;
					}

				case QueryElementType.JoinedTable:
					{
						//var join = (SqlQuery.JoinedTable)element;
						Visit1(((SqlQuery.JoinedTable)element).Table);
						Visit1(((SqlQuery.JoinedTable)element).Condition);
						break;
					}

				case QueryElementType.SearchCondition:
					{
						foreach (var c in ((SqlQuery.SearchCondition)element).Conditions) Visit1(c);
						break;
					}

				case QueryElementType.Condition:
					{
						Visit1(((SqlQuery.Condition)element).Predicate);
						break;
					}

				case QueryElementType.ExprPredicate:
					{
						Visit1(((SqlQuery.Predicate.Expr)element).Expr1);
						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						Visit1(((SqlQuery.Predicate.NotExpr)element).Expr1);
						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						//var p = ((SqlQuery.Predicate.ExprExpr)element);
						Visit1(((SqlQuery.Predicate.ExprExpr)element).Expr1);
						Visit1(((SqlQuery.Predicate.ExprExpr)element).Expr2);
						break;
					}

				case QueryElementType.LikePredicate:
					{
						//var p = ((SqlQuery.Predicate.Like)element);
						Visit1(((SqlQuery.Predicate.Like)element).Expr1);
						Visit1(((SqlQuery.Predicate.Like)element).Expr2);
						Visit1(((SqlQuery.Predicate.Like)element).Escape);
						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						//var p = (SqlQuery.Predicate.Between)element;
						Visit1(((SqlQuery.Predicate.Between)element).Expr1);
						Visit1(((SqlQuery.Predicate.Between)element).Expr2);
						Visit1(((SqlQuery.Predicate.Between)element).Expr3);
						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						Visit1(((SqlQuery.Predicate.IsNull)element).Expr1);
						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						//var p = (SqlQuery.Predicate.InSubQuery)element;
						Visit1(((SqlQuery.Predicate.InSubQuery)element).Expr1);
						Visit1(((SqlQuery.Predicate.InSubQuery)element).SubQuery);
						break;
					}

				case QueryElementType.InListPredicate:
					{
						//var p = (SqlQuery.Predicate.InList)element;
						Visit1(((SqlQuery.Predicate.InList)element).Expr1);
						foreach (var value in ((SqlQuery.Predicate.InList)element).Values) Visit1(value);
						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						Visit1(((SqlQuery.Predicate.FuncLike)element).Function);
						break;
					}

				case QueryElementType.SetExpression:
					{
						//var s = (SqlQuery.SetExpression)element;
						Visit1(((SqlQuery.SetExpression)element).Column);
						Visit1(((SqlQuery.SetExpression)element).Expression);
						break;
					}

				case QueryElementType.InsertClause:
					{
						//var sc = (SqlQuery.InsertClause)element;

						if (((SqlQuery.InsertClause)element).Into != null)
							Visit1(((SqlQuery.InsertClause)element).Into);

						foreach (var c in ((SqlQuery.InsertClause)element).Items.ToArray()) Visit1(c);
						break;
					}

				case QueryElementType.UpdateClause:
					{
						//var sc = (SqlQuery.UpdateClause)element;

						if (((SqlQuery.UpdateClause)element).Table != null)
							Visit1(((SqlQuery.UpdateClause)element).Table);

						foreach (var c in ((SqlQuery.UpdateClause)element).Items.ToArray()) Visit1(c);
						foreach (var c in ((SqlQuery.UpdateClause)element).Keys. ToArray()) Visit1(c);
						break;
					}

				case QueryElementType.DeleteClause:
					{
						if (((SqlQuery.DeleteClause)element).Table != null)
							Visit1(((SqlQuery.DeleteClause)element).Table);
						break;
					}

				case QueryElementType.SelectClause:
					{
						//var sc = (SqlQuery.SelectClause)element;
						Visit1(((SqlQuery.SelectClause)element).TakeValue);
						Visit1(((SqlQuery.SelectClause)element).SkipValue);

						foreach (var c in ((SqlQuery.SelectClause)element).Columns.ToArray()) Visit1(c);
						break;
					}

				case QueryElementType.FromClause:
					{
						foreach (var t in ((SqlQuery.FromClause)element).Tables) Visit1(t);
						break;
					}

				case QueryElementType.WhereClause:
					{
						Visit1(((SqlQuery.WhereClause)element).SearchCondition);
						break;
					}

				case QueryElementType.GroupByClause:
					{
						foreach (var i in ((SqlQuery.GroupByClause)element).Items) Visit1(i);
						break;
					}

				case QueryElementType.OrderByClause:
					{
						foreach (var i in ((SqlQuery.OrderByClause)element).Items) Visit1(i);
						break;
					}

				case QueryElementType.OrderByItem:
					{
						Visit1(((SqlQuery.OrderByItem)element).Expression);
						break;
					}

				case QueryElementType.Union:
					Visit1(((SqlQuery.Union)element).SqlQuery);
					break;

				case QueryElementType.SqlQuery:
					{
						if (_all)
						{
							if (_visitedElements.ContainsKey(element))
								return;
							_visitedElements.Add(element, element);
						}

						var q = (SqlQuery)element;

						switch (q.QueryType)
						{
							case QueryType.InsertOrUpdate :
								Visit1(q.Insert);
								Visit1(q.Update);

								if (q.From.Tables.Count == 0)
									break;

								goto default;

							case QueryType.Update :
								Visit1(q.Update);
								break;

							case QueryType.Delete :
								Visit1(q.Delete);
								Visit1(q.Select);
								break;

							case QueryType.Insert :
								Visit1(q.Insert);

								if (q.From.Tables.Count != 0)
									Visit1(q.Select);
			
								break;

							default :
								Visit1(q.Select);
								break;
						}

						Visit1(q.From);
						Visit1(q.Where);
						Visit1(q.GroupBy);
						Visit1(q.Having);
						Visit1(q.OrderBy);

						if (q.HasUnion)
						{
							foreach (var i in q.Unions)
							{
								if (i.SqlQuery == q)
									throw new InvalidOperationException();

								Visit1(i);
							}
						}

						break;
					}
			}
		}

		public void Visit(IQueryElement element, Action<IQueryElement> action)
		{
			_visitedElements.Clear();
			_all         = false;
			_action2     = action;
			Visit2(element);
		}

		public void VisitAll(IQueryElement element, Action<IQueryElement> action)
		{
			_visitedElements.Clear();
			_all         = true;
			_action2     = action;
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
						foreach (var p in ((SqlFunction)element).Parameters) Visit2(p);
						break;
					}

				case QueryElementType.SqlExpression:
					{
						foreach (var v in ((SqlExpression)element).Parameters) Visit2(v);
						break;
					}

				case QueryElementType.SqlBinaryExpression:
					{
						//var bexpr = (SqlBinaryExpression)element;
						Visit2(((SqlBinaryExpression)element).Expr1);
						Visit2(((SqlBinaryExpression)element).Expr2);
						break;
					}

				case QueryElementType.SqlTable:
					{
						var table = (SqlTable)element;

						Visit2(table.All);
						foreach (var field in table.Fields.Values) Visit2(field);

						if (table.TableArguments != null)
							foreach (var a in table.TableArguments) Visit2(a);

						break;
					}

				case QueryElementType.Column:
					{
						Visit2(((SqlQuery.Column)element).Expression);
						break;
					}

				case QueryElementType.TableSource:
					{
						//var table = ((SqlQuery.TableSource)element);

						Visit2(((SqlQuery.TableSource)element).Source);
						foreach (var j in ((SqlQuery.TableSource)element).Joins) Visit2(j);
						break;
					}

				case QueryElementType.JoinedTable:
					{
						//var join = (SqlQuery.JoinedTable)element;
						Visit2(((SqlQuery.JoinedTable)element).Table);
						Visit2(((SqlQuery.JoinedTable)element).Condition);
						break;
					}

				case QueryElementType.SearchCondition:
					{
						foreach (var c in ((SqlQuery.SearchCondition)element).Conditions) Visit2(c);
						break;
					}

				case QueryElementType.Condition:
					{
						Visit2(((SqlQuery.Condition)element).Predicate);
						break;
					}

				case QueryElementType.ExprPredicate:
					{
						Visit2(((SqlQuery.Predicate.Expr)element).Expr1);
						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						Visit2(((SqlQuery.Predicate.NotExpr)element).Expr1);
						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						//var p = ((SqlQuery.Predicate.ExprExpr)element);
						Visit2(((SqlQuery.Predicate.ExprExpr)element).Expr1);
						Visit2(((SqlQuery.Predicate.ExprExpr)element).Expr2);
						break;
					}

				case QueryElementType.LikePredicate:
					{
						//var p = ((SqlQuery.Predicate.Like)element);
						Visit2(((SqlQuery.Predicate.Like)element).Expr1);
						Visit2(((SqlQuery.Predicate.Like)element).Expr2);
						Visit2(((SqlQuery.Predicate.Like)element).Escape);
						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						//var p = (SqlQuery.Predicate.Between)element;
						Visit2(((SqlQuery.Predicate.Between)element).Expr1);
						Visit2(((SqlQuery.Predicate.Between)element).Expr2);
						Visit2(((SqlQuery.Predicate.Between)element).Expr3);
						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						Visit2(((SqlQuery.Predicate.IsNull)element).Expr1);
						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						//var p = (SqlQuery.Predicate.InSubQuery)element;
						Visit2(((SqlQuery.Predicate.InSubQuery)element).Expr1);
						Visit2(((SqlQuery.Predicate.InSubQuery)element).SubQuery);
						break;
					}

				case QueryElementType.InListPredicate:
					{
						//var p = (SqlQuery.Predicate.InList)element;
						Visit2(((SqlQuery.Predicate.InList)element).Expr1);
						foreach (var value in ((SqlQuery.Predicate.InList)element).Values) Visit2(value);
						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						Visit2(((SqlQuery.Predicate.FuncLike)element).Function);
						break;
					}

				case QueryElementType.SetExpression:
					{
						//var s = (SqlQuery.SetExpression)element;
						Visit2(((SqlQuery.SetExpression)element).Column);
						Visit2(((SqlQuery.SetExpression)element).Expression);
						break;
					}

				case QueryElementType.InsertClause:
					{
						//var sc = (SqlQuery.InsertClause)element;

						if (((SqlQuery.InsertClause)element).Into != null)
							Visit2(((SqlQuery.InsertClause)element).Into);

						foreach (var c in ((SqlQuery.InsertClause)element).Items.ToArray()) Visit2(c);
						break;
					}

				case QueryElementType.UpdateClause:
					{
						//var sc = (SqlQuery.UpdateClause)element;

						if (((SqlQuery.UpdateClause)element).Table != null)
							Visit2(((SqlQuery.UpdateClause)element).Table);

						foreach (var c in ((SqlQuery.UpdateClause)element).Items.ToArray()) Visit2(c);
						foreach (var c in ((SqlQuery.UpdateClause)element).Keys. ToArray()) Visit2(c);
						break;
					}

				case QueryElementType.DeleteClause:
					{
						if (((SqlQuery.DeleteClause)element).Table != null)
							Visit2(((SqlQuery.DeleteClause)element).Table);
						break;
					}

				case QueryElementType.SelectClause:
					{
						//var sc = (SqlQuery.SelectClause)element;
						Visit2(((SqlQuery.SelectClause)element).TakeValue);
						Visit2(((SqlQuery.SelectClause)element).SkipValue);

						foreach (var c in ((SqlQuery.SelectClause)element).Columns.ToArray()) Visit2(c);
						break;
					}

				case QueryElementType.FromClause:
					{
						foreach (var t in ((SqlQuery.FromClause)element).Tables) Visit2(t);
						break;
					}

				case QueryElementType.WhereClause:
					{
						Visit2(((SqlQuery.WhereClause)element).SearchCondition);
						break;
					}

				case QueryElementType.GroupByClause:
					{
						foreach (var i in ((SqlQuery.GroupByClause)element).Items) Visit2(i);
						break;
					}

				case QueryElementType.OrderByClause:
					{
						foreach (var i in ((SqlQuery.OrderByClause)element).Items) Visit2(i);
						break;
					}

				case QueryElementType.OrderByItem:
					{
						Visit2(((SqlQuery.OrderByItem)element).Expression);
						break;
					}

				case QueryElementType.Union:
					Visit2(((SqlQuery.Union)element).SqlQuery);
					break;

				case QueryElementType.SqlQuery:
					{
						if (_all)
						{
							if (_visitedElements.ContainsKey(element))
								return;
							_visitedElements.Add(element, element);
						}

						var q = (SqlQuery)element;

						switch (q.QueryType)
						{
							case QueryType.InsertOrUpdate :
								Visit2(q.Insert);
								Visit2(q.Update);

								if (q.From.Tables.Count == 0)
									break;

								goto default;

							case QueryType.Update :
								Visit2(q.Update);
								break;

							case QueryType.Delete :
								Visit2(q.Delete);
								Visit2(q.Select);
								break;

							case QueryType.Insert :
								Visit2(q.Insert);

								if (q.From.Tables.Count != 0)
									Visit2(q.Select);

								break;

							default :
								Visit2(q.Select);
								break;
						}

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
									if (!_all)
										_visitedElements.Add(t, t);
								}
							}
							_action2(q.From);
							if (!_all)
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
								if (i.SqlQuery == q)
									throw new InvalidOperationException();

								Visit2(i);
							}
						}

						break;
					}
			}

			_action2(element);
			if (!_all)
				_visitedElements.Add(element, element);
		}

		#endregion

		#region Find

		IQueryElement Find<T>(IEnumerable<T> arr, Func<IQueryElement, bool> find)
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

		public IQueryElement Find(IQueryElement element, Func<IQueryElement, bool> find)
		{
			if (element == null || find(element))
				return element;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction       : return Find(((SqlFunction)                element).Parameters,      find);
				case QueryElementType.SqlExpression     : return Find(((SqlExpression)              element).Parameters,      find);
				case QueryElementType.Column            : return Find(((SqlQuery.Column)            element).Expression,      find);
				case QueryElementType.SearchCondition   : return Find(((SqlQuery.SearchCondition)   element).Conditions,      find);
				case QueryElementType.Condition         : return Find(((SqlQuery.Condition)         element).Predicate,       find);
				case QueryElementType.ExprPredicate     : return Find(((SqlQuery.Predicate.Expr)    element).Expr1,           find);
				case QueryElementType.NotExprPredicate  : return Find(((SqlQuery.Predicate.NotExpr) element).Expr1,           find);
				case QueryElementType.IsNullPredicate   : return Find(((SqlQuery.Predicate.IsNull)  element).Expr1,           find);
				case QueryElementType.FromClause        : return Find(((SqlQuery.FromClause)        element).Tables,          find);
				case QueryElementType.WhereClause       : return Find(((SqlQuery.WhereClause)       element).SearchCondition, find);
				case QueryElementType.GroupByClause     : return Find(((SqlQuery.GroupByClause)     element).Items,           find);
				case QueryElementType.OrderByClause     : return Find(((SqlQuery.OrderByClause)     element).Items,           find);
				case QueryElementType.OrderByItem       : return Find(((SqlQuery.OrderByItem)       element).Expression,      find);
				case QueryElementType.Union             : return Find(((SqlQuery.Union)             element).SqlQuery,        find);
				case QueryElementType.FuncLikePredicate : return Find(((SqlQuery.Predicate.FuncLike)element).Function,        find);

				case QueryElementType.SqlBinaryExpression:
					{
						var bexpr = (SqlBinaryExpression)element;
						return
							Find(bexpr.Expr1, find) ??
							Find(bexpr.Expr2, find);
					}

				case QueryElementType.SqlTable:
					{
						var table = (SqlTable)element;
						return
							Find(table.All,            find) ??
							Find(table.Fields.Values,  find) ??
							Find(table.TableArguments, find);
					}

				case QueryElementType.TableSource:
					{
						var table = (SqlQuery.TableSource)element;
						return
							Find(table.Source, find) ??
							Find(table.Joins,  find);
					}

				case QueryElementType.JoinedTable:
					{
						var join = (SqlQuery.JoinedTable)element;
						return
							Find(join.Table,     find) ??
							Find(join.Condition, find);
					}

				case QueryElementType.ExprExprPredicate:
					{
						var p = (SqlQuery.Predicate.ExprExpr)element;
						return
							Find(p.Expr1, find) ??
							Find(p.Expr2, find);
					}

				case QueryElementType.LikePredicate:
					{
						var p = (SqlQuery.Predicate.Like)element;
						return
							Find(p.Expr1,  find) ??
							Find(p.Expr2,  find) ??
							Find(p.Escape, find);
					}

				case QueryElementType.BetweenPredicate:
					{
						var p = (SqlQuery.Predicate.Between)element;
						return
							Find(p.Expr1, find) ??
							Find(p.Expr2, find) ??
							Find(p.Expr3, find);
					}

				case QueryElementType.InSubQueryPredicate:
					{
						var p = (SqlQuery.Predicate.InSubQuery)element;
						return
							Find(p.Expr1,    find) ??
							Find(p.SubQuery, find);
					}

				case QueryElementType.InListPredicate:
					{
						var p = (SqlQuery.Predicate.InList)element;
						return
							Find(p.Expr1,  find) ??
							Find(p.Values, find);
					}

				case QueryElementType.SetExpression:
					{
						var s = (SqlQuery.SetExpression)element;
						return
							Find(s.Column,     find) ??
							Find(s.Expression, find);
					}

				case QueryElementType.InsertClause:
					{
						var sc = (SqlQuery.InsertClause)element;
						return
							Find(sc.Into,  find) ??
							Find(sc.Items, find);
					}

				case QueryElementType.UpdateClause:
					{
						var sc = (SqlQuery.UpdateClause)element;
						return
							Find(sc.Table, find) ??
							Find(sc.Items, find) ??
							Find(sc.Keys,  find);
					}

				case QueryElementType.DeleteClause:
					{
						var sc = (SqlQuery.DeleteClause)element;
						return Find(sc.Table, find);
					}

				case QueryElementType.SelectClause:
					{
						var sc = (SqlQuery.SelectClause)element;
						return
							Find(sc.TakeValue, find) ??
							Find(sc.SkipValue, find) ??
							Find(sc.Columns,   find);
					}

				case QueryElementType.SqlQuery:
					{
						var q = (SqlQuery)element;
						return
							Find(q.Select,  find) ??
							(q.IsInsert ? Find(q.Insert, find) : null) ??
							(q.IsUpdate ? Find(q.Update, find) : null) ??
							Find(q.From,    find) ??
							Find(q.Where,   find) ??
							Find(q.GroupBy, find) ??
							Find(q.Having,  find) ??
							Find(q.OrderBy, find) ??
							(q.HasUnion ? Find(q.Unions, find) : null);
					}
			}

			return null;
		}

		#endregion

		#region Convert

		public T Convert<T>(T element, Func<IQueryElement, IQueryElement> action)
			where T : class, IQueryElement
		{
			_visitedElements.Clear();
			return (T)ConvertInternal(element, action) ?? element;
		}

		IQueryElement ConvertInternal(IQueryElement element, Func<IQueryElement, IQueryElement> action)
		{
			if (element == null)
				return null;

			IQueryElement newElement;

			if (_visitedElements.TryGetValue(element, out newElement))
				return newElement;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction:
					{
						var func  = (SqlFunction)element;
						var parms = Convert(func.Parameters, action);

						if (parms != null && !ReferenceEquals(parms, func.Parameters))
							newElement = new SqlFunction(func.SystemType, func.Name, func.Precedence, parms);

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
						var fields2 = Convert(fields1,     action, f => new SqlField(f));
						var targs   = table.TableArguments == null ? null : Convert(table.TableArguments, action);

						var fe = fields2 == null || ReferenceEquals(fields1, fields2);
						var ta = ReferenceEquals(table.TableArguments, targs);

						if (!fe || !ta)
						{
							if (fe)
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

				case QueryElementType.Column:
					{
						var col  = (SqlQuery.Column)element;
						var expr = (ISqlExpression)ConvertInternal(col.Expression, action);

						IQueryElement parent;
						_visitedElements.TryGetValue(col.Parent, out parent);

						if (parent != null || expr != null && !ReferenceEquals(expr, col.Expression))
							newElement = new SqlQuery.Column(parent == null ? col.Parent : (SqlQuery)parent, expr ?? col.Expression, col._alias);

						break;
					}

				case QueryElementType.TableSource:
					{
						var table  = (SqlQuery.TableSource)element;
						var source = (ISqlTableSource)ConvertInternal(table.Source, action);
						var joins  = Convert(table.Joins, action);

						if (source != null && !ReferenceEquals(source, table.Source) ||
							joins  != null && !ReferenceEquals(table.Joins, joins))
							newElement = new SqlQuery.TableSource(source ?? table.Source, table._alias, joins ?? table.Joins);

						break;
					}

				case QueryElementType.JoinedTable:
					{
						var join  = (SqlQuery.JoinedTable)element;
						var table = (SqlQuery.TableSource)    ConvertInternal(join.Table,     action);
						var cond  = (SqlQuery.SearchCondition)ConvertInternal(join.Condition, action);

						if (table != null && !ReferenceEquals(table, join.Table) ||
							cond  != null && !ReferenceEquals(cond,  join.Condition))
							newElement = new SqlQuery.JoinedTable(join.JoinType, table ?? join.Table, join.IsWeak, cond ?? join.Condition);

						break;
					}

				case QueryElementType.SearchCondition:
					{
						var sc    = (SqlQuery.SearchCondition)element;
						var conds = Convert(sc.Conditions, action);

						if (conds != null && !ReferenceEquals(sc.Conditions, conds))
							newElement = new SqlQuery.SearchCondition(conds);

						break;
					}

				case QueryElementType.Condition:
					{
						var c = (SqlQuery.Condition)element;
						var p = (ISqlPredicate)ConvertInternal(c.Predicate, action);

						if (p != null && !ReferenceEquals(c.Predicate, p))
							newElement = new SqlQuery.Condition(c.IsNot, p, c.IsOr);

						break;
					}

				case QueryElementType.ExprPredicate:
					{
						var p = (SqlQuery.Predicate.Expr)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1, action);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlQuery.Predicate.Expr(e, p.Precedence);

						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						var p = (SqlQuery.Predicate.NotExpr)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1, action);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlQuery.Predicate.NotExpr(e, p.IsNot, p.Precedence);

						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						var p  = (SqlQuery.Predicate.ExprExpr)element;
						var e1 = (ISqlExpression)ConvertInternal(p.Expr1, action);
						var e2 = (ISqlExpression)ConvertInternal(p.Expr2, action);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) || e2 != null && !ReferenceEquals(p.Expr2, e2))
							newElement = new SqlQuery.Predicate.ExprExpr(e1 ?? p.Expr1, p.Operator, e2 ?? p.Expr2);

						break;
					}

				case QueryElementType.LikePredicate:
					{
						var p  = (SqlQuery.Predicate.Like)element;
						var e1 = (ISqlExpression)ConvertInternal(p.Expr1,  action);
						var e2 = (ISqlExpression)ConvertInternal(p.Expr2,  action);
						var es = (ISqlExpression)ConvertInternal(p.Escape, action);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							es != null && !ReferenceEquals(p.Escape, es))
							newElement = new SqlQuery.Predicate.Like(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, es ?? p.Escape);

						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						var p = (SqlQuery.Predicate.Between)element;
						var e1 = (ISqlExpression)ConvertInternal(p.Expr1, action);
						var e2 = (ISqlExpression)ConvertInternal(p.Expr2, action);
						var e3 = (ISqlExpression)ConvertInternal(p.Expr3, action);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							e3 != null && !ReferenceEquals(p.Expr3, e3))
							newElement = new SqlQuery.Predicate.Between(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, e3 ?? p.Expr3);

						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						var p = (SqlQuery.Predicate.IsNull)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1, action);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlQuery.Predicate.IsNull(e, p.IsNot);

						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						var p = (SqlQuery.Predicate.InSubQuery)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1,    action);
						var q = (SqlQuery)ConvertInternal(p.SubQuery, action);

						if (e != null && !ReferenceEquals(p.Expr1, e) || q != null && !ReferenceEquals(p.SubQuery, q))
							newElement = new SqlQuery.Predicate.InSubQuery(e ?? p.Expr1, p.IsNot, q ?? p.SubQuery);

						break;
					}

				case QueryElementType.InListPredicate:
					{
						var p = (SqlQuery.Predicate.InList)element;
						var e = (ISqlExpression)ConvertInternal(p.Expr1,    action);
						var v = Convert(p.Values, action);

						if (e != null && !ReferenceEquals(p.Expr1, e) || v != null && !ReferenceEquals(p.Values, v))
							newElement = new SqlQuery.Predicate.InList(e ?? p.Expr1, p.IsNot, v ?? p.Values);

						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						var p = (SqlQuery.Predicate.FuncLike)element;
						var f = (SqlFunction)ConvertInternal(p.Function, action);

						if (f != null && !ReferenceEquals(p.Function, f))
							newElement = new SqlQuery.Predicate.FuncLike(f);

						break;
					}

				case QueryElementType.SetExpression:
					{
						var s = (SqlQuery.SetExpression)element;
						var c = (ISqlExpression)ConvertInternal(s.Column,     action);
						var e = (ISqlExpression)ConvertInternal(s.Expression, action);

						if (c != null && !ReferenceEquals(s.Column, c) || e != null && !ReferenceEquals(s.Expression, e))
							newElement = new SqlQuery.SetExpression(c ?? s.Column, e ?? s.Expression);

						break;
					}

				case QueryElementType.InsertClause:
					{
						var s = (SqlQuery.InsertClause)element;
						var t = s.Into != null ? (SqlTable)ConvertInternal(s.Into, action) : null;
						var i = Convert(s.Items, action);

						if (t != null && !ReferenceEquals(s.Into, t) || i != null && !ReferenceEquals(s.Items, i))
						{
							var sc = new SqlQuery.InsertClause { Into = t ?? s.Into };

							sc.Items.AddRange(i ?? s.Items);
							sc.WithIdentity = s.WithIdentity;

							newElement = sc;
						}

						break;
					}

				case QueryElementType.UpdateClause:
					{
						var s = (SqlQuery.UpdateClause)element;
						var t = s.Table != null ? (SqlTable)ConvertInternal(s.Table, action) : null;
						var i = Convert(s.Items, action);
						var k = Convert(s.Keys,  action);

						if (t != null && !ReferenceEquals(s.Table, t) ||
							i != null && !ReferenceEquals(s.Items, i) ||
							k != null && !ReferenceEquals(s.Keys,  k))
						{
							var sc = new SqlQuery.UpdateClause { Table = t ?? s.Table };

							sc.Items.AddRange(i ?? s.Items);
							sc.Keys. AddRange(k ?? s.Keys);

							newElement = sc;
						}

						break;
					}

				case QueryElementType.DeleteClause:
					{
						var s = (SqlQuery.DeleteClause)element;
						var t = s.Table != null ? (SqlTable)ConvertInternal(s.Table, action) : null;

						if (t != null && !ReferenceEquals(s.Table, t))
						{
							newElement = new SqlQuery.DeleteClause { Table = t ?? s.Table };
						}

						break;
					}

				case QueryElementType.SelectClause:
					{
						var sc   = (SqlQuery.SelectClause)element;
						var cols = Convert(sc.Columns, action);
						var take = (ISqlExpression)ConvertInternal(sc.TakeValue, action);
						var skip = (ISqlExpression)ConvertInternal(sc.SkipValue, action);

						IQueryElement parent;
						_visitedElements.TryGetValue(sc.SqlQuery, out parent);

						if (parent != null ||
							cols != null && !ReferenceEquals(sc.Columns,   cols) ||
							take != null && !ReferenceEquals(sc.TakeValue, take) ||
							skip != null && !ReferenceEquals(sc.SkipValue, skip))
						{
							newElement = new SqlQuery.SelectClause(sc.IsDistinct, take ?? sc.TakeValue, skip ?? sc.SkipValue, cols ?? sc.Columns);
							((SqlQuery.SelectClause)newElement).SetSqlQuery((SqlQuery)parent);
						}

						break;
					}

				case QueryElementType.FromClause:
					{
						var fc   = (SqlQuery.FromClause)element;
						var ts = Convert(fc.Tables, action);

						IQueryElement parent;
						_visitedElements.TryGetValue(fc.SqlQuery, out parent);

						if (parent != null || ts != null && !ReferenceEquals(fc.Tables, ts))
						{
							newElement = new SqlQuery.FromClause(ts ?? fc.Tables);
							((SqlQuery.FromClause)newElement).SetSqlQuery((SqlQuery)parent);
						}

						break;
					}

				case QueryElementType.WhereClause:
					{
						var wc   = (SqlQuery.WhereClause)element;
						var cond = (SqlQuery.SearchCondition)ConvertInternal(wc.SearchCondition, action);

						IQueryElement parent;
						_visitedElements.TryGetValue(wc.SqlQuery, out parent);

						if (parent != null || cond != null && !ReferenceEquals(wc.SearchCondition, cond))
						{
							newElement = new SqlQuery.WhereClause(cond ?? wc.SearchCondition);
							((SqlQuery.WhereClause)newElement).SetSqlQuery((SqlQuery)parent);
						}

						break;
					}

				case QueryElementType.GroupByClause:
					{
						var gc = (SqlQuery.GroupByClause)element;
						var es = Convert(gc.Items, action);

						IQueryElement parent;
						_visitedElements.TryGetValue(gc.SqlQuery, out parent);

						if (parent != null || es != null && !ReferenceEquals(gc.Items, es))
						{
							newElement = new SqlQuery.GroupByClause(es ?? gc.Items);
							((SqlQuery.GroupByClause)newElement).SetSqlQuery((SqlQuery)parent);
						}

						break;
					}

				case QueryElementType.OrderByClause:
					{
						var oc = (SqlQuery.OrderByClause)element;
						var es = Convert(oc.Items, action);

						IQueryElement parent;
						_visitedElements.TryGetValue(oc.SqlQuery, out parent);

						if (parent != null || es != null && !ReferenceEquals(oc.Items, es))
						{
							newElement = new SqlQuery.OrderByClause(es ?? oc.Items);
							((SqlQuery.OrderByClause)newElement).SetSqlQuery((SqlQuery)parent);
						}

						break;
					}

				case QueryElementType.OrderByItem:
					{
						var i = (SqlQuery.OrderByItem)element;
						var e = (ISqlExpression)ConvertInternal(i.Expression, action);

						if (e != null && !ReferenceEquals(i.Expression, e))
							newElement = new SqlQuery.OrderByItem(e, i.IsDescending);

						break;
					}

				case QueryElementType.Union:
					{
						var u = (SqlQuery.Union)element;
						var q = (SqlQuery)ConvertInternal(u.SqlQuery, action);

						if (q != null && !ReferenceEquals(u.SqlQuery, q))
							newElement = new SqlQuery.Union(q, u.IsAll);

						break;
					}

				case QueryElementType.SqlQuery:
					{
						var q = (SqlQuery)element;
						IQueryElement parent = null;

						var doConvert = q.ParentSql != null && !_visitedElements.TryGetValue(q.ParentSql, out parent);

						if (!doConvert)
						{
							doConvert = null != Find(q, e =>
							{
								if (_visitedElements.ContainsKey(e) && _visitedElements[e] != e)
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

						var nq = new SqlQuery { QueryType = q.QueryType };

						_visitedElements.Add(q, nq);

						var fc = (SqlQuery.FromClause)   ConvertInternal(q.From,    action) ?? q.From;
						var sc = (SqlQuery.SelectClause) ConvertInternal(q.Select,  action) ?? q.Select;
						var ic = q.IsInsert ? ((SqlQuery.InsertClause)ConvertInternal(q.Insert, action) ?? q.Insert) : null;
						var uc = q.IsUpdate ? ((SqlQuery.UpdateClause)ConvertInternal(q.Update, action) ?? q.Update) : null;
						var dc = q.IsDelete ? ((SqlQuery.DeleteClause)ConvertInternal(q.Delete, action) ?? q.Delete) : null;
						var wc = (SqlQuery.WhereClause)  ConvertInternal(q.Where,   action) ?? q.Where;
						var gc = (SqlQuery.GroupByClause)ConvertInternal(q.GroupBy, action) ?? q.GroupBy;
						var hc = (SqlQuery.WhereClause)  ConvertInternal(q.Having,  action) ?? q.Having;
						var oc = (SqlQuery.OrderByClause)ConvertInternal(q.OrderBy, action) ?? q.OrderBy;
						var us = q.HasUnion ? Convert(q.Unions, action) : q.Unions;

						var ps = new List<SqlParameter>(q.Parameters.Count);

						foreach (var p in q.Parameters)
						{
							IQueryElement e;

							if (_visitedElements.TryGetValue(p, out e))
							{
								if (e == null)
									ps.Add(p);
								else if (e is SqlParameter)
									ps.Add((SqlParameter)e);
							}
						}

						nq.Init(ic, uc, dc, sc, fc, wc, gc, hc, oc, us, (SqlQuery)parent, q.IsParameterDependent, ps);

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
				else if (list2 != null)
					list2.Add(clone == null ? elem1 : clone(elem1));
			}

			return list2;
		}

		#endregion
	}
}
