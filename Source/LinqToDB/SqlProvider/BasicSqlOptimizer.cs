using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace LinqToDB.SqlProvider
{
	using Common;
	using Extensions;
	using SqlQuery;
	using Tools;
	using Mapping;
	using DataProvider;

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
			FixEmptySelect(statement);

			FinalizeCte(statement);

//statement.EnsureFindTables();
			//TODO: We can use Walk here but OptimizeUnions fails with subqueries. Needs revising.
			statement.WalkQueries(
				selectQuery =>
				{
					new SelectQueryOptimizer(SqlProviderFlags, statement, selectQuery, 0).FinalizeAndValidate(
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
						new SelectQueryOptimizer(SqlProviderFlags, statement, selectQuery, 0).FinalizeAndValidate(
							SqlProviderFlags.IsApplyJoinSupported,
							SqlProviderFlags.IsGroupByExpressionSupported);

						return selectQuery;
					}
				);
			}


//statement.EnsureFindTables();
			if (Configuration.Linq.OptimizeJoins)
			{
				OptimizeJoins(statement);

				// Do it again after JOIN Optimization
				FinalizeCte(statement);
			}

			BuildSqlValueTableParameters(statement);

//statement.EnsureFindTables();
			return statement;
		}

		protected virtual void FixEmptySelect(SqlStatement statement)
		{
			// avoid SELECT * top level queries, as they could create a lot of unwanted traffic
			// and such queries are not supported by remote context
			if (statement.QueryType == QueryType.Select && statement.SelectQuery!.Select.Columns.Count == 0)
				statement.SelectQuery!.Select.Add(new SqlValue(1));
		}

		public virtual SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement;
		}

		void FinalizeCte(SqlStatement statement)
		{
			if (statement is SqlStatementWithQueryBase select)
			{
				var foundCte  = new Dictionary<CteClause, HashSet<CteClause>>();

				void RegisterDependency(CteClause cteClause)
				{
					if (foundCte.ContainsKey(cteClause))
						return;

					var dependsOn = new HashSet<CteClause>();
					new QueryVisitor().Visit(cteClause.Body!, ce =>
					{
						if (ce.ElementType == QueryElementType.SqlCteTable)
						{
							var subCte = ((SqlCteTable)ce).Cte!;
							dependsOn.Add(subCte);
						}

					});
					// self-reference is allowed, so we do not need to add dependency
					dependsOn.Remove(cteClause);
					foundCte.Add(cteClause, dependsOn);

					foreach (var clause in dependsOn)
					{
						RegisterDependency(clause);
					}
				}

				new QueryVisitor().Visit(select.SelectQuery, e =>
					{
						if (e.ElementType == QueryElementType.SqlCteTable)
						{
							var cte = ((SqlCteTable)e).Cte!;
							RegisterDependency(cte);
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

					var ordered = TopoSorting.TopoSort(foundCte.Keys, i => foundCte[i]).ToList();

					Utils.MakeUniqueNames(ordered, null, (n, a) => !ReservedWords.IsReserved(n), c => c.Name, (c, n, a) => c.Name = n,
						c => c.Name.IsNullOrEmpty() ? "CTE_1" : c.Name, StringComparer.OrdinalIgnoreCase);

					select.With = new SqlWithClause();
					select.With.Clauses.AddRange(ordered);
				}
			}
		}


		protected static bool HasParameters(ISqlExpression expr)
		{
			var hasParameters  = null != new QueryVisitor().Find(expr,
				el => el.ElementType == QueryElementType.SqlParameter);

			return hasParameters;
		}

		static T NormalizeExpressions<T>(T expression) 
			where T : class, IQueryElement
		{
			var result = ConvertVisitor.Convert(expression, (visitor, e) =>
			{
				if (e.ElementType == QueryElementType.SqlExpression)
				{
					var expr = (SqlExpression)e;
					var newExpression = expr;

					// we interested in modifying only expressions which have parameters
					if (HasParameters(expr))
					{
						if (expr.Expr.IsNullOrEmpty() || expr.Parameters.Length == 0)
							return expr;

						var newExpressions = new List<ISqlExpression>();

						var changed = false;

						var newExpr = QueryHelper.TransformExpressionIndexes(expr.Expr,
							idx =>
							{
								if (idx >= 0 && idx < expr.Parameters.Length)
								{
									var paramExpr  = expr.Parameters[idx];
									var normalized = NormalizeExpressions(paramExpr);

									if (!changed && !ReferenceEquals(normalized, paramExpr))
										changed = true;

									var newIndex   = newExpressions.Count;

									newExpressions.Add(normalized);
									return newIndex;
								}
								return idx;
							});

						changed = changed || newExpr != expr.Expr;

						if (changed)
							newExpression = new SqlExpression(expr.SystemType, newExpr, expr.Precedence, expr.IsAggregate, expr.IsPure, newExpressions.ToArray());

						return newExpression;
					}
				}
				return e;
			});

			return result;
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
					subQuery.Where.SearchCondition = SelectQueryOptimizer.OptimizeSearchCondition(subQuery.Where.SearchCondition);

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
						return e.ElementType switch
						{
							QueryElementType.SqlField => !allTables.Contains(((SqlField)e).Table!),
							QueryElementType.Column   => !allTables.Contains(((SqlColumn)e).Parent!),
							_                         => false,
						};
					}

					var join = subQuery.LeftJoin();

					query.From.Tables[0].Joins.Add(join.JoinedTable);

					for (var j = 0; j < subQuery.Where.SearchCondition.Conditions.Count; j++)
					{
						var cond = subQuery.Where.SearchCondition.Conditions[j];

						if (new QueryVisitor().Find(cond, CheckTable) == null)
							continue;

						var replaced = new Dictionary<IQueryElement,IQueryElement>();

						var nc = ConvertVisitor.Convert(cond, (v, e) =>
						{
							var ne = e;

							switch (e.ElementType)
							{
								case QueryElementType.SqlField :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SqlField)e).Table!))
									{
										subQuery.GroupBy.Expr((SqlField)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SqlField)e)];
									}

									break;

								case QueryElementType.Column   :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SqlColumn)e).Parent!))
									{
										subQuery.GroupBy.Expr((SqlColumn)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SqlColumn)e)];
									}

									break;
							}

							if (ne != null && !ReferenceEquals(e, ne))
								replaced.Add(e, ne);

							return ne ?? e;
						});

						if (nc != null && !ReferenceEquals(nc, cond))
						{
							join.JoinedTable.Condition.Conditions.Add(nc);
							subQuery.Where.SearchCondition.Conditions.RemoveAt(j);
							j--;
						}
					}

					if (!query.GroupBy.IsEmpty)
					{
						var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

						subQuery.Select.Columns.RemoveAt(0);

						var parm = subQuery.Select.Columns.Count > 0 ? (ISqlExpression)subQuery.Select.Columns[0] : query.All;

						query.Select.Columns[i].Expression = new SqlFunction(oldFunc.SystemType, oldFunc.Name, parm);
					}
					else
					{
						query.Select.Columns[i].Expression = subQuery.Select.Columns[0];
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
							return e.ElementType switch
							{
								QueryElementType.SqlField => !allTables.Contains(((SqlField)e).Table!),
								QueryElementType.Column	  => !allTables.Contains(((SqlColumn)e).Parent!),
								_                         => false,
							};
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

						if (SqlProviderFlags.IsSubQueryColumnSupported && new QueryVisitor().Find(subQuery, CheckTable) == null)
							continue;

						// Join should not have ParentSelect, while SubQuery has
						subQuery.ParentSelect = null;

						var join = subQuery.LeftJoin();

						query.From.Tables[0].Joins.Add(join.JoinedTable);

						subQuery.Where.SearchCondition = SelectQueryOptimizer.OptimizeSearchCondition(subQuery.Where.SearchCondition);

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

							if (new QueryVisitor().Find(cond, CheckTable) == null)
								continue;

							var replaced = new Dictionary<IQueryElement,IQueryElement>();

							var nc = ConvertVisitor.Convert(cond, (v, e) =>
							{
								var ne = e;

								switch (e.ElementType)
								{
									case QueryElementType.SqlField :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SqlField)e).Table!))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SqlField)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SqlField)e)]!;
										}

										break;

									case QueryElementType.Column   :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SqlColumn)e).Parent!))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SqlColumn)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SqlColumn)e)]!;
										}

										break;
								}

								if (ne != null && !ReferenceEquals(e, ne))
									replaced.Add(e, ne);

								return ne ?? e;
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

			selectQuery = ConvertVisitor.Convert(selectQuery, (v, e) => dic.TryGetValue(e, out var ne) ? ne : e);

			return selectQuery;
		}

		#region Optimization

		public static ISqlExpression CreateSqlValue(object value, SqlBinaryExpression be)
		{
			return CreateSqlValue(value, be.Expr1, be.Expr2);
		}

		public static ISqlExpression CreateSqlValue(object value, params ISqlExpression[] basedOn)
		{
			SqlParameter? foundParam = null;

			foreach (var element in basedOn)
			{
				if (element.ElementType == QueryElementType.SqlParameter)
				{
					var param = (SqlParameter)element;
					if (param.IsQueryParameter)
						return new SqlParameter(param.Type, param.Name, value);
					foundParam ??= param;
				}
			}

			if (foundParam != null)
				return new SqlParameter(foundParam.Type, foundParam.Name, value) { IsQueryParameter = false };

			return new SqlValue(value);
		}

		public virtual ISqlExpression OptimizeExpression(ISqlExpression expression, IReadOnlyParameterValues? parameterValues)
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
							var v1 = be.Expr1.TryEvaluateExpression(parameterValues, out var value1);
							if (v1)
							{
								switch (value1)
								{
									case short   h when h == 0  :
									case int     i when i == 0  :
									case long    l when l == 0  :
									case decimal d when d == 0  :
									case string  s when s == "" : return be.Expr2;
								}
							}

							var v2 = be.Expr2.TryEvaluateExpression(parameterValues, out var value2);
							if (v2)
							{
								switch (value2)
								{
									case int vi when vi == 0 : return be.Expr1;
									case int vi when
										be.Expr1    is SqlBinaryExpression be1 &&
										be1.Expr2.TryEvaluateExpression(parameterValues, out var be1v2) &&
										be1v2 is int be1v2i :
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

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, be), be.Precedence);
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

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, be), be.Precedence);
											}
										}

										break;
									}

									case string vs when vs == "" : return be.Expr1;
									case string vs when
										be.Expr1    is SqlBinaryExpression be1 &&
										//be1.Operation == "+"                   &&
										be1.Expr2.TryEvaluateExpression(parameterValues, out var be1v2) &&
										be1v2 is string be1v2s :
									{
										return new SqlBinaryExpression(
											be1.SystemType,
											be1.Expr1,
											be1.Operation,
											new SqlValue(string.Concat(be1v2s, vs)));
									}
								}
							}

							if (v1 && v2)
							{
								if (value1 is int i1 && value2 is int i2) return CreateSqlValue(i1 + i2, be);
								if (value1 is string || value2 is string) return CreateSqlValue(value1?.ToString() + value2, be);
							}

							break;
						}

						case "-":
						{
							var v2 = be.Expr2.TryEvaluateExpression(parameterValues, out var value2);
							if (v2)
							{
								switch (value2)
								{
									case int vi when vi == 0 : return be.Expr1;
									case int vi when
										be.Expr1 is SqlBinaryExpression be1 &&
										be1.Expr2.TryEvaluateExpression(parameterValues, out var be1v2) &&
										be1v2 is int be1v2i :
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

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, be), be.Precedence);
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

												return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, be), be.Precedence);
											}
										}

										break;
									}
								}
							}

							if (v2 && be.Expr1.TryEvaluateExpression(parameterValues, out var value1))
							{
								if (value1 is int i1 && value2 is int i2) return CreateSqlValue(i1 - i2, be);
							}

							break;
						}

						case "*":
						{
							var v1 = be.Expr1.TryEvaluateExpression(parameterValues, out var value1);
							if (v1)
							{
								switch (value1)
								{
									case int i when i == 0 : return CreateSqlValue(0, be);
									case int i when i == 1 : return be.Expr2;
									case int i when
										be.Expr2    is SqlBinaryExpression be2 &&
										be2.Operation == "*"                   &&
										be2.Expr1.TryEvaluateExpression(parameterValues, out var be2v1)  &&
										be2v1 is int bi :
									{
										return OptimizeExpression(
											new SqlBinaryExpression(be2.SystemType, CreateSqlValue(i * bi, be), "*", be2.Expr2), parameterValues);
									}
								}
							}

							var v2 = be.Expr2.TryEvaluateExpression(parameterValues, out var value2);
							if (v2)
							{
								switch (value2)
								{
									case int i when i == 0 : return CreateSqlValue(0, be);
									case int i when i == 1 : return be.Expr1;
								}
							}

							if (v1 && v2)
							{
								switch (value1)
								{
									case int    i1 when value2 is int    i2 : return CreateSqlValue(i1 * i2, be);
									case int    i1 when value2 is double d2 : return CreateSqlValue(i1 * d2, be);
									case double d1 when value2 is int    i2 : return CreateSqlValue(d1 * i2, be);
									case double d1 when value2 is double d2 : return CreateSqlValue(d1 * d2, be);
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
						case "CASE"     :
						{
							var parms = func.Parameters;
							var len   = parms.Length;

							for (var i = 0; i < parms.Length - 1; i += 2)
							{
								var boolValue = QueryHelper.GetBoolValue(parms[i], parameterValues);
								if (boolValue != null)
								{
									if (boolValue == false)
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

							if (parms.Length == 3 && parms[0].ElementType == QueryElementType.SqlFunction)
							{
								var boolValue1 = QueryHelper.GetBoolValue(parms[1], parameterValues);
								var boolValue2 = QueryHelper.GetBoolValue(parms[2], parameterValues);

								if (boolValue1 != null && boolValue2 != null)
								{
									if (boolValue1 == boolValue2)
										return new SqlValue(true);
										
									if (!boolValue1.Value)
										return new SqlSearchCondition(new SqlCondition(true, new SqlPredicate.Expr(parms[0], parms[0].Precedence)));

									return parms[0];
								}
							}
						}

						break;

						case "EXISTS":
						{
							if (func.Parameters.Length == 1 && func.Parameters[0] is SelectQuery query && query.Select.Columns.Count > 0)
							{
								var isAggregateQuery =
									query.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression));

								if (isAggregateQuery)
									return new SqlValue(true);
							}

							break;
						}
					}

					break;
				}
				#endregion

				case QueryElementType.SearchCondition :
				{
					expression = SelectQueryOptimizer.OptimizeSearchCondition((SqlSearchCondition)expression, parameterValues);
					break;
				}

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

		public virtual ISqlPredicate OptimizePredicate(ISqlPredicate predicate, IReadOnlyParameterValues? parameterValues)
		{
			// Avoiding infinite recursion
			//
			if (predicate.ElementType == QueryElementType.ExprPredicate)
			{
				var exprPredicate = (SqlPredicate.Expr)predicate;
				if (exprPredicate.Expr1.ElementType == QueryElementType.SqlValue)
					return predicate;
			}

			if (predicate.TryEvaluateExpression(parameterValues, out var value) && value != null)
			{
				return new SqlPredicate.Expr(new SqlValue(value));
			}

			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					var expr = (SqlPredicate.ExprExpr)predicate;

					if (expr.WithNull == null && expr.Operator.In(SqlPredicate.Operator.Equal, SqlPredicate.Operator.NotEqual))
					{
						if (expr.Expr2 is ISqlPredicate)
						{
							var boolValue1 = QueryHelper.GetBoolValue(expr.Expr1, parameterValues);
							if (boolValue1 != null)
							{
								ISqlPredicate transformed = new SqlPredicate.Expr(expr.Expr2);
								var isNot = boolValue1.Value != (expr.Operator == SqlPredicate.Operator.Equal);
								if (isNot)
								{
									transformed =
										new SqlPredicate.NotExpr(expr.Expr2, true, Precedence.LogicalNegation);
								}

								return transformed;
							}
						}							
							
						if (expr.Expr1 is ISqlPredicate)
						{
							var boolValue2 = QueryHelper.GetBoolValue(expr.Expr2, parameterValues);
							if (boolValue2 != null)
							{
								ISqlPredicate transformed = new SqlPredicate.Expr(expr.Expr1);
								var isNot = boolValue2.Value != (expr.Operator == SqlPredicate.Operator.Equal);
								if (isNot)
								{
									transformed =
										new SqlPredicate.NotExpr(expr.Expr1, true, Precedence.LogicalNegation);
								}

								return transformed;
							}
						}
					}

					break;
				}

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
									return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.NotEqual, ee.Expr2, Configuration.Linq.CompareNullsAsValues ? true : (bool?)null);

								if (ee.Operator == SqlPredicate.Operator.NotEqual)
									return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.Equal, ee.Expr2, Configuration.Linq.CompareNullsAsValues ? true : (bool?)null);
							}
						}
					}

					break;
				}
			}

			return predicate;
		}

		#endregion

		#region Conversion

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
							if (be.Expr1.SystemType == typeof(string) && be.Expr2.SystemType != typeof(string))
							{
								var len = be.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr2.SystemType).Type.DataType);

								if (len == null || len <= 0)
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
								var len = be.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr1.SystemType).Type.DataType);

								if (len == null || len <= 0)
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

						case "Convert":
							{
								var typef = func.SystemType.ToUnderlying();

								if (func.Parameters[1] is SqlFunction from && from.Name == "Convert" && from.Parameters[1].SystemType!.ToUnderlying() == typef)
									return from.Parameters[1];

								if (func.Parameters[1] is SqlExpression fe && fe.Expr == "Cast({0} as {1})" && fe.Parameters[0].SystemType!.ToUnderlying() == typef)
									return fe.Parameters[0];
							}

							break;
					}

					return ConvertFunction(func);
				}
				#endregion

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

		protected virtual ISqlExpression ConvertFunction(SqlFunction func)
		{
			return func;
		}

		public virtual ISqlPredicate ConvertPredicate(MappingSchema mappingSchema, ISqlPredicate predicate, IReadOnlyParameterValues? parameterValues)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					return ((SqlPredicate.ExprExpr)predicate).Reduce(parameterValues);
				case QueryElementType.IsTruePredicate:
					return ((SqlPredicate.IsTrue)predicate).Reduce();
				case QueryElementType.LikePredicate:
					return ConvertLikePredicate(mappingSchema, (SqlPredicate.Like)predicate, parameterValues);
				case QueryElementType.SearchStringPredicate:
					return ConvertContainsPredicate(mappingSchema, (SqlPredicate.SearchString)predicate, parameterValues);
				case QueryElementType.InListPredicate:
				{
					var inList = (SqlPredicate.InList)predicate;
					var reduced = inList.Reduce(parameterValues);

					if (ReferenceEquals(reduced, inList))
						return ConvertInListPredicate(mappingSchema, inList, parameterValues);

					return reduced;
				}
			}
			return predicate;
		}


		public virtual string LikeEscapeCharacter => "~";
		public virtual string LikeWildcardCharacter => "%";

		public virtual bool LikeHasCharacterSetSupport => true;
		public virtual bool LikeParameterSupport => true;
		public virtual bool LikeIsEscapeSupported => true;

		protected static string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]", "-"};
		public virtual string[]   LikeCharactersToEscape => StandardLikeCharactersToEscape;

		public virtual string EscapeLikeCharacters(string str, string escape)
		{
			var newStr = str;

			/*if (LikeHasCharacterSetSupport)
				newStr = DataTools.EscapeUnterminatedBracket(newStr);*/

			newStr = newStr.Replace(escape, escape + escape);


			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newStr = newStr.Replace(s, escape + s);
			}

			return newStr;
		}


		static ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character, ISqlExpression escapeCharacter)
		{
			var result = new SqlFunction(typeof(string), "Replace", false, true, expression, character,
				new SqlBinaryExpression(typeof(string), escapeCharacter, "+", character, Precedence.Additive));
			return result;
		}

		public static ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character)
		{
			var result = new SqlFunction(typeof(string), "Replace", false, true, expression, character,
				new SqlBinaryExpression(typeof(string), new SqlValue("["), "+",
					new SqlBinaryExpression(typeof(string), character, "+", new SqlValue("]"), Precedence.Additive),
					Precedence.Additive));
			return result;
		}


		public virtual ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			var newExpr = expression;

			if (escape == null)
				escape = new SqlValue(LikeEscapeCharacter);

			newExpr = GenerateEscapeReplacement(newExpr, escape, escape);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newExpr = GenerateEscapeReplacement(newExpr, new SqlValue(s), escape);
			}

			return newExpr;
		}

		public virtual ISqlPredicate ConvertLikePredicate(MappingSchema mappingSchema, SqlPredicate.Like predicate,
			IReadOnlyParameterValues? parameterValues)
		{
			return predicate;
		}

		public virtual ISqlPredicate ConvertContainsPredicate(MappingSchema mappingSchema, SqlPredicate.SearchString predicate,
			IReadOnlyParameterValues? parameterValues)
		{
			if (!predicate.IgnoreCase)
				throw new NotImplementedException("!predicate.IgnoreCase");

			if (predicate.Expr2.TryEvaluateExpression(parameterValues, out var patternRaw))
			{
				var patternRawValue = patternRaw as string;

				if (patternRawValue == null)
				{
					return new SqlPredicate.IsTrue(new SqlValue(true), new SqlValue(true), new SqlValue(false), null, predicate.IsNot);
				}

				var patternValue = EscapeLikeCharacters(patternRawValue, LikeEscapeCharacter);

				patternValue = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => patternValue + LikeWildcardCharacter,
					SqlPredicate.SearchString.SearchKind.EndsWith   => LikeWildcardCharacter + patternValue,
					SqlPredicate.SearchString.SearchKind.Contains   => LikeWildcardCharacter + patternValue + LikeWildcardCharacter,
					_ => throw new ArgumentOutOfRangeException()
				};

				var patternExpr = LikeParameterSupport ? CreateSqlValue(patternValue, predicate.Expr2) : new SqlValue(patternValue);

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr,
					LikeIsEscapeSupported && (patternValue != patternRawValue) ? new SqlValue(LikeEscapeCharacter) : null,
					true);
			}
			else
			{
				ISqlExpression? escape = null;

				var patternExpr = EscapeLikeCharacters(predicate.Expr2, ref escape);

				var anyCharacterExpr = new SqlValue(LikeWildcardCharacter);

				patternExpr = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => new SqlBinaryExpression(typeof(string), patternExpr, "+", anyCharacterExpr, Precedence.Additive),
					SqlPredicate.SearchString.SearchKind.EndsWith   => new SqlBinaryExpression(typeof(string), anyCharacterExpr, "+", patternExpr, Precedence.Additive),
					SqlPredicate.SearchString.SearchKind.Contains   => new SqlBinaryExpression(typeof(string), new SqlBinaryExpression(typeof(string), anyCharacterExpr, "+", patternExpr, Precedence.Additive), "+", anyCharacterExpr, Precedence.Additive),
					_ => throw new ArgumentOutOfRangeException()
				};


				patternExpr = (ISqlExpression)OptimizeElements(patternExpr, parameterValues);

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr,
					LikeIsEscapeSupported ? escape : null,
					true);
			}
		}

		static SqlField ExpectsUnderlyingField(ISqlExpression expr)
		{
			var result = QueryHelper.GetUnderlyingField(expr);
			if (result == null)
				throw new InvalidOperationException($"Cannot retrieve underlying field for '{expr.ToDebugString()}'.");
			return result;
		}

		public virtual ISqlPredicate ConvertInListPredicate(MappingSchema mappingSchema, SqlPredicate.InList p, IReadOnlyParameterValues? parameterValues)
		{
			if (p.Values == null || p.Values.Count == 0)
				return new SqlPredicate.Expr(new SqlValue(p.IsNot));

			if (p.Values.Count == 1 && p.Values[0] is SqlParameter parameter)
			{
				var paramValue = parameter.GetParameterValue(parameterValues);

				if (paramValue.Value == null)
					return new SqlPredicate.Expr(new SqlValue(p.IsNot));

				if (paramValue.Value is IEnumerable items)
				{
					if (p.Expr1 is ISqlTableSource table)
					{
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new SqlException("Cant create IN expression.");

						if (keys.Count == 1)
						{
							var values = new List<ISqlExpression>();
							var field  = ExpectsUnderlyingField(keys[0]);
							var cd     = field.ColumnDescriptor;

							foreach (var item in items)
							{
								var value = cd.MemberAccessor.GetValue(item!);
								values.Add(mappingSchema.GetSqlValue(cd.MemberType, value));
							}

							if (values.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							return new SqlPredicate.InList(keys[0], null, p.IsNot, values);
						}

						{
							var sc = new SqlSearchCondition();

							foreach (var item in items)
							{
								var itemCond = new SqlSearchCondition();

								foreach (var key in keys)
								{
									var field = ExpectsUnderlyingField(key);
									var cd    = field.ColumnDescriptor;
									var value = cd.MemberAccessor.GetValue(item!);
									//TODO: review
									var cond  = value == null ?
										new SqlCondition(false, new SqlPredicate.IsNull  (field, false)) :
										new SqlCondition(false, new SqlPredicate.ExprExpr(field, SqlPredicate.Operator.Equal, mappingSchema.GetSqlValue(value), null));

									itemCond.Conditions.Add(cond);
								}

								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
							}

							if (sc.Conditions.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							if (p.IsNot)
								return new SqlPredicate.NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

							return new SqlPredicate.Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
						}
					}

					if (p.Expr1 is ObjectSqlExpression expr)
					{
						if (expr.Parameters.Length == 1)
						{
							var values = new List<ISqlExpression>();

							foreach (var item in items)
							{
								var value = expr.GetValue(item!, 0);
								values.Add(new SqlValue(value));
							}

							if (values.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							return new SqlPredicate.InList(expr.Parameters[0], null, p.IsNot, values);
						}

						var sc = new SqlSearchCondition();

						foreach (var item in items)
						{
							var itemCond = new SqlSearchCondition();

							for (var i = 0; i < expr.Parameters.Length; i++)
							{
								var sql   = expr.Parameters[i];
								var value = expr.GetValue(item!, i);
								var cond  = value == null ?
									new SqlCondition(false, new SqlPredicate.IsNull  (sql, false)) :
									new SqlCondition(false, new SqlPredicate.ExprExpr(sql, SqlPredicate.Operator.Equal, new SqlValue(value), null));

								itemCond.Conditions.Add(cond);
							}

							sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
						}

						if (sc.Conditions.Count == 0)
							return new SqlPredicate.Expr(new SqlValue(p.IsNot));

						if (p.IsNot)
							return new SqlPredicate.NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

						return new SqlPredicate.Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
					}
				}
			}

			return p;
		}

		#endregion

		#endregion

		#region DataTypes

		protected virtual int? GetMaxLength     (SqlDataType type) { return SqlDataType.GetMaxLength     (type.Type.DataType); }
		protected virtual int? GetMaxPrecision  (SqlDataType type) { return SqlDataType.GetMaxPrecision  (type.Type.DataType); }
		protected virtual int? GetMaxScale      (SqlDataType type) { return SqlDataType.GetMaxScale      (type.Type.DataType); }
		protected virtual int? GetMaxDisplaySize(SqlDataType type) { return SqlDataType.GetMaxDisplaySize(type.Type.DataType); }

		protected virtual ISqlExpression ConvertConvertion(SqlFunction func)
		{
			var from = (SqlDataType)func.Parameters[1];
			var to   = (SqlDataType)func.Parameters[0];

			if (to.Type.SystemType == typeof(object))
				return func.Parameters[2];

			if (to.Type.Length > 0)
			{
				var maxLength = to.Type.SystemType == typeof(string) ? GetMaxDisplaySize(from) : GetMaxLength(from);
				var newLength = maxLength != null && maxLength >= 0 ? Math.Min(to.Type.Length ?? 0, maxLength.Value) : to.Type.Length;

				if (to.Type.Length != newLength)
					to = new SqlDataType(to.Type.WithLength(newLength));
			}
			else if (from.Type.SystemType == typeof(short) && to.Type.SystemType == typeof(int))
				return func.Parameters[2];

			return ConvertExpression(new SqlFunction(func.SystemType, "Convert", to, func.Parameters[2]));
		}

		#endregion

		#region Alternative Builders

		protected ISqlExpression? AlternativeConvertToBoolean(SqlFunction func, int paramNumber)
		{
			var par = func.Parameters[paramNumber];

			if (par.SystemType!.IsFloatType() || par.SystemType!.IsIntegerType())
			{
				var sc = new SqlSearchCondition();

				sc.Conditions.Add(
					new SqlCondition(false, new SqlPredicate.ExprExpr(par, SqlPredicate.Operator.Equal, new SqlValue(0), Configuration.Linq.CompareNullsAsValues ? true : (bool?)null)));

				return ConvertExpression(new SqlFunction(func.SystemType, "CASE", sc, new SqlValue(false), new SqlValue(true)));
			}

			return null;
		}

		protected static bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.Date,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == dateName,
				_                              => false,
			};
		}

		protected static bool IsDateDataOffsetType(ISqlExpression expr)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType => ((SqlDataType)expr).Type.DataType == DataType.DateTimeOffset,
				_                            => false,
			};
		}

		protected static bool IsTimeDataType(ISqlExpression expr)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.Time,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == "Time",
				_                              => false,
			};
		}

		protected ISqlExpression FloorBeforeConvert(SqlFunction func)
		{
			var par1 = func.Parameters[1];

			return par1.SystemType!.IsFloatType() && func.SystemType.IsIntegerType() ?
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
							new SqlPredicate.ExprExpr(copyKeys[i], SqlPredicate.Operator.Equal, tableKeys[i], Configuration.Linq.CompareNullsAsValues ? true : (bool?)null)));
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

		SqlTableSource? GetMainTableSource(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count > 0 && selectQuery.From.Tables[0] is SqlTableSource tableSource)
				return tableSource;
			return null;
		}

		public SqlStatement GetAlternativeUpdateFrom(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.HasModifier)
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery);

			// removing joins
			statement.SelectQuery.TransformInnerJoinsToWhere();

			var tableSource = GetMainTableSource(statement.SelectQuery);
			if (tableSource == null)
				throw new LinqToDBException("Invalid query for Update.");

			if (statement.SelectQuery.Select.HasModifier)
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery);

			SqlTable? tableToUpdate  = statement.Update.Table;
			SqlTable? tableToCompare = null;

			switch (tableSource.Source)
			{
				case SqlTable table:
					{
						if (tableSource.Joins.Count == 0 && (tableToUpdate == null || QueryHelper.IsEqualTables(table, tableToUpdate)))
						{
							// remove table from FROM clause
							statement.SelectQuery.From.Tables.RemoveAt(0);
							if (tableToUpdate != null && tableToUpdate != table)
							{
								statement.Walk(new WalkOptions(), e =>
								{
									if (e is SqlField field && field.Table == tableToUpdate)
									{
										return table.Fields[field.Name];
									}

									return e;
								});
							}
							tableToUpdate = table;
						}
						else
						{
							if (tableToUpdate == null)
							{
								tableToUpdate = QueryHelper.EnumerateAccessibleSources(statement.SelectQuery)
									.OfType<SqlTable>()
									.FirstOrDefault();
							}

							if (tableToUpdate == null)
								throw new LinqToDBException("Can not decide which table to update");

							tableToCompare = QueryHelper.EnumerateAccessibleSources(statement.SelectQuery)
								.OfType<SqlTable>()
								.FirstOrDefault(t => QueryHelper.IsEqualTables(t, tableToUpdate));
						}

						break;
					}
				case SelectQuery query:
					{
						if (tableToUpdate == null)
						{
							tableToUpdate = QueryHelper.EnumerateAccessibleSources(query)
								.OfType<SqlTable>()
								.FirstOrDefault();

							if (tableToUpdate == null)
								throw new LinqToDBException("Can not decide which table to update");

							tableToUpdate = tableToUpdate.Clone();

							foreach (var item in statement.Update.Items)
							{
								var setField = QueryHelper.GetUnderlyingField(item.Column);
								if (setField == null)
									throw new LinqToDBException($"Unexpected element in setter expression: {item.Column}");

								item.Column = tableToUpdate.Fields[setField.Name];
							}

						}

						// return first matched table
						tableToCompare = QueryHelper.EnumerateAccessibleSources(query)
							.OfType<SqlTable>()
							.FirstOrDefault(t => QueryHelper.IsEqualTables(t, tableToUpdate));

						if (tableToCompare == null)
							throw new LinqToDBException("Query can't be translated to UPDATE Statement.");

						break;
					}
			}

			if (ReferenceEquals(tableToUpdate, tableToCompare))
			{
				// we have to create clone
				tableToUpdate = tableToCompare!.Clone();

				var ts = statement.SelectQuery.GetTableSource(tableToCompare!);

				for (var i = 0; i < statement.Update.Items.Count; i++)
				{
					var item = statement.Update.Items[i];
					var newItem = ConvertVisitor.Convert(item, (v, e) =>
					{
						if (e is SqlField field && field.Table == tableToCompare)
							return tableToUpdate.Fields[field.Name];

						return e;
					});

					var updateField = QueryHelper.GetUnderlyingField(newItem.Column);
					if (updateField != null)
						newItem.Column = tableToUpdate.Fields[updateField.Name];

					statement.Update.Items[i] = newItem;
				}
			}

			if (statement.SelectQuery.From.Tables.Count > 0 && tableToCompare != null)
			{

				var keys1 = tableToUpdate!.GetKeys(true);
				var keys2 = tableToCompare.GetKeys(true);

				if (keys1.Count == 0)
					throw new LinqToDBException($"Table {tableToUpdate.Name} do not have primary key. Update transformation is not available.");

				for (int i = 0; i < keys1.Count; i++)
				{
					var column = QueryHelper.NeedColumnForExpression(statement.SelectQuery, keys2[i], false);
					if (column == null)
						throw new LinqToDBException($"Can not create query column for expression '{keys2[i]}'.");

					var compare = QueryHelper.GenerateEquality(keys1[i], column);
					statement.SelectQuery.Where.SearchCondition.Conditions.Add(compare);
				}
			}

			if (tableToUpdate != null)
				tableToUpdate.Alias = "$F";

			statement.Update.Table = tableToUpdate;

			return statement;
		}

		public static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction func)
				return func.IsAggregate;

			if (expr is SqlExpression expression)
				return expression.IsAggregate;

			return false;
		}

		protected bool NeedsEnvelopingForUpdate(SelectQuery query)
		{
			if (query.Select.HasModifier || !query.GroupBy.IsEmpty)
				return true;

			if (!query.Where.IsEmpty)
			{
				if (new QueryVisitor().Find(query.Where, e => IsAggregationFunction(e)) != null)
					return true;
			}

			return false;
		}

		protected SqlUpdateStatement GetAlternativeUpdate(SqlUpdateStatement updateStatement)
		{
			var sourcesCount  = QueryHelper.EnumerateAccessibleSources(updateStatement.SelectQuery).Skip(1).Take(2).Count();

			// It covers subqueries also. Simple subquery will have sourcesCount == 2
			if (sourcesCount > 1)
			{
				if (NeedsEnvelopingForUpdate(updateStatement.SelectQuery))
					updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery);

				var sql = new SelectQuery { IsParameterDependent = updateStatement.IsParameterDependent  };

				var newUpdateStatement = new SqlUpdateStatement(sql);
				updateStatement.SelectQuery.ParentSelect = sql;

				SqlTable? tableToUpdate = updateStatement.Update.Table;
				if (tableToUpdate == null)
				{
					tableToUpdate = QueryHelper.EnumerateAccessibleSources(updateStatement.SelectQuery)
						.OfType<SqlTable>()
						.FirstOrDefault();
				}

				if (tableToUpdate == null)
					throw new LinqToDBException("Query can't be translated to UPDATE Statement.");

				// we have to ensure that clone do not contain tableToUpdate
				var objectTree = new Dictionary<ICloneableElement, ICloneableElement>();
				var clonedQuery = (SelectQuery)updateStatement.SelectQuery.Clone(
					objectTree,
					e => !(e is SqlParameter)
				);

				var tableToUpdateMapping = new Dictionary<ICloneableElement,ICloneableElement>(objectTree);
				// remove mapping from updatable table
				objectTree.Remove(tableToUpdate);
				foreach (var field in tableToUpdate.Fields.Values)
				{
					objectTree.Remove(field);
				} 

				var tableToCompare = QueryHelper.EnumerateAccessibleSources(clonedQuery)
					.Select(ts => ts as SqlTable)
					.FirstOrDefault(t => QueryHelper.IsEqualTables(t, tableToUpdate));

				if (tableToCompare == null)
					throw new LinqToDBException("Query can't be translated to UPDATE Statement.");

				var compareKeys = tableToCompare.GetKeys(true);
				var tableKeys   = tableToUpdate.GetKeys(true);

				clonedQuery.Where.EnsureConjunction();
				for (var i = 0; i < tableKeys.Count; i++)
				{
					var column = QueryHelper.NeedColumnForExpression(clonedQuery, compareKeys[i], false);
					if (column == null)
						throw new LinqToDBException($"Can not create query column for expression '{compareKeys[i]}'.");
					var compare = QueryHelper.GenerateEquality(tableKeys[i], column);
					clonedQuery.Where.SearchCondition.Conditions.Add(compare);
				}

				clonedQuery.Select.Columns.Clear();
				newUpdateStatement.SelectQuery.From.Table(tableToUpdate).Where.Exists(clonedQuery);

				foreach (var item in updateStatement.Update.Items)
				{
					var ex = ConvertVisitor.Convert(item.Expression!, (v, expr) =>
						expr is ICloneableElement cloneable && objectTree.TryGetValue(cloneable, out var newValue)
							? (ISqlExpression) newValue
							: expr);

					var usedSources = new HashSet<ISqlTableSource>();
					QueryHelper.GetUsedSources(ex, usedSources);
					usedSources.Remove(tableToUpdate);
					if (objectTree.TryGetValue(tableToUpdate, out var replaced))
						usedSources.Remove((ISqlTableSource)replaced);

					if (usedSources.Count > 0)
					{
						// it means that update value column depends on other tables and we have to generate more complicated query

						var innerQuery = (SelectQuery) clonedQuery.Clone(
							new Dictionary<ICloneableElement, ICloneableElement>(),
							e => !(e is SqlParameter) && !(e is SqlTable));

						innerQuery.ParentSelect = sql;

						innerQuery.Select.Columns.Clear();

						var remapped = ConvertVisitor.Convert(ex,
							(v, e) =>
							{
								if (!(e is ICloneableElement c))
									return e;

								if (tableToUpdateMapping.TryGetValue(c, out var n))
									e = (IQueryElement) n;

								if (e is SqlColumn clmn && clmn.Parent != innerQuery || e is SqlField)
								{
									var column = QueryHelper.NeedColumnForExpression(innerQuery, (ISqlExpression)e, false);
									if (column != null)
										return column;
								}

								return e;

							});

						innerQuery.Select.AddNew(remapped);
						ex = innerQuery;
					}

					item.Column = tableToUpdate.Fields[QueryHelper.GetUnderlyingField(item.Column)!.Name];
					item.Expression = ex;
					newUpdateStatement.Update.Items.Add(item);
				}

				newUpdateStatement.Parameters.AddRange(updateStatement.Parameters);
				newUpdateStatement.Update.Table = updateStatement.Update.Table != null ? tableToUpdate : null;
				newUpdateStatement.With         = updateStatement.With;

				updateStatement.Parameters.Clear();
				updateStatement.Update.Items.Clear();

				updateStatement = newUpdateStatement;

				var tableSource = GetMainTableSource(updateStatement.SelectQuery);
				tableSource!.Alias = "$F";
			}
			else
			{
				var tableSource = GetMainTableSource(updateStatement.SelectQuery);
				if (tableSource!.Source is SqlTable || updateStatement.Update.Table != null)
				{
					tableSource.Alias = "$F";
				}
			}

			return updateStatement;
		}


		/// <summary>
		/// Corrects situation when update table is located in JOIN clause. 
		/// Usually it is generated by associations.
		/// </summary>
		/// <param name="statement">Statement to examine.</param>
		/// <returns>Corrected statement.</returns>
		protected SqlUpdateStatement CorrectUpdateTable(SqlUpdateStatement statement)
		{
			var updateTable = statement.Update.Table;
			if (updateTable != null)
			{
				var firstTable = statement.SelectQuery.From.Tables[0];
				if (!(firstTable.Source is SqlTable ft) || !QueryHelper.IsEqualTables(ft, updateTable))
				{
					foreach (var joinedTable in firstTable.Joins)
					{
						if (joinedTable.Table.Source is SqlTable jt &&
							QueryHelper.IsEqualTables(jt, updateTable) && (joinedTable.JoinType == JoinType.Inner || joinedTable.JoinType == JoinType.Left))
						{
							joinedTable.JoinType = JoinType.Inner;
							joinedTable.Table.Source = firstTable.Source;
							firstTable.Source = jt;

							statement.Update.Table = jt;

							statement.Walk(new WalkOptions(), exp =>
							{
								if (exp is SqlField field && field.Table == updateTable)
								{
									return jt.Fields[field.Name];
								}
								return exp;
							});

							break;
						}
					}
				}
				else if (firstTable.Source is SqlTable newUpdateTable && newUpdateTable != updateTable && QueryHelper.IsEqualTables(newUpdateTable, updateTable))
				{
					statement.Update.Table = newUpdateTable;
					statement.Update = ConvertVisitor.Convert(statement.Update, (v, e) =>
					{
						if (e is SqlField field && field.Table == updateTable)
							return newUpdateTable.Fields[field.Name];

						return e;
					});
				}
			}

			return statement;
		}

		#endregion

		#region Helpers

		static string? SetAlias(string? alias, int maxLen)
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
			return new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive);
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
			return new SqlBinaryExpression(type, expr1, "-", expr2, Precedence.Subtraction);
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
			return new SqlBinaryExpression(type, expr1, "*", expr2, Precedence.Multiplicative);
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
			return new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative);
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
			((ISqlExpressionWalkable) statement).Walk(new WalkOptions(), element =>
			{
				if (element is SelectQuery query)
					new JoinOptimizer().OptimizeJoins(statement, query);
				return element;
			});
		}

		#endregion

		#region Optimizing Statement

		public IQueryElement OptimizeElements(IQueryElement root, IReadOnlyParameterValues? parameterValues)
		{
			var newElement = ConvertVisitor.ConvertAll(root, (visitor, e) =>
			{
				var ne = e;
				for (;;)
				{
					if (ne is ISqlExpression sqlExpression)
						ne = OptimizeExpression(sqlExpression, parameterValues);

					if (ne is ISqlPredicate sqlPredicate)
						ne = OptimizePredicate(sqlPredicate, parameterValues);

					if (ReferenceEquals(ne, e))
						break;
					e = ne;
				}				
				return e;
			});

			if (!ReferenceEquals(newElement, root))
				newElement = OptimizeElements(newElement, parameterValues);

			return newElement;
		}

		public virtual SqlStatement OptimizeStatement(SqlStatement statement, IReadOnlyParameterValues? parameterValues)
		{
			statement = (SqlStatement)OptimizeElements(statement, parameterValues);

			statement = OptimizeAggregates(statement);

			return statement;
		}

		public virtual bool IsParameterDependedQuery(SelectQuery query)
		{
			var takeValue = query.Select.TakeValue;
			if (takeValue != null)
			{
				var supportsParameter = SqlProviderFlags.GetAcceptsTakeAsParameterFlag(query);

				if (!supportsParameter)
				{
					if (takeValue.ElementType != QueryElementType.SqlValue && takeValue.CanBeEvaluated(true))
						return true;
				}
				else if (takeValue.ElementType != QueryElementType.SqlParameter)
					return true;

			}

			var skipValue = query.Select.SkipValue;
			if (skipValue != null)
			{
				var supportsParameter = SqlProviderFlags.AcceptsTakeAsParameter;

				if (!supportsParameter)
				{
					if (skipValue.ElementType != QueryElementType.SqlValue && skipValue.CanBeEvaluated(true))
						return true;
				}
				else if (skipValue.ElementType != QueryElementType.SqlParameter)
					return true;
			}

			return false;
		}

		public virtual bool IsParameterDependedElement(IQueryElement element)
		{
			switch (element.ElementType)
			{
				case QueryElementType.SelectStatement:
				case QueryElementType.InsertStatement:
				case QueryElementType.InsertOrUpdateStatement:
				case QueryElementType.UpdateStatement:
				case QueryElementType.DeleteStatement:
				case QueryElementType.CreateTableStatement:
				case QueryElementType.DropTableStatement:
				case QueryElementType.MergeStatement:
				{
					var statement = (SqlStatement)element;
					return statement.IsParameterDependent;
				}
				case QueryElementType.SqlParameter:
				{
					return !((SqlParameter)element).IsQueryParameter;
				}
				case QueryElementType.SqlQuery:
				{
					if (((SelectQuery)element).IsParameterDependent)
						return true;
					return IsParameterDependedQuery((SelectQuery)element);
				}
				case QueryElementType.SqlBinaryExpression:
				{
					return element.CanBeEvaluated(true) && !element.CanBeEvaluated(false);
				}
				case QueryElementType.ExprPredicate:
				{
					var exprExpr = (SqlPredicate.Expr)element;
					
					if (exprExpr.Expr1.CanBeEvaluated(true) && !exprExpr.Expr1.CanBeEvaluated(false))
						return true;
					return false;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var exprExpr = (SqlPredicate.ExprExpr)element;

					var isMutable1 = exprExpr.Expr1.CanBeEvaluated(true) && !exprExpr.Expr1.CanBeEvaluated(false);

					if (isMutable1 && exprExpr.Expr2.CanBeEvaluated(false))
						return true;

					var isMutable2 = exprExpr.Expr2.CanBeEvaluated(true) && !exprExpr.Expr2.CanBeEvaluated(false);

					if (isMutable2 && exprExpr.Expr1.CanBeEvaluated(false))
						return true;

					if ((isMutable1 || isMutable2) && exprExpr.WithNull != null 
					                               && (exprExpr.Expr1.ShouldCheckForNull() || exprExpr.Expr2.ShouldCheckForNull()))
						return true;

					return false;
				}
				case QueryElementType.IsTruePredicate:
				{
					var isTruePredicate = (SqlPredicate.IsTrue)element;

					if (isTruePredicate.Expr1.CanBeEvaluated(true) && !isTruePredicate.Expr1.CanBeEvaluated(false))
						return true;
					return false;
				}
				case QueryElementType.InListPredicate:
				{
					return true;
				}
				case QueryElementType.SearchStringPredicate:
				{
					var containsPredicate = (SqlPredicate.SearchString)element;
					if (containsPredicate.Expr2.ElementType != QueryElementType.SqlValue)
						return true;

					return false;
				}
				case QueryElementType.SqlFunction:
				{
					var sqlFunc = (SqlFunction)element;
					switch (sqlFunc.Name)
					{
						case "CASE":
						{
							for (int i = 0; i < sqlFunc.Parameters.Length - 2; i += 2)
							{
								var testParam = sqlFunc.Parameters[i];
								if (testParam.CanBeEvaluated(true))
									return true;
							}
							break;
						}
					}
					break;
				}
			}

			return false;
		}

		public bool IsParameterDependent(SqlStatement statement)
		{
			return null != new QueryVisitor().Find(statement, e => IsParameterDependedElement(e));
		}

		public IQueryElement ConvertElements(MappingSchema mappingSchema, IQueryElement element, IReadOnlyParameterValues? parameterValues)
		{
			var newElement = ConvertVisitor.ConvertAll(element, (visitor, e) =>
			{
				var ne = e;
				for (;;)
				{
					if (ne is ISqlExpression sqlExpression)
						ne = ConvertExpression(sqlExpression);

					if (ne is SqlPredicate sqlPredicate)
						ne = ConvertPredicate(mappingSchema, sqlPredicate, parameterValues);

					if (ReferenceEquals(ne, e))
						break;

					e = ne;
				}
				
				return e;
			});

			if (!ReferenceEquals(newElement, element))
				newElement = ConvertElements(mappingSchema, newElement, parameterValues);

			return newElement;
		}

		public virtual SqlStatement ConvertStatement(MappingSchema mappingSchema, SqlStatement statement, IReadOnlyParameterValues? parameterValues)
		{
			statement = (SqlStatement)ConvertElements(mappingSchema, statement, parameterValues);
			statement = FinalizeStatement(statement, parameterValues);
			return statement;
		}

		public virtual SqlStatement FinalizeStatement(SqlStatement statement, IReadOnlyParameterValues? parameterValues)
		{
			statement = TransformStatement(statement);
			statement = CorrectSkipTake(statement, parameterValues);

			if (SqlProviderFlags.IsParameterOrderDependent)
			{
				// ensure that parameters in expressions are well sorted
				statement = NormalizeExpressions(statement);
			}

			return statement;
		}

		static void BuildSqlValueTableParameters(SqlStatement statement)
		{
			if (statement.IsParameterDependent)
			{
				new QueryVisitor().Visit(statement, e =>
				{
					if (e is SqlValuesTable table)
						table.BuildRows();
				});
			}
		}

		public SqlStatement OptimizeAggregates(SqlStatement statement)
		{
			var newStatement = QueryHelper.JoinRemoval(statement, (currentStatement, join) =>
			{
				if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
				{
					if (join.Table.Source is SelectQuery query && query.Select.Columns.Count > 0)
					{
						var isAggregateQuery =
							query.Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression));
						if (isAggregateQuery)
						{
							// remove unwanted join
							if (!QueryHelper.IsDependsOn(statement, new HashSet<ISqlTableSource> { query },
								new HashSet<IQueryElement> { join }))
								return true;
						}
					}
				}

				return false;
			});

			return newStatement;
		}

		public virtual SqlStatement CorrectSkipTake(SqlStatement statement, IReadOnlyParameterValues? parameterValues)
		{
			if (parameterValues == null)
				return statement;

			statement = ConvertVisitor.ConvertAll(statement, (visitor, e) =>
			{
				// make skip take as parameters or evaluate otherwise
				if (visitor.ParentElement?.ElementType == QueryElementType.SelectClause && e is ISqlExpression expr)
				{
					var selectClause = (SqlSelectClause)visitor.ParentElement;
					if (selectClause.TakeValue != null && ReferenceEquals(expr, selectClause.TakeValue))
					{
						var take = expr;
						if (SqlProviderFlags.GetAcceptsTakeAsParameterFlag(selectClause.SelectQuery))
						{
							if (expr.ElementType.NotIn(QueryElementType.SqlParameter, QueryElementType.SqlValue))
							{
								var takeValue = take.EvaluateExpression(parameterValues)!;
								take = new SqlParameter(new DbDataType(takeValue.GetType()), "take", takeValue)
									{ IsQueryParameter = !QueryHelper.NeedParameterInlining(take) && Configuration.Linq.ParameterizeTakeSkip };
							}
						}
						else if (take.ElementType != QueryElementType.SqlValue)
							take = new SqlValue(take.EvaluateExpression(parameterValues)!);

						return take;
					}
					
					if (selectClause.SkipValue != null && ReferenceEquals(expr, selectClause.SkipValue))
					{ 
						var skip = expr;
						if (SqlProviderFlags.GetIsSkipSupportedFlag(selectClause.SelectQuery)
						    && SqlProviderFlags.AcceptsTakeAsParameter)
						{
							if (expr.ElementType.NotIn(QueryElementType.SqlParameter, QueryElementType.SqlValue))
							{
								var skipValue = skip.EvaluateExpression(parameterValues)!;
								skip = new SqlParameter(new DbDataType(skipValue.GetType()), "skip", skipValue)
									{ IsQueryParameter = !QueryHelper.NeedParameterInlining(skip) && Configuration.Linq.ParameterizeTakeSkip };
							}
						}
						else if (skip.ElementType != QueryElementType.SqlValue)
							skip = new SqlValue(skip.EvaluateExpression(parameterValues)!);

						return skip;
					}
				}

				return e;
			});

			return statement;
		}

		#endregion

		/// <summary>
		/// Moves Distinct query into another subquery. Useful when preserving ordering is required, because some providers do not support DISTINCT ORDER BY.
		/// <code>
		/// -- before
		/// SELECT DISTINCT TAKE 10 c1, c2
		/// FROM A
		/// ORDER BY c1
		/// -- after
		/// SELECT TAKE 10 B.c1, B.c2
		/// FROM
		///   (
		///     SELECT DISTINCT c1, c2
		///     FROM A
		///   ) B
		/// ORDER BY B.c1
		/// </code>
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip and Distinct modifiers.</param>
		/// <param name="queryFilter">Query filter predicate to determine if query needs processing.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement SeparateDistinctFromPagination(SqlStatement statement, Func<SelectQuery, bool> queryFilter)
		{
			return QueryHelper.WrapQuery(statement,
				q => q.Select.IsDistinct && queryFilter(q),
				(p, q) =>
				{
					p.Select.SkipValue = q.Select.SkipValue;
					p.Select.Take(q.Select.TakeValue, q.Select.TakeHints);

					q.Select.SkipValue = null;
					q.Select.Take(null, null);

					QueryHelper.MoveOrderByUp(p, q);
				});
		}

		/// <summary>
		/// Replaces pagination by Window function ROW_NUMBER().
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="supportsEmptyOrderBy">Indicates that database supports OVER () syntax.</param>
		/// <param name="onlySubqueries">Indicates when transformation needed only for subqueries.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNumber(SqlStatement statement, bool supportsEmptyOrderBy, bool onlySubqueries)
		{
			return ReplaceTakeSkipWithRowNumber(statement, query =>
			{
				if (onlySubqueries && query.ParentSelect == null)
					return false;
				return true;
			}, supportsEmptyOrderBy);
		}

		/// <summary>
		/// Replaces pagination by Window function ROW_NUMBER().
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="supportsEmptyOrderBy">Indicates that database supports OVER () syntax.</param>
		/// <param name="predicate">Indicates when the transformation is needed</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNumber(SqlStatement statement, Predicate<SelectQuery> predicate, bool supportsEmptyOrderBy)
		{
			return QueryHelper.WrapQuery(statement,
				query => 
				{
					if ((query.Select.TakeValue == null || query.Select.TakeHints != null) && query.Select.SkipValue == null)
						return 0;
					return predicate(query) ? 1 : 0;
				}
				, queries =>
				{
					var query = queries[queries.Count - 1];
					var processingQuery = queries[queries.Count - 2];

					SqlOrderByItem[]? orderByItems = null;
					if (!query.OrderBy.IsEmpty)
						orderByItems = query.OrderBy.Items.ToArray();
					//else if (query.Select.Columns.Count > 0)
					//{
					//	orderByItems = query.Select.Columns
					//		.Select(c => QueryHelper.NeedColumnForExpression(query, c, false))
					//		.Where(e => e != null)
					//		.Take(1)
					//		.Select(e => new SqlOrderByItem(e, false))
					//		.ToArray();
					//}

					if (orderByItems == null || orderByItems.Length == 0)
						orderByItems = supportsEmptyOrderBy ? Array<SqlOrderByItem>.Empty : new[] { new SqlOrderByItem(new SqlExpression("SELECT NULL"), false) };

					var orderBy = string.Join(", ",
						orderByItems.Select((oi, i) => oi.IsDescending ? $"{{{i}}} DESC" : $"{{{i}}}"));

					query.OrderBy.Items.Clear();

					var parameters = orderByItems.Select(oi => oi.Expression).ToArray();

					var rowNumberExpression = parameters.Length == 0
						? new SqlExpression(typeof(long), "ROW_NUMBER() OVER ()", Precedence.Primary, true, true)
						: new SqlExpression(typeof(long), $"ROW_NUMBER() OVER (ORDER BY {orderBy})", Precedence.Primary, true, true, parameters);

					var rowNumberColumn = query.Select.AddNewColumn(rowNumberExpression);
					rowNumberColumn.Alias = "RN";

					if (query.Select.SkipValue != null)
					{
						processingQuery.Where.EnsureConjunction().Expr(rowNumberColumn).Greater
							.Expr(query.Select.SkipValue);

						if (query.Select.TakeValue != null)
							processingQuery.Where.Expr(rowNumberColumn).LessOrEqual.Expr(
								new SqlBinaryExpression(query.Select.SkipValue.SystemType!,
									query.Select.SkipValue, "+", query.Select.TakeValue));
					}
					else
					{
						processingQuery.Where.EnsureConjunction().Expr(rowNumberColumn).LessOrEqual
							.Expr(query.Select.TakeValue!);
					}

					query.Select.SkipValue = null;
					query.Select.Take(null, null);

				});
		}

		/// <summary>
		/// Alternative mechanism how to prevent loosing sorting in Distinct queries.
		/// </summary>
		/// <param name="statement">Statement which may contain Distinct queries.</param>
		/// <param name="queryFilter">Query filter predicate to determine if query needs processing.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceDistinctOrderByWithRowNumber(SqlStatement statement, Func<SelectQuery, bool> queryFilter)
		{
			return QueryHelper.WrapQuery(statement,
				q => (q.Select.IsDistinct && !q.Select.OrderBy.IsEmpty && queryFilter(q)) /*|| q.Select.TakeValue != null || q.Select.SkipValue != null*/,
				(p, q) =>
				{
					var columnItems  = q.Select.Columns.Select(c => c.Expression).ToArray();
					var orderItems   = q.Select.OrderBy.Items.Select(o => o.Expression).ToArray();

					var projectionItems = columnItems.Union(orderItems).ToArray();
					if (projectionItems.Length < columnItems.Length)
					{
						// Sort columns not in projection, transforming to 
						/*
							 SELECT {S.columnItems}, S.RN FROM 
							 (
								  SELECT {columnItems + orderItems}, RN = ROW_NUMBER() OVER (PARTITION BY {columnItems} ORDER BY {orderItems}) FROM T
							 )
							 WHERE S.RN = 1
						*/

						var orderByItems = q.Select.OrderBy.Items;

						var partitionBy = string.Join(", ", columnItems.Select((oi, i) => $"{{{i}}}"));

						var orderBy = string.Join(", ",
							orderByItems.Select((oi, i) =>
								oi.IsDescending
									? $"{{{i + columnItems.Length}}} DESC"
									: $"{{{i + columnItems.Length}}}"));

						var parameters = columnItems.Concat(orderByItems.Select(oi => oi.Expression)).ToArray();

						var rnExpr = new SqlExpression(typeof(long),
							$"ROW_NUMBER() OVER (PARTITION BY {partitionBy} ORDER BY {orderBy})", Precedence.Primary,
							true, true, parameters);

						var additionalProjection = orderItems.Except(columnItems).ToArray();
						foreach (var expr in additionalProjection)
						{
							q.Select.AddNew(expr);
						}

						var rnColumn = q.Select.AddNewColumn(rnExpr);
						rnColumn.Alias = "RN";

						q.Select.IsDistinct = false;
						q.OrderBy.Items.Clear();
						p.Select.Where.EnsureConjunction().Expr(rnColumn).Equal.Value(1);
					}
					else
					{
						// All sorting columns in projection, transforming to 
						/*
							 SELECT {S.columnItems} FROM 
							 (
								  SELECT DISTINCT {columnItems} FROM T
							 )
							 ORDER BY {orderItems}

						*/

						QueryHelper.MoveOrderByUp(p, q);
					}
				});
		}

		#region Helper functions

		protected static bool IsBooleanParameter(ISqlExpression expr, int count, int i)
		{
			if ((i % 2 == 1 || i == count - 1) && expr.SystemType == typeof(bool) || expr.SystemType == typeof(bool?))
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SearchCondition: return true;
				}
			}

			return false;
		}

		protected SqlFunction ConvertFunctionParameters(SqlFunction func, bool withParameters = false)
		{
			if (func.Name == "CASE" &&
			    func.Parameters.Select((p, i) => new { p, i }).Any(p => IsBooleanParameter(p.p, func.Parameters.Length, p.i)))
			{
				return new SqlFunction(
					func.SystemType,
					func.Name,
					false,
					func.Precedence,
					func.Parameters.Select((p, i) =>
						IsBooleanParameter(p, func.Parameters.Length, i) ?
							new SqlFunction(typeof(bool), "CASE", p, new SqlValue(true), new SqlValue(false)) :
							p
					).ToArray());
			}

			return func;
		}
		

		#endregion
	}
}
