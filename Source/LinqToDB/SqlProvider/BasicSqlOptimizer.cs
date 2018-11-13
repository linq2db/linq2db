using System;
using System.Linq;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace LinqToDB.SqlProvider
{
	using Common;
	using Extensions;
	using SqlQuery;

	public class BasicSqlOptimizer : ISqlOptimizer
	{
		#region Init

		protected BasicSqlOptimizer(SqlProviderFlags sqlProviderFlags)
		{
			SqlProviderFlags = sqlProviderFlags;
		}

		public SqlProviderFlags SqlProviderFlags { get; }

		#endregion

		#region ISqlOptimizer Members

		public virtual SqlStatement Finalize(SqlStatement statement)
		{
			FinalizeCte(statement);

//statement.EnsureFindTables();
			//TODO: We can use Walk here but OptimizeUnions fails with subqueris. Needs revising.
			statement.WalkQueries(
				selectQuery =>
				{
					new SelectQueryOptimizer(SqlProviderFlags, statement, selectQuery).FinalizeAndValidate(
						SqlProviderFlags.IsApplyJoinSupported,
						SqlProviderFlags.IsGroupByExpressionSupported);

					return selectQuery;
				}
			);

			statement.WalkQueries(
				selectQuery =>
				{
					if (!SqlProviderFlags.IsCountSubQuerySupported)  selectQuery = MoveCountSubQuery (selectQuery);
					if (!SqlProviderFlags.IsSubQueryColumnSupported) selectQuery = MoveSubQueryColumn(selectQuery);

					return selectQuery;
				}
			);

			if (!SqlProviderFlags.IsCountSubQuerySupported || !SqlProviderFlags.IsSubQueryColumnSupported)
			{
				statement.WalkQueries(
					selectQuery =>
					{
						new SelectQueryOptimizer(SqlProviderFlags, statement, selectQuery).FinalizeAndValidate(
							SqlProviderFlags.IsApplyJoinSupported,
							SqlProviderFlags.IsGroupByExpressionSupported);

						return selectQuery;
					}
				);
			}


//statement.EnsureFindTables();
			if (Configuration.Linq.OptimizeJoins)
				OptimizeJoins(statement);

//statement.EnsureFindTables();
			statement.SetAliases();

			return statement;
		}

		void FinalizeCte(SqlStatement statement)
		{
			if (statement is SqlStatementWithQueryBase select)
			{
				var foundCte  = new Dictionary<CteClause, HashSet<CteClause>>();

				new QueryVisitor().Visit(select.SelectQuery, e =>
					{
						if (e.ElementType == QueryElementType.SqlCteTable)
						{
							var cte = ((SqlCteTable)e).Cte;
							if (!foundCte.ContainsKey(cte))
							{
								var dependsOn = new HashSet<CteClause>();
								new QueryVisitor().Visit(cte.Body, ce =>
								{
									if (ce.ElementType == QueryElementType.SqlCteTable)
									{
										var subCte = ((SqlCteTable)ce).Cte;
										dependsOn.Add(subCte);
									}

								});
								// self-reference is allowed, so we do not need to add dependency
								dependsOn.Remove(cte);
								foundCte.Add(cte, dependsOn);
							}
						}
					}
				);

				if (foundCte.Count == 0)
					select.With = null;
				else
				{
					// TODO: Ideally if there is no recursive CTEs we can convert them to SubQueries
					if (!SqlProviderFlags.IsCommonTableExpressionsSupported)
						throw new LinqToDBException("DataProvider do not supports Common Table Expressions.");

					// append missing CTE's from dependencies
					var hasSet = new HashSet<CteClause>(foundCte.Values.SelectMany(hs => hs));
					foreach (var clause in hasSet)
					{
						if (!foundCte.ContainsKey(clause))
							foundCte.Add(clause, new HashSet<CteClause>());
					}

					var ordered = TopoSorting.TopoSort(foundCte.Keys, i => foundCte[i]).ToList();

					Utils.MakeUniqueNames(ordered, c => c.Name, (c, n) => c.Name = n, "CTE_1");

					select.With = new SqlWithClause();
					select.With.Clauses.AddRange(ordered);
				}
			}
		}

		SelectQuery MoveCountSubQuery(SelectQuery selectQuery)
		{
			new QueryVisitor().Visit(selectQuery, MoveCountSubQuery);
			return selectQuery;
		}

		void MoveCountSubQuery(IQueryElement element)
		{
			if (element.ElementType != QueryElementType.SqlQuery)
				return;

			var query = (SelectQuery)element;

			for (var i = 0; i < query.Select.Columns.Count; i++)
			{
				var col = query.Select.Columns[i];

				// The column is a subquery.
				//
				if (col.Expression.ElementType == QueryElementType.SqlQuery)
				{
					var subQuery = (SelectQuery)col.Expression;
					var isCount  = false;

					// Check if subquery is Count subquery.
					//
					if (subQuery.Select.Columns.Count == 1)
					{
						var subCol = subQuery.Select.Columns[0];

						if (subCol.Expression.ElementType == QueryElementType.SqlFunction)
							isCount = ((SqlFunction)subCol.Expression).Name == "Count";
					}

					if (!isCount)
						continue;

					// Check if subquery where clause does not have ORs.
					//
					SelectQueryOptimizer.OptimizeSearchCondition(subQuery.Where.SearchCondition);

					var allAnd = true;

					for (var j = 0; allAnd && j < subQuery.Where.SearchCondition.Conditions.Count - 1; j++)
					{
						var cond = subQuery.Where.SearchCondition.Conditions[j];

						if (cond.IsOr)
							allAnd = false;
					}

					if (!allAnd || !ConvertCountSubQuery(subQuery))
						continue;

					// Collect tables.
					//
					var allTables   = new HashSet<ISqlTableSource>();
					var levelTables = new HashSet<ISqlTableSource>();

					new QueryVisitor().Visit(subQuery, e =>
					{
						if (e is ISqlTableSource source)
							allTables.Add(source);
					});

					new QueryVisitor().Visit(subQuery, e =>
					{
						if (e is ISqlTableSource source)
							if (subQuery.From.IsChild(source))
								levelTables.Add(source);
					});

					bool CheckTable(IQueryElement e)
					{
						switch (e.ElementType)
						{
							case QueryElementType.SqlField : return !allTables.Contains(((SqlField) e).Table);
							case QueryElementType.Column   : return !allTables.Contains(((SqlColumn)e).Parent);
						}

						return false;
					}

					var join = subQuery.LeftJoin();

					query.From.Tables[0].Joins.Add(join.JoinedTable);

					for (var j = 0; j < subQuery.Where.SearchCondition.Conditions.Count; j++)
					{
						var cond = subQuery.Where.SearchCondition.Conditions[j];

						if (QueryVisitor.Find(cond, CheckTable) == null)
							continue;

						var replaced = new Dictionary<IQueryElement,IQueryElement>();

						var nc = new QueryVisitor().Convert(cond, e =>
						{
							var ne = e;

							switch (e.ElementType)
							{
								case QueryElementType.SqlField :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SqlField)e).Table))
									{
										subQuery.GroupBy.Expr((SqlField)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SqlField)e)];
									}

									break;

								case QueryElementType.Column   :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SqlColumn)e).Parent))
									{
										subQuery.GroupBy.Expr((SqlColumn)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SqlColumn)e)];
									}

									break;
							}

							if (!ReferenceEquals(e, ne))
								replaced.Add(e, ne);

							return ne;
						});

						if (nc != null && !ReferenceEquals(nc, cond))
						{
							join.JoinedTable.Condition.Conditions.Add(nc);
							subQuery.Where.SearchCondition.Conditions.RemoveAt(j);
							j--;
						}
					}

					if (!query.GroupBy.IsEmpty/* && subQuery.Select.Columns.Count > 1*/)
					{
						var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

						subQuery.Select.Columns.RemoveAt(0);

						query.Select.Columns[i].Expression =
							new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[0]);
					}
					else
					{
						// Added Coalesce(column, 0) because Count() can be null in case of LEFT JOIN which may fail fast mapper.
						var column = subQuery.Select.Columns[0];
						var newExpr = new SqlFunction(column.SystemType, "Coalesce", column, new SqlValue(0));

						query.Select.Columns[i].Expression = newExpr;
					}
				}
			}
		}

		public virtual bool ConvertCountSubQuery(SelectQuery subQuery)
		{
			return true;
		}

		SelectQuery MoveSubQueryColumn(SelectQuery selectQuery)
		{
			var dic = new Dictionary<IQueryElement,IQueryElement>();

			new QueryVisitor().Visit(selectQuery, element =>
			{
				if (element.ElementType != QueryElementType.SqlQuery)
					return;

				var query = (SelectQuery)element;

				for (var i = 0; i < query.Select.Columns.Count; i++)
				{
					var col = query.Select.Columns[i];

					if (col.Expression.ElementType == QueryElementType.SqlQuery)
					{
						var subQuery    = (SelectQuery)col.Expression;
						var allTables   = new HashSet<ISqlTableSource>();
						var levelTables = new HashSet<ISqlTableSource>();

						bool CheckTable(IQueryElement e)
						{
							switch (e.ElementType)
							{
								case QueryElementType.SqlField : return !allTables.Contains(((SqlField) e).Table);
								case QueryElementType.Column   : return !allTables.Contains(((SqlColumn)e).Parent);
							}

							return false;
						}

						new QueryVisitor().Visit(subQuery, e =>
						{
							if (e is ISqlTableSource source)
								allTables.Add(source);
						});

						new QueryVisitor().Visit(subQuery, e =>
						{
							if (e is ISqlTableSource source && subQuery.From.IsChild(source))
								levelTables.Add(source);
						});

						if (SqlProviderFlags.IsSubQueryColumnSupported && QueryVisitor.Find(subQuery, CheckTable) == null)
							continue;

						// Join should not have ParentSelect, while SubQuery has
						subQuery.ParentSelect = null;

						var join = subQuery.LeftJoin();

						query.From.Tables[0].Joins.Add(join.JoinedTable);

						SelectQueryOptimizer.OptimizeSearchCondition(subQuery.Where.SearchCondition);

						var isCount      = false;
						var isAggregated = false;

						if (subQuery.Select.Columns.Count == 1)
						{
							var subCol = subQuery.Select.Columns[0];

							if (subCol.Expression.ElementType == QueryElementType.SqlFunction)
							{
								switch (((SqlFunction)subCol.Expression).Name)
								{
									case "Count" : isCount = true; break;
								}

								isAggregated = ((SqlFunction) subCol.Expression).IsAggregate;
							}
						}

						if (SqlProviderFlags.IsSubQueryColumnSupported && !isCount)
							continue;

						var allAnd = true;

						for (var j = 0; allAnd && j < subQuery.Where.SearchCondition.Conditions.Count - 1; j++)
						{
							var cond = subQuery.Where.SearchCondition.Conditions[j];

							if (cond.IsOr)
								allAnd = false;
						}

						if (!allAnd)
							continue;

						var modified = false;

						for (var j = 0; j < subQuery.Where.SearchCondition.Conditions.Count; j++)
						{
							var cond = subQuery.Where.SearchCondition.Conditions[j];

							if (QueryVisitor.Find(cond, CheckTable) == null)
								continue;

							var replaced = new Dictionary<IQueryElement,IQueryElement>();

							var nc = new QueryVisitor().Convert(cond, e =>
							{
								var ne = e;

								switch (e.ElementType)
								{
									case QueryElementType.SqlField :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SqlField)e).Table))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SqlField)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SqlField)e)];
										}

										break;

									case QueryElementType.Column   :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SqlColumn)e).Parent))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SqlColumn)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SqlColumn)e)];
										}

										break;
								}

								if (!ReferenceEquals(e, ne))
									replaced.Add(e, ne);

								return ne;
							});

							if (nc != null && !ReferenceEquals(nc, cond))
							{
								modified = true;

								join.JoinedTable.Condition.Conditions.Add(nc);
								subQuery.Where.SearchCondition.Conditions.RemoveAt(j);
								j--;
							}
						}

						if (modified || isAggregated)
						{
							SqlColumn newColumn;
							if (isCount && !query.GroupBy.IsEmpty)
							{
								var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

								subQuery.Select.Columns.RemoveAt(0);

								newColumn = new SqlColumn(
									query,
									new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[0]));
							}
							else if (isAggregated && !query.GroupBy.IsEmpty)
							{
								var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

								subQuery.Select.Columns.RemoveAt(0);

								var idx = subQuery.Select.Add(oldFunc.Parameters[0]);

								newColumn = new SqlColumn(
									query,
									new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[idx]));
							}
							else
							{
								newColumn = new SqlColumn(query, subQuery.Select.Columns[0]);
							}

							dic.Add(col, newColumn);
						}
					}
				}
			});

			selectQuery = new QueryVisitor().Convert(selectQuery, e => dic.TryGetValue(e, out var ne) ? ne : null);

			return selectQuery;
		}

		public virtual ISqlExpression ConvertExpression(ISqlExpression expression)
		{
			switch (expression.ElementType)
			{
				case QueryElementType.SqlBinaryExpression :
				#region SqlBinaryExpression
				{
					var be = (SqlBinaryExpression)expression;

					switch (be.Operation)
					{
						case "+":
						{
							if (be.Expr1 is SqlValue v1)
							{
								switch (v1.Value)
								{
									case short   h when h == 0  :
									case int     i when i == 0  :
									case long    l when l == 0  :
									case decimal d when d == 0  :
									case string  s when s == "" : return be.Expr2;
								}
							}
							else v1 = null;

							if (be.Expr2 is SqlValue v2)
							{
								switch (v2.Value)
								{
									case int vi when vi == 0 : return be.Expr1;
									case int vi when
										be.Expr1    is SqlBinaryExpression be1 &&
										be1.Expr2   is SqlValue be1v2          &&
										be1v2.Value is int      be1v2i :
									{
										switch (be1.Operation)
										{
											case "+":
											{
												var value = be1v2i + vi;
												var oper  = be1.Operation;

												if (value < 0)
												{
													value = -value;
													oper  = "-";
												}

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
											}

											case "-":
											{
												var value = be1v2i - vi;
												var oper  = be1.Operation;

												if (value < 0)
												{
													value = -value;
													oper  = "+";
												}

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
											}
										}

										break;
									}

									case string vs when vs == "" : return be.Expr1;
									case string vs when
										be.Expr1    is SqlBinaryExpression be1 &&
										//be1.Operation == "+"                   &&
										be1.Expr2   is SqlValue be1v2          &&
										be1v2.Value is string   be1v2s :
									{
										return new SqlBinaryExpression(
											be1.SystemType,
											be1.Expr1,
											be1.Operation,
											new SqlValue(string.Concat(be1v2s, vs)));
									}
								}
							}
							else v2 = null;

							if (v1 != null && v2 != null)
							{
								if (v1.Value is int i1 && v2.Value is int i2) return new SqlValue(i1 + i2);
								if (v1.Value is string || v2.Value is string) return new SqlValue(v1.Value?.ToString() + v2.Value);
							}

							if (be.Expr1.SystemType == typeof(string) && be.Expr2.SystemType != typeof(string))
							{
								var len = be.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr2.SystemType).DataType);

								if (len <= 0)
									len = 100;

								return new SqlBinaryExpression(
									be.SystemType,
									be.Expr1,
									be.Operation,
									ConvertExpression(new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), be.Expr2)),
									be.Precedence);
							}

							if (be.Expr1.SystemType != typeof(string) && be.Expr2.SystemType == typeof(string))
							{
								var len = be.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr1.SystemType).DataType);

								if (len <= 0)
									len = 100;

								return new SqlBinaryExpression(
									be.SystemType,
									ConvertExpression(new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), be.Expr1)),
									be.Operation,
									be.Expr2,
									be.Precedence);
							}

							break;
						}

						case "-":
						{
							if (be.Expr2 is SqlValue v2)
							{
								switch (v2.Value)
								{
									case int vi when vi == 0 : return be.Expr1;
									case int vi when
										be.Expr1    is SqlBinaryExpression be1 &&
										be1.Expr2   is SqlValue be1v2          &&
										be1v2.Value is int      be1v2i :
									{
										switch (be1.Operation)
										{
											case "+":
											{
												var value = be1v2i - vi;
												var oper  = be1.Operation;

												if (value < 0)
												{
													value = -value;
													oper  = "-";
												}

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
											}

											case "-":
											{
												var value = be1v2i + vi;
												var oper  = be1.Operation;

												if (value < 0)
												{
													value = -value;
													oper  = "+";
												}

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
											}
										}

										break;
									}
								}
							}
							else v2 = null;

							if (be.Expr1 is SqlValue v1 && v2 != null)
							{
								if (v1.Value is int i1 && v2.Value is int i2) return new SqlValue(i1 - i2);
							}

							break;
						}

						case "*":
						{
							if (be.Expr1 is SqlValue v1)
							{
								switch (v1.Value)
								{
									case int i when i == 0 : return new SqlValue(0);
									case int i when i == 1 : return be.Expr2;
									case int i when
										be.Expr2    is SqlBinaryExpression be2 &&
										be2.Operation == "*"                   &&
										be2.Expr1   is SqlValue be2v1          &&
										be2v1.Value is int bi :
									{
										return ConvertExpression(
											new SqlBinaryExpression(be2.SystemType, new SqlValue(i * bi), "*", be2.Expr2));
									}
								}
							}
							else v1 = null;

							if (be.Expr2 is SqlValue v2)
							{
								switch (v2.Value)
								{
									case int i when i == 0 : return new SqlValue(0);
									case int i when i == 1 : return be.Expr1;
								}
							}
							else v2 = null;

							if (v1 != null && v2 != null)
							{
								switch (v1.Value)
								{
									case int    i1 when v2.Value is int    i2 : return new SqlValue(i1 * i2);
									case int    i1 when v2.Value is double d2 : return new SqlValue(i1 * d2);
									case double d1 when v2.Value is int    i2 : return new SqlValue(d1 * i2);
									case double d1 when v2.Value is double d2 : return new SqlValue(d1 * d2);
								}
							}

							break;
						}
					}

					break;
				}
				#endregion

				case QueryElementType.SqlFunction :
				#region SqlFunction
				{
					var func = (SqlFunction)expression;

					switch (func.Name)
					{
						case "ConvertToCaseCompareTo":
							return ConvertExpression(new SqlFunction(func.SystemType, "CASE",
								new SqlSearchCondition().Expr(func.Parameters[0]). Greater .Expr(func.Parameters[1]).ToExpr(), new SqlValue(1),
								new SqlSearchCondition().Expr(func.Parameters[0]). Equal   .Expr(func.Parameters[1]).ToExpr(), new SqlValue(0),
								new SqlValue(-1)));

						case "$Convert$": return ConvertConvertion(func);
						case "Average"  : return new SqlFunction(func.SystemType, "Avg", func.Parameters);
						case "Max"      :
						case "Min"      :
							{
								if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
								{
									return new SqlFunction(typeof(int), func.Name,
										new SqlFunction(func.SystemType, "CASE", func.Parameters[0], new SqlValue(1), new SqlValue(0)));
								}

								break;
							}

						case "CASE"     :
							{
								var parms = func.Parameters;
								var len   = parms.Length;

								for (var i = 0; i < parms.Length - 1; i += 2)
								{
									if (parms[i] is SqlValue value)
									{
										if ((bool)value.Value == false)
										{
											var newParms = new ISqlExpression[parms.Length - 2];

											if (i != 0)
												Array.Copy(parms, 0, newParms, 0, i);

											Array.Copy(parms, i + 2, newParms, i, parms.Length - i - 2);

											parms = newParms;
											i -= 2;
										}
										else
										{
											var newParms = new ISqlExpression[i + 1];

											if (i != 0)
												Array.Copy(parms, 0, newParms, 0, i);

											newParms[i] = parms[i + 1];

											parms = newParms;
											break;
										}
									}
								}

								if (parms.Length == 1)
									return parms[0];

								if (parms.Length != len)
									return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, parms);
							}

							break;

						case "Convert":
							{
								var typef = func.SystemType.ToUnderlying();

								if (func.Parameters[1] is SqlFunction from && from.Name == "Convert" && from.Parameters[1].SystemType.ToUnderlying() == typef)
									return from.Parameters[1];

								if (func.Parameters[1] is SqlExpression fe && fe.Expr == "Cast({0} as {1})" && fe.Parameters[0].SystemType.ToUnderlying() == typef)
									return fe.Parameters[0];
							}

							break;
					}

					break;
				}
				#endregion

				case QueryElementType.SearchCondition :
					SelectQueryOptimizer.OptimizeSearchCondition((SqlSearchCondition)expression);
					break;

				case QueryElementType.SqlExpression   :
				{
					var se = (SqlExpression)expression;

					if (se.Expr == "{0}" && se.Parameters.Length == 1 && se.Parameters[0] != null && se.CanBeNull == se.Parameters[0].CanBeNull)
						return se.Parameters[0];

					break;
				}
			}

			return expression;
		}

		public virtual ISqlPredicate ConvertPredicate(SelectQuery selectQuery, ISqlPredicate predicate)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					{
						var expr = (SqlPredicate.ExprExpr)predicate;

						//if (expr.Expr1 is SqlField && expr.Expr2 is SqlParameter)
						//{
						//	if (((SqlParameter)expr.Expr2).DataType == DataType.Undefined)
						//		((SqlParameter)expr.Expr2).DataType = ((SqlField)expr.Expr1).DataType;
						//}
						//else if (expr.Expr2 is SqlField && expr.Expr1 is SqlParameter)
						//{
						//	if (((SqlParameter)expr.Expr1).DataType == DataType.Undefined)
						//		((SqlParameter)expr.Expr1).DataType = ((SqlField)expr.Expr2).DataType;
						//}

						if (expr.Expr2 is SqlParameter parameterExpr2 && parameterExpr2.DataType == DataType.Undefined)
						{
							var innerExpr = expr.Expr1;

							while (innerExpr is SqlColumn c)
								innerExpr = c.Expression;

							if (innerExpr is SqlField field)
								parameterExpr2.DataType = field.DataType;
						}

						if (expr.Expr1 is SqlParameter parameterExpr1 && parameterExpr1.DataType == DataType.Undefined)
						{
							var innerExpr = expr.Expr2;

							while (innerExpr is SqlColumn c)
								innerExpr = c.Expression;

							if (innerExpr is SqlField field)
								parameterExpr1.DataType = field.DataType;
						}

						if (expr.Operator == SqlPredicate.Operator.Equal &&
						    expr.Expr1 is SqlValue sqlValue &&
						    expr.Expr2 is SqlValue value1)
						{
							var value = Equals(sqlValue.Value, value1.Value);
							return new SqlPredicate.Expr(new SqlValue(value), Precedence.Comparison);
						}

						switch (expr.Operator)
						{
							case SqlPredicate.Operator.Equal          :
							case SqlPredicate.Operator.NotEqual       :
							case SqlPredicate.Operator.Greater        :
							case SqlPredicate.Operator.GreaterOrEqual :
							case SqlPredicate.Operator.Less           :
							case SqlPredicate.Operator.LessOrEqual    :
								predicate = OptimizeCase(selectQuery, expr);
								break;
						}

						if (predicate is SqlPredicate.ExprExpr ex)
						{
							switch (ex.Operator)
							{
								case SqlPredicate.Operator.Equal      :
								case SqlPredicate.Operator.NotEqual   :
									var expr1 = ex.Expr1;
									var expr2 = ex.Expr2;

									if (Configuration.Linq.CompareNullsAsValues && expr1.CanBeNull && expr2.CanBeNull)
									{
										if (expr1 is SqlParameter || expr2 is SqlParameter)
											selectQuery.IsParameterDependent = true;
										else
											if (expr1 is SqlColumn || expr1 is SqlField)
											if (expr2 is SqlColumn || expr2 is SqlField)
												predicate = ConvertEqualPredicate(ex);
									}

									break;
							}
						}
					}

					break;

				case QueryElementType.NotExprPredicate:
					{
						var expr = (SqlPredicate.NotExpr)predicate;

						if (expr.IsNot && expr.Expr1 is SqlSearchCondition sc)
						{
							if (sc.Conditions.Count == 1)
							{
								var cond = sc.Conditions[0];

								if (cond.IsNot)
									return cond.Predicate;

								if (cond.Predicate is SqlPredicate.ExprExpr ee)
								{
									if (ee.Operator == SqlPredicate.Operator.Equal)
										return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.NotEqual, ee.Expr2);

									if (ee.Operator == SqlPredicate.Operator.NotEqual)
										return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.Equal, ee.Expr2);
								}
							}
						}
					}

					break;
			}

			return predicate;
		}

		protected ISqlPredicate ConvertEqualPredicate(SqlPredicate.ExprExpr expr)
		{
			var expr1 = expr.Expr1;
			var expr2 = expr.Expr2;
			var cond  = new SqlSearchCondition();

			if (expr.Operator == SqlPredicate.Operator.Equal)
				cond
					.Expr(expr1).IsNull.    And .Expr(expr2).IsNull. Or
					/*.Expr(expr1).IsNotNull. And .Expr(expr2).IsNotNull. And */.Expr(expr1).Equal.Expr(expr2);
			else
				cond
					.Expr(expr1).IsNull.    And .Expr(expr2).IsNotNull. Or
					.Expr(expr1).IsNotNull. And .Expr(expr2).IsNull.    Or
					.Expr(expr1).NotEqual.Expr(expr2);

			return cond;
		}

		static SqlPredicate.Operator InvertOperator(SqlPredicate.Operator op, bool skipEqual)
		{
			switch (op)
			{
				case SqlPredicate.Operator.Equal          : return skipEqual ? op : SqlPredicate.Operator.NotEqual;
				case SqlPredicate.Operator.NotEqual       : return skipEqual ? op : SqlPredicate.Operator.Equal;
				case SqlPredicate.Operator.Greater        : return SqlPredicate.Operator.LessOrEqual;
				case SqlPredicate.Operator.NotLess        :
				case SqlPredicate.Operator.GreaterOrEqual : return SqlPredicate.Operator.Less;
				case SqlPredicate.Operator.Less           : return SqlPredicate.Operator.GreaterOrEqual;
				case SqlPredicate.Operator.NotGreater     :
				case SqlPredicate.Operator.LessOrEqual    : return SqlPredicate.Operator.Greater;
				default: throw new InvalidOperationException();
			}
		}

		internal static ISqlPredicate OptimizePredicate(ISqlPredicate predicate, ref bool isNot)
		{
			if (isNot)
			{
				if (predicate is SqlPredicate.ExprExpr expr)
				{
					var newOperator = InvertOperator(expr.Operator, false);
					if (newOperator != expr.Operator)
					{
						predicate = new SqlPredicate.ExprExpr(expr.Expr1, newOperator, expr.Expr2);
						isNot     = false;
					}
				}
			}

			return predicate;
		}

		ISqlPredicate OptimizeCase(SelectQuery selectQuery, SqlPredicate.ExprExpr expr)
		{
			var value = expr.Expr1 as SqlValue;
			var func  = expr.Expr2 as SqlFunction;
			var valueFirst = false;

			if (value != null && func != null)
			{
				valueFirst = true;
			}
			else
			{
				value = expr.Expr2 as SqlValue;
				func  = expr.Expr1 as SqlFunction;
			}

			if (value != null && func != null && func.Name == "CASE")
			{
				if (value.Value is int n && func.Parameters.Length == 5)
				{
					if (func.Parameters[0] is SqlSearchCondition c1 && c1.Conditions.Count == 1 &&
					    func.Parameters[1] is SqlValue           v1 && v1.Value is int i1 &&
					    func.Parameters[2] is SqlSearchCondition c2 && c2.Conditions.Count == 1 &&
					    func.Parameters[3] is SqlValue           v2 && v2.Value is int i2 &&
					    func.Parameters[4] is SqlValue           v3 && v3.Value is int i3)
					{
						if (c1.Conditions[0].Predicate is SqlPredicate.ExprExpr ee1 &&
						    c2.Conditions[0].Predicate is SqlPredicate.ExprExpr ee2 &&
						    ee1.Expr1.Equals(ee2.Expr1) && ee1.Expr2.Equals(ee2.Expr2))
						{
							int e = 0, g = 0, l = 0;

							if (ee1.Operator == SqlPredicate.Operator.Equal   || ee2.Operator == SqlPredicate.Operator.Equal)   e = 1;
							if (ee1.Operator == SqlPredicate.Operator.Greater || ee2.Operator == SqlPredicate.Operator.Greater) g = 1;
							if (ee1.Operator == SqlPredicate.Operator.Less    || ee2.Operator == SqlPredicate.Operator.Less)    l = 1;

							if (e + g + l == 2)
							{
								var n1 = Compare(valueFirst ? n : i1, valueFirst ? i1 : n, expr.Operator) ? 1 : 0;
								var n2 = Compare(valueFirst ? n : i2, valueFirst ? i2 : n, expr.Operator) ? 1 : 0;
								var n3 = Compare(valueFirst ? n : i3, valueFirst ? i3 : n, expr.Operator) ? 1 : 0;

								if (n1 + n2 + n3 == 1)
								{
									if (n1 == 1) return ee1;
									if (n2 == 1) return ee2;

									return ConvertPredicate(
										selectQuery,
										new SqlPredicate.ExprExpr(
											ee1.Expr1,
											e == 0 ? SqlPredicate.Operator.Equal :
											g == 0 ? SqlPredicate.Operator.Greater :
													 SqlPredicate.Operator.Less,
											ee1.Expr2));
								}

								//	CASE
								//		WHEN [p].[FirstName] > 'John'
								//			THEN 1
								//		WHEN [p].[FirstName] = 'John'
								//			THEN 0
								//		ELSE -1
								//	END <= 0
								if (ee1.Operator == SqlPredicate.Operator.Greater && i1 == 1 &&
									ee2.Operator == SqlPredicate.Operator.Equal   && i2 == 0 &&
									i3 == -1 && n == 0)
								{
									return ConvertPredicate(
										selectQuery,
										new SqlPredicate.ExprExpr(
											ee1.Expr1,
											valueFirst ? InvertOperator(expr.Operator, true) : expr.Operator,
											ee1.Expr2));
								}
							}
						}
					}
				}
				else if (value.Value is bool bv && func.Parameters.Length == 3)
				{
					if (func.Parameters[0] is SqlSearchCondition c1 && c1.Conditions.Count == 1 &&
					    func.Parameters[1] is SqlValue           v1 && v1.Value is bool bv1     &&
					    func.Parameters[2] is SqlValue           v2 && v2.Value is bool bv2)
					{
						if (bv == bv1 && expr.Operator == SqlPredicate.Operator.Equal ||
							bv != bv1 && expr.Operator == SqlPredicate.Operator.NotEqual)
						{
							return c1;
						}

						if (bv == bv2 && expr.Operator == SqlPredicate.Operator.NotEqual ||
							bv != bv1 && expr.Operator == SqlPredicate.Operator.Equal)
						{
							if (c1.Conditions[0].Predicate is SqlPredicate.ExprExpr ee)
							{
								var op = InvertOperator(ee.Operator, false);
								return new SqlPredicate.ExprExpr(ee.Expr1, op, ee.Expr2);
							}

							var sc = new SqlSearchCondition();

							sc.Conditions.Add(new SqlCondition(true, c1));

							return sc;
						}
					}
				}
				else if (expr.Operator == SqlPredicate.Operator.Equal && func.Parameters.Length == 3)
				{
					if (func.Parameters[0] is SqlSearchCondition sc &&
					    func.Parameters[1] is SqlValue v1 &&
					    func.Parameters[2] is SqlValue v2)
					{
						if (Equals(value.Value, v1.Value))
							return sc;

						if (Equals(value.Value, v2.Value) && !sc.CanBeNull)
							return ConvertPredicate(
								selectQuery,
								new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation));
					}
				}
			}

			return expr;
		}

		static bool Compare(int v1, int v2, SqlPredicate.Operator op)
		{
			switch (op)
			{
				case SqlPredicate.Operator.Equal:           return v1 == v2;
				case SqlPredicate.Operator.NotEqual:        return v1 != v2;
				case SqlPredicate.Operator.Greater:         return v1 >  v2;
				case SqlPredicate.Operator.NotLess:
				case SqlPredicate.Operator.GreaterOrEqual:  return v1 >= v2;
				case SqlPredicate.Operator.Less:            return v1 <  v2;
				case SqlPredicate.Operator.NotGreater:
				case SqlPredicate.Operator.LessOrEqual:     return v1 <= v2;
			}

			throw new InvalidOperationException();
		}

		#endregion

		#region DataTypes

		protected virtual int GetMaxLength     (SqlDataType type) { return SqlDataType.GetMaxLength     (type.DataType); }
		protected virtual int GetMaxPrecision  (SqlDataType type) { return SqlDataType.GetMaxPrecision  (type.DataType); }
		protected virtual int GetMaxScale      (SqlDataType type) { return SqlDataType.GetMaxScale      (type.DataType); }
		protected virtual int GetMaxDisplaySize(SqlDataType type) { return SqlDataType.GetMaxDisplaySize(type.DataType); }

		protected virtual ISqlExpression ConvertConvertion(SqlFunction func)
		{
			var from = (SqlDataType)func.Parameters[1];
			var to   = (SqlDataType)func.Parameters[0];

			if (to.Type == typeof(object))
				return func.Parameters[2];

			if (to.Length > 0)
			{
				var maxLength = to.Type == typeof(string) ? GetMaxDisplaySize(from) : GetMaxLength(from);
				var newLength = maxLength >= 0 ? Math.Min(to.Length ?? 0, maxLength) : to.Length;

				if (to.Length != newLength)
					to = new SqlDataType(to.DataType, to.Type, newLength, null, null);
			}
			else if (from.Type == typeof(short) && to.Type == typeof(int))
				return func.Parameters[2];

			return ConvertExpression(new SqlFunction(func.SystemType, "Convert", to, func.Parameters[2]));
		}

		#endregion

		#region Alternative Builders

		protected ISqlExpression AlternativeConvertToBoolean(SqlFunction func, int paramNumber)
		{
			var par = func.Parameters[paramNumber];

			if (par.SystemType.IsFloatType() || par.SystemType.IsIntegerType())
			{
				var sc = new SqlSearchCondition();

				sc.Conditions.Add(
					new SqlCondition(false, new SqlPredicate.ExprExpr(par, SqlPredicate.Operator.Equal, new SqlValue(0))));

				return ConvertExpression(new SqlFunction(func.SystemType, "CASE", sc, new SqlValue(false), new SqlValue(true)));
			}

			return null;
		}

		protected static bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType   : return ((SqlDataType)  expr).DataType == DataType.Date;
				case QueryElementType.SqlExpression : return ((SqlExpression)expr).Expr     == dateName;
			}

			return false;
		}

		protected static bool IsTimeDataType(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType   : return ((SqlDataType)expr).  DataType == DataType.Time;
				case QueryElementType.SqlExpression : return ((SqlExpression)expr).Expr     == "Time";
			}

			return false;
		}

		protected ISqlExpression FloorBeforeConvert(SqlFunction func)
		{
			var par1 = func.Parameters[1];

			return par1.SystemType.IsFloatType() && func.SystemType.IsIntegerType() ?
				new SqlFunction(func.SystemType, "Floor", par1) : par1;
		}

		protected SqlDeleteStatement GetAlternativeDelete(SqlDeleteStatement deleteStatement)
		{
			if ((deleteStatement.SelectQuery.From.Tables.Count > 1 || deleteStatement.SelectQuery.From.Tables[0].Joins.Count > 0) &&
				deleteStatement.SelectQuery.From.Tables[0].Source is SqlTable table)
			{
				var sql = new SelectQuery { IsParameterDependent = deleteStatement.IsParameterDependent };

				var newDeleteStatement = new SqlDeleteStatement(sql);

				deleteStatement.SelectQuery.ParentSelect = sql;

				var copy      = new SqlTable(table) { Alias = null };
				var tableKeys = table.GetKeys(true);
				var copyKeys  = copy. GetKeys(true);

				if (deleteStatement.SelectQuery.Where.SearchCondition.Conditions.Any(c => c.IsOr))
				{
					var sc1 = new SqlSearchCondition(deleteStatement.SelectQuery.Where.SearchCondition.Conditions);
					var sc2 = new SqlSearchCondition();

					for (var i = 0; i < tableKeys.Count; i++)
					{
						sc2.Conditions.Add(new SqlCondition(
							false,
							new SqlPredicate.ExprExpr(copyKeys[i], SqlPredicate.Operator.Equal, tableKeys[i])));
					}

					deleteStatement.SelectQuery.Where.SearchCondition.Conditions.Clear();
					deleteStatement.SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, sc1));
					deleteStatement.SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, sc2));
				}
				else
				{
					for (var i = 0; i < tableKeys.Count; i++)
						deleteStatement.SelectQuery.Where.Expr(copyKeys[i]).Equal.Expr(tableKeys[i]);
				}

				newDeleteStatement.SelectQuery.From.Table(copy).Where.Exists(deleteStatement.SelectQuery);
				newDeleteStatement.Parameters.AddRange(deleteStatement.Parameters);
				newDeleteStatement.With = deleteStatement.With;

				deleteStatement.Parameters.Clear();

				deleteStatement = newDeleteStatement;
			}

			return deleteStatement;
		}

		protected SqlStatement GetAlternativeUpdate(SqlUpdateStatement updateStatement)
		{
			if (updateStatement.SelectQuery.From.Tables[0].Source is SqlTable || updateStatement.Update.Table != null)
			{
				if (updateStatement.SelectQuery.From.Tables.Count > 1 || updateStatement.SelectQuery.From.Tables[0].Joins.Count > 0)
				{
					var sql = new SelectQuery { IsParameterDependent = updateStatement.IsParameterDependent  };

					var newUpdateStatement = new SqlUpdateStatement(sql);
					updateStatement.SelectQuery.ParentSelect = sql;

					var table = updateStatement.Update.Table ?? (SqlTable)updateStatement.SelectQuery.From.Tables[0].Source;

					if (updateStatement.Update.Table != null)
						if (QueryVisitor.Find(updateStatement.SelectQuery.From, t => t == table) == null)
							table = (SqlTable)QueryVisitor.Find(updateStatement.SelectQuery.From,
								ex => ex is SqlTable sqlTable && sqlTable.ObjectType == table.ObjectType) ?? table;

					var copy = new SqlTable(table);

					var tableKeys = table.GetKeys(true);
					var copyKeys  = copy. GetKeys(true);

					for (var i = 0; i < tableKeys.Count; i++)
						updateStatement.SelectQuery.Where
							.Expr(copyKeys[i]).Equal.Expr(tableKeys[i]);

					newUpdateStatement.SelectQuery.From.Table(copy).Where.Exists(updateStatement.SelectQuery);

					var map = new Dictionary<SqlField,SqlField>(table.Fields.Count);

					foreach (var field in table.Fields.Values)
						map.Add(field, copy[field.Name]);

					foreach (var item in updateStatement.Update.Items)
					{
						var ex = new QueryVisitor().Convert(item, expr =>
							expr is SqlField fld && map.TryGetValue(fld, out fld) ? fld : expr);

						newUpdateStatement.Update.Items.Add(ex);
					}

					newUpdateStatement.Parameters.AddRange(updateStatement.Parameters);
					newUpdateStatement.Update.Table = updateStatement.Update.Table;
					newUpdateStatement.With         = updateStatement.With;

					updateStatement.Parameters.Clear();
					updateStatement.Update.Items.Clear();

					updateStatement = newUpdateStatement;
				}

				updateStatement.SelectQuery.From.Tables[0].Alias = "$";
			}

			return updateStatement;
		}

		#endregion

		#region Helpers

		static string SetAlias(string alias, int maxLen)
		{
			if (alias == null)
				return null;

			alias = alias.TrimStart('_');

			var cs      = alias.ToCharArray();
			var replace = false;

			for (var i = 0; i < cs.Length; i++)
			{
				var c = cs[i];

				if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')
					continue;

				cs[i] = ' ';
				replace = true;
			}

			if (replace)
				alias = new string(cs).Replace(" ", "");

			return alias.Length == 0 || alias.Length > maxLen ? null : alias;
		}

		protected void CheckAliases(SqlStatement statement, int maxLen)
		{
			new QueryVisitor().Visit(statement, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField     : ((SqlField)               e).Alias = SetAlias(((SqlField)               e).Alias, maxLen); break;
					case QueryElementType.SqlParameter : ((SqlParameter)           e).Name  = SetAlias(((SqlParameter)           e).Name,  maxLen); break;
					case QueryElementType.SqlTable     : ((SqlTable)               e).Alias = SetAlias(((SqlTable)               e).Alias, maxLen); break;
					case QueryElementType.Column       : ((SqlColumn)     e).Alias = SetAlias(((SqlColumn)     e).Alias, maxLen); break;
					case QueryElementType.TableSource  : ((SqlTableSource)e).Alias = SetAlias(((SqlTableSource)e).Alias, maxLen); break;
				}
			});
		}

		public ISqlExpression Add(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive));
		}

		public ISqlExpression Add<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		public ISqlExpression Add(ISqlExpression expr1, int value)
		{
			return Add<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Inc(ISqlExpression expr1)
		{
			return Add(expr1, 1);
		}

		public ISqlExpression Sub(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "-", expr2, Precedence.Subtraction));
		}

		public ISqlExpression Sub<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Sub(expr1, expr2, typeof(T));
		}

		public ISqlExpression Sub(ISqlExpression expr1, int value)
		{
			return Sub<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Dec(ISqlExpression expr1)
		{
			return Sub(expr1, 1);
		}

		public ISqlExpression Mul(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "*", expr2, Precedence.Multiplicative));
		}

		public ISqlExpression Mul<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Mul(expr1, expr2, typeof(T));
		}

		public ISqlExpression Mul(ISqlExpression expr1, int value)
		{
			return Mul<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Div(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative));
		}

		public ISqlExpression Div<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Div(expr1, expr2, typeof(T));
		}

		public ISqlExpression Div(ISqlExpression expr1, int value)
		{
			return Div<int>(expr1, new SqlValue(value));
		}

		#endregion

		#region Optimizing Joins

		public void OptimizeJoins(SqlStatement statement)
		{
			((ISqlExpressionWalkable) statement).Walk(false, element =>
			{
				if (element is SelectQuery query)
					new JoinOptimizer().OptimizeJoins(statement, query);
				return element;
			});
		}

		#endregion
	}
}
