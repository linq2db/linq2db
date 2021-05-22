using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public readonly struct QueryParentVisitor<TContext>
	{
		public   readonly Dictionary<IQueryElement,IQueryElement?>  VisitedElements;

		readonly TContext?                           _context;
		readonly bool                                _all;
		readonly Func<TContext, IQueryElement,bool>? _visit;
		readonly Func<IQueryElement,bool>?           _visitStatic;

		public QueryParentVisitor(TContext context, bool all, Func<TContext, IQueryElement, bool> visit)
		{
			_context        = context;
			_all            = all;
			_visit          = visit;
			_visitStatic    = null;
			VisitedElements = new();
		}

		public QueryParentVisitor(bool all, Func<IQueryElement, bool> visit)
		{
			_context        = default;
			_all            = all;
			_visit          = null;
			_visitStatic    = visit;
			VisitedElements = new();
		}

		public void Visit(IQueryElement? element)
		{
			if (element == null || !_all && VisitedElements.ContainsKey(element))
				return;
			
			if (!_all)
				VisitedElements.Add(element, element);

			if (_visitStatic != null ? !_visitStatic(element) : !_visit!(_context!, element))
				return;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction:
					{
						VisitX((SqlFunction)element);
						break;
					}

				case QueryElementType.SqlExpression:
				{
					VisitX((SqlExpression)element);
					break;
				}

				case QueryElementType.SqlObjectExpression:
				{
					VisitX((SqlObjectExpression)element);
					break;
				}

				case QueryElementType.SqlBinaryExpression:
					{
						Visit(((SqlBinaryExpression)element).Expr1);
						Visit(((SqlBinaryExpression)element).Expr2);
						break;
					}

				case QueryElementType.SqlTable:
					{
						VisitX((SqlTable)element);
						break;
					}

				case QueryElementType.SqlCteTable:
					{
						VisitX((SqlCteTable)element);
						break;
					}

				case QueryElementType.SqlRawSqlTable:
					{
						VisitX((SqlRawSqlTable)element);
						break;
					}

				case QueryElementType.OutputClause:
					{
						VisitX((SqlOutputClause)element);
						break;
					}

				case QueryElementType.Column:
					{
						Visit(((SqlColumn)element).Expression);
						break;
					}

				case QueryElementType.TableSource:
					{
						VisitX((SqlTableSource)element);
						break;
					}

				case QueryElementType.JoinedTable:
					{
						Visit(((SqlJoinedTable)element).Table);
						Visit(((SqlJoinedTable)element).Condition);
						break;
					}

				case QueryElementType.SearchCondition:
					{
						VisitX((SqlSearchCondition)element);
						break;
					}

				case QueryElementType.Condition:
					{
						Visit(((SqlCondition)element).Predicate);
						break;
					}

				case QueryElementType.ExprPredicate:
					{
						Visit(((SqlPredicate.Expr)element).Expr1);
						break;
					}

				case QueryElementType.NotExprPredicate:
					{
						Visit(((SqlPredicate.NotExpr)element).Expr1);
						break;
					}

				case QueryElementType.ExprExprPredicate:
					{
						Visit(((SqlPredicate.ExprExpr)element).Expr1);
						Visit(((SqlPredicate.ExprExpr)element).Expr2);
						break;
					}

				case QueryElementType.LikePredicate:
					{
						Visit(((SqlPredicate.Like)element).Expr1);
						Visit(((SqlPredicate.Like)element).Expr2);
						Visit(((SqlPredicate.Like)element).Escape);
						break;
					}

				case QueryElementType.SearchStringPredicate:
					{
						Visit(((SqlPredicate.SearchString)element).Expr1);
						Visit(((SqlPredicate.SearchString)element).Expr2);
						break;
					}

				case QueryElementType.BetweenPredicate:
					{
						Visit(((SqlPredicate.Between)element).Expr1);
						Visit(((SqlPredicate.Between)element).Expr2);
						Visit(((SqlPredicate.Between)element).Expr3);
						break;
					}

				case QueryElementType.IsTruePredicate:
					{
						Visit(((SqlPredicate.IsTrue)element).Expr1);
						Visit(((SqlPredicate.IsTrue)element).TrueValue);
						Visit(((SqlPredicate.IsTrue)element).FalseValue);
						break;
					}

				case QueryElementType.IsNullPredicate:
					{
						Visit(((SqlPredicate.IsNull)element).Expr1);
						break;
					}

				case QueryElementType.IsDistinctPredicate:
					{
						var p = (SqlPredicate.IsDistinct)element;
						Visit(p.Expr1);
						Visit(p.Expr2);
						break;
					}

				case QueryElementType.InSubQueryPredicate:
					{
						Visit(((SqlPredicate.InSubQuery)element).Expr1);
						Visit(((SqlPredicate.InSubQuery)element).SubQuery);
						break;
					}

				case QueryElementType.InListPredicate:
					{
						VisitX((SqlPredicate.InList)element);
						break;
					}

				case QueryElementType.FuncLikePredicate:
					{
						Visit(((SqlPredicate.FuncLike)element).Function);
						break;
					}

				case QueryElementType.SetExpression:
					{
						Visit(((SqlSetExpression)element).Column);
						Visit(((SqlSetExpression)element).Expression);
						break;
					}

				case QueryElementType.InsertClause:
					{
						VisitX((SqlInsertClause)element);
						break;
					}

				case QueryElementType.UpdateClause:
					{
						VisitX((SqlUpdateClause)element);
						break;
					}

				case QueryElementType.CteClause:
					{
						VisitX((CteClause)element);
						break;
					}

				case QueryElementType.WithClause:
					{
						VisitX(((SqlWithClause)element));
						break;
					}

				case QueryElementType.SelectStatement:
					{
						Visit(((SqlSelectStatement)element).Tag);
						Visit(((SqlSelectStatement)element).With);
						Visit(((SqlSelectStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.InsertStatement:
					{
						Visit(((SqlInsertStatement)element).Tag);
						Visit(((SqlInsertStatement)element).With);
						Visit(((SqlInsertStatement)element).Insert);
						Visit(((SqlInsertStatement)element).Output);
						Visit(((SqlInsertStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.UpdateStatement:
					{
						Visit(((SqlUpdateStatement)element).Tag);
						Visit(((SqlUpdateStatement)element).With);
						Visit(((SqlUpdateStatement)element).Update);
						Visit(((SqlUpdateStatement)element).Output);
						Visit(((SqlUpdateStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						Visit(((SqlInsertOrUpdateStatement)element).Tag);
						Visit(((SqlInsertOrUpdateStatement)element).With);
						Visit(((SqlInsertOrUpdateStatement)element).Insert);
						Visit(((SqlInsertOrUpdateStatement)element).Update);
						Visit(((SqlInsertOrUpdateStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.DeleteStatement:
					{
						Visit(((SqlDeleteStatement)element).Tag);
						Visit(((SqlDeleteStatement)element).With);
						Visit(((SqlDeleteStatement)element).Table);
						Visit(((SqlDeleteStatement)element).Output);
						Visit(((SqlDeleteStatement)element).Top);
						Visit(((SqlDeleteStatement)element).SelectQuery);
						break;
					}

				case QueryElementType.CreateTableStatement:
					{
						Visit(((SqlCreateTableStatement)element).Tag);
						if (((SqlCreateTableStatement)element).Table != null)
							Visit(((SqlCreateTableStatement)element).Table);
						break;
					}

				case QueryElementType.DropTableStatement:
					{
						Visit(((SqlDropTableStatement)element).Tag);
						if (((SqlDropTableStatement)element).Table != null)
							Visit(((SqlDropTableStatement)element).Table);
						break;
					}

				case QueryElementType.TruncateTableStatement:
					{
						Visit(((SqlTruncateTableStatement)element).Tag);
						if (((SqlTruncateTableStatement)element).Table != null)
							Visit(((SqlTruncateTableStatement)element).Table);
						break;
					}

				case QueryElementType.SelectClause:
					{
						VisitX((SqlSelectClause)element);
						break;
					}

				case QueryElementType.FromClause:
					{
						VisitX((SqlFromClause)element);
						break;
					}

				case QueryElementType.WhereClause:
					{
						Visit(((SqlWhereClause)element).SearchCondition);
						break;
					}

				case QueryElementType.GroupByClause:
					{
						VisitX((SqlGroupByClause)element);
						break;
					}

				case QueryElementType.GroupingSet:
					{
						VisitX((SqlGroupingSet)element);
						break;
					}

				case QueryElementType.OrderByClause:
					{
						VisitX((SqlOrderByClause)element);
						break;
					}

				case QueryElementType.OrderByItem:
					{
						Visit(((SqlOrderByItem)element).Expression);
						break;
					}

				case QueryElementType.SetOperator:
					{
						Visit(((SqlSetOperator)element).SelectQuery);
						break;
					}

				case QueryElementType.SqlQuery:
					{
						if (_all)
						{
							if (VisitedElements.ContainsKey(element))
								return;
							VisitedElements.Add(element, element);
						}

						VisitX((SelectQuery)element);
						break;
					}

				case QueryElementType.MergeStatement:
					VisitX((SqlMergeStatement)element);
					break;

				case QueryElementType.MultiInsertStatement:
					VisitX((SqlMultiInsertStatement)element);
					break;

				case QueryElementType.ConditionalInsertClause:
					VisitX((SqlConditionalInsertClause)element);
					break;

				case QueryElementType.MergeSourceTable:
					VisitX((SqlTableLikeSource)element);
					break;

				case QueryElementType.SqlValuesTable:
					VisitX((SqlValuesTable)element);
					break;

				case QueryElementType.MergeOperationClause:
					VisitX((SqlMergeOperationClause)element);
					break;

				case QueryElementType.SqlField:
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlValue:
				case QueryElementType.SqlDataType:
				case QueryElementType.SqlAliasPlaceholder:
				case QueryElementType.Comment:
					break;

				default:
					throw new InvalidOperationException($"Visit visitor not implemented for element {element.ElementType}");
			}
		}

		void VisitX(SelectQuery q)
		{
			Visit(q.Select);
			Visit(q.From);
			Visit(q.Where);
			Visit(q.GroupBy);
			Visit(q.Having);
			Visit(q.OrderBy);

			if (q.HasSetOperators)
			{
				foreach (var i in q.SetOperators)
				{
					if (i.SelectQuery == q)
						throw new InvalidOperationException();

					Visit(i);
				}
			}

			// decided to do not enumerate unique keys
//			if (q.HasUniqueKeys)
//				foreach (var keyList in q.UniqueKeys)
//				{
//					VisitX(keyList);
//				}
		}

		void VisitX(SqlOrderByClause element)
		{
			foreach (var i in element.Items) Visit(i);
		}

		void VisitX(SqlGroupByClause element)
		{
			foreach (var i in element.Items) Visit(i);
		}

		void VisitX(SqlGroupingSet element)
		{
			foreach (var i in element.Items) Visit(i);
		}

		void VisitX(SqlFromClause element)
		{
			foreach (var t in element.Tables) Visit(t);
		}

		void VisitX(SqlSelectClause sc)
		{
			Visit(sc.TakeValue);
			Visit(sc.SkipValue);

			foreach (var c in sc.Columns) Visit(c);
		}

		void VisitX(SqlUpdateClause sc)
		{
			if (sc.Table != null)
				Visit(sc.Table);

			foreach (var c in sc.Items) Visit(c);
			foreach (var c in sc.Keys ) Visit(c);
		}

		void VisitX(CteClause sc)
		{
			foreach (var c in sc.Fields!) Visit(c);
			Visit(sc.Body);
		}

		void VisitX(SqlInsertClause sc)
		{
			if (sc.Into != null)
				Visit(sc.Into);

			foreach (var c in sc.Items) Visit(c);
		}

		void VisitX(SqlPredicate.InList p)
		{
			Visit(p.Expr1);
			foreach (var value in p.Values) Visit(value);
		}

		void VisitX(SqlSearchCondition element)
		{
			foreach (var c in element.Conditions) Visit(c);
		}

		void VisitX(SqlTableSource table)
		{
			Visit(table.Source);
			foreach (var j in table.Joins) Visit(j);
		}

		void VisitX(SqlTable table)
		{
			if (table == null)
				return;

			Visit(table.All);
			foreach (var field in table.Fields) Visit(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit(a);
		}

		void VisitX(SqlOutputClause outputClause)
		{
			if (outputClause == null)
				return;

			VisitX(outputClause.SourceTable);
			VisitX(outputClause.DeletedTable);
			VisitX(outputClause.InsertedTable);
			VisitX(outputClause.OutputTable);
			if (outputClause.OutputQuery != null)
				VisitX(outputClause.OutputQuery);

			if (outputClause.HasOutputItems)
				foreach (var item in outputClause.OutputItems)
					Visit(item);
		}

		void VisitX(SqlWithClause element)
		{
			foreach (var clause in element.Clauses) Visit(clause);
		}

		void VisitX(SqlCteTable table)
		{
			Visit(table.All);
			foreach (var field in table.Fields) Visit(field);

			if (table.TableArguments != null)
				foreach (var a in table.TableArguments) Visit(a);

//			Visit(table.CTE);
		}

		void VisitX(SqlRawSqlTable table)
		{
			Visit(table.All);
			foreach (var field in table.Fields) Visit(field);

			if (table.Parameters != null)
				foreach (var a in table.Parameters) Visit(a);
		}

		void VisitX(SqlExpression element)
		{
			foreach (var v in element.Parameters) Visit(v);
		}

		void VisitX(SqlObjectExpression element)
		{
			foreach (var v in element.InfoParameters) Visit(v.Sql);
		}

		void VisitX(SqlFunction element)
		{
			foreach (var p in element.Parameters) Visit(p);
		}

		void VisitX(SqlMergeStatement element)
		{
			Visit(element.Tag);
			Visit(element.Target);
			Visit(element.Source);
			Visit(element.On);

			foreach (var operation in element.Operations)
				Visit(operation);
		}

		void VisitX(SqlMultiInsertStatement element)
		{
			Visit(element.Source);

			foreach (var insert in element.Inserts)
				Visit(insert);
		}

		void VisitX(SqlConditionalInsertClause element)
		{
			Visit(element.When);
			Visit(element.Insert);
		}

		void VisitX(SqlTableLikeSource element)
		{
			Visit(element.Source);

			foreach (var field in element.SourceFields)
				Visit(field);
		}

		void VisitX(SqlValuesTable element)
		{
			foreach (var field in element.Fields)
				Visit(field);

			if (element.Rows != null)
				foreach (var row in element.Rows)
					foreach (var value in row)
						Visit(value);
		}

		void VisitX(SqlMergeOperationClause element)
		{
			Visit(element.Where);
			Visit(element.WhereDelete);

			foreach (var item in element.Items)
				Visit(item);
		}
	}
}
