using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Linq.Builder;

	public class VisitArgs<TContext>
	{
		public VisitArgs(ConvertVisitor<TContext> visitor)
		{
			Visitor = visitor;
		}

		public readonly ConvertVisitor<TContext> Visitor;
		public          IQueryElement            Element = null!;
	}

	public class ConvertVisitor<TContext>
	{
		// when true, only changed (and explicitly added) elements added to VisitedElements
		// greatly reduce memory allocation for majority of cases, where there is nothing to replace
		private readonly bool                                                       _visitAll;
		private readonly Func<ConvertVisitor<TContext>,IQueryElement,IQueryElement> _convert;
		private readonly Func<VisitArgs<TContext>, bool>?                           _parentAction;
		private readonly VisitArgs<TContext>?                                       _visitArgs;

		public readonly TContext Context;
		public readonly bool     AllowMutation;

		delegate T Clone<T>(T obj);

		public Dictionary<IQueryElement, IQueryElement?> VisitedElements     { get; } = new ();
		public List<IQueryElement>                       Stack               { get; } = new ();
		public IQueryElement?                            ParentElement               => Stack.Count == 0 ? null : Stack[Stack.Count - 1];
		public IQueryElement?                            SecondParentElement         => Stack.Count < 2  ? null : Stack[Stack.Count - 2];

		internal ConvertVisitor(TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction, bool visitAll, bool allowMutation, Func<VisitArgs<TContext>, bool>? parentAction = default)
		{
			_visitAll     = visitAll;
			_convert      = convertAction;
			AllowMutation = allowMutation;
			_parentAction = parentAction;
			Context       = context;

			if (_parentAction != null)
				_visitArgs = new VisitArgs<TContext>(this);
		}

		void CorrectQueryHierarchy(SelectQuery? parentQuery)
		{
			if (parentQuery == null)
				return;

			parentQuery.Visit(parentQuery, static (parentQuery, element) =>
			{
				if (element is SelectQuery q)
					q.ParentSelect = parentQuery;
			});

			parentQuery.ParentSelect = null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void AddVisited(IQueryElement element, IQueryElement? newElement)
		{
			VisitedElements[element] = newElement;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ReplaceVisited(IQueryElement element, IQueryElement? newElement)
		{
			List<IQueryElement>? forDelete = null;

			foreach (var pair in VisitedElements)
				if (pair.Value != null && QueryHelper.ContainsElement(pair.Value, element))
					(forDelete ??= new ()).Add(pair.Key);

			if (forDelete != null)
				foreach (var e in forDelete)
					VisitedElements.Remove(e);

			VisitedElements[element] = newElement;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IQueryElement? GetCurrentReplaced(IQueryElement element)
		{
			if (VisitedElements.TryGetValue(element, out var replaced))
			{
				if (replaced != null && replaced != element)
				{
					while (replaced != null && VisitedElements.TryGetValue(replaced, out var another))
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

		[return: NotNullIfNotNull("element")]
		internal IQueryElement? ConvertInternal(IQueryElement? element)
		{
			if (element == null)
				return null;

			// if element manually added outside to VisitedElements as null, it will be processed continuously.
			// Useful when we have to duplicate such items, especially parameters
			var newElement = GetCurrentReplaced(element);
			if (newElement != null)
				return newElement;

			if (_parentAction != null)
			{
				_visitArgs!.Element = element;
				var stop            = !_parentAction(_visitArgs!);
				element             = _visitArgs!.Element;
				if (stop)
					return element;
			}

			Stack.Add(element);
			{
				switch (element.ElementType)
				{
					case QueryElementType.SqlFunction:
					{
						var func  = (SqlFunction)element;
						var parms = Convert(func.Parameters);

						if (parms != null && !ReferenceEquals(parms, func.Parameters))
							newElement =
								new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.IsPure, func.Precedence, parms)
								{ CanBeNull = func.CanBeNull, DoNotOptimize = func.DoNotOptimize };

						break;
					}

					case QueryElementType.SqlExpression:
					{
						var expr      = (SqlExpression)element;
						var parameter = Convert(expr.Parameters);

						if (parameter != null && !ReferenceEquals(parameter, expr.Parameters))
							newElement = new SqlExpression(expr.SystemType, expr.Expr, expr.Precedence, expr.Flags, parameter);

						break;
					}

					case QueryElementType.SqlObjectExpression:
					{
						var expr      = (SqlObjectExpression)element;

						if (AllowMutation)
						{
							for (int i = 0; i < expr.InfoParameters.Length; i++)
							{
								var sqlInfo = expr.InfoParameters[i];

								expr.InfoParameters[i] = sqlInfo.WithSql((ISqlExpression)ConvertInternal(sqlInfo.Sql));
							}
						}
						else
						{
							SqlInfo[]? currentParams = null;

							for (int i = 0; i < expr.InfoParameters.Length; i++)
							{
								var sqlInfo = expr.InfoParameters[i];

								var newExpr = (ISqlExpression)ConvertInternal(sqlInfo.Sql);

								if (!ReferenceEquals(newExpr, sqlInfo.Sql))
								{
									if (currentParams == null)
									{
										currentParams = new SqlInfo[expr.InfoParameters.Length];
										Array.Copy(expr.InfoParameters, currentParams, i);
									}

									var newInfo = sqlInfo.WithSql(newExpr);
									currentParams[i] = newInfo;
								}
								else if (currentParams != null)
									currentParams[i] = sqlInfo;
							}

							if (currentParams != null)
								newElement = new SqlObjectExpression(expr.MappingSchema, currentParams);
						}

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
						var newTable = (SqlTable)_convert(this, table);

						if (ReferenceEquals(newTable, table))
						{
							var targs = table.TableArguments == null || table.TableArguments.Length == 0 ?
									null : Convert(table.TableArguments);

							if (targs != null && !ReferenceEquals(table.TableArguments, targs))
							{
								var newFields = table.Fields.Select(static f => new SqlField(f));
								newTable = new SqlTable(table, newFields, targs);
							}
						}

						if (!ReferenceEquals(table, newTable))
						{
							AddVisited(table.All, newTable.All);
							foreach (var prevField in table.Fields)
							{
								var newField = newTable[prevField.Name];
								if (newField != null)
									AddVisited(prevField, newField);
							}
						}

						newElement = newTable;

						break;
					}

					case QueryElementType.SqlCteTable:
					{
						var table = (SqlCteTable)element;
						var cte   = (CteClause?)ConvertInternal(table.Cte);

						if (cte != null && !ReferenceEquals(table.Cte, cte))
						{
							var newFields = table.Fields.Select(static f => new SqlField(f));
							var newTable  = new SqlCteTable(table, newFields, cte!);

							ReplaceVisited(table.All, newTable.All);
							foreach (var prevField in table.Fields)
							{
								var newField = newTable[prevField.Name];
								if (newField != null)
								{
									ReplaceVisited(prevField, newField);
								}
							}

							newElement = newTable;
						}

						break;
					}

					case QueryElementType.Column:
					{
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
						joins != null && !ReferenceEquals(table.Joins, joins))
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
							cond != null && !ReferenceEquals(cond, join.Condition))
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
							newElement = new SqlPredicate.ExprExpr(e1 ?? p.Expr1, p.Operator, e2 ?? p.Expr2, p.WithNull);

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
							newElement = new SqlPredicate.Like(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, es ?? p.Escape);

						break;
					}

					case QueryElementType.SearchStringPredicate:
					{
						var p  = (SqlPredicate.SearchString)element;
						var e1 = (ISqlExpression?)ConvertInternal(p.Expr1 );
						var e2 = (ISqlExpression?)ConvertInternal(p.Expr2 );
						var cs = (ISqlExpression?)ConvertInternal(p.CaseSensitive);

						if (e1 != null && !ReferenceEquals(p.Expr1, e1) ||
							e2 != null && !ReferenceEquals(p.Expr2, e2) ||
							cs != null && !ReferenceEquals(p.CaseSensitive, cs)
							)
							newElement = new SqlPredicate.SearchString(e1 ?? p.Expr1, p.IsNot, e2 ?? p.Expr2, p.Kind, cs ?? p.CaseSensitive);

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

					case QueryElementType.IsTruePredicate:
					{
						var p = (SqlPredicate.IsTrue)element;
						var e = (ISqlExpression?)ConvertInternal(p.Expr1);
						var t = (ISqlExpression?)ConvertInternal(p.TrueValue);
						var f = (ISqlExpression?)ConvertInternal(p.FalseValue);

						if (e != null && !ReferenceEquals(p.Expr1, e) ||
							t != null && !ReferenceEquals(p.TrueValue, t) ||
							f != null && !ReferenceEquals(p.FalseValue, f)
							)
							newElement = new SqlPredicate.IsTrue(e ?? p.Expr1, t ?? p.TrueValue, f ?? p.FalseValue, p.WithNull, p.IsNot);

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

					case QueryElementType.IsDistinctPredicate:
						{
							var p  = (SqlPredicate.IsDistinct)element;
							var e1 = (ISqlExpression?)ConvertInternal(p.Expr1) ?? p.Expr1;
							var e2 = (ISqlExpression?)ConvertInternal(p.Expr2) ?? p.Expr2;

							if (!ReferenceEquals(p.Expr1, e1) || !ReferenceEquals(p.Expr2, e2))
								newElement = new SqlPredicate.IsDistinct(e1, p.IsNot, e2);

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
							newElement = new SqlPredicate.InList(e ?? p.Expr1, p.WithNull, p.IsNot, v ?? p.Values);

						break;
					}

					case QueryElementType.FuncLikePredicate:
					{
						var p = (SqlPredicate.FuncLike)element;
						var f = (ISqlExpression?)ConvertInternal(p.Function);

						if (f != null && !ReferenceEquals(p.Function, f))
						{
							if (f is SqlFunction function)
								newElement = new SqlPredicate.FuncLike(function);
							else if (f is ISqlPredicate predicate)
								newElement = predicate;
							else
								throw new InvalidCastException("Converted FuncLikePredicate expression is not a Predicate expression.");
						}

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
							k != null && !ReferenceEquals(s.Keys, k))
						{
							var sc = new SqlUpdateClause { Table = t ?? s.Table };

							sc.Items.AddRange(i ?? s.Items);
							sc.Keys.AddRange(k ?? s.Keys);

							newElement = sc;
						}

						break;
					}

					case QueryElementType.SelectStatement:
					{
						var s = (SqlSelectStatement)element;
						var tag         = s.Tag         != null ? (SqlComment?   )ConvertInternal(s.Tag ) : null;
						var with        = s.With        != null ? (SqlWithClause?)ConvertInternal(s.With) : null;
						var selectQuery = (SelectQuery?)ConvertInternal(s.SelectQuery);

						if (selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery) ||
							tag         != null && !ReferenceEquals(s.Tag, tag)                 ||
							with        != null && !ReferenceEquals(s.With, with))
						{
							newElement = new SqlSelectStatement(selectQuery ?? s.SelectQuery)
							{
								Tag  = tag ?? s.Tag,
								With = with ?? s.With
							};
							CorrectQueryHierarchy(((SqlSelectStatement)newElement).SelectQuery);
						}

						break;
					}

					case QueryElementType.InsertStatement:
					{
						var s = (SqlInsertStatement)element;
						var tag         = s.Tag         != null ? (SqlComment?   )  ConvertInternal(s.Tag   ) : null;
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With  ) : null;
						var selectQuery = (SelectQuery?    )ConvertInternal(s.SelectQuery);
						var insert      = (SqlInsertClause?)ConvertInternal(s.Insert);
						var output      = s.Output      != null ? (SqlOutputClause?)ConvertInternal(s.Output) : null;

						if (insert      != null && !ReferenceEquals(s.Insert, insert)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery) ||
							tag         != null && !ReferenceEquals(s.Tag, tag)                 ||
							with        != null && !ReferenceEquals(s.With, with)               ||
							output      != null && !ReferenceEquals(s.Output, output))
						{
							newElement = new SqlInsertStatement(selectQuery ?? s.SelectQuery)
							{
								Insert = insert ?? s.Insert,
								Output = output ?? s.Output,
								Tag    = tag    ?? s.Tag,
								With   = with   ?? s.With
							};
							CorrectQueryHierarchy(((SqlInsertStatement)newElement).SelectQuery);
						}

						break;
					}

					case QueryElementType.UpdateStatement:
					{
						var s = (SqlUpdateStatement)element;
						var tag         = s.Tag         != null ? (SqlComment?   )  ConvertInternal(s.Tag ) : null;
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With) : null;
						var selectQuery = (SelectQuery?    )ConvertInternal(s.SelectQuery);
						var update      = (SqlUpdateClause?)ConvertInternal(s.Update);

						if (update      != null && !ReferenceEquals(s.Update, update)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery) ||
							tag         != null && !ReferenceEquals(s.Tag, tag)                 ||
							with        != null && !ReferenceEquals(s.With, with))
						{
							newElement = new SqlUpdateStatement(selectQuery ?? s.SelectQuery)
							{
								Update = update ?? s.Update,
								Tag    = tag    ?? s.Tag,
								With   = with   ?? s.With
							};
							CorrectQueryHierarchy(((SqlUpdateStatement)newElement).SelectQuery);
						}

						break;
					}

					case QueryElementType.InsertOrUpdateStatement:
					{
						var s = (SqlInsertOrUpdateStatement)element;

						var tag         = s.Tag         != null ? (SqlComment?   )  ConvertInternal(s.Tag ) : null;
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With) : null;
						var selectQuery = (SelectQuery?    )ConvertInternal(s.SelectQuery);
						var insert      = (SqlInsertClause?)ConvertInternal(s.Insert);
						var update      = (SqlUpdateClause?)ConvertInternal(s.Update);

						if (insert      != null && !ReferenceEquals(s.Insert, insert)           ||
							update      != null && !ReferenceEquals(s.Update, update)           ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery) ||
							tag         != null && !ReferenceEquals(s.Tag, tag)                 ||
							with        != null && !ReferenceEquals(s.With, with))
						{
							newElement = new SqlInsertOrUpdateStatement(selectQuery ?? s.SelectQuery)
							{
								Insert = insert ?? s.Insert,
								Update = update ?? s.Update,
								Tag    = tag    ?? s.Tag,
								With   = with   ?? s.With
							};
							CorrectQueryHierarchy(((SqlInsertOrUpdateStatement)newElement).SelectQuery);
						}

						break;
					}

					case QueryElementType.DeleteStatement:
					{
						var s = (SqlDeleteStatement)element;
						var tag         = s.Tag         != null ? (SqlComment?   )  ConvertInternal(s.Tag        ) : null;
						var with        = s.With        != null ? (SqlWithClause?)  ConvertInternal(s.With       ) : null;
						var selectQuery = s.SelectQuery != null ? (SelectQuery?)    ConvertInternal(s.SelectQuery) : null;
						var table       = s.Table       != null ? (SqlTable?)       ConvertInternal(s.Table      ) : null;
						var top         = s.Top         != null ? (ISqlExpression?) ConvertInternal(s.Top        ) : null;
						var output      = s.Output      != null ? (SqlOutputClause?)ConvertInternal(s.Output     ) : null;

						if (table       != null && !ReferenceEquals(s.Table, table)             ||
							top         != null && !ReferenceEquals(s.Top, top)                 ||
							selectQuery != null && !ReferenceEquals(s.SelectQuery, selectQuery) ||
							tag         != null && !ReferenceEquals(s.Tag, tag)                 ||
							with        != null && !ReferenceEquals(s.With, with)               ||
							output      != null && !ReferenceEquals(s.Output, output))
						{
							newElement = new SqlDeleteStatement
							{
								Table                = table       ?? s.Table,
								SelectQuery          = selectQuery ?? s.SelectQuery,
								Top                  = top         ?? s.Top!,
								Output               = output      ?? s.Output,
								Tag                  = tag         ?? s.Tag,
								With                 = with        ?? s.With,
								IsParameterDependent = s.IsParameterDependent,
							};
							CorrectQueryHierarchy(((SqlDeleteStatement)newElement).SelectQuery);
						}

						break;
					}

					case QueryElementType.CreateTableStatement:
					{
						var s   = (SqlCreateTableStatement)element;
						var tag = s.Tag != null ? (SqlComment?)ConvertInternal(s.Tag) : null;
						var t   = (SqlTable)ConvertInternal(s.Table);

						if (t   != null && !ReferenceEquals(s.Table, t) ||
							tag != null && !ReferenceEquals(s.Tag, tag))
						{
							newElement = new SqlCreateTableStatement(t ?? s.Table)
							{
								Tag = tag ?? s.Tag
							};
						}

						break;
					}

					case QueryElementType.DropTableStatement:
					{
						var s   = (SqlDropTableStatement)element;
						var tag = s.Tag != null ? (SqlComment?)ConvertInternal(s.Tag) : null;
						var t   = (SqlTable)ConvertInternal(s.Table);

						if (!ReferenceEquals(s.Table, t))
						{
							newElement = new SqlDropTableStatement(t)
							{
								Tag = tag ?? s.Tag
							};
						}

						break;
					}

					case QueryElementType.SelectClause:
					{
						var sc   = (SqlSelectClause)element;

						List<SqlColumn>? cols = null;

						if (AllowMutation)
						{
							for (int i = 0; i < sc.Columns.Count; i++)
							{
								var column = sc.Columns[i];

								Stack.Add(column);
								var expr = (ISqlExpression)ConvertInternal(column.Expression);
								Stack.RemoveAt(Stack.Count - 1);

								if (!ReferenceEquals(column.Expression, expr))
									column.Expression = expr;
							}
						}
						else
						{
							for (int i = 0; i < sc.Columns.Count; i++)
							{
								var column = sc.Columns[i];

								Stack.Add(column);
								var expr = (ISqlExpression)ConvertInternal(column.Expression);
								Stack.RemoveAt(Stack.Count - 1);

								if (!ReferenceEquals(expr, column.Expression))
								{
									if (cols == null)
									{
										cols = new List<SqlColumn>(sc.Columns.Take(i));
									}

									var newColumn = new SqlColumn(null, expr, column.Alias);
									cols.Add(newColumn);
									AddVisited(column, newColumn);
								}
								else
								{
									cols?.Add(column);
									AddVisited(column, column);
								}
							}
						}

						var take = (ISqlExpression?)ConvertInternal(sc.TakeValue);
						var skip = (ISqlExpression?)ConvertInternal(sc.SkipValue);

						if (
							cols != null && !ReferenceEquals(sc.Columns, cols)   ||
							take != null && !ReferenceEquals(sc.TakeValue, take) ||
							skip != null && !ReferenceEquals(sc.SkipValue, skip))
						{
							newElement = new SqlSelectClause(sc.IsDistinct, take ?? sc.TakeValue, sc.TakeHints, skip ?? sc.SkipValue, cols ?? sc.Columns);
						}

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
							newElement = new SqlGroupByClause(gc.GroupingType, es ?? gc.Items);
							((SqlGroupByClause)newElement).SetSqlQuery(gc.SelectQuery);
						}

						break;
					}

					case QueryElementType.GroupingSet:
					{
						var gc = (SqlGroupingSet)element;
						var es = Convert(gc.Items);

						if (es != null && !ReferenceEquals(gc.Items, es))
							newElement = new SqlGroupingSet(es ?? gc.Items);

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
						var us = q.HasSetOperators ?Convert(q.SetOperators)     : q.SetOperators;

						List<ISqlExpression[]>? uk = null;
						if (q.HasUniqueKeys)
							uk = ConvertListArray(q.UniqueKeys, null) ?? q.UniqueKeys;

						if (!ReferenceEquals(fc, q.From)
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

							var objTree = new Dictionary<IQueryElement, IQueryElement>();

							// try to correct if there q.* in columns expressions
							// TODO: not performant, it is bad that Columns has reference on this select query
							sc = sc.ConvertAll((q, nq), static (v, e) => ReferenceEquals(e, v.Context.q.All) ? v.Context.nq.All : e);

							// TODO: refactor
							if (ReferenceEquals(sc, q.Select))
							{
								sc = new SqlSelectClause(nq)
								{
									IsDistinct = q.Select.IsDistinct,
									TakeValue  = q.Select.TakeValue.Clone(q, objTree, static (q, e) => e is SqlColumn c && c.Parent == q),
									SkipValue  = q.Select.SkipValue.Clone(q, objTree, static (q, e) => e is SqlColumn c && c.Parent == q),
								};

								foreach (var column in q.Select.Columns)
									sc.Columns.Add(column.Clone(q, objTree, static (q, e) => e is SqlColumn c && c.Parent == q));
							}

							if (ReferenceEquals(fc, q.From))
							{
								fc = new SqlFromClause(nq);
								fc.Tables.AddRange(q.From.Tables);
							}
							if (ReferenceEquals(wc, q.Where))
							{
								wc = new SqlWhereClause(nq);
								wc.SearchCondition = q.Where.SearchCondition;
							}
							if (ReferenceEquals(gc, q.GroupBy))
							{
								gc = new SqlGroupByClause(nq)
								{
									GroupingType = q.GroupBy.GroupingType
								};
								gc.Items.AddRange(q.GroupBy.Items);
							}
							if (ReferenceEquals(hc, q.Having))
							{
								hc = new SqlWhereClause(nq);
								hc.SearchCondition = q.Having.SearchCondition;
							}
							if (ReferenceEquals(oc, q.OrderBy))
							{
								oc = new SqlOrderByClause(nq);
								oc.Items.AddRange(q.OrderBy.Items);
							}
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
									VisitedElements[queryElement] = pair.Value;
							}

							newElement = nq;
						}
						break;
					}

					case QueryElementType.MergeStatement:
					{
						var merge = (SqlMergeStatement)element;

						var tag        = merge.Tag != null ? (SqlComment?)ConvertInternal(merge.Tag) : null;
						var target     = (SqlTableSource?)    ConvertInternal(merge.Target);
						var source     = (SqlTableLikeSource?)ConvertInternal(merge.Source);
						var on         = (SqlSearchCondition?)ConvertInternal(merge.On);
						var operations = ConvertSafe(merge.Operations);

						if (target     != null && !ReferenceEquals(merge.Target, target) ||
							source     != null && !ReferenceEquals(merge.Source, source) ||
							on         != null && !ReferenceEquals(merge.On, on)         ||
							tag        != null && !ReferenceEquals(merge.Tag, tag)       ||
							operations != null && !ReferenceEquals(merge.Operations, operations))
						{
							newElement = new SqlMergeStatement(
								merge.Hint,
								target     ?? merge.Target,
								source     ?? merge.Source,
								on         ?? merge.On,
								operations ?? merge.Operations)
							{
								Tag = tag ?? merge.Tag
							};
						}

						break;
					}

					case QueryElementType.MultiInsertStatement:
					{
						var insert  = (SqlMultiInsertStatement)element;
						var source  = (SqlTableLikeSource?    )ConvertInternal(insert.Source);
						var inserts = ConvertSafe(insert.Inserts);

						if (source  != null && !ReferenceEquals(insert.Source, source) ||
							inserts != null && !ReferenceEquals(insert.Inserts, inserts))
						{
							newElement = new SqlMultiInsertStatement(
								insert.InsertType,
								source ?? insert.Source,
								inserts ?? insert.Inserts);
						}

						break;
					}

					case QueryElementType.ConditionalInsertClause:
					{
						var clause = (SqlConditionalInsertClause)element;
						var when   = (SqlSearchCondition?)ConvertInternal(clause.When);
						var insert = (SqlInsertClause?   )ConvertInternal(clause.Insert);

						if (when   != null && !ReferenceEquals(clause.When, when) ||
							insert != null && !ReferenceEquals(clause.Insert, insert))
						{
							newElement = new SqlConditionalInsertClause(
								insert ?? clause.Insert,
								when   ?? clause.When);
						}

						break;
					}

					case QueryElementType.MergeSourceTable:
					{
						var source = (SqlTableLikeSource)element;

						var enumerableSource = (SqlValuesTable?)ConvertInternal(source.SourceEnumerable);
						var querySource      = (SelectQuery?)   ConvertInternal(source.SourceQuery);

						if (enumerableSource != null && !ReferenceEquals(source.SourceEnumerable, enumerableSource) ||
							querySource      != null && !ReferenceEquals(source.SourceQuery, querySource))
						{
							var newFields = new SqlField[source.SourceFields.Count];
							for (var i = 0; i < source.SourceFields.Count; i++)
							{
								var oldField = source.SourceFields[i];
								var newField = newFields[i] = new SqlField(oldField);
								ReplaceVisited(oldField, newField);
							}

							newElement = new SqlTableLikeSource(
								source.SourceID,
								enumerableSource ?? source.SourceEnumerable!,
								querySource      ?? source.SourceQuery!,
								newFields);

							ReplaceVisited(((ISqlTableSource)source).All, ((ISqlTableSource)newElement).All);
						}

						break;
					}

					case QueryElementType.SqlValuesTable:
					{
						var table = (SqlValuesTable)element;

						List<ISqlExpression[]>? convertedRows = null;
						var rowsConverted = false;

						if (table.Rows != null)
						{
							convertedRows = new List<ISqlExpression[]>();
							foreach (var row in table.Rows)
							{
								var convertedRow = ConvertSafe(row);
								rowsConverted = rowsConverted || (convertedRow != null && !ReferenceEquals(convertedRow, row));

								convertedRows.Add(convertedRow ?? row!);
							}
						}

						var fields1 = table.Fields;
						var fields2 = Convert(table.Fields, static f => new SqlField(f));

						var fieldsConverted = fields2 != null && !ReferenceEquals(fields1, fields2);

						if (fieldsConverted || rowsConverted)
						{
							if (!fieldsConverted)
							{
								fields2 = fields1;

								for (var i = 0; i < fields2.Count; i++)
								{
									var field = fields2[i];

									fields2[i] = new SqlField(field);

									VisitedElements[field] = fields2[i];
								}
							}

							newElement = new SqlValuesTable(table.Source!, table.ValueBuilders!, fields2!, rowsConverted ? convertedRows : table.Rows);
						}

						break;
					}

					case QueryElementType.OutputClause:
					{
						var output    = (SqlOutputClause)element;
						var sourceT   = ConvertInternal(output.SourceTable)   as SqlTable;
						var insertedT = ConvertInternal(output.InsertedTable) as SqlTable;
						var deletedT  = ConvertInternal(output.DeletedTable)  as SqlTable;
						var outputT   = ConvertInternal(output.OutputTable)   as SqlTable;
						var outputQ   = output.OutputQuery != null ? ConvertInternal(output.OutputQuery) as SelectQuery : null;

						if (
							sourceT   != null && !ReferenceEquals(output.SourceTable, sourceT)     ||
							insertedT != null && !ReferenceEquals(output.InsertedTable, insertedT) ||
							deletedT  != null && !ReferenceEquals(output.DeletedTable, deletedT)   ||
							outputT   != null && !ReferenceEquals(output.OutputTable, outputT)     ||
							outputQ   != null && !ReferenceEquals(output.OutputQuery, outputQ)
						)
						{
							newElement = new SqlOutputClause
							{
								SourceTable   = sourceT   ?? output.SourceTable,
								InsertedTable = insertedT ?? output.InsertedTable,
								DeletedTable  = deletedT  ?? output.DeletedTable,
								OutputTable   = outputT   ?? output.OutputTable,
								OutputQuery   = outputQ   ?? output.OutputQuery,
							};
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

						var table = (SqlTable?)ConvertInternal(truncate.Table);
						var tag   = truncate.Tag != null ? (SqlComment?)ConvertInternal(truncate.Tag) : null;

						if ((table != null && !ReferenceEquals(truncate.Table, table)) ||
							(tag   != null && !ReferenceEquals(truncate.Tag, tag)))
						{
							newElement = new SqlTruncateTableStatement()
							{
								Table         = table ?? truncate.Table,
								Tag           = tag ?? truncate.Tag,
								ResetIdentity = truncate.ResetIdentity,
							};
						}

						break;
					}

					case QueryElementType.SqlRawSqlTable:
					{
						var table   = (SqlRawSqlTable)element;
						var targs   = table.Parameters == null || table.Parameters.Length == 0 ?
								null : Convert(table.Parameters);

						if (targs != null && !ReferenceEquals(table.Parameters, targs))
						{
							var newTable = new SqlRawSqlTable(table, targs ?? table.Parameters!);
							newElement   = newTable;

							AddVisited(table.All, newTable.All);
							foreach (var prevField in table.Fields)
							{
								var newField = new SqlField(prevField);
								newTable.Add(newField);
								AddVisited(prevField, newField);
							}
						}

						break;
					}

					case QueryElementType.CteClause:
					{
						var cte = (CteClause)element;

						// for avoiding recursion
						if (SecondParentElement?.ElementType != QueryElementType.WithClause)
							break;

						var body   = (SelectQuery?)ConvertInternal(cte.Body);

						if (body != null && !ReferenceEquals(cte.Body, body))
						{
							var objTree = new Dictionary<IQueryElement, IQueryElement>();

							var clonedFields = cte.Fields!.Clone(objTree);

							newElement = new CteClause(
								body,
								clonedFields,
								cte.ObjectType,
								cte.IsRecursive,
								cte.Name);

							var correctedBody = body.Convert(
									(cte, newElement, objTree),
									static (v, e) =>
									{
										if (e.ElementType == QueryElementType.CteClause)
										{
											var inner = (CteClause)e;
											if (ReferenceEquals(inner, v.Context.cte))
												return v.Context.newElement;
										}

										if (v.Context.objTree.TryGetValue(e, out var newValue))
											return newValue;
										return e;

									});

							// update visited for cloned fields
							foreach (var pair in objTree)
							{
								if (pair.Key is IQueryElement queryElement)
									VisitedElements[queryElement] = pair.Value;
							}

							VisitedElements.Remove(element);
							AddVisited(element, newElement);

							((CteClause)newElement).Body = correctedBody;
						}

						break;
					}

					case QueryElementType.WithClause:
					{
						var with = (SqlWithClause)element;

						var clauses = ConvertSafe(with.Clauses);

						if (clauses != null && !ReferenceEquals(with.Clauses, clauses))
						{
							newElement = new SqlWithClause()
							{
								Clauses = clauses
							};

							newElement = new SqlWithClause() { Clauses = clauses };
						}
						break;
					}

					case QueryElementType.SqlField:
					case QueryElementType.SqlParameter:
					case QueryElementType.SqlValue:
					case QueryElementType.SqlDataType:
					case QueryElementType.SqlAliasPlaceholder:
					case QueryElementType.Comment:
						break;

					default:
						throw new InvalidOperationException($"Convert visitor not implemented for element {element.ElementType}");
				}
			}
			Stack.RemoveAt(Stack.Count - 1);

			newElement = _convert(this, newElement ?? element);

			if (!_visitAll || !ReferenceEquals(element, newElement))
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
			if (AllowMutation)
			{
				for (var i = 0; i < arr1.Length; i++)
				{
					var elem = (T?)ConvertInternal(arr1[i]);
					if (elem != null)
						arr1[i] = elem;
				}
				return arr1;
			}

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

		List<T>? ConvertSafe<T>(List<T> list)
			where T : class, IQueryElement
		{
			return ConvertSafe(list, null);
		}

		T[]? ConvertSafe<T>(T[] array)
			where T : class, IQueryElement
		{
			return ConvertSafe(array, null);
		}

		List<T>? ConvertSafe<T>(List<T> list1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			if (AllowMutation)
			{
				for (var i = 0; i < list1.Count; i++)
				{
					var elem = (T?)ConvertInternal(list1[i]);
					if (elem != null)
						list1[i] = elem;
				}
				return null;
			}

			List<T>? list2 = null;

			for (var i = 0; i < list1.Count; i++)
			{
				var elem1 = list1[i];

				if (ConvertInternal(elem1) is T elem2 && !ReferenceEquals(elem1, elem2))
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

		T[]? ConvertSafe<T>(T[] array1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			if (AllowMutation)
			{
				for (var i = 0; i < array1.Length; i++)
				{
					var elem = (T?)ConvertInternal(array1[i]);
					if (elem != null)
						array1[i] = elem;
				}
				return null;
			}

			T[]? array2 = null;

			for (var i = 0; i < array1.Length; i++)
			{
				var elem1 = array1[i];

				if (ConvertInternal(elem1) is T elem2 && !ReferenceEquals(elem1, elem2))
				{
					if (array2 == null)
					{
						array2 = new T[array1.Length];

						for (var j = 0; j < i; j++)
							array2[j] = clone == null ? array1[j] : clone(array1[j]);
					}

					array2[i] = elem2;
				}
				else if (array2 != null)
					array2[i] = clone == null ? elem1 : clone(elem1);
			}

			return array2;
		}

		List<T>? Convert<T>(List<T> list)
			where T : class, IQueryElement
		{
			return Convert(list, null);
		}

		List<T>? Convert<T>(List<T> list1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			if (AllowMutation)
			{
				for (var i = 0; i < list1.Count; i++)
				{
					var elem = (T?)ConvertInternal(list1[i]);
					if (elem != null)
						list1[i] = elem;
				}
				return list1;
			}

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
								VisitedElements[elem] = elem = clone(elem);

							list2.Add(elem);
						}
					}

					list2.Add(elem2);
				}
				else if (list2 != null)
				{
					if (clone != null)
						VisitedElements[elem1] = elem1 = clone(elem1);

					list2.Add(elem1);
				}
			}

			return list2;
		}

		List<T[]>? ConvertListArray<T>(List<T[]> list1, Clone<T>? clone)
			where T : class, IQueryElement
		{
			if (AllowMutation)
			{
				for (var i = 0; i < list1.Count; i++)
				{
					var elem = Convert(list1[i]);
					if (elem != null)
						list1[i] = elem;
				}
				return list1;
			}

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
						{
							if (clone == null)
								list2.Add(list1[j]);
							else
							{
								var clonedArr = new T[list1[j].Length];
								for (var ii = 0; ii < clonedArr.Length; ii++)
									clonedArr[ii] = clone(list1[j][ii]);

								list2.Add(clonedArr);
							}
						}
					}

					list2.Add(elem2);
				}
				else if (list2 != null)
				{
					if (clone == null)
						list2.Add(elem1);
					else
					{
						var clonedArr = new T[elem1.Length];
						for (var ii = 0; ii < clonedArr.Length; ii++)
							clonedArr[ii] = clone(elem1[ii]);

						list2.Add(clonedArr);
					}
				}
			}

			return list2;
		}
	}
}
