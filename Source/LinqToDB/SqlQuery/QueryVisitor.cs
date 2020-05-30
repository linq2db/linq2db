using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class QueryVisitor
	{
		#region Visit

		readonly ISet<IQueryElement>                      _visitedFind     = new HashSet<IQueryElement>();
		readonly Dictionary<IQueryElement,IQueryElement?> _visitedElements = new Dictionary<IQueryElement, IQueryElement?>();
		public   Dictionary<IQueryElement,IQueryElement?>  VisitedElements => _visitedElements;

		bool                                 _all;
		Func<IQueryElement,bool>?            _action1;
		Action<IQueryElement>?               _action2;
		Func<IQueryElement, bool>?           _find;

		public void VisitParentFirst(IQueryElement element, Func<IQueryElement,bool> action)
		{
			_visitedElements.Clear();
			_action1 = action;
			Visit1(element);
		}

		void Visit1(IQueryElement? element)
		{
			if (element == null || _visitedElements.ContainsKey(element))
				return;

			_visitedElements.Add(element, element);

			if (!_action1!(element))
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

				case QueryElementType.SqlRawSqlTable:
					{
						Visit1X((SqlRawSqlTable)element);
						break;
					}

				case QueryElementType.OutputClause:
					{
						Visit1X((SqlOutputClause)element);
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
						Visit1(((SqlInsertStatement)element).Output);
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

				case QueryElementType.GroupingSet:
					{
						Visit1X((SqlGroupingSet)element);
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

				case QueryElementType.SetOperator:
					{
						Visit1(((SqlSetOperator)element).SelectQuery);
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

				case QueryElementType.MergeStatement:
					Visit1X((SqlMergeStatement)element);
					break;

				case QueryElementType.MergeSourceTable:
					Visit1X((SqlMergeSourceTable)element);
					break;

				case QueryElementType.SqlValuesTable:
					Visit1X((SqlValuesTable)element);
					break;

				case QueryElementType.MergeOperationClause:
					Visit1X((SqlMergeOperationClause)element);
					break;

				case QueryElementType.SqlField:
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlValue:
				case QueryElementType.SqlDataType:
				case QueryElementType.SqlAliasPlaceholder:
					break;

				default:
					throw new InvalidOperationException($"Visit1 visitor not implemented for element {element.ElementType}");
			}
		}

		void Visit1X(SelectQuery q)
		{
			Visit1(q.Select);
			Visit1(q.From);
			Visit1(q.Where);
			Visit1(q.GroupBy);
			Visit1(q.Having);
			Visit1(q.OrderBy);

			if (q.HasSetOperators)
			{
				foreach (var i in q.SetOperators)
				{
					if (i.SelectQuery == q)
						throw new InvalidOperationException();

					Visit1(i);
				}
			}

			// decided to do not enumerate unique keys
//			if (q.HasUniqueKeys)
//				foreach (var keyList in q.UniqueKeys)
//				{
//					Visit1X(keyList);
//				}
		}

		void Visit1X(SqlOrderByClause element)
		{
			foreach (var i in element.Items) Visit1(i);
		}

		void Visit1X(SqlGroupByClause element)
		{
			foreach (var i in element.Items) Visit1(i);
		}

		void Visit1X(SqlGroupingSet element)
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
			foreach (var c in sc.Fields!) Visit1(c);
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
			if (table == null)
				return;

			Visit1(table.All);
			foreach (var field in table.Fields.Values) Visit1(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit1(a);
		}

		void Visit1X(SqlOutputClause outputClause)
		{
			if (outputClause == null)
				return;

			Visit1X(outputClause.SourceTable);
			Visit1X(outputClause.DeletedTable);
			Visit1X(outputClause.InsertedTable);
			Visit1X(outputClause.OutputTable);
			if (outputClause.OutputQuery != null)
				Visit1X(outputClause.OutputQuery);

			if (outputClause.HasOutputItems)
				foreach (var item in outputClause.OutputItems)
					Visit1(item);
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

		void Visit1X(SqlRawSqlTable table)
		{
			Visit1(table.All);
			foreach (var field in table.Fields.Values) Visit1(field);

			if (table.Parameters != null)
				foreach (var a in table.Parameters) Visit1(a);
		}

		void Visit1X(SqlExpression element)
		{
			foreach (var v in element.Parameters) Visit1(v);
		}

		void Visit1X(SqlFunction element)
		{
			foreach (var p in element.Parameters) Visit1(p);
		}

		void Visit1X(SqlMergeStatement element)
		{
			Visit1(element.Target);
			Visit1(element.Source);
			Visit1(element.On);

			foreach (var operation in element.Operations)
				Visit1(operation);
		}

		void Visit1X(SqlMergeSourceTable element)
		{
			Visit1(element.Source);

			foreach (var field in element.SourceFields)
				Visit1(field);
		}

		void Visit1X(SqlValuesTable element)
		{
			foreach (var field in element.Fields.Values)
				Visit1(field);

			foreach (var row in element.Rows)
				foreach (var value in row)
					Visit1(value);
		}

		void Visit1X(SqlMergeOperationClause element)
		{
			Visit1(element.Where);
			Visit1(element.WhereDelete);

			foreach (var item in element.Items)
				Visit1(item);
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

		void Visit2(IQueryElement? element)
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

				case QueryElementType.SqlRawSqlTable:
					{
						Visit2X((SqlRawSqlTable)element);
						break;
					}

				case QueryElementType.OutputClause:
					{
						Visit2X((SqlOutputClause)element);
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
						Visit2(((SqlInsertStatement)element).Output);
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

				case QueryElementType.GroupingSet:
					{
						
						Visit2X((SqlGroupingSet)element);
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

				case QueryElementType.SetOperator:
					Visit2(((SqlSetOperator)element).SelectQuery);
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

				case QueryElementType.MergeStatement:
					Visit2X((SqlMergeStatement)element);
					break;

				case QueryElementType.MergeSourceTable:
					Visit2X((SqlMergeSourceTable)element);
					break;

				case QueryElementType.SqlValuesTable:
					Visit2X((SqlValuesTable)element);
					break;

				case QueryElementType.MergeOperationClause:
					Visit2X((SqlMergeOperationClause)element);
					break;

				case QueryElementType.SqlField:
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlValue:
				case QueryElementType.SqlDataType:
				case QueryElementType.SqlAliasPlaceholder:
					break;

				default:
					throw new InvalidOperationException($"Visit2 visitor not implemented for element {element.ElementType}");
			}

			_action2!(element);

			if (!_all && !_visitedElements.ContainsKey(element))
				_visitedElements.Add(element, element);
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

						_action2!(t);
						if (!_all && !_visitedElements.ContainsKey(t))
							_visitedElements.Add(t, t);
					}
				}

				_action2!(q.From);
				if (!_all && !_visitedElements.ContainsKey(q.From))
					_visitedElements.Add(q.From, q.From);
			}

			Visit2(q.Where);
			Visit2(q.GroupBy);
			Visit2(q.Having);
			Visit2(q.OrderBy);

			if (q.HasSetOperators)
			{
				foreach (var i in q.SetOperators)
				{
					if (i.SelectQuery == q)
						throw new InvalidOperationException();

					Visit2(i);
				}
			}

			// decided to do not enumerate unique keys
//			if (q.HasUniqueKeys)
//				foreach (var keyList in q.UniqueKeys)
//				{
//					Visit2X(keyList);
//				}
		}

		void Visit2X(SqlOrderByClause element)
		{
			foreach (var i in element.Items) Visit2(i);
		}

		void Visit2X(SqlGroupByClause element)
		{
			foreach (var i in element.Items) Visit2(i);
		}

		void Visit2X(SqlGroupingSet element)
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
			foreach (var c in sc.Fields!) Visit2(c);
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
			if (table == null)
				return;

			Visit2(table.All);
			foreach (var field in table.Fields.Values) Visit2(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit2(a);
		}

		void Visit2X(SqlRawSqlTable table)
		{
			Visit2(table.All);
			foreach (var field in table.Fields.Values) Visit2(field);

			if (table.Parameters != null)
				foreach (var a in table.Parameters) Visit2(a);
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

		void Visit2X(SqlOutputClause outputClause)
		{
			if (outputClause == null)
				return;

			Visit2X(outputClause.SourceTable);
			Visit2(outputClause.DeletedTable);
			Visit2(outputClause.InsertedTable);
			Visit2X(outputClause.OutputTable);
			if (outputClause.OutputQuery != null)
				Visit2(outputClause.OutputQuery);

			if (outputClause.HasOutputItems)
				foreach (var item in outputClause.OutputItems)
					Visit2(item);
		}

		void Visit2X(SqlExpression element)
		{
			foreach (var v in element.Parameters) Visit2(v);
		}

		void Visit2X(SqlFunction element)
		{
			foreach (var p in element.Parameters) Visit2(p);
		}

		void Visit2X(SqlMergeStatement element)
		{
			Visit2(element.Target);
			Visit2(element.Source);
			Visit2(element.On);

			foreach (var operation in element.Operations)
				Visit2(operation);
		}

		void Visit2X(SqlMergeSourceTable element)
		{
			Visit2(element.Source);

			foreach (var field in element.SourceFields)
				Visit2(field);
		}

		void Visit2X(SqlValuesTable element)
		{
			foreach (var field in element.Fields.Values)
				Visit2(field);

			foreach (var row in element.Rows)
				foreach (var value in row)
					Visit2(value);
		}

		void Visit2X(SqlMergeOperationClause element)
		{
			Visit2(element.Where);
			Visit2(element.WhereDelete);

			foreach (var item in element.Items)
				Visit2(item);
		}

		#endregion

		#region Find

		IQueryElement? Find<T>(IEnumerable<T>? arr)
			where T : class, IQueryElement
		{
			if (arr == null)
				return null;

			foreach (var item in arr)
			{
				var e = Find(item);
				if (e != null)
					return e;
			}

			return null;
		}

		IQueryElement? FindX(SqlSearchCondition sc)
		{
			if (sc.Conditions == null)
				return null;

			foreach (var item in sc.Conditions)
			{
				var e = Find(item.Predicate);
				if (e != null)
					return e;
			}

			return null;
		}

		public IQueryElement? Find(IQueryElement? element, Func<IQueryElement, bool> find)
		{
			_visitedFind.Clear();
			_find = find;
			return Find(element);
		}

		IQueryElement? Find(IQueryElement? element)
		{
			if (element == null || !_visitedFind.Add(element))
				return null;

			if (_find!(element))
				return element;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction       : return Find(((SqlFunction)          element).Parameters     );
				case QueryElementType.SqlExpression     : return Find(((SqlExpression)        element).Parameters     );
				case QueryElementType.Column            : return Find(((SqlColumn)            element).Expression     );
				case QueryElementType.SearchCondition   : return FindX((SqlSearchCondition)   element                 );
				case QueryElementType.Condition         : return Find(((SqlCondition)         element).Predicate      );
				case QueryElementType.ExprPredicate     : return Find(((SqlPredicate.Expr)    element).Expr1          );
				case QueryElementType.NotExprPredicate  : return Find(((SqlPredicate.NotExpr) element).Expr1          );
				case QueryElementType.IsNullPredicate   : return Find(((SqlPredicate.IsNull)  element).Expr1          );
				case QueryElementType.FromClause        : return Find(((SqlFromClause)        element).Tables         );
				case QueryElementType.WhereClause       : return Find(((SqlWhereClause)       element).SearchCondition);
				case QueryElementType.GroupByClause     : return Find(((SqlGroupByClause)     element).Items          );
				case QueryElementType.GroupingSet       : return Find(((SqlGroupingSet)       element).Items          );
				case QueryElementType.OrderByClause     : return Find(((SqlOrderByClause)     element).Items          );
				case QueryElementType.OrderByItem       : return Find(((SqlOrderByItem)       element).Expression     );
				case QueryElementType.SetOperator       : return Find(((SqlSetOperator)       element).SelectQuery    );
				case QueryElementType.FuncLikePredicate : return Find(((SqlPredicate.FuncLike)element).Function       );

				case QueryElementType.SqlBinaryExpression:
					{
						return
							Find(((SqlBinaryExpression)element).Expr1) ??
							Find(((SqlBinaryExpression)element).Expr2);
					}

				case QueryElementType.SqlTable:
					{
						return
							Find(((SqlTable)element).All           ) ??
							Find(((SqlTable)element).Fields.Values ) ??
							Find(((SqlTable)element).TableArguments);
					}

				case QueryElementType.SqlCteTable:
					{
						return
							Find(((SqlCteTable)element).All           ) ??
							Find(((SqlCteTable)element).Fields.Values ) ??
							Find(((SqlCteTable)element).TableArguments) ??
							Find(((SqlCteTable)element).Cte);
					}

				case QueryElementType.SqlRawSqlTable:
					{
						return
							Find(((SqlRawSqlTable)element).All          ) ??
							Find(((SqlRawSqlTable)element).Fields.Values) ??
							Find(((SqlRawSqlTable)element).Parameters   );
					}

				case QueryElementType.OutputClause:
					{
						return
							Find(((SqlOutputClause)element).SourceTable)   ??
							Find(((SqlOutputClause)element).DeletedTable)  ??
							Find(((SqlOutputClause)element).InsertedTable) ??
							Find(((SqlOutputClause)element).OutputTable)   ??
							(((SqlOutputClause)element).HasOutputItems ? Find(((SqlOutputClause)element).OutputItems) : null);
					}

				case QueryElementType.TableSource:
					{
						return
							Find(((SqlTableSource)element).Source) ??
							Find(((SqlTableSource)element).Joins );
					}

				case QueryElementType.JoinedTable:
					{
						return
							Find(((SqlJoinedTable)element).Table    ) ??
							Find(((SqlJoinedTable)element).Condition);
					}

				case QueryElementType.ExprExprPredicate:
					{
						return
							Find(((SqlPredicate.ExprExpr)element).Expr1) ??
							Find(((SqlPredicate.ExprExpr)element).Expr2);
					}

				case QueryElementType.LikePredicate:
					{
						return
							Find(((SqlPredicate.Like)element).Expr1 ) ??
							Find(((SqlPredicate.Like)element).Expr2 ) ??
							Find(((SqlPredicate.Like)element).Escape);
					}

				case QueryElementType.BetweenPredicate:
					{
						return
							Find(((SqlPredicate.Between)element).Expr1) ??
							Find(((SqlPredicate.Between)element).Expr2) ??
							Find(((SqlPredicate.Between)element).Expr3);
					}

				case QueryElementType.InSubQueryPredicate:
					{
						return
							Find(((SqlPredicate.InSubQuery)element).Expr1   ) ??
							Find(((SqlPredicate.InSubQuery)element).SubQuery);
					}

				case QueryElementType.InListPredicate:
					{
						return
							Find(((SqlPredicate.InList)element).Expr1 ) ??
							Find(((SqlPredicate.InList)element).Values);
					}

				case QueryElementType.SetExpression:
					{
						return
							Find(((SqlSetExpression)element).Column    ) ??
							Find(((SqlSetExpression)element).Expression);
					}

				case QueryElementType.InsertClause:
					{
						return
							Find(((SqlInsertClause)element).Into ) ??
							Find(((SqlInsertClause)element).Items);
					}

				case QueryElementType.UpdateClause:
					{
						return
							Find(((SqlUpdateClause)element).Table) ??
							Find(((SqlUpdateClause)element).Items) ??
							Find(((SqlUpdateClause)element).Keys );
					}

				case QueryElementType.SelectStatement:
					{
						return Find(((SqlSelectStatement)element).SelectQuery) ??
						       Find(((SqlSelectStatement)element).With       );
					}

				case QueryElementType.InsertStatement:
					{
						return Find(((SqlInsertStatement)element).SelectQuery) ??
						       Find(((SqlInsertStatement)element).Insert     ) ??
						       Find(((SqlInsertStatement)element).With       );
					}

				case QueryElementType.UpdateStatement:
					{
						return Find(((SqlUpdateStatement)element).SelectQuery) ??
						       Find(((SqlUpdateStatement)element).Update     ) ??
						       Find(((SqlUpdateStatement)element).With       );
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						return Find(((SqlInsertOrUpdateStatement)element).SelectQuery) ??
						       Find(((SqlInsertOrUpdateStatement)element).Insert     ) ??
						       Find(((SqlInsertOrUpdateStatement)element).Update     ) ??
						       Find(((SqlInsertOrUpdateStatement)element).With       );
					}

				case QueryElementType.DeleteStatement:
					{
						return
							Find(((SqlDeleteStatement)element).Table      ) ??
							Find(((SqlDeleteStatement)element).Top        ) ??
							Find(((SqlDeleteStatement)element).SelectQuery);
					}

				case QueryElementType.CreateTableStatement:
					{
						return
							Find(((SqlCreateTableStatement)element).Table);
					}

				case QueryElementType.DropTableStatement:
					{
						return
							Find(((SqlDropTableStatement)element).Table);
					}

				case QueryElementType.SelectClause:
					{
						return
							Find(((SqlSelectClause)element).TakeValue) ??
							Find(((SqlSelectClause)element).SkipValue) ??
							Find(((SqlSelectClause)element).Columns  );
					}

				case QueryElementType.SqlQuery:
					{
						return
							Find(((SelectQuery)element).Select ) ??
							Find(((SelectQuery)element).From   ) ??
							Find(((SelectQuery)element).Where  ) ??
							Find(((SelectQuery)element).GroupBy) ??
							Find(((SelectQuery)element).Having ) ??
							Find(((SelectQuery)element).OrderBy) ??
							(((SelectQuery)element).HasSetOperators ? Find(((SelectQuery)element).SetOperators) : null);
					}

				case QueryElementType.TruncateTableStatement:
					{
						return
							Find(((SqlTruncateTableStatement)element).Table);
					}

				case QueryElementType.CteClause:
					{
						return
							Find(((CteClause)element).Fields) ??
							Find(((CteClause)element).Body  );
					}

				case QueryElementType.WithClause:
					{
						return Find(((SqlWithClause)element).Clauses);
					}

				case QueryElementType.MergeStatement:
					{
						return
							Find(((SqlMergeStatement)element).Target    ) ??
							Find(((SqlMergeStatement)element).Source    ) ??
							Find(((SqlMergeStatement)element).On        ) ??
							Find(((SqlMergeStatement)element).Target    ) ??
							Find(((SqlMergeStatement)element).Operations);
					}

				case QueryElementType.MergeSourceTable:
					{
						return
							Find(((SqlMergeSourceTable)element).SourceEnumerable) ??
							Find(((SqlMergeSourceTable)element).SourceQuery     ) ??
							Find(((SqlMergeSourceTable)element).SourceFields    );
					}

				case QueryElementType.MergeOperationClause:
					{
						return
							Find(((SqlMergeOperationClause)element).Where      ) ??
							Find(((SqlMergeOperationClause)element).WhereDelete) ??
							Find(((SqlMergeOperationClause)element).Items      );
					}

				case QueryElementType.SqlValuesTable:
					{
						return 
							Find(((SqlValuesTable)element).Fields.Values          ) ??
							Find(((SqlValuesTable)element).Rows.SelectMany(r => r));
					}

				case QueryElementType.SqlField:
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlValue:
				case QueryElementType.SqlDataType:
				case QueryElementType.SqlAliasPlaceholder:
					break;

				default:
					throw new InvalidOperationException($"Find visitor not implemented for element {element.ElementType}");
			}

			return null;
		}

		#endregion

	}
}
