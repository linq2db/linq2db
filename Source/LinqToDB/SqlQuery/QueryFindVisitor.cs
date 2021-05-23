using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public readonly struct QueryFindVisitor<TContext>
	{
		private readonly HashSet<IQueryElement>               _visitedFind;
		private readonly TContext?                             _context;
		private readonly Func<TContext, IQueryElement, bool>? _find;
		private readonly Func<IQueryElement, bool>?           _findStatic;

		public QueryFindVisitor(TContext context, Func<TContext, IQueryElement, bool> find)
		{
			_context     = context;
			_find        = find;
			_findStatic  = null;
			_visitedFind = new();
		}

		public QueryFindVisitor(Func<IQueryElement, bool> find)
		{
			_context     = default;
			_find        = null;
			_findStatic  = find;
			_visitedFind = new();
		}

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

		IQueryElement? FindX(SqlObjectExpression oe)
		{
			foreach (var item in oe.InfoParameters)
			{
				var e = Find(item.Sql);
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

		public IQueryElement? Find(IQueryElement? element)
		{
			if (element == null || !_visitedFind.Add(element))
				return null;

			if (_findStatic != null ? _findStatic(element) : _find!(_context!, element))
				return element;

			switch (element.ElementType)
			{
				case QueryElementType.SqlFunction         : return Find(((SqlFunction)          element).Parameters     );
				case QueryElementType.SqlExpression       : return Find(((SqlExpression)        element).Parameters     );
				case QueryElementType.SqlObjectExpression : return FindX(((SqlObjectExpression) element)                );
				case QueryElementType.Column              : return Find(((SqlColumn)            element).Expression     );
				case QueryElementType.SearchCondition     : return FindX((SqlSearchCondition)   element                 );
				case QueryElementType.Condition           : return Find(((SqlCondition)         element).Predicate      );
				case QueryElementType.ExprPredicate       : return Find(((SqlPredicate.Expr)    element).Expr1          );
				case QueryElementType.NotExprPredicate    : return Find(((SqlPredicate.NotExpr) element).Expr1          );
				case QueryElementType.IsNullPredicate     : return Find(((SqlPredicate.IsNull)  element).Expr1          );
				case QueryElementType.FromClause          : return Find(((SqlFromClause)        element).Tables         );
				case QueryElementType.WhereClause         : return Find(((SqlWhereClause)       element).SearchCondition);
				case QueryElementType.GroupByClause       : return Find(((SqlGroupByClause)     element).Items          );
				case QueryElementType.GroupingSet         : return Find(((SqlGroupingSet)       element).Items          );
				case QueryElementType.OrderByClause       : return Find(((SqlOrderByClause)     element).Items          );
				case QueryElementType.OrderByItem         : return Find(((SqlOrderByItem)       element).Expression     );
				case QueryElementType.SetOperator         : return Find(((SqlSetOperator)       element).SelectQuery    );
				case QueryElementType.FuncLikePredicate   : return Find(((SqlPredicate.FuncLike)element).Function       );

				case QueryElementType.IsTruePredicate:
					{
						return 
							Find(((SqlPredicate.IsTrue)element).Expr1) ?? 
							Find(((SqlPredicate.IsTrue)element).TrueValue) ??
							Find(((SqlPredicate.IsTrue)element).FalseValue);
					}

				case QueryElementType.IsDistinctPredicate:
					{
						var p = (SqlPredicate.IsDistinct)element;
						return
							Find(p.Expr1) ??
							Find(p.Expr2);
					}

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
							Find(((SqlTable)element).Fields        ) ??
							Find(((SqlTable)element).TableArguments);
					}

				case QueryElementType.SqlCteTable:
					{
						return
							Find(((SqlCteTable)element).All           ) ??
							Find(((SqlCteTable)element).Fields        ) ??
							Find(((SqlCteTable)element).TableArguments) ??
							Find(((SqlCteTable)element).Cte);
					}

				case QueryElementType.SqlRawSqlTable:
					{
						return
							Find(((SqlRawSqlTable)element).All       ) ??
							Find(((SqlRawSqlTable)element).Fields    ) ??
							Find(((SqlRawSqlTable)element).Parameters);
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

				case QueryElementType.SearchStringPredicate:
					{
						return
							Find(((SqlPredicate.SearchString)element).Expr1 ) ??
							Find(((SqlPredicate.SearchString)element).Expr2 );
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
						       Find(((SqlSelectStatement)element).With       ) ??
						       Find(((SqlSelectStatement)element).Tag        );
					}

				case QueryElementType.InsertStatement:
					{
						return Find(((SqlInsertStatement)element).SelectQuery) ??
						       Find(((SqlInsertStatement)element).Insert     ) ??
						       Find(((SqlInsertStatement)element).Output     ) ??
						       Find(((SqlInsertStatement)element).With       ) ??
						       Find(((SqlInsertStatement)element).Tag        );
					}

				case QueryElementType.UpdateStatement:
					{
						return Find(((SqlUpdateStatement)element).SelectQuery) ??
						       Find(((SqlUpdateStatement)element).Update     ) ??
						       Find(((SqlUpdateStatement)element).Output     ) ??
						       Find(((SqlUpdateStatement)element).With       ) ??
						       Find(((SqlUpdateStatement)element).Tag        );
					}

				case QueryElementType.InsertOrUpdateStatement:
					{
						return Find(((SqlInsertOrUpdateStatement)element).SelectQuery) ??
						       Find(((SqlInsertOrUpdateStatement)element).Insert     ) ??
						       Find(((SqlInsertOrUpdateStatement)element).Update     ) ??
						       Find(((SqlInsertOrUpdateStatement)element).With       ) ??
						       Find(((SqlInsertOrUpdateStatement)element).Tag        );
					}

				case QueryElementType.DeleteStatement:
					{
						return
							Find(((SqlDeleteStatement)element).Table      ) ??
							Find(((SqlDeleteStatement)element).Top        ) ??
							Find(((SqlDeleteStatement)element).SelectQuery) ??
							Find(((SqlDeleteStatement)element).Tag        );
					}

				case QueryElementType.CreateTableStatement:
					{
						return
							Find(((SqlCreateTableStatement)element).Table) ??
							Find(((SqlCreateTableStatement)element).Tag  );
					}

				case QueryElementType.DropTableStatement:
					{
						return
							Find(((SqlDropTableStatement)element).Table) ??
							Find(((SqlDropTableStatement)element).Tag  );
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
							Find(((SqlTruncateTableStatement)element).Table) ??
							Find(((SqlTruncateTableStatement)element).Tag  );
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
							Find(((SqlMergeStatement)element).Operations) ??
							Find(((SqlMergeStatement)element).Tag       );
					}

				case QueryElementType.MergeSourceTable:
					{
						return
							Find(((SqlTableLikeSource)element).SourceEnumerable) ??
							Find(((SqlTableLikeSource)element).SourceQuery     ) ??
							Find(((SqlTableLikeSource)element).SourceFields    );
					}

				case QueryElementType.MergeOperationClause:
					{
						return
							Find(((SqlMergeOperationClause)element).Where      ) ??
							Find(((SqlMergeOperationClause)element).WhereDelete) ??
							Find(((SqlMergeOperationClause)element).Items      );
					}

				case QueryElementType.MultiInsertStatement:
					{
						return
							Find(((SqlMultiInsertStatement)element).Source) ??
							Find(((SqlMultiInsertStatement)element).Inserts);
					}

				case QueryElementType.ConditionalInsertClause:
					{
						return
							Find(((SqlConditionalInsertClause)element).When) ??
							Find(((SqlConditionalInsertClause)element).Insert);
					}

				case QueryElementType.SqlValuesTable:
					{
						return 
							Find(((SqlValuesTable)element).Fields                  ) ??
							Find(((SqlValuesTable)element).Rows?.SelectMany(static r => r));
					}

				case QueryElementType.SqlField:
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlValue:
				case QueryElementType.SqlDataType:
				case QueryElementType.SqlAliasPlaceholder:
				case QueryElementType.Comment:
					break;

				default:
					throw new InvalidOperationException($"Find visitor not implemented for element {element.ElementType}");
			}

			return null;
		}
	}
}
