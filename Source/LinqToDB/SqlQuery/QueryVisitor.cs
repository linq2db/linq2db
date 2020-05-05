using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
		Func<IQueryElement, IQueryElement?>? _convert;

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

		#region Convert

		static TE[] ToArray<TK,TE>(IDictionary<TK,TE> dic)
		{
			var es = new TE[dic.Count];
			var i  = 0;

			foreach (var e in dic.Values)
				es[i++] = e;

			return es;
		}

		delegate T Clone<T>(T obj);

		public List<IQueryElement> Stack     { get; } = new List<IQueryElement>();
		public IQueryElement? ParentElement => Stack.Count == 0 ? null : Stack[Stack.Count - 1];

		public T Convert<T>(T element, Func<IQueryElement,IQueryElement> action)
			where T : class, IQueryElement
		{
			_visitedElements.Clear();
			_convert = action;
			return (T?)ConvertInternal(element) ?? element;
		}

		class ConvertScope : IDisposable
		{
			private QueryVisitor _visitor;

			public ConvertScope(QueryVisitor visitor, IQueryElement parent)
			{
				_visitor = visitor;
				_visitor.Stack.Add(parent);
			}

			public void Dispose()
			{
				_visitor.Stack.RemoveAt(_visitor.Stack.Count - 1);
			}
		}

		ConvertScope Scope(IQueryElement parent)
		{
			return new ConvertScope(this, parent);
		}

		void CorrectQueryHierarchy(SelectQuery? parentQuery)
		{
			if (parentQuery == null)
				return;
			new QueryVisitor().Visit(parentQuery, element =>
			{
				if (element is SelectQuery q)
					q.ParentSelect = parentQuery;
			});
			parentQuery.ParentSelect = null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void AddVisited(IQueryElement element, IQueryElement? newElement)
		{
			if (!_visitedElements.ContainsKey(element))
				_visitedElements[element] = newElement;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IQueryElement? GetCurrentReplaced(IQueryElement element)
		{
			if (_visitedElements.TryGetValue(element, out var replaced))
			{
				if (replaced != null && replaced != element)
				{
					while (replaced != null && _visitedElements.TryGetValue(replaced, out var another))
					{
						if (replaced == another)
							break;
						replaced = another;
					}
				}
				return replaced;
			}

			return null;
		}
		
		IQueryElement? ConvertInternal(IQueryElement? element)
		{
			if (element == null)
				return null;

			// if element manually added outside to VisistedElements as null, it will be processed continuously.
			// Useful when we have to duplicate such items, especially parameters
			var newElement = GetCurrentReplaced(element);
			if (newElement != null)
				return newElement;

			using (Scope(element))
			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction:
					{
						var func  = (SqlFunction)element;
						var parms = Convert(func.Parameters);

						if (parms != null && !ReferenceEquals(parms, func.Parameters))
							newElement = new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, parms);

						break;
					}

				case QueryElementType.SqlExpression:
					{
						var expr      = (SqlExpression)element;
						var parameter = Convert(expr.Parameters);

						if (parameter != null && !ReferenceEquals(parameter, expr.Parameters))
							newElement = new SqlExpression(expr.SystemType, expr.Expr, expr.Precedence, parameter);

						break;
					}

				case QueryElementType.SqlBinaryExpression:
					{
						var bexpr = (SqlBinaryExpression)element;
						var expr1 = (ISqlExpression?)ConvertInternal(bexpr.Expr1);
						var expr2 = (ISqlExpression?)ConvertInternal(bexpr.Expr2);

						if (expr1 != null && !ReferenceEquals(expr1, bexpr.Expr1) ||
							expr2 != null && !ReferenceEquals(expr2, bexpr.Expr2))
							newElement = new SqlBinaryExpression(bexpr.SystemType, expr1 ?? bexpr.Expr1, bexpr.Operation, expr2 ?? bexpr.Expr2, bexpr.Precedence);

						break;
					}

				case QueryElementType.SqlTable:
					{
						var table    = (SqlTable)element;
						var newTable = (SqlTable)_convert!(table)!;

						if (ReferenceEquals(newTable, table))
						{
							var targs = table.TableArguments == null || table.TableArguments.Length == 0 ?
								null : Convert(table.TableArguments);

							if (targs != null && !ReferenceEquals(table.TableArguments, targs))
							{
								var newFields = table.Fields.Values.Select(f => new SqlField(f));
								newTable = new SqlTable(table, newFields, targs);
							}
						}

						if (!ReferenceEquals(table, newTable))
						{
							AddVisited(table.All, newTable.All);
							foreach (var prevField in table.Fields.Values)
							{
								if (newTable.Fields.TryGetValue(prevField.Name, out var newField))
									AddVisited(prevField, newField);
							}
						}

						newElement = newTable;

						break;
					}

				case QueryElementType.SqlCteTable:
					{
						var table    = (SqlCteTable)element;
						var newTable = (SqlCteTable)_convert!(table)!;

						if (ReferenceEquals(newTable, table))
						{
							var cte = (CteClause?)ConvertInternal(table.Cte);
							var ce  = cte   != null && !ReferenceEquals(table.Cte, cte);

							if (ce)
							{
								var newFields = table.Fields.Values.Select(f => new SqlField(f));
								newTable = new SqlCteTable(table, newFields, cte!);
							}
						}

						if (!ReferenceEquals(table, newTable))
						{
							AddVisited(table.All, newTable.All);
							foreach (var prevField in table.Fields.Values)
							{
								if (newTable.Fields.TryGetValue(prevField.Name, out var newField))
									AddVisited(prevField, newField);
							}
						}

						newElement = newTable;

						break;
					}

				case QueryElementType.Column:
					{
						var col  = (SqlColumn)element;
						var expr = (ISqlExpression?)ConvertInternal(col.Expression);

						if (expr != null && !ReferenceEquals(expr, col.Expression))
							newElement = new SqlColumn(col.Parent, expr, col.RawAlias);

						break;
					}

				case QueryElementType.TableSource:
					{
						var table  = (SqlTableSource)element;
						var source = (ISqlTableSource?)ConvertInternal(table.Source);
						var joins  = Convert(table.Joins);

						List<ISqlExpression[]>? uk = null;
							if (table.HasUniqueKeys)
								uk = ConvertListArray(table.UniqueKeys, null);

							if (source != null && !ReferenceEquals(source, table.Source) ||
							joins  != null && !ReferenceEquals(table.Joins, joins))
							newElement = new SqlTableSource(
								source ?? table.Source,
								table._alias,
								joins ?? table.Joins,
								uk ?? (table.HasUniqueKeys ? table.UniqueKeys : null));

						break;
					}

				case QueryElementType.JoinedTable:
					{
						var join  = (SqlJoinedTable)element;
						var table = (SqlTableSource?)    ConvertInternal(join.Table    );
						var cond  = (SqlSearchCondition?)ConvertInternal(join.Condition);

						if (table != null && !ReferenceEquals(table, join.Table) ||
							cond  != null && !ReferenceEquals(cond,  join.Condition))
							newElement = new SqlJoinedTable(join.JoinType, table ?? join.Table, join.IsWeak, cond ?? join.Condition);

						break;
					}

				case QueryElementType.SearchCondition:
					{
						var sc    = (SqlSearchCondition)element;
						var conds = Convert(sc.Conditions);

						if (conds != null && !ReferenceEquals(sc.Conditions, conds))
							newElement = new SqlSearchCondition(conds);

						break;
					}

				case QueryElementType.Condition:
					{
						var c = (SqlCondition)element;
						var p = (ISqlPredicate?)ConvertInternal(c.Predicate);

						if (p != null && !ReferenceEquals(c.Predicate, p))
							newElement = new SqlCondition(c.IsNot, p, c.IsOr);

						break;
					}

				case QueryElementType.ExprPredicate:
					{
						var p = (SqlPredicate.Expr)element;
						var e = (ISqlExpression?)ConvertInternal(p.Expr1);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlPredicate.Expr(e, p.Precedence);

						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						var p = (SqlPredicate.NotExpr)element;
						var e = (ISqlExpression?)ConvertInternal(p.Expr1);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlPredicate.NotExpr(e, p.IsNot, p.Precedence);

						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						var p  = (SqlPredicate.ExprExpr)element;
						var e1 = (ISqlExpression?)ConvertInternal(p.Expr1);
						var e2 = (ISqlExpression?)ConvertInternal(p.Expr2);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) || e2 != null && !ReferenceEquals(p.Expr2, e2))
							newElement = new SqlPredicate.ExprExpr(e1 ?? p.Expr1, p.Operator, e2 ?? p.Expr2);

						break;
					}

				case QueryElementType.LikePredicate:
					{
						var p  = (SqlPredicate.Like)element;
						var e1 = (ISqlExpression?)ConvertInternal(p.Expr1 );
						var e2 = (ISqlExpression?)ConvertInternal(p.Expr2 );
						var es = (ISqlExpression?)ConvertInternal(p.Escape);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							es != null && !ReferenceEquals(p.Escape, es))
							newElement = new SqlPredicate.Like(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, es ?? p.Escape, p.IsSqlLike);

						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						var p = (SqlPredicate.Between)element;
						var e1 = (ISqlExpression?)ConvertInternal(p.Expr1);
						var e2 = (ISqlExpression?)ConvertInternal(p.Expr2);
						var e3 = (ISqlExpression?)ConvertInternal(p.Expr3);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							e3 != null && !ReferenceEquals(p.Expr3, e3))
							newElement = new SqlPredicate.Between(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, e3 ?? p.Expr3);

						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						var p = (SqlPredicate.IsNull)element;
						var e = (ISqlExpression?)ConvertInternal(p.Expr1);

						if (e != null && !ReferenceEquals(p.Expr1, e))
							newElement = new SqlPredicate.IsNull(e, p.IsNot);

						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						var p = (SqlPredicate.InSubQuery)element;
						var e = (ISqlExpression?)ConvertInternal(p.Expr1);
						var q = (SelectQuery?)   ConvertInternal(p.SubQuery);

						if (e != null && !ReferenceEquals(p.Expr1, e) || q != null && !ReferenceEquals(p.SubQuery, q))
							newElement = new SqlPredicate.InSubQuery(e ?? p.Expr1, p.IsNot, q ?? p.SubQuery);

						break;
					}

				case QueryElementType.InListPredicate:
					{
						var p = (SqlPredicate.InList)element;
						var e = (ISqlExpression?)ConvertInternal(p.Expr1);
						var v = Convert(p.Values);

						if (e != null && !ReferenceEquals(p.Expr1, e) || v != null && !ReferenceEquals(p.Values, v))
							newElement = new SqlPredicate.InList(e ?? p.Expr1, p.IsNot, v ?? p.Values);

						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						var p = (SqlPredicate.FuncLike)element;
						var f = (SqlFunction?)ConvertInternal(p.Function);

						if (f != null && !ReferenceEquals(p.Function, f))
							newElement = new SqlPredicate.FuncLike(f);

						break;
					}

				case QueryElementType.SetExpression:
					{
						var s = (SqlSetExpression)element;
						var c = (ISqlExpression?)ConvertInternal(s.Column    );
						var e = (ISqlExpression?)ConvertInternal(s.Expression);

						if (c != null && !ReferenceEquals(s.Column, c) || e != null && !ReferenceEquals(s.Expression, e))
							newElement = new SqlSetExpression(c ?? s.Column, e ?? s.Expression!);

						break;
					}

				case QueryElementType.InsertClause:
					{
						var s = (SqlInsertClause)element;
						var t = s.Into != null ? (SqlTable?)ConvertInternal(s.Into) : null;
						var i = Convert(s.Items);

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
						var t = s.Table != null ? (SqlTable?)ConvertInternal(s.Table) : null;
						var i = Convert(s.Items);
						var k = Convert(s.Keys );

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
						var selectQuery = (SelectQuery?)ConvertInternal(s.SelectQuery);
						var with        = s.With        != null ? (SqlWithClause?)ConvertInternal(s.With       ) : null;
						var ps          = ConvertSafe(s.Parameters);

						if (ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery)  ||
							with        != null && !ReferenceEquals(s.With,        with))
						{
							newElement = new SqlSelectStatement(selectQuery ?? s.SelectQuery);
							((SqlSelectStatement)newElement).Parameters.AddRange(ps ?? s.Parameters);
							((SqlSelectStatement)newElement).With = with ?? s.With;
							CorrectQueryHierarchy(((SqlSelectStatement) newElement).SelectQuery);
						}

						break;
					}

				case QueryElementType.InsertStatement:
					{
						var s = (SqlInsertStatement)element;
						var selectQuery = (SelectQuery?    )ConvertInternal(s.SelectQuery);
						var insert      = (SqlInsertClause?)ConvertInternal(s.Insert);
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With       ) : null;
						var ps          = ConvertSafe(s.Parameters);

						if (insert      != null && !ReferenceEquals(s.Insert,      insert)       ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery)  ||
							with        != null && !ReferenceEquals(s.With,        with))
						{
							newElement = new SqlInsertStatement(selectQuery ?? s.SelectQuery) { Insert = insert ?? s.Insert };
							((SqlInsertStatement)newElement).Parameters.AddRange(ps ?? s.Parameters);
							((SqlInsertStatement)newElement).With = with ?? s.With;
							CorrectQueryHierarchy(((SqlInsertStatement) newElement).SelectQuery);
						}

						break;
					}

				case QueryElementType.UpdateStatement:
					{
						var s = (SqlUpdateStatement)element;
						var selectQuery = (SelectQuery?    )ConvertInternal(s.SelectQuery);
						var update      = (SqlUpdateClause?)ConvertInternal(s.Update);
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With       ) : null;
						var ps          = ConvertSafe(s.Parameters);

						if (update      != null && !ReferenceEquals(s.Update,      update)       ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery)  ||
							with        != null && !ReferenceEquals(s.With,        with))
						{
							newElement = new SqlUpdateStatement(selectQuery ?? s.SelectQuery) { Update = update ?? s.Update };
							((SqlUpdateStatement)newElement).Parameters.AddRange(ps ?? s.Parameters);
							((SqlUpdateStatement)newElement).With = with ?? s.With;
							CorrectQueryHierarchy(((SqlUpdateStatement) newElement).SelectQuery);
						}

						break;
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						var s = (SqlInsertOrUpdateStatement)element;

						var selectQuery = (SelectQuery?    )ConvertInternal(s.SelectQuery);
						var insert      = (SqlInsertClause?)ConvertInternal(s.Insert);
						var update      = (SqlUpdateClause?)ConvertInternal(s.Update);
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With       ) : null;
						var ps          = ConvertSafe(s.Parameters);

						if (insert      != null && !ReferenceEquals(s.Insert,      insert)       ||
							update      != null && !ReferenceEquals(s.Update,      update)       ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery)  ||
							with        != null && !ReferenceEquals(s.With,        with))
						{
							newElement = new SqlInsertOrUpdateStatement(selectQuery ?? s.SelectQuery) { Insert = insert ?? s.Insert, Update = update ?? s.Update };
							((SqlInsertOrUpdateStatement)newElement).Parameters.AddRange(ps ?? s.Parameters);
							((SqlInsertOrUpdateStatement)newElement).With = with ?? s.With;
							CorrectQueryHierarchy(((SqlInsertOrUpdateStatement) newElement).SelectQuery);
						}

						break;
					}

				case QueryElementType.DeleteStatement:
					{
						var s = (SqlDeleteStatement)element;
						var selectQuery = s.SelectQuery != null ? (SelectQuery?)   ConvertInternal(s.SelectQuery) : null;
						var table       = s.Table       != null ? (SqlTable?)      ConvertInternal(s.Table      ) : null;
						var top         = s.Top         != null ? (ISqlExpression?)ConvertInternal(s.Top        ) : null;
						var with        = s.With        != null ? (SqlWithClause?) ConvertInternal(s.With       ) : null;
						var ps          = ConvertSafe(s.Parameters);

						if (table       != null && !ReferenceEquals(s.Table,       table)       ||
							top         != null && !ReferenceEquals(s.Top,         top)         ||
							ps          != null && !ReferenceEquals(s.Parameters,  ps)          ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery) ||
							with        != null && !ReferenceEquals(s.With,        with))
						{
							newElement = new SqlDeleteStatement
							{
								Table                = table       ?? s.Table,
								SelectQuery          = selectQuery ?? s.SelectQuery,
								Top                  = top         ?? s.Top!,
								IsParameterDependent = s.IsParameterDependent
							};
							((SqlDeleteStatement)newElement).Parameters.AddRange(ps ?? s.Parameters);
							((SqlDeleteStatement)newElement).With = with ?? s.With;
							CorrectQueryHierarchy(((SqlDeleteStatement)newElement).SelectQuery);
						}

						break;
					}

				case QueryElementType.CreateTableStatement:
					{
						var s  = (SqlCreateTableStatement)element;
						var t  = s.Table != null ? (SqlTable?)ConvertInternal(s.Table) : null;
						var ps = ConvertSafe(s.Parameters);

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
						var s  = (SqlDropTableStatement)element;
						var t  = s.Table != null ? (SqlTable?)ConvertInternal(s.Table) : null;
						var ps = ConvertSafe(s.Parameters);

						if (t  != null && !ReferenceEquals(s.Table, t) ||
							ps != null && !ReferenceEquals(s.Parameters,  ps))
						{
							newElement = new SqlDropTableStatement(s.IfExists) { Table = t ?? s.Table };
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
						var cols = Convert(sc.Columns, CloneColumn);
						var take = (ISqlExpression?)ConvertInternal(sc.TakeValue);
						var skip = (ISqlExpression?)ConvertInternal(sc.SkipValue);

						if (
							cols != null && !ReferenceEquals(sc.Columns,   cols) ||
							take != null && !ReferenceEquals(sc.TakeValue, take) ||
							skip != null && !ReferenceEquals(sc.SkipValue, skip))
						{
							newElement = new SqlSelectClause(sc.IsDistinct, take ?? sc.TakeValue, sc.TakeHints, skip ?? sc.SkipValue, cols ?? sc.Columns);
							((SqlSelectClause)newElement).SetSqlQuery(sc.SelectQuery);
						}

						static SqlColumn CloneColumn(SqlColumn column) => new SqlColumn(column.Parent!, column.Expression);
						break;
					}

				case QueryElementType.FromClause:
					{
						var fc   = (SqlFromClause)element;
						var ts = Convert(fc.Tables);

						if (ts != null && !ReferenceEquals(fc.Tables, ts))
						{
							newElement = new SqlFromClause(ts ?? fc.Tables);
							((SqlFromClause)newElement).SetSqlQuery(fc.SelectQuery);
						}

						break;
					}

				case QueryElementType.WhereClause:
					{
						var wc   = (SqlWhereClause)element;
						var cond = (SqlSearchCondition?)ConvertInternal(wc.SearchCondition);

						if (cond != null && !ReferenceEquals(wc.SearchCondition, cond))
						{
							newElement = new SqlWhereClause(cond ?? wc.SearchCondition);
							((SqlWhereClause)newElement).SetSqlQuery(wc.SelectQuery);
						}

						break;
					}

				case QueryElementType.GroupByClause:
					{
						var gc = (SqlGroupByClause)element;
						var es = Convert(gc.Items);

						if (es != null && !ReferenceEquals(gc.Items, es))
						{
							newElement = new SqlGroupByClause(es ?? gc.Items);
							((SqlGroupByClause)newElement).SetSqlQuery(gc.SelectQuery);
						}

						break;
					}

				case QueryElementType.OrderByClause:
					{
						var oc = (SqlOrderByClause)element;
						var es = Convert(oc.Items);

						if (es != null && !ReferenceEquals(oc.Items, es))
						{
							newElement = new SqlOrderByClause(es ?? oc.Items);
							((SqlOrderByClause)newElement).SetSqlQuery(oc.SelectQuery);
						}

						break;
					}

				case QueryElementType.OrderByItem:
					{
						var i = (SqlOrderByItem)element;
						var e = (ISqlExpression?)ConvertInternal(i.Expression);

						if (e != null && !ReferenceEquals(i.Expression, e))
							newElement = new SqlOrderByItem(e, i.IsDescending);

						break;
					}

				case QueryElementType.SetOperator:
					{
						var u = (SqlSetOperator)element;
						var q = (SelectQuery?)ConvertInternal(u.SelectQuery);

						if (q != null && !ReferenceEquals(u.SelectQuery, q))
							newElement = new SqlSetOperator(q, u.Operation);

						break;
					}

				case QueryElementType.SqlQuery:
					{
						var q = (SelectQuery)element;

						var fc = (SqlFromClause?)   ConvertInternal(q.From   ) ?? q.From;
						var sc = (SqlSelectClause?) ConvertInternal(q.Select ) ?? q.Select;
						var wc = (SqlWhereClause?)  ConvertInternal(q.Where  ) ?? q.Where;
						var gc = (SqlGroupByClause?)ConvertInternal(q.GroupBy) ?? q.GroupBy;
						var hc = (SqlWhereClause?)  ConvertInternal(q.Having ) ?? q.Having;
						var oc = (SqlOrderByClause?)ConvertInternal(q.OrderBy) ?? q.OrderBy;
						var us = q.HasSetOperators ? Convert(q.SetOperators) : q.SetOperators;

						List<ISqlExpression[]>? uk = null;
						if (q.HasUniqueKeys)
							uk = ConvertListArray(q.UniqueKeys, null) ?? q.UniqueKeys;

						if (   !ReferenceEquals(fc, q.From)
						    || !ReferenceEquals(sc, q.Select)
						    || !ReferenceEquals(wc, q.Where)
						    || !ReferenceEquals(gc, q.GroupBy)
						    || !ReferenceEquals(hc, q.Having)
						    || !ReferenceEquals(oc, q.OrderBy)
						    || us != null && !ReferenceEquals(us, q.SetOperators)
							|| uk != null && !ReferenceEquals(uk, q.UniqueKeys)
						)
						{
							var nq = new SelectQuery();

							var objTree = new Dictionary<ICloneableElement, ICloneableElement>();

							if (ReferenceEquals(sc, q.Select))
								sc = new SqlSelectClause (nq, sc, objTree, e => e is SqlColumn c && c.Parent == q);
							if (ReferenceEquals(fc, q.From))
								fc = new SqlFromClause   (nq, fc, objTree, e => false);
							if (ReferenceEquals(wc, q.Where))
								wc = new SqlWhereClause  (nq, wc, objTree, e => false);
							if (ReferenceEquals(gc, q.GroupBy))
								gc = new SqlGroupByClause(nq, gc, objTree, e => false);
							if (ReferenceEquals(hc, q.Having))
								hc = new SqlWhereClause  (nq, hc, objTree, e => false);
							if (ReferenceEquals(oc, q.OrderBy))
								oc = new SqlOrderByClause(nq, oc, objTree, e => false);
							if (us == null || ReferenceEquals(us, q.SetOperators))
								us = new List<SqlSetOperator>(us ?? q.SetOperators);

							AddVisited(q.All, nq.All);

							nq.Init(sc, fc, wc, gc, hc, oc, us, uk,
								q.ParentSelect,
								q.IsParameterDependent);

							// update visited in case if columns were cloned
							foreach (var pair in objTree)
							{
								if (pair.Key is IQueryElement queryElement)
								{
									_visitedElements.Remove(queryElement);
									_visitedElements.Add(queryElement, (IQueryElement)pair.Value);
								}
							}

							newElement = nq;
						}
						break;
					}

				case QueryElementType.MergeStatement:
					{
						var merge = (SqlMergeStatement)element;

						var target     = (SqlTableSource?)     ConvertInternal(merge.Target);
						var source     = (SqlMergeSourceTable?)ConvertInternal(merge.Source);
						var on         = (SqlSearchCondition?) ConvertInternal(merge.On);
						var operations = ConvertSafe(merge.Operations);

						if (target     != null && !ReferenceEquals(merge.Target, target) ||
							source     != null && !ReferenceEquals(merge.Source, source) ||
							on         != null && !ReferenceEquals(merge.On, on) ||
							operations != null && !ReferenceEquals(merge.Operations, operations))
						{
							newElement = new SqlMergeStatement(
								merge.Hint,
								target ?? merge.Target,
								source ?? merge.Source,
								on ?? merge.On,
								operations ?? merge.Operations);
						}

						break;
					}

				case QueryElementType.MergeSourceTable:
					{
						var source = (SqlMergeSourceTable)element;

						var enumerableSource = (SqlValuesTable?)ConvertInternal(source.SourceEnumerable);
						var querySource      = (SelectQuery?)   ConvertInternal(source.SourceQuery);
						var fields           = ConvertSafe(source.SourceFields);

						if (enumerableSource != null && !ReferenceEquals(source.SourceEnumerable, enumerableSource) ||
							querySource      != null && !ReferenceEquals(source.SourceQuery, querySource)           ||
							fields           != null && !ReferenceEquals(source.SourceFields, fields))
						{
							newElement = new SqlMergeSourceTable(
								source.SourceID,
								enumerableSource ?? source.SourceEnumerable!,
								querySource ?? source.SourceQuery!,
								fields ?? source.SourceFields);
						}

							break;
						}

				case QueryElementType.SqlValuesTable:
					{
						var table = (SqlValuesTable)element;

						var covertedRows = new List<IList<ISqlExpression>>();
						var rowsConverted = false;

						foreach (var row in table.Rows)
						{
							var convertedRow = ConvertSafe(row);
							rowsConverted    = rowsConverted || (row != null && !ReferenceEquals(convertedRow, row));

							covertedRows.Add(convertedRow ?? row!);
						}

						var fields1 = ToArray(table.Fields);
						var fields2 = Convert(fields1, f => new SqlField(f));

						var fieldsConverted = fields2 != null && !ReferenceEquals(fields1, fields2);

						if (fieldsConverted || rowsConverted)
						{
							if (!fieldsConverted)
							{
								fields2 = fields1;

								for (var i = 0; i < fields2.Length; i++)
								{
									var field = fields2[i];

									fields2[i] = new SqlField(field);

									_visitedElements[field] = fields2[i];
								}
							}

							newElement = new SqlValuesTable(fields2!, rowsConverted ? covertedRows : table.Rows);
						}

						break;
					}

				case QueryElementType.MergeOperationClause:
					{
						var operation = (SqlMergeOperationClause)element;

						var where       = (SqlSearchCondition?)ConvertInternal(operation.Where);
						var whereDelete = (SqlSearchCondition?)ConvertInternal(operation.WhereDelete);
						var items       = ConvertSafe(operation.Items);

						if (where       != null && !ReferenceEquals(operation.Where, where)             ||
							whereDelete != null && !ReferenceEquals(operation.WhereDelete, whereDelete) ||
							items       != null && !ReferenceEquals(operation.Items, items))
						{
							newElement = new SqlMergeOperationClause(
								operation.OperationType,
								where ?? operation.Where,
								whereDelete ?? operation.WhereDelete,
								items ?? operation.Items);
						}

						break;
					}

				case QueryElementType.TruncateTableStatement:
					{
						var truncate = (SqlTruncateTableStatement)element;

						if (truncate.Table != null)
						{
							var table = (SqlTable?)ConvertInternal(truncate.Table);

							if (table != null && !ReferenceEquals(truncate.Table, table))
							{
								newElement = new SqlTruncateTableStatement()
									{
										Table = table,
										ResetIdentity = truncate.ResetIdentity
									};
							}
						}

						break;
					}

				case QueryElementType.SqlRawSqlTable:
					{
						var table   = (SqlRawSqlTable)element;
						var fields1 = ToArray(table.Fields);
						var fields2 = Convert(fields1, f => new SqlField(f));
						var targs   = table.Parameters == null || table.Parameters.Length == 0 ?
							null : Convert(table.Parameters);

						var fe = fields2 != null && !ReferenceEquals(fields1, fields2);
						var ta = targs != null && !ReferenceEquals(table.Parameters, targs);

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

							newElement = new SqlRawSqlTable(table, fields2!, targs ?? table.Parameters!);

							_visitedElements[((SqlRawSqlTable)newElement).All] = table.All;
						}

						break;
					}

				case QueryElementType.CteClause:
					{
						var cte = (CteClause)element;

						if (new QueryVisitor().Find(cte.Body, e => e == cte) == null)
						{
							// non-recursive
							var body   = (SelectQuery?)ConvertInternal(cte.Body);
							var fields = Convert(cte.Fields!);

							if (body   != null && !ReferenceEquals(cte.Body, body) ||
								fields != null && !ReferenceEquals(cte.Fields, fields))
							{
								newElement = new CteClause(
									body ?? cte.Body,
									fields ?? cte.Fields!,
									cte.ObjectType,
									cte.IsRecursive,
									cte.Name);
							}
						}
						else
						{
							var newCte = new CteClause(cte.ObjectType, cte.IsRecursive, cte.Name);

							_visitedElements.Add(cte, newCte);

							var body   = (SelectQuery?)ConvertInternal(cte.Body);
							var fields = Convert(cte.Fields!);

							newCte.Init(body ?? cte.Body, fields ?? cte.Fields!);

							var elem = _convert!(newCte) ?? newCte;
							_visitedElements[cte] = elem;

							return elem;
						}

						break;
					}

				case QueryElementType.WithClause:
					{
						var with = (SqlWithClause)element;

						var clauses = ConvertSafe(with.Clauses);

						if (clauses != null && !ReferenceEquals(with.Clauses, clauses))
							newElement = new SqlWithClause()
							{
								Clauses = clauses
							};

						break;
					}

				case QueryElementType.SqlField           :
				case QueryElementType.SqlParameter       :
				case QueryElementType.SqlValue           :
				case QueryElementType.SqlDataType        :
				case QueryElementType.SqlAliasPlaceholder:
					break;

				default:
					throw new InvalidOperationException($"Convert visitor not implemented for element {element.ElementType}");
			}

			newElement = newElement == null ? _convert!(element) : (_convert!(newElement) ?? newElement);

			AddVisited(element, newElement);

			return newElement;
		}

		T[]? Convert<T>(T[] arr)
			where T : class, IQueryElement
		{
			return Convert(arr, null);
		}

		T[]? Convert<T>(T[] arr1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			T[]? arr2 = null;

			for (var i = 0; i < arr1.Length; i++)
			{
				var elem1 = arr1[i];
				var elem2 = (T?)ConvertInternal(elem1);

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

		List<T>? ConvertSafe<T>(IList<T> list)
			where T : class, IQueryElement
		{
			return ConvertSafe(list, null);
		}

		List<T>? ConvertSafe<T>(IList<T> list1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			List<T>? list2 = null;

			for (var i = 0; i < list1.Count; i++)
			{
				var elem1 = list1[i];
				var elem2 = ConvertInternal(elem1) as T;

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

		List<T>? Convert<T>(List<T> list)
			where T : class, IQueryElement
		{
			return Convert(list, null);
		}

		List<T>? Convert<T>(List<T> list1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			List<T>? list2 = null;

			for (var i = 0; i < list1.Count; i++)
			{
				var elem1 = list1[i];
				var elem2 = (T?)ConvertInternal(elem1);

				if (elem2 != null && !ReferenceEquals(elem1, elem2))
				{
					if (list2 == null)
					{
						list2 = new List<T>(list1.Count);

						for (var j = 0; j < i; j++)
						{
							var elem = list1[j];
							if (clone != null)
								_visitedElements[elem] = elem = clone(elem);

							list2.Add(elem);
						}
					}

					list2.Add(elem2);
				}
				else if (list2 != null)
				{
					if (clone != null)
						_visitedElements[elem1] = elem1 = clone(elem1);

					list2.Add(elem1);
				}
			}

			return list2;
		}

		List<T[]>? ConvertListArray<T>(List<T[]> list1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			List<T[]>? list2 = null;

			for (var i = 0; i < list1.Count; i++)
			{
				var elem1 = list1[i];
				var elem2 = Convert(elem1);

				if (elem2 != null && !ReferenceEquals(elem1, elem2))
				{
					if (list2 == null)
					{
						list2 = new List<T[]>(list1.Count);

						for (var j = 0; j < i; j++)
							list2.Add(clone == null ? list1[j] : list1[j].Select(e => clone(e)).ToArray() );
					}

					list2.Add(elem2);
				}
				else
					list2?.Add(clone == null ? elem1 : elem1.Select(e => clone(e)).ToArray());
			}

			return list2;
		}

		#endregion
	}
}
