using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable InconsistentNaming

namespace LinqToDB.SqlProvider
{
	using Common;
	using Extensions;
	using Linq;
	using SqlQuery;
	using Tools;
	using Mapping;
	using DataProvider;
	using Common.Internal;

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

			var evaluationContext = new EvaluationContext(null);

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

//			statement = OptimizeStatement(statement, evaluationContext);

			statement.WalkQueries(
				selectQuery =>
				{
					if (!SqlProviderFlags.IsCountSubQuerySupported)  selectQuery = MoveCountSubQuery (selectQuery, evaluationContext);
					if (!SqlProviderFlags.IsSubQueryColumnSupported) selectQuery = MoveSubQueryColumn(selectQuery, evaluationContext);

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

			// provider specific query correction
			statement = FinalizeStatement(statement, evaluationContext);
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

		public SqlStatement ConvertStatement(MappingSchema mappingSchema, SqlStatement statement, OptimizationContext optimizationContext)
		{
			var newStatement = (SqlStatement)OptimizeElement(mappingSchema, statement, optimizationContext)!;
			newStatement = TransformStatementImmutable(newStatement, optimizationContext);
			return newStatement;
		}

		public SqlStatement FinalizeStatement(MappingSchema mappingSchema, SqlStatement statement, OptimizationContext optimizationContext)
		{
			var newStatement = statement;
			newStatement = TransformStatementImmutable(newStatement, optimizationContext);
			return newStatement;
		}

		/// <summary>
		/// Used for correcting statement and should return new statement if changes were made.
		/// </summary>
		/// <param name="statement"></param>
		/// <returns></returns>
		public virtual SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement;
		}

		public virtual SqlStatement TransformStatementImmutable(SqlStatement statement, OptimizationContext optimizationContext)
		{
			var newStatement = statement;
			return newStatement;
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

		static T NormalizeExpressions<T>(T expression, bool allowMutation) 
			where T : class, IQueryElement
		{
			var result = ConvertVisitor.ConvertAll(expression, allowMutation, (visitor, e) =>
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
									var normalized = NormalizeExpressions(paramExpr, allowMutation);

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

		SelectQuery MoveCountSubQuery(SelectQuery selectQuery, EvaluationContext context)
		{
			new QueryVisitor().Visit(selectQuery, e => MoveCountSubQuery(e, context));
			return selectQuery;
		}

		void MoveCountSubQuery(IQueryElement element, EvaluationContext context)
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
					subQuery.Where.SearchCondition = SelectQueryOptimizer.OptimizeSearchCondition(subQuery.Where.SearchCondition, context);

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
						var modified = false;
						var nc = ConvertVisitor.ConvertAll(cond, true, (v, e) =>
						{
							var ne = e;
							switch (e.ElementType)
							{
								case QueryElementType.SqlField:

									if (levelTables.Contains(((SqlField)e).Table!))
									{
										subQuery.GroupBy.Expr((SqlField)e);
										ne = subQuery.Select.AddColumn((SqlField)e);
									}

									break;

								case QueryElementType.Column:
									if (levelTables.Contains(((SqlColumn)e).Parent!))
									{
										subQuery.GroupBy.Expr((SqlColumn)e);
										ne = subQuery.Select.AddColumn((SqlColumn)e);
									}

									break;
							}

							modified = modified || !ReferenceEquals(e, ne);
							return e;
						});

						if (modified)
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

		SelectQuery MoveSubQueryColumn(SelectQuery selectQuery, EvaluationContext context)
		{
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

						subQuery.Where.SearchCondition = SelectQueryOptimizer.OptimizeSearchCondition(subQuery.Where.SearchCondition, context);

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

							var nc = ConvertVisitor.ConvertAll(cond, true, (v, e) =>
							{
								var ne = e;
								/*
								if (v.ParentElement?.ElementType == QueryElementType.Column)
								{
									var column = (SqlColumn)v.ParentElement;
									if (column.Parent != null && levelTables.Contains(column.Parent!))
									{
										if (isAggregated)
											subQuery.GroupBy.Expr((ISqlExpression)e);
										var newColumn = subQuery.Select.AddColumn(column);
										replaced[e] = newColumn;
										return newColumn;
									}
								}

								*/
								switch (e.ElementType)
								{
									case QueryElementType.SqlField:

										if (levelTables.Contains(((SqlField)e).Table!))
										{

											if (isAggregated)
												subQuery.GroupBy.Expr((SqlField)e);
											ne = subQuery.Select.AddColumn((SqlField)e);
										}

										break;

									case QueryElementType.Column:
										if (levelTables.Contains(((SqlColumn)e).Parent!))
										{

											if (isAggregated)
												subQuery.GroupBy.Expr((SqlColumn)e);
											ne = subQuery.Select.AddColumn((SqlColumn)e);
										}

										break;
								}

								modified = modified || !ReferenceEquals(e, ne);
								return ne;
							});

							if (modified)
							{
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

							col.Expression = newColumn.Expression;
						}
					}
				}
			});

			return selectQuery;
		}

		#region Optimization

		public static ISqlExpression CreateSqlValue(object value, OptimizationContext optimizationContext, SqlBinaryExpression be)
		{
			return CreateSqlValue(value, optimizationContext, be.Expr1, be.Expr2);
		}

		public static ISqlExpression CreateSqlValue(object value, OptimizationContext optimizationContext, params ISqlExpression[] basedOn)
		{
			SqlParameter? foundParam = null;

			foreach (var element in basedOn)
			{
				if (element.ElementType == QueryElementType.SqlParameter)
				{
					var param = (SqlParameter)element;
					if (param.IsQueryParameter)
					{
						foundParam = param;
					}
					else
						foundParam ??= param;
				}
			}

			if (foundParam != null)
			{
				var newParam = new SqlParameter(foundParam.Type.WithLength(null), foundParam.Name, value)
				{
					IsQueryParameter = foundParam.IsQueryParameter
				};

				return newParam;
			}

			return new SqlValue(value);
		}

		public virtual ISqlExpression OptimizeExpression(ISqlExpression expression, ConvertVisitor convertVisitor,
			OptimizationContext optimizationContext)
		{
			switch (expression.ElementType)
			{
				case QueryElementType.SqlBinaryExpression :
				{
					return OptimizeBinaryExpression((SqlBinaryExpression)expression, optimizationContext);
				}

				case QueryElementType.SqlFunction :
					#region SqlFunction
				{
					var func = (SqlFunction)expression;
					if (func.DoNotOptimize)
						break;

					switch (func.Name)
					{
						case "CASE"     :
						{
							var parms = func.Parameters;
							var len   = parms.Length;

							for (var i = 0; i < parms.Length - 1; i += 2)
							{
								var boolValue = QueryHelper.GetBoolValue(parms[i], optimizationContext.Context);
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

							if (parms.Length == 3 
								&& !parms[0].ShouldCheckForNull()
								&& parms[0].ElementType.In(QueryElementType.SqlFunction, QueryElementType.SearchCondition))
							{
								var boolValue1 = QueryHelper.GetBoolValue(parms[1], optimizationContext.Context);
								var boolValue2 = QueryHelper.GetBoolValue(parms[2], optimizationContext.Context);

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

						case "$Convert$":
						{
							var typef = func.SystemType.ToUnderlying();

							if (func.Parameters[2] is SqlFunction from && from.Name == "$Convert$" && from.Parameters[1].SystemType!.ToUnderlying() == typef)
								return from.Parameters[2];

							break;
						}

						case "Convert":
						{
							var typef = func.SystemType.ToUnderlying();

							if (func.Parameters[1] is SqlFunction from && from.Name == "Convert" && from.Parameters[1].SystemType!.ToUnderlying() == typef)
								return from.Parameters[1];

							if (func.Parameters[1] is SqlExpression fe && fe.Expr == "Cast({0} as {1})" && fe.Parameters[0].SystemType!.ToUnderlying() == typef)
								return fe.Parameters[0];

							break;
						}

					}

					break;
				}
				#endregion

				/*
				case QueryElementType.SearchCondition :
				{
					expression = SelectQueryOptimizer.OptimizeSearchCondition((SqlSearchCondition)expression, context);
					break;
				}
				*/

				case QueryElementType.SqlExpression   :
				{
					var se = (SqlExpression)expression;

					if (se.Expr == "{0}" && se.Parameters.Length == 1 && se.Parameters[0] != null && se.CanBeNull == se.Parameters[0].CanBeNull)
						return se.Parameters[0];

					break;
				}

				case QueryElementType.SqlValuesTable:
				{
					return ReduceSqlValueTable((SqlValuesTable)expression, optimizationContext.Context);
				}
			}

			return expression;
		}

		static SqlPredicate.Operator InvertOperator(SqlPredicate.Operator op, bool preserveEqual)
		{
			switch (op)
			{
				case SqlPredicate.Operator.Equal          : return preserveEqual ? op : SqlPredicate.Operator.NotEqual;
				case SqlPredicate.Operator.NotEqual       : return preserveEqual ? op : SqlPredicate.Operator.Equal;
				case SqlPredicate.Operator.Greater        : return SqlPredicate.Operator.LessOrEqual;
				case SqlPredicate.Operator.NotLess        :
				case SqlPredicate.Operator.GreaterOrEqual : return preserveEqual ? SqlPredicate.Operator.LessOrEqual : SqlPredicate.Operator.Less;
				case SqlPredicate.Operator.Less           : return SqlPredicate.Operator.GreaterOrEqual;
				case SqlPredicate.Operator.NotGreater     :
				case SqlPredicate.Operator.LessOrEqual    : return preserveEqual ? SqlPredicate.Operator.GreaterOrEqual : SqlPredicate.Operator.Greater;
				default: throw new InvalidOperationException();
			}
		}

		ISqlPredicate OptimizeCase(SqlPredicate.ExprExpr expr, EvaluationContext context)
		{
			SqlFunction? func;
			var valueFirst = expr.Expr1.TryEvaluateExpression(context, out var value);
			var isValue    = valueFirst;
			if (valueFirst)
				func = expr.Expr2 as SqlFunction;
			else
			{
				func = expr.Expr1 as SqlFunction;
				isValue = expr.Expr2.TryEvaluateExpression(context, out value);
			}	

			if (isValue && func != null && func.Name == "CASE")
			{
				if (value is int n && func.Parameters.Length == 5)
				{
					if (func.Parameters[0] is SqlSearchCondition c1 && c1.Conditions.Count == 1 &&
					    func.Parameters[1].TryEvaluateExpression(context, out var value1) && value1 is int i1 &&
					    func.Parameters[2] is SqlSearchCondition c2 && c2.Conditions.Count == 1 &&
					    func.Parameters[3].TryEvaluateExpression(context, out var value2) && value2 is int i2 &&
					    func.Parameters[4].TryEvaluateExpression(context, out var value3) && value3 is int i3)
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

									return 
										new SqlPredicate.ExprExpr(
											ee1.Expr1,
											e == 0 ? SqlPredicate.Operator.Equal :
											g == 0 ? SqlPredicate.Operator.Greater :
													 SqlPredicate.Operator.Less,
											ee1.Expr2, null);
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
									return new SqlPredicate.ExprExpr(
											ee1.Expr1,
											valueFirst ? InvertOperator(expr.Operator, true) : expr.Operator,
											ee1.Expr2, null);
								}
							}
						}
					}
				}
				else if (value is bool bv && func.Parameters.Length == 3)
				{
					if (func.Parameters[0] is SqlSearchCondition c1 && c1.Conditions.Count == 1 &&
					    func.Parameters[1].TryEvaluateExpression(context, out var v1) && v1 is bool bv1  &&
					    func.Parameters[2].TryEvaluateExpression(context, out var v2) && v2 is bool bv2)
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
								return (ISqlPredicate)ee.Invert();
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
					    func.Parameters[1].TryEvaluateExpression(context, out var v1) &&
					    func.Parameters[2].TryEvaluateExpression(context, out var v2))
					{
						if (Equals(value, v1))
							return sc;

						if (Equals(value, v2) && !sc.CanBeNull)
							return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);
					}
				}
			}


			if (!expr.Expr1.CanBeNull && !expr.Expr2.CanBeNull && expr.Expr1.SystemType.IsSignedType() && expr.Expr2.SystemType.IsSignedType())
			{
				var newExpr = expr switch
				{
					(SqlBinaryExpression binary, var op, var v, _) when v.CanBeEvaluated(context) =>
				
						// binary < v
						binary switch
						{
							// e + some < v ===> some < v - e
							(var e, "+", var some) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, v, "-", e), null),
							// e - some < v ===>  e - v < some
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, e, "-", v), op, some, null),

							// some + e < v ===> some < v - e
							(var some, "+", var e) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, v, "-", e), null),
							// some - e < v ===> some < v + e
							(var some, "-", var e) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, v, "+", e), null),

							_ => null
						},

					(var v, var op, SqlBinaryExpression binary, _) when v.CanBeEvaluated(context) =>
				
						// v < binary
						binary switch
						{
							// v < e + some ===> v - e < some
							(var e, "+", var some) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, v, "-", e), op, some, null),
							// v < e - some ===> some < e - v
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(some, op, new SqlBinaryExpression(v.SystemType!, e, "-", v), null),

							// v < some + e ===> v - e < some
							(var some, "+", var e) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, v, "-", e), op, some, null),
							// v < some - e ===> v + e < some
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlPredicate.ExprExpr(new SqlBinaryExpression(v.SystemType!, v, "+", e), op, some, null),

							_ => null
						},


					_ => null
				};

				expr = newExpr ?? expr;
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


		public virtual ISqlPredicate OptimizePredicate(ISqlPredicate predicate, EvaluationContext context)
		{
			// Avoiding infinite recursion
			//
			if (predicate.ElementType == QueryElementType.ExprPredicate)
			{
				var exprPredicate = (SqlPredicate.Expr)predicate;
				if (exprPredicate.Expr1.ElementType == QueryElementType.SqlValue)
					return predicate;
			}

			if (predicate.TryEvaluateExpression(context, out var value) && value != null)
			{
				return new SqlPredicate.Expr(new SqlValue(value));
			}

			switch (predicate.ElementType)
			{
				case QueryElementType.SearchCondition:
					return SelectQueryOptimizer.OptimizeSearchCondition((SqlSearchCondition)predicate, context);

				case QueryElementType.ExprExprPredicate:
				{
					var expr = (SqlPredicate.ExprExpr)predicate;

					if (expr.WithNull == null && expr.Operator.In(SqlPredicate.Operator.Equal, SqlPredicate.Operator.NotEqual))
					{
						if (expr.Expr2 is ISqlPredicate)
						{
							var boolValue1 = QueryHelper.GetBoolValue(expr.Expr1, context);
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
							var boolValue2 = QueryHelper.GetBoolValue(expr.Expr2, context);
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

					switch (expr.Operator)
					{
						case SqlPredicate.Operator.Equal          :
						case SqlPredicate.Operator.NotEqual       :
						case SqlPredicate.Operator.Greater        :
						case SqlPredicate.Operator.GreaterOrEqual :
						case SqlPredicate.Operator.Less           :
						case SqlPredicate.Operator.LessOrEqual    :
							predicate = OptimizeCase(expr, context);
							break;
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

		static SelectQuery? FindQuery(IReadOnlyList<IQueryElement> stack, int skip)
		{
			for (int i = stack.Count - 1; i >= 0; i--)
			{
				if (stack[i] is SelectQuery sc && skip-- == 0)
					return sc;
			}

			return null;
		}

		static Tuple<SelectQuery?, SqlColumn?> FindQueryWithColumn(ConvertVisitor visitor, int skip)
		{
			SqlColumn? column = null;
			SelectQuery? selectQuery = null;

			for (int i = visitor.Stack.Count - 1; i >= 0; i--)
			{
				var element = visitor.Stack[i];
				if (element.ElementType == QueryElementType.SqlQuery)
				{
					if (skip-- == 0)
					{
						selectQuery = (SelectQuery)element;
						break;
					}
				}
				else if (element.ElementType == QueryElementType.Column)
				{
					column = (SqlColumn)element;
				}
			}

			return Tuple.Create(selectQuery, column);
		}


		static bool CheckColumn(SelectQuery parentQuery, SqlColumn column, EvaluationContext context)
		{
			var expr = QueryHelper.UnwrapExpression(column.Expression);

			if (expr.ElementType.In(QueryElementType.SqlField, QueryElementType.Column, QueryElementType.SqlRawSqlTable))
				return false;

			if (expr.CanBeEvaluated(true))
				return false;

			if (new QueryVisitor().Find(expr, ex => QueryHelper.IsAggregationFunction(ex)) == null)
			{
				var elementsToIgnore = new HashSet<IQueryElement> { parentQuery };

				var depends = QueryHelper.IsDependsOn(parentQuery.GroupBy, column, elementsToIgnore);
				if (depends)
					return true;

				if (expr.IsComplexExpression())
				{
					depends =
						QueryHelper.IsDependsOn(parentQuery.Where, column, elementsToIgnore)
						|| QueryHelper.IsDependsOn(parentQuery.OrderBy, column, elementsToIgnore);

					if (depends)
						return true;
				}

				var dependsCount = QueryHelper.DependencyCount(parentQuery, column, elementsToIgnore);

				return dependsCount > 1;
			}

			return true;
		}

		public virtual IQueryElement OptimizeQueryElement(ConvertVisitor visitor, IQueryElement root,
			IQueryElement element, EvaluationContext context)
		{
			switch (element.ElementType)
			{
				case QueryElementType.Condition:
				{
					var condition = (SqlCondition)element;

					return SelectQueryOptimizer.OptimizeCondition(condition);
				}

				/*case QueryElementType.TableSource:
				{
					// Removing simple subquery

					var tableSource = (SqlTableSource)element;

					var q = FindQuery(visitor.Stack, 0);
					if (q == null)
						break;

					if (q.Select.From.Tables.Count != 1 || !ReferenceEquals(tableSource, q.Select.From.Tables[0]))
						break;

					if (tableSource.Joins.Count > 0)
						break;

					if (tableSource.Source is SelectQuery subQuery)
					{
						if (!subQuery.IsSimple)
							break;

						if (subQuery.From.Tables.Count != 1)
							break;

						var isColumnsOk = !subQuery.Select.Columns.Any(c => CheckColumn(q, c, context));

						if (!isColumnsOk)
							break;

						for (int index = 0; index < subQuery.Select.Columns.Count; index++)
						{
							var column = subQuery.Select.Columns[index];
							visitor.VisitedElements[column] = column.Expression;
						}

						return new SqlTableSource(subQuery.From.Tables[0].Source, tableSource.Alias, subQuery.From.Tables[0].Joins, subQuery.From.Tables[0].UniqueKeys);
					}

					break;
				}

				case QueryElementType.SelectClause:
				{
					var selectClause = (SqlSelectClause)element;

					if (selectClause.SelectQuery == null)
						break;

					if (selectClause.SelectQuery.HasSetOperators || selectClause.SelectQuery.Select.IsDistinct)
						break;

					var findResult = FindQueryWithColumn(visitor, 1);
					var parentQuery = findResult.Item1;
					if (parentQuery == null || parentQuery.HasSetOperators || findResult.Item2 != null)
						break;

					var filter = new HashSet<IQueryElement> {selectClause};
					List<SqlColumn>? columns = null;
					for (int i = 0; i < selectClause.Columns.Count; i++)
					{
						var column = selectClause.Columns[i];

						// Column is changed, waiting for another loop
						if (column.Parent == null)
							break;

						// removing column which has no usage
						//
						if (!CheckColumn(parentQuery, column, context) &&
						    !QueryHelper.IsDependsOn(root, column, filter) &&
						    (columns == null && selectClause.Columns.Count > 1 || columns != null && columns.Count > 0))
						{
							columns ??= selectClause.Columns.Take(i).ToList();
						}
						else
						{
							columns?.Add(column);
						}
					}

					if (columns != null)
					{
						var newClause = new SqlSelectClause(selectClause.IsDistinct, selectClause.TakeValue,
							selectClause.TakeHints, selectClause.SkipValue, columns);
						newClause.SetSqlQuery(selectClause.SelectQuery);
						return newClause;
					}

					break;
				}

				case QueryElementType.Column:
				{
					var column = (SqlColumn)element;

					if (column.Parent == null || column.Parent.HasSetOperators)
						break;

					if (column.Expression.ElementType == QueryElementType.Column)
					{
						var subColumn = (SqlColumn)column.Expression;
						// optimizing out columns which are constants or evaluable
						//
						if (subColumn.Parent != null && !subColumn.Parent.HasSetOperators && subColumn.Expression.CanBeEvaluated(true))
						{
							// throw new NotImplementedException();
							var ts = QueryHelper.EnumerateInnerJoined(column.Parent)
								.FirstOrDefault(ts => ts.Source == subColumn.Parent);
							if (ts != null)
							{
								return new SqlColumn(null, subColumn.Expression, column.RawAlias);
							}
						}
					}

					break;
				}

				case QueryElementType.GroupByClause:
				{
					var groupBy = (SqlGroupByClause)element;
					if (groupBy.Items.Count > 0)
					{
						List<ISqlExpression>? items = null;
						var processed = new HashSet<ISqlExpression>();
						for (int i = 0; i < groupBy.Items.Count; i++)
						{
							var item = groupBy.Items[i];

							// skipping evaluable grouping items
							if (!processed.Add(item) || item.CanBeEvaluated(true))
							{
								items ??= groupBy.Items.Take(i).ToList();
							}
							else
							{
								items?.Add(item);
							}
						}

						if (items != null)
						{
							return new SqlGroupByClause(groupBy.GroupingType, items);
						}
					}

					break;
				}*/

			}

			return element;
		}

		public virtual ISqlExpression OptimizeBinaryExpression(SqlBinaryExpression be, OptimizationContext optimizationContext)
		{
			switch (be.Operation)
			{
				case "+":
				{
					var v1 = be.Expr1.TryEvaluateExpression(optimizationContext.Context, out var value1);
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

					var v2 = be.Expr2.TryEvaluateExpression(optimizationContext.Context, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return be.Expr1;
							case int vi when
								be.Expr1    is SqlBinaryExpression be1 &&
								be1.Expr2.TryEvaluateExpression(optimizationContext.Context, out var be1v2) &&
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

										return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, optimizationContext, be), be.Precedence);
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

										return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, optimizationContext, be), be.Precedence);
									}
								}

								break;
							}

							case string vs when vs == "" : return be.Expr1;
							case string vs when
								be.Expr1    is SqlBinaryExpression be1 &&
								//be1.Operation == "+"                   &&
								be1.Expr2.TryEvaluateExpression(optimizationContext.Context, out var be1v2) &&
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
						if (value1 is int i1 && value2 is int i2) return CreateSqlValue(i1 + i2, optimizationContext, be);
						if (value1 is string || value2 is string) return CreateSqlValue(value1?.ToString() + value2, optimizationContext, be);
					}

					break;
				}

				case "-":
				{
					var v2 = be.Expr2.TryEvaluateExpression(optimizationContext.Context, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return be.Expr1;
							case int vi when
								be.Expr1 is SqlBinaryExpression be1 &&
								be1.Expr2.TryEvaluateExpression(optimizationContext.Context, out var be1v2) &&
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

										return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, optimizationContext, be), be.Precedence);
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

										return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, CreateSqlValue(value, optimizationContext, be), be.Precedence);
									}
								}

								break;
							}
						}
					}

					if (v2 && be.Expr1.TryEvaluateExpression(optimizationContext.Context, out var value1))
					{
						if (value1 is int i1 && value2 is int i2) return CreateSqlValue(i1 - i2, optimizationContext, be);
					}

					break;
				}

				case "*":
				{
					var v1 = be.Expr1.TryEvaluateExpression(optimizationContext.Context, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case int i when i == 0 : return CreateSqlValue(0, optimizationContext, be);
							case int i when i == 1 : return be.Expr2;
							case int i when
								be.Expr2    is SqlBinaryExpression be2 &&
								be2.Operation == "*"                   &&
								be2.Expr1.TryEvaluateExpression(optimizationContext.Context, out var be2v1)  &&
								be2v1 is int bi :
							{
								return new SqlBinaryExpression(be2.SystemType, CreateSqlValue(i * bi, optimizationContext, be), "*", be2.Expr2);
							}
						}
					}

					var v2 = be.Expr2.TryEvaluateExpression(optimizationContext.Context, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int i when i == 0 : return CreateSqlValue(0, optimizationContext, be);
							case int i when i == 1 : return be.Expr1;
						}
					}

					if (v1 && v2)
					{
						switch (value1)
						{
							case int    i1 when value2 is int    i2 : return CreateSqlValue(i1 * i2, optimizationContext, be);
							case int    i1 when value2 is double d2 : return CreateSqlValue(i1 * d2, optimizationContext, be);
							case double d1 when value2 is int    i2 : return CreateSqlValue(d1 * i2, optimizationContext, be);
							case double d1 when value2 is double d2 : return CreateSqlValue(d1 * d2, optimizationContext, be);
						}
					}

					break;
				}
			}

			if (be.Operation.In("+", "-") && be.Expr1.SystemType == be.Expr2.SystemType)
			{
				ISqlExpression? newExpr = be switch
				{
					// (binary + v)
					(SqlBinaryExpression binary, "+", var v) when v.CanBeEvaluated(optimizationContext.Context) =>
						binary switch
						{
							// (some + e) + v ===> some + (e + v)
							(var some, "+", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "+", v)),

							// (some - e) + v ===> some + (v - e)
							(var some, "-", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, v, "-", e)),

							// (e + some) + v ===> some + (e + v)
							(var e, "+", var some) when e.SystemType.IsNumericType() && e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "+", v)),

							// (e - some) + v ===> (e + v) - some
							(var e, "-", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, e, "+", v), "-", some),

							_ => null
						},

					// (binary - v)
					(SqlBinaryExpression binary, "-", var v) when v.CanBeEvaluated(optimizationContext.Context) =>
						binary switch
						{
							// (some + e) - v ===> some + (e - v)
							(var some, "+", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "-", v)),

							// (some - e) - v ===> some - (e + v)
							(var some, "-", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "+", v)),

							// (e + some) - v ===> some + (e - v)
							(var e, "+", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "-", v)),

							// (e - some) - v ===> (e - v) - some
							(var e, "-", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, e, "-", v), "-", some),

							_ => null
						},

					// (v + binary)
					(var v, "+", SqlBinaryExpression binary) when v.CanBeEvaluated(optimizationContext.Context) =>
						binary switch
						{
							// v + (some + e) ===> (v + e) + some
							(var some, "+", var e) when e.SystemType.IsNumericType() && e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "+", some),

							// v + (some - e) + v ===> (v - e) + some
							(var some, "-", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "+", some),

							// v + (e + some) ===> (v + e) + some
							(var e, "+", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "+", some),

							// v + (e - some) ===> (v + e) - some
							(var e, "-", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "-", some),

							_ => null
						},

					// (v - binary)
					(var v, "+", SqlBinaryExpression binary) when v.CanBeEvaluated(optimizationContext.Context) =>
						binary switch
						{
							// v - (some + e) ===> (v - e) - some
							(var some, "+", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "-", some),

							// v - (some - e) + v ===> (v + e) - some
							(var some, "-", var e) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "-", some),

							// v - (e + some) ===> (v - e) - some
							(var e, "+", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "-", some),

							// v - (e - some) ===> (v - e) + some
							(var e, "-", var some) when e.CanBeEvaluated(optimizationContext.Context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "+", some),

							_ => null
						},

					// (some - some) ==> 0
					(var some1, "-", var some2) when some1.Equals(some2) => new SqlValue(be.SystemType, 0),

					// (some - (s1 - s2)) ==> (some - s1) + s2
					(var some, "-", SqlBinaryExpression(var s1, "-", var s2)) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, some, "-", s1), "+", s2),

					_ => null
				};

				if (newExpr != null)
					return newExpr;
			}

			return be;
		}

		#endregion

		#region Conversion

		[return: NotNullIfNotNull("expr")]
		public virtual ISqlExpression? ConvertExpression(MappingSchema mappingSchema, ISqlExpression? expression, OptimizationContext optimizationContext)
		{
			return OptimizeElement(mappingSchema, expression, optimizationContext) as ISqlExpression;
		}

		public virtual ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor visitor, EvaluationContext context)
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
									ConvertExpressionImpl(new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), be.Expr2), visitor, context),
									be.Precedence);
							}

							if (be.Expr1.SystemType != typeof(string) && be.Expr2.SystemType == typeof(string))
							{
								var len = be.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr1.SystemType).Type.DataType);

								if (len == null || len <= 0)
									len = 100;

								return new SqlBinaryExpression(
									be.SystemType,
									ConvertExpressionImpl(new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), be.Expr1), visitor, context),
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
					return ConvertFunction((SqlFunction)expression);
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
			switch (func.Name)
			{
				case "ConvertToCaseCompareTo":
					return new SqlFunction(func.SystemType, "CASE",
							new SqlSearchCondition().Expr(func.Parameters[0]).Greater.Expr(func.Parameters[1]).ToExpr(), new SqlValue(1),
							new SqlSearchCondition().Expr(func.Parameters[0]).Equal.Expr(func.Parameters[1]).ToExpr(), new SqlValue(0),
							new SqlValue(-1))
						{ CanBeNull = false };

				case "Average": return new SqlFunction(func.SystemType, "Avg", func.Parameters);
				case "Max":
				case "Min":
				{
					if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
					{
						return new SqlFunction(typeof(int), func.Name,
							new SqlFunction(func.SystemType, "CASE", func.Parameters[0], new SqlValue(1), new SqlValue(0)) { CanBeNull = false });
					}

					break;
				}

				case "$Convert$":
					return ConvertConvertion(func);


				case "$ToLower$": return new SqlFunction(func.SystemType, "Lower", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
				case "$ToUpper$": return new SqlFunction(func.SystemType, "Upper", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
				case "$Replace$": return new SqlFunction(func.SystemType, "Replace", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);

			}

			return func;
		}

		static IQueryElement ApplyMutation(MappingSchema mappingSchema, IQueryElement element,
			OptimizationContext optimizationContext, BasicSqlOptimizer optimizer, bool register, Func<MappingSchema, OptimizationContext, BasicSqlOptimizer, ConvertVisitor, IQueryElement, IQueryElement, IQueryElement> func)
		{
			for (;;)
			{
				var newElement = ConvertVisitor.ConvertAll(element, (visitor, e) =>
				{
					if (optimizationContext.IsOptimized(e, out var expr))
						return expr!;

					var prev = e;
					var ne   = e;
					for (;;)
					{
						ne = func(mappingSchema, optimizationContext, optimizer, visitor, element, e);

						if (ReferenceEquals(ne, e))
							break;

						e = ne;
					}

					if (register)
						optimizationContext.RegisterOptimized(prev, e);

					return e;
				});

				if (ReferenceEquals(newElement, element))
					return element;

				element = newElement;
			}
		}

		public IQueryElement? OptimizeElement(MappingSchema mappingSchema, IQueryElement? element, OptimizationContext optimizationContext)
		{
			if (element == null)
				return null;

			if (optimizationContext.IsOptimized(element, out var newElement))
				return newElement!;

			newElement = ApplyMutation(mappingSchema, element, optimizationContext, this, false,
				static (ms, ctx, opt, visitor, root, e) =>
				{
					var ne = e;
					if (ne is ISqlExpression expr1)
						ne = opt.OptimizeExpression(expr1, visitor, ctx);

					if (ne is ISqlPredicate pred1)
						ne = opt.OptimizePredicate(pred1, ctx.Context);

					if (!ReferenceEquals(ne, e))
						return ne;

					ne = opt.OptimizeQueryElement(visitor, root, ne, ctx.Context);

					return ne;
				});

			newElement = ApplyMutation(mappingSchema, newElement, optimizationContext, this, true,
				static (ms, ctx, opt, visitor, root, e) =>
				{
					var ne = e;

					if (ne is ISqlExpression expr2)
						ne = opt.ConvertExpressionImpl(expr2, visitor, ctx.Context);

					if (!ReferenceEquals(ne, e))
						return ne;

					if (ne is ISqlPredicate pred3)
						ne = opt.ConvertPredicateImpl(ms, pred3, visitor, ctx);

					return ne;
				});

			return newElement;
		}


		public ISqlPredicate ConvertPredicate(MappingSchema mappingSchema, ISqlPredicate predicate, OptimizationContext optimizationContext)
		{
			return (ISqlPredicate)OptimizeElement(mappingSchema, predicate, optimizationContext)!;
		}

		public virtual ISqlPredicate ConvertPredicateImpl(MappingSchema mappingSchema, ISqlPredicate predicate, ConvertVisitor visitor, OptimizationContext optimizationContext)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					return ((SqlPredicate.ExprExpr)predicate).Reduce(optimizationContext.Context);
				case QueryElementType.IsTruePredicate:
					return ((SqlPredicate.IsTrue)predicate).Reduce();
				case QueryElementType.LikePredicate:
					return ConvertLikePredicate(mappingSchema, (SqlPredicate.Like)predicate, optimizationContext.Context);
				case QueryElementType.SearchStringPredicate:
					return ConvertSearchStringPredicate(mappingSchema, (SqlPredicate.SearchString)predicate, visitor, optimizationContext);
				case QueryElementType.InListPredicate:
				{
					var inList = (SqlPredicate.InList)predicate;
					return ConvertInListPredicate(mappingSchema, inList, optimizationContext.Context);
				}
			}
			return predicate;
		}


		public virtual string LikeEscapeCharacter => "~";
		public virtual string LikeWildcardCharacter => "%";

		public virtual bool LikeHasCharacterSetSupport => true;
		public virtual bool LikeParameterSupport => true;
		public virtual bool LikeIsEscapeSupported => true;

		protected static string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]"};
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
			var result = new SqlFunction(typeof(string), "$Replace$", false, true, expression, character,
				new SqlBinaryExpression(typeof(string), escapeCharacter, "+", character, Precedence.Additive));
			return result;
		}

		public static ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character)
		{
			var result = new SqlFunction(typeof(string), "$Replace$", false, true, expression, character,
				new SqlBinaryExpression(typeof(string), new SqlValue("["), "+",
					new SqlBinaryExpression(typeof(string), character, "+", new SqlValue("]"), Precedence.Additive),
					Precedence.Additive));
			return result;
		}

		protected virtual string EscapeLikeCharactersBrackets(string str, string[] toEscape)
		{
			var newStr = DataTools.EscapeUnterminatedBracket(str);
			if (newStr == str)
				newStr = newStr.Replace("[", "[[]");

			foreach (var s in toEscape)
			{
				if (s != "[" && s != "]")
					newStr = newStr.Replace(s, "[" + s + "]");
			}

			return newStr;
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
			EvaluationContext context)
		{
			return predicate;
		}

		protected ISqlPredicate ConvertSearchStringPredicateViaLike(MappingSchema mappingSchema,
			SqlPredicate.SearchString predicate,
			ConvertVisitor visitor, 
			OptimizationContext optimizationContext)
		{
			if (predicate.Expr2.TryEvaluateExpression(optimizationContext.Context, out var patternRaw))
			{
				var patternRawValue = patternRaw as string;

				if (patternRawValue == null)
				{
					return new SqlPredicate.IsTrue(new SqlValue(true), new SqlValue(true), new SqlValue(false), null, predicate.IsNot);
				}

				var patternValue = LikeIsEscapeSupported
					? EscapeLikeCharacters(patternRawValue, LikeEscapeCharacter)
					: EscapeLikeCharactersBrackets(patternRawValue, LikeCharactersToEscape);

				patternValue = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => patternValue + LikeWildcardCharacter,
					SqlPredicate.SearchString.SearchKind.EndsWith   => LikeWildcardCharacter + patternValue,
					SqlPredicate.SearchString.SearchKind.Contains   => LikeWildcardCharacter + patternValue + LikeWildcardCharacter,
					_ => throw new ArgumentOutOfRangeException()
				};

				var patternExpr = LikeParameterSupport ? CreateSqlValue(patternValue, optimizationContext, predicate.Expr2) : new SqlValue(patternValue);

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr,
					LikeIsEscapeSupported && (patternValue != patternRawValue) ? new SqlValue(LikeEscapeCharacter) : null);
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


				patternExpr = OptimizeExpression(patternExpr, visitor, optimizationContext);

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr,
					LikeIsEscapeSupported ? escape : null);
			}
		}

		public virtual ISqlPredicate ConvertSearchStringPredicate(MappingSchema mappingSchema, SqlPredicate.SearchString predicate,
			ConvertVisitor visitor, 
			OptimizationContext optimizationContext)
		{
			if (!predicate.IgnoreCase)
				throw new NotImplementedException("!predicate.IgnoreCase");

			return ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor, optimizationContext);
		}

		static SqlField ExpectsUnderlyingField(ISqlExpression expr)
		{
			var result = QueryHelper.GetUnderlyingField(expr);
			if (result == null)
				throw new InvalidOperationException($"Cannot retrieve underlying field for '{expr.ToDebugString()}'.");
			return result;
		}

		public virtual ISqlPredicate ConvertInListPredicate(MappingSchema mappingSchema, SqlPredicate.InList p, EvaluationContext context)
		{
			if (p.Values == null || p.Values.Count == 0)
				return new SqlPredicate.Expr(new SqlValue(p.IsNot));

			if (p.Values.Count == 1 && p.Values[0] is SqlParameter parameter)
			{
				var paramValue = parameter.GetParameterValue(context.ParameterValues);

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


		protected ISqlExpression ConvertCoalesceToBinaryFunc(SqlFunction func, string funcName)
		{
			var last = func.Parameters[func.Parameters.Length - 1];
			for (int i = func.Parameters.Length - 2; i >= 0; i--)
			{
				last = new SqlFunction(func.SystemType, funcName, func.Parameters[i], last);
			}
			return last;
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

			return new SqlFunction(func.SystemType, "Convert", to, func.Parameters[2]);
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
					new SqlCondition(false,
						new SqlPredicate.ExprExpr(par, SqlPredicate.Operator.NotEqual, new SqlValue(0),
							Configuration.Linq.CompareNullsAsValues ? false : (bool?)null)));

				return new SqlFunction(func.SystemType, "CASE", sc, new SqlValue(true), new SqlValue(false))
				{
					CanBeNull = false
				};
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
				newDeleteStatement.With = deleteStatement.With;

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
										return table[field.Name] ?? throw new LinqException($"Field {field.Name} not found in table {table}");

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

								item.Column = tableToUpdate[setField.Name] ?? throw new LinqException($"Field {setField.Name} not found in table {tableToUpdate}");
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
							return tableToUpdate[field.Name] ?? throw new LinqException($"Field {field.Name} not found in table {tableToUpdate}");

						return e;
					});

					var updateField = QueryHelper.GetUnderlyingField(newItem.Column);
					if (updateField != null)
						newItem.Column = tableToUpdate[updateField.Name] ?? throw new LinqException($"Field {updateField.Name} not found in table {tableToUpdate}");

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
				foreach (var field in tableToUpdate.Fields)
					objectTree.Remove(field);

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

					item.Column     = tableToUpdate[QueryHelper.GetUnderlyingField(item.Column)!.Name] ?? throw new LinqException($"Field {QueryHelper.GetUnderlyingField(item.Column)!.Name} not found in table {tableToUpdate}");
					item.Expression = ex;
					newUpdateStatement.Update.Items.Add(item);
				}

				newUpdateStatement.Update.Table = updateStatement.Update.Table != null ? tableToUpdate : null;
				newUpdateStatement.With         = updateStatement.With;

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
									return jt[field.Name] ?? throw new LinqException($"Field {field.Name} not found in table {jt}");
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
							return newUpdateTable[field.Name] ?? throw new LinqException($"Field {field.Name} not found in table {newUpdateTable}");

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

		/*
		public T OptimizeElements<T>(T root, EvaluationContext context, bool allowMutation)
			where T : class, IQueryElement
		{
			// return root;
			var mutated = false;
			var newElement = ConvertVisitor.ConvertAll(root, allowMutation, (visitor, e) =>
			{
				var ne = e;
				if (ne is ISqlExpression sqlExpression)
					ne = OptimizeExpression(sqlExpression, visitor, context);

				if (ne is ISqlPredicate sqlPredicate)
					ne = OptimizePredicate(sqlPredicate, context);

				ne = OptimizeQueryElement(visitor, root, ne, context);

				mutated = mutated || !ReferenceEquals(e, ne);

				return ne;
			});

			if (mutated)
				newElement = OptimizeElements(newElement, context, allowMutation);

			return newElement;
		}

		*/
		public SqlStatement OptimizeStatement(SqlStatement statement, EvaluationContext context)
		{
			//statement = OptimizeElements(statement, context, context.ParameterValues == null);

			// statement = OptimizeAggregates(statement);

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

				var supportsParameter = SqlProviderFlags.GetIsSkipSupportedFlag(query.Select.TakeValue, query.Select.SkipValue)
				                        && SqlProviderFlags.AcceptsTakeAsParameter;

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
				case QueryElementType.SqlValuesTable:
				{
					return !((SqlValuesTable)element).IsRowsBuilt;
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
					var isMutable2 = exprExpr.Expr2.CanBeEvaluated(true) && !exprExpr.Expr2.CanBeEvaluated(false);

					if (isMutable1 && isMutable2)
						return true;

					if (isMutable1 && exprExpr.Expr1.ShouldCheckForNull())
						return true;

					if (isMutable2 && exprExpr.Expr2.ShouldCheckForNull())
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
						case "Length":
						{
							if (sqlFunc.Parameters[0].CanBeEvaluated(true))
								return true;
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

		public IQueryElement ConvertFunctions(IQueryElement element, EvaluationContext context)
		{
			var alreadyStable = new HashSet<IQueryElement>();

			var newElement = ConvertVisitor.ConvertAll(element, (visitor, e) =>
			{
				if (alreadyStable.Contains(e))
					return e;

				var ne = e;
				for (;;)
				{
					if (ne.ElementType == QueryElementType.SqlFunction)
						ne = ConvertFunction((SqlFunction)ne);

					if (ReferenceEquals(ne, e))
						break;

					e = ne;
				}

				alreadyStable.Add(e);

				return e;
			});

			if (!ReferenceEquals(newElement, element))
				newElement = ConvertFunctions(newElement, context);

			return newElement;
		}

		public virtual SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context)
		{
			var newStatement = TransformStatement(statement);

			if (SqlProviderFlags.IsParameterOrderDependent)
			{
				// ensure that parameters in expressions are well sorted
				newStatement = NormalizeExpressions(newStatement, context.ParameterValues == null);
			}

			return newStatement;
		}

		static SqlValuesTable ReduceSqlValueTable(SqlValuesTable table, EvaluationContext context)
		{
			if (context == null)
				return table;
			return table.BuildRows(context);
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

		public virtual void ConvertSkipTake(MappingSchema mappingSchema, SelectQuery selectQuery, OptimizationContext optimizationContext, out ISqlExpression? takeExpr, out ISqlExpression? skipExpr)
		{
			// make skip take as parameters or evaluate otherwise

			takeExpr = ConvertExpression(mappingSchema, selectQuery.Select.TakeValue, optimizationContext);
			skipExpr = ConvertExpression(mappingSchema, selectQuery.Select.SkipValue, optimizationContext);

			if (takeExpr != null)
			{
				var supportsParameter = SqlProviderFlags.GetAcceptsTakeAsParameterFlag(selectQuery);

				if (supportsParameter)
				{
					if (takeExpr.ElementType.NotIn(QueryElementType.SqlParameter, QueryElementType.SqlValue))
					{
						var takeValue = takeExpr.EvaluateExpression(optimizationContext.Context)!;
						var takeParameter = new SqlParameter(new DbDataType(takeValue.GetType()), "take", takeValue)
						{
							IsQueryParameter = !QueryHelper.NeedParameterInlining(takeExpr) &&
							                   Configuration.Linq.ParameterizeTakeSkip
						};
						takeExpr = takeParameter;
					}
				}
				else if (takeExpr.ElementType != QueryElementType.SqlValue)
					takeExpr = new SqlValue(takeExpr.EvaluateExpression(optimizationContext.Context)!);
			}

			if (skipExpr != null)
			{
				var supportsParameter = SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery.Select.TakeValue, selectQuery.Select.SkipValue)
				                        && SqlProviderFlags.AcceptsTakeAsParameter;

				if (supportsParameter)
				{
					if (skipExpr.ElementType.NotIn(QueryElementType.SqlParameter, QueryElementType.SqlValue))
					{
						var skipValue = skipExpr.EvaluateExpression(optimizationContext.Context)!;
						var skipParameter = new SqlParameter(new DbDataType(skipValue.GetType()), "skip", skipValue)
						{
							IsQueryParameter = !QueryHelper.NeedParameterInlining(skipExpr) &&
							                   Configuration.Linq.ParameterizeTakeSkip
						};
						skipExpr = skipParameter;
					}
				}
				else if (skipExpr.ElementType != QueryElementType.SqlValue)
					skipExpr = new SqlValue(skipExpr.EvaluateExpression(optimizationContext.Context)!);

			}
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
							new SqlFunction(typeof(bool), "CASE", p, new SqlValue(true), new SqlValue(false))
							{
								CanBeNull = false, 
								DoNotOptimize = true
							} :
							p
					).ToArray());
			}

			return func;
		}
		

		#endregion
	}
}
