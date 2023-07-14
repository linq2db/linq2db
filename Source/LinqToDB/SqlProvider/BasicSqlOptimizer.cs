using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable InconsistentNaming

namespace LinqToDB.SqlProvider
{
	using Common;
	using Common.Internal;
	using Expressions;
	using Extensions;
	using Linq;
	using Mapping;
	using Tools;
	using SqlQuery;
	using SqlQuery.Visitors;

	public class BasicSqlOptimizer : ISqlOptimizer
	{
		internal static ObjectPool<SelectQueryOptimizerVisitor> SelectOptimizer =
			new(() => new SelectQueryOptimizerVisitor(), v => v.Cleanup(), 100);

		#region Init

		protected BasicSqlOptimizer(SqlProviderFlags sqlProviderFlags)
		{
			SqlProviderFlags = sqlProviderFlags;
		}

		public SqlProviderFlags SqlProviderFlags { get; }

		#endregion

		#region ISqlOptimizer Members

		public virtual SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			FixRootSelect (statement);
			FixEmptySelect(statement);
			FinalizeCte   (statement);

			var evaluationContext = new EvaluationContext(null);

			using var visitor = SelectOptimizer.Allocate();

#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var sqlText = statement.SqlText;
#endif

			statement = (SqlStatement)visitor.Value.OptimizeQueries(statement, SqlProviderFlags, dataOptions,
				evaluationContext, statement, 0);

#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var newSqlText = statement.SqlText;
#endif

//statement.EnsureFindTables();

			//statement.EnsureFindTables();

            if (dataOptions.LinqOptions.OptimizeJoins)
			{
				OptimizeJoins(statement);

				// Do it again after JOIN Optimization
				FinalizeCte(statement);
			}

			statement = FinalizeInsert(statement);
			statement = FinalizeSelect(statement);
			statement = CorrectUnionOrderBy(statement);
			statement = FixSetOperationNulls(statement);
			statement = OptimizeUpdateSubqueries(statement, dataOptions);

			// provider specific query correction
			statement = FinalizeStatement(statement, evaluationContext, dataOptions);

//statement.EnsureFindTables();

			return statement;
		}

		protected virtual SqlStatement FinalizeInsert(SqlStatement statement)
		{
			if (statement is SqlInsertStatement insertStatement)
			{
				var isSelfInsert =
					insertStatement.SelectQuery.From.Tables.Count     == 1 &&
					insertStatement.SelectQuery.From.Tables[0].Source == insertStatement.Insert.Into;

				if (isSelfInsert)
				{
					if (insertStatement.SelectQuery.IsSimple)
					{
						// simplify insert
						//
						insertStatement.Insert.Items.ForEach(item =>
						{
							if (item.Expression is SqlColumn column)
								item.Expression = column.Expression;
						});
						insertStatement.SelectQuery.From.Tables.Clear();
					}
				}
			}

			return statement;
		}

		internal static (SqlTableSource? tableSource, List<IQueryElement>? queryPath) FindTableSource(Stack<IQueryElement> currentPath, SqlTableSource source, SqlTable table)
		{
			if (source.Source == table)
				return (source, currentPath.ToList());

			if (source.Source is SelectQuery selectQuery)
			{
				var result = FindTableSource(currentPath, selectQuery, table);
				if (result.tableSource != null)
					return result;
			}

			foreach (var join in source.Joins)
			{
				currentPath.Push(join);
				var result = FindTableSource(currentPath, join.Table, table);
				currentPath.Pop();
				if (result.tableSource != null)
				{
					return result;
				}
			}

			return default;
		}

		internal static (SqlTableSource? tableSource, List<IQueryElement>? queryPath) FindTableSource(Stack<IQueryElement> currentPath, SelectQuery selectQuery, SqlTable table)
		{
			currentPath.Push(selectQuery);
			foreach (var source in selectQuery.From.Tables)
			{
				var result = FindTableSource(currentPath, source, table);
				if (result.tableSource != null)
					return result;
			}
			currentPath.Pop();

			return default;
		}

		static bool IsCompatibleForUpdate(SelectQuery selectQuery)
		{
			return !selectQuery.Select.IsDistinct && selectQuery.Select.GroupBy.IsEmpty;
		}

		static bool IsCompatibleForUpdate(SqlJoinedTable joinedTable)
		{
			return joinedTable.JoinType == JoinType.Inner || joinedTable.JoinType == JoinType.Left ||
			       joinedTable.JoinType == JoinType.Right;
		}

		public static bool IsCompatibleForUpdate(List<IQueryElement> path)
		{
			if (path.Count > 2)
				return false;

			var result = path.All(e =>
			{
				if (e is SelectQuery sc)
					return IsCompatibleForUpdate(sc);
				if (e is SqlJoinedTable jt)
					return IsCompatibleForUpdate(jt);
				return true;
			});

			return result;
		}

		public static bool IsCompatibleForUpdate(SelectQuery query, SqlTable updateTable, int level = 0)
		{
			if (!IsCompatibleForUpdate(query))
				return false;

			foreach (var ts in query.From.Tables)
			{
				if (ts.Source == updateTable)
					return true;

				foreach (var join in ts.Joins)
				{
					if (join.Table.Source == updateTable)
					{
						return IsCompatibleForUpdate(join);
					}
					
					if (IsCompatibleForUpdate(join) && join.Table.Source is SelectQuery sc)
					{
						if (IsCompatibleForUpdate(sc, updateTable))
							return true;
					}
				}
			}
			
			return false;
		}

		public static ISqlExpression? PopulateNesting(List<SelectQuery> queryPath, ISqlExpression expression, int ignoreCount)
		{
			var current = expression;
			for (var index = 0; index < queryPath.Count - ignoreCount; index++)
			{
				var selectQuery = queryPath[index];
				var idx         = selectQuery.Select.Columns.FindIndex(c => c.Expression == current);
				if (idx < 0)
				{
					if (selectQuery.Select.IsDistinct || !selectQuery.GroupBy.IsEmpty)
						return null;

					current = selectQuery.Select.AddNewColumn(current);
				}
				else
					current = selectQuery.Select.Columns[idx];
			}

			return current;
		}

		protected static void ApplyUpdateTableComparison(SelectQuery updateQuery, SqlUpdateClause updateClause, SqlTable inQueryTable, DataOptions dataOptions)
		{
			var compareKeys = inQueryTable.GetKeys(true);
			var tableKeys   = updateClause.Table!.GetKeys(true);

			var found = false;
			updateQuery.Where.EnsureConjunction();
			for (var i = 0; i < tableKeys.Count; i++)
			{
				var tableKey = tableKeys[i];

				var column = QueryHelper.NeedColumnForExpression(updateQuery, compareKeys[i], false);
				if (column == null)
					throw new LinqToDBException(
						$"Can not create query column for expression '{compareKeys[i]}'.");

				found = true;
				var compare       = QueryHelper.GenerateEquality(tableKey, column, dataOptions.LinqOptions.CompareNullsAsValues);
				updateQuery.Where.SearchCondition.Conditions.Add(compare);
			}

			if (!found)
				throw new LinqToDBException("Could not generate update statement.");
		}

		protected virtual SqlUpdateStatement BasicCorrectUpdate(SqlUpdateStatement statement, DataOptions dataOptions, bool wrapForOutput)
		{
			if (statement.Update.Table != null)
			{
				var (tableSource, queryPath) = FindTableSource(new Stack<IQueryElement>(), statement.SelectQuery, statement.Update.Table);

				if (tableSource != null && queryPath != null)
				{
					statement.Update.TableSource = tableSource;

					var forceWrapping = wrapForOutput && statement.Output != null &&
					                    (statement.SelectQuery.From.Tables.Count != 1 ||
					                     statement.SelectQuery.From.Tables.Count          == 1 &&
					                     statement.SelectQuery.From.Tables[0].Joins.Count == 0);
					
					if (forceWrapping || !IsCompatibleForUpdate(queryPath))
					{
						// we have to create new Update table and join via Keys

						var queries = queryPath.OfType<SelectQuery>().ToList();
						var keys    = statement.Update.Table.GetKeys(true).ToArray();

						if (keys.Length == 0)
						{
							keys = queries[0].Select.Columns
								.Where(c => c.Expression is SqlField field && field.Table == statement.Update.Table)
								.Select(c => (SqlField)c.Expression)
								.ToArray();
						}

						if (keys.Length == 0)
						{
							throw new LinqToDBException("Invalid update query.");
						}

						var keysColumns = new List<ISqlExpression>(keys.Length);
						foreach(var key in keys)
						{
							var newColumn = PopulateNesting(queries, key, 1);
							if (newColumn == null)
							{
								throw new LinqToDBException("Invalid update query. Could not create comparision key. It can be GROUP BY or DISTINCT query modifier.");
							}

							keysColumns.Add(newColumn);
						}

						var originalTableForUpdate = statement.Update.Table;
						var newTable = CloneTable(originalTableForUpdate, out var objectMap);

						var sc    = new SqlSearchCondition();

						for (var index = 0; index < keys.Length; index++)
						{
							var originalField = keys[index];

							if (!objectMap.TryGetValue(originalField, out var newField))
							{
								throw new InvalidOperationException();
							}

							var originalColumn = keysColumns[index];

							sc.Conditions.Add(QueryHelper.GenerateEquality((ISqlExpression)newField, originalColumn, dataOptions.LinqOptions.CompareNullsAsValues));
						}

						if (!SqlProviderFlags.IsUpdateFromSupported)
						{
							// build join
							//

							var tsIndex = statement.SelectQuery.From.Tables.FindIndex(ts =>
								queries.Contains(ts.Source));

							if (tsIndex < 0)
								throw new InvalidOperationException();

							var ts   = statement.SelectQuery.From.Tables[tsIndex];
							var join = new SqlJoinedTable(JoinType.Inner, ts, false, sc);

							statement.SelectQuery.From.Tables.RemoveAt(tsIndex);
							statement.SelectQuery.From.Tables.Insert(0, new SqlTableSource(newTable, "t", join));
						}
						else
						{
							statement.SelectQuery.Where.EnsureConjunction().ConcatSearchCondition(sc);
						}

						for (var index = 0; index < statement.Update.Items.Count; index++)
						{
							var item = statement.Update.Items[index];
							if (item.Column is SqlColumn column)
								item.Column = QueryHelper.GetUnderlyingField(column.Expression) ?? column.Expression;

							item = item.ConvertAll(this, (v, e) =>
							{
								if (objectMap.TryGetValue(e, out var newValue))
								{
									return newValue;
								}

								return e;
							});

							statement.Update.Items[index] = item;
						}

						statement.Update.Table       = newTable;
						statement.Update.TableSource = null;
					}
					else
					{
						if (queryPath.Count > 0)
						{
							var ts = statement.SelectQuery.From.Tables.FirstOrDefault();
							if (ts != null)
							{
								if (ts.Source is SelectQuery)
									statement.Update.TableSource = ts;
							}
						}
					}

					CorrectUpdateSetters(statement);
				}
			}

			return statement;
		}

		protected virtual SqlStatement FinalizeUpdate(SqlStatement statement, DataOptions dataOptions)
		{
			if (statement is SqlUpdateStatement updateStatement)
			{
				// get from columns expression
				//
				updateStatement.Update.Items.ForEach(item =>
				{
					item.Expression = QueryHelper.SimplifyColumnExpression(item.Expression);
				});

			}

			return statement;
		}

		protected virtual SqlStatement FinalizeSelect(SqlStatement statement)
		{
			if (statement is SqlSelectStatement selectStatement)
			{
				// When selecting a SqlRow, expand the row into individual columns.

				var selectQuery = selectStatement.SelectQuery;
				var columns     = selectQuery.Select.Columns;
	
				for (var i = 0; i < columns.Count; i++)
				{
					var c = columns[i];
					if (c.Expression.ElementType == QueryElementType.SqlRow)
					{
						if (columns.Count > 1)
							throw new LinqToDBException("SqlRow expression must be the only result in a SELECT");
	
						var row = (SqlRow)columns[0].Expression;
						columns.Clear();
						foreach (var value in row.Values)
							selectQuery.Select.AddNew(value);
	
						break;
					}
				}
			}

			return statement;
		}

		protected virtual SqlStatement CorrectUnionOrderBy(SqlStatement statement)
		{
			var queriesToWrap = new HashSet<SelectQuery>();

			statement.Visit(queriesToWrap, (wrap, e) =>
			{
				if (e is SelectQuery sc && sc.HasSetOperators)
				{
					var prevQuery = sc;

					for (int i = 0; i < sc.SetOperators.Count; i++)
					{
						var currentOperator = sc.SetOperators[i];
						var currentQuery    = currentOperator.SelectQuery;

						if (currentOperator.Operation == SetOperation.Union)
						{
							if (!prevQuery.Select.HasModifier && !prevQuery.OrderBy.IsEmpty)
							{
								prevQuery.OrderBy.Items.Clear();
							}

							if (!currentQuery.Select.HasModifier && !currentQuery.OrderBy.IsEmpty)
							{
								currentQuery.OrderBy.Items.Clear();
							}
						}
						else
						{
							if (!prevQuery.OrderBy.IsEmpty)
							{
								wrap.Add(prevQuery);
							}

							if (!currentQuery.OrderBy.IsEmpty)
							{
								wrap.Add(currentQuery);
							}
						}

						prevQuery = currentOperator.SelectQuery;
					}
				}
			});

			if (queriesToWrap.Count == 0)
				return statement;

			return QueryHelper.WrapQuery(
				queriesToWrap,
				statement,
				static (wrap, q, parentElement) => wrap.Contains(q),
				null,
				allowMutation: true,
				withStack: true);
		}

		static void CorrelateNullValueTypes(ref ISqlExpression toCorrect, ISqlExpression reference)
		{
			if (toCorrect.ElementType == QueryElementType.Column)
			{
				var column     = (SqlColumn)toCorrect;
				var columnExpr = column.Expression;
				CorrelateNullValueTypes(ref columnExpr, reference);
				column.Expression = columnExpr;
			}
			else if (toCorrect.ElementType == QueryElementType.SqlValue)
			{
				var value = (SqlValue)toCorrect;
				if (value.Value == null)
				{
					var suggested = QueryHelper.SuggestDbDataType(reference);
					if (suggested != null)
					{
						toCorrect = new SqlValue(suggested.Value, null);
					}
				}
			}
		}

		protected virtual SqlStatement FixSetOperationNulls(SqlStatement statement)
		{
			statement.VisitParentFirst(static e =>
			{
				if (e.ElementType == QueryElementType.SqlQuery)
				{
					var query = (SelectQuery)e;
					if (query.HasSetOperators)
					{
						for (var i = 0; i < query.Select.Columns.Count; i++)
						{
							var column     = query.Select.Columns[i];
							var columnExpr = column.Expression;

							foreach (var setOperator in query.SetOperators)
							{
								var otherColumn = setOperator.SelectQuery.Select.Columns[i];
								var otherExpr   = otherColumn.Expression;

								CorrelateNullValueTypes(ref columnExpr, otherExpr);
								CorrelateNullValueTypes(ref otherExpr, columnExpr);

								otherColumn.Expression = otherExpr;
							}

							column.Expression = columnExpr;
						}
					}
				}

				return true;
			});

			return statement;
		}

		bool FixRootSelect(SqlStatement statement)
		{
			if (statement.SelectQuery is {} query         &&
				query.Select.HasModifier == false         &&
				query.DoNotRemove        == false         &&
				query.QueryName is null                   &&
				query.Where.  IsEmpty                     &&
				query.GroupBy.IsEmpty                     &&
				query.Having. IsEmpty                     &&
				query.OrderBy.IsEmpty                     &&
				query.From.Tables is { Count : 1 } tables &&
				tables[0].Source  is SelectQuery   child  &&
				tables[0].Joins.Count      == 0           &&
				child.DoNotRemove          == false       &&
				query.Select.Columns.Count == child.Select.Columns.Count)
			{
				for (var i = 0; i < query.Select.Columns.Count; i++)
				{
					var pc = query.Select.Columns[i];
					var cc = child.Select.Columns[i];

					if (pc.Expression != cc)
						return false;
				}

				if (statement is SqlSelectStatement)
				{
					if (statement.SelectQuery.SqlQueryExtensions != null)
						(child.SqlQueryExtensions ??= new()).AddRange(statement.SelectQuery.SqlQueryExtensions);
					statement.SelectQuery = child;
				}
				else
				{
					var dic = new Dictionary<ISqlExpression,ISqlExpression>(query.Select.Columns.Count + 1)
					{
						{ statement.SelectQuery, child }
					};

					foreach (var pc in query.Select.Columns)
						dic.Add(pc, pc.Expression);

					statement.Walk(WalkOptions.Default, dic, static (d, ex) => d.TryGetValue(ex, out var e) ? e : ex);
				}

				return true;
			}

			return false;
		}

		//TODO: move tis to standard optimizer
		protected virtual SqlStatement OptimizeUpdateSubqueries(SqlStatement statement, DataOptions dataOptions)
		{
			/*
			if (statement is SqlUpdateStatement updateStatement)
			{
				var evaluationContext = new EvaluationContext();
				foreach (var setItem in updateStatement.Update.Items) 
				{
					if (setItem.Expression is SelectQuery q)
					{
						var optimizer = new SelectQueryOptimizer(SqlProviderFlags, q, q, 0);
						optimizer.FinalizeAndValidate(SqlProviderFlags.IsApplyJoinSupported);
					}
				}
			}
			*/

			return statement;
		}

		protected virtual void FixEmptySelect(SqlStatement statement)
		{
			// avoid SELECT * top level queries, as they could create a lot of unwanted traffic
			// and such queries are not supported by remote context
			if (statement.QueryType == QueryType.Select && statement.SelectQuery!.Select.Columns.Count == 0)
				statement.SelectQuery!.Select.Add(new SqlValue(1));
		}

		/// <summary>
		/// Used for correcting statement and should return new statement if changes were made.
		/// </summary>
		/// <param name="statement"></param>
		/// <param name="dataOptions"></param>
		/// <returns></returns>
		public virtual SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			return statement;
		}

		static void RegisterDependency(CteClause cteClause, Dictionary<CteClause, HashSet<CteClause>> foundCte)
		{
			if (foundCte.ContainsKey(cteClause))
				return;

			var dependsOn = new HashSet<CteClause>();
			cteClause.Body!.Visit(dependsOn, static (dependsOn, ce) =>
			{
				if (ce.ElementType == QueryElementType.SqlCteTable)
				{
					var subCte = ((SqlCteTable)ce).Cte!;
					dependsOn.Add(subCte);
				}

			});

			foundCte.Add(cteClause, dependsOn);

			foreach (var clause in dependsOn)
			{
				RegisterDependency(clause, foundCte);
			}
		}

		void FinalizeCte(SqlStatement statement)
		{
			if (statement is SqlStatementWithQueryBase select)
			{
				// one-field class is cheaper than dictionary instance
				var cteHolder = new WritableContext<Dictionary<CteClause, HashSet<CteClause>>?>();

				if (select is SqlMergeStatement merge)
				{
					merge.Target.Visit(cteHolder, static (foundCte, e) =>
						{
							if (e.ElementType == QueryElementType.SqlCteTable)
							{
								var cte = ((SqlCteTable)e).Cte!;
								RegisterDependency(cte, foundCte.WriteableValue ??= new());
							}
						}
					);
					merge.Source.Visit(cteHolder, static (foundCte, e) =>
						{
							if (e.ElementType == QueryElementType.SqlCteTable)
							{
								var cte = ((SqlCteTable)e).Cte!;
								RegisterDependency(cte, foundCte.WriteableValue ??= new());
							}
						}
					);
				}
				else
				{
					select.SelectQuery.Visit(cteHolder, static (foundCte, e) =>
						{
							if (e.ElementType == QueryElementType.SqlCteTable)
							{
								var cte = ((SqlCteTable)e).Cte!;
								CorrectEmptyCte(cte);
								RegisterDependency(cte, foundCte.WriteableValue ??= new());
							}
						}
					);
				}

				if (cteHolder.WriteableValue == null || cteHolder.WriteableValue.Count == 0)
					select.With = null;
				else
				{
					// TODO: Ideally if there is no recursive CTEs we can convert them to SubQueries
					if (!SqlProviderFlags.IsCommonTableExpressionsSupported)
						throw new LinqToDBException("DataProvider do not supports Common Table Expressions.");

					// basic detection of non-recursive CTEs
					// for more complex cases we will need dependency cycles detection
					foreach (var kvp in cteHolder.WriteableValue)
					{
						if (kvp.Value.Count == 0)
							kvp.Key.IsRecursive = false;

						// remove self-reference for topo-sort
						kvp.Value.Remove(kvp.Key);
					}

					var ordered = TopoSorting.TopoSort(cteHolder.WriteableValue.Keys, cteHolder, static (cteHolder, i) => cteHolder.WriteableValue![i]).ToList();

					Utils.MakeUniqueNames(ordered, null, static (n, a) => !ReservedWords.IsReserved(n), static c => c.Name, static (c, n, a) => c.Name = n,
						static c => string.IsNullOrEmpty(c.Name) ? "CTE_1" : c.Name, StringComparer.OrdinalIgnoreCase);

					select.With = new SqlWithClause();
					select.With.Clauses.AddRange(ordered);
				}
			}
		}

		static void CorrectEmptyCte(CteClause cte)
		{
			/*if (cte.Fields.Count == 0)
			{
				cte.Fields.Add(new SqlField(new DbDataType(typeof(int)), "any", false));
				cte.Body!.Select.AddNew(new SqlValue(1), "any");
			}*/
		}

		protected static bool HasParameters(ISqlExpression expr)
		{
			var hasParameters  = null != expr.Find(QueryElementType.SqlParameter);

			return hasParameters;
		}

		static T NormalizeExpressions<T>(T expression, bool allowMutation)
			where T : class, IQueryElement
		{
			var result = expression.ConvertAll(allowMutation: allowMutation, static (visitor, e) =>
			{
				if (e.ElementType == QueryElementType.SqlExpression)
				{
					var expr = (SqlExpression)e;
					var newExpression = expr;

					// we interested in modifying only expressions which have parameters
					if (HasParameters(expr))
					{
						if (string.IsNullOrEmpty(expr.Expr) || expr.Parameters.Length == 0)
							return expr;

						var newExpressions = new List<ISqlExpression>();

						var ctx = WritableContext.Create(false, (newExpressions, visitor, expr));

						var newExpr = QueryHelper.TransformExpressionIndexes(
							ctx,
							expr.Expr,
							static (context, idx) =>
							{
								if (idx >= 0 && idx < context.StaticValue.expr.Parameters.Length)
								{
									var paramExpr  = context.StaticValue.expr.Parameters[idx];
									var normalized = NormalizeExpressions(paramExpr, context.StaticValue.visitor.AllowMutation);

									if (!context.WriteableValue && !ReferenceEquals(normalized, paramExpr))
										context.WriteableValue = true;

									var newIndex   = context.StaticValue.newExpressions.Count;

									context.StaticValue.newExpressions.Add(normalized);
									return newIndex;
								}
								return idx;
							});

						var changed = ctx.WriteableValue || newExpr != expr.Expr;

						if (changed)
							newExpression = new SqlExpression(expr.SystemType, newExpr, expr.Precedence, expr.Flags, expr.NullabilityType, null, newExpressions.ToArray());

						return newExpression;
					}
				}
				return e;
			});

			return result;
		}

		#region Optimization

		public static ISqlExpression CreateSqlValue(object? value, SqlBinaryExpression be)
		{
			return CreateSqlValue(value, be.GetExpressionType(), be.Expr1, be.Expr2);
		}

		public static ISqlExpression CreateSqlValue(object? value, DbDataType dbDataType, params ISqlExpression[] basedOn)
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
				var newParam = new SqlParameter(dbDataType, foundParam.Name, value)
				{
					IsQueryParameter = foundParam.IsQueryParameter
				};

				return newParam;
			}

			return new SqlValue(dbDataType, value);
		}

		public virtual ISqlExpression OptimizeExpression(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> convertVisitor)
		{
			switch (expression.ElementType)
			{
				case QueryElementType.SqlBinaryExpression :
				{
					return OptimizeBinaryExpression((SqlBinaryExpression)expression, convertVisitor.Context.OptimizationContext.Context);
				}

				case QueryElementType.SqlFunction :
				{
					var func = (SqlFunction)expression;
					if (func.DoNotOptimize)
						break;

					return OptimizeFunction(convertVisitor.Context.Nullability, func, convertVisitor.Context.OptimizationContext.Context);
				}

				case QueryElementType.SqlExpression   :
				{
					var se = (SqlExpression)expression;

					if (se.Expr == "{0}" && se.Parameters.Length == 1 && se.Parameters[0] != null && se.CanBeNull == se.Parameters[0].CanBeNullable(convertVisitor.Context.Nullability))
						return se.Parameters[0];

					break;
				}
			}

			return expression;
		}

		public virtual ISqlExpression OptimizeFunction(NullabilityContext nullability, SqlFunction func, EvaluationContext context)
		{
			if (func.TryEvaluateExpression(context, out var value))
			{
				return CreateSqlValue(value, func.GetExpressionType(), func.Parameters);
			}

			switch (func.Name)
			{
				case PseudoFunctions.COALESCE:
				{
					var parms = func.Parameters;
					if (parms.Length == 2)
					{
						if (parms[0] is SqlValue val1 && parms[1] is not SqlValue)
							return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, CreateSqlValue(val1.Value, parms[1].GetExpressionType(), parms[0]), parms[1])
							{
								DoNotOptimize = true,
								CanBeNull     = func.CanBeNull
							};
						else if (parms[1] is SqlValue val2 && parms[0] is not SqlValue)
							return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, parms[0], CreateSqlValue(val2.Value, parms[0].GetExpressionType(), parms[1]))
							{
								DoNotOptimize = true,
								CanBeNull     = func.CanBeNull
							};
					}

					break;
				}
				case "CASE":
				{
					var parms = func.Parameters;
					var len   = parms.Length;

					for (var i = 0; i < parms.Length - 1; i += 2)
					{
						var boolValue = QueryHelper.GetBoolValue(parms[i], context);
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

					if (!func.DoNotOptimize && parms.Length == 3
						&& !parms[0].ShouldCheckForNull(nullability)
						&& (parms[0].ElementType == QueryElementType.SqlFunction || parms[0].ElementType == QueryElementType.SearchCondition))
					{
						var boolValue1 = QueryHelper.GetBoolValue(parms[1], context);
						var boolValue2 = QueryHelper.GetBoolValue(parms[2], context);

						if (boolValue1 != null && boolValue2 != null)
						{
							if (boolValue1 == boolValue2)
								return new SqlValue(true);

							if (!boolValue1.Value)
								return new SqlSearchCondition(new SqlCondition(true, new SqlPredicate.Expr(parms[0], parms[0].Precedence)));

							return parms[0];
						}

						// TODO: is it correct?
						// type constant by value from other branch
						if (parms[1] is SqlValue val1 && parms[2] is not SqlValue)
							return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, parms[0], CreateSqlValue(val1.Value, parms[2].GetExpressionType(), parms[1]), parms[2])
							{
								DoNotOptimize = true
							};
						else if (parms[2] is SqlValue val2 && parms[1] is not SqlValue)
							return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, parms[0], parms[1], CreateSqlValue(val2.Value, parms[1].GetExpressionType(), parms[2]))
							{
								DoNotOptimize = true
							};
					}
				}

				break;

				case "EXISTS":
				{
					if (func.Parameters.Length == 1 && func.Parameters[0] is SelectQuery query && query.Select.Columns.Count > 0)
					{
						var isAggregateQuery =
									query.Select.Columns.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));

						if (isAggregateQuery)
							return new SqlValue(true);
					}

					break;
				}

				case PseudoFunctions.CONVERT:
				{
					var typef = func.SystemType.ToUnderlying();

					if (func.Parameters[2] is SqlFunction from && from.Name == PseudoFunctions.CONVERT && from.Parameters[1].SystemType!.ToUnderlying() == typef)
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

				case "ConvertToCaseCompareTo":
					return new SqlFunction(func.SystemType, "CASE",
							new SqlSearchCondition().Expr(func.Parameters[0]).Greater.Expr(func.Parameters[1]).ToExpr(), new SqlValue(1),
							new SqlSearchCondition().Expr(func.Parameters[0]).Equal.Expr(func.Parameters[1]).ToExpr(), new SqlValue(0),
							new SqlValue(-1))
						{ CanBeNull = false };

			}

			return func;
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

		ISqlPredicate OptimizeCase(SqlPredicate.IsTrue isTrue, EvaluationContext context)
		{
			//TODO: refactor CASE optimization

			if ((isTrue.WithNull == null || isTrue.WithNull == false) && isTrue.Expr1 is SqlFunction func && func.Name == "CASE")
			{
				if (func.Parameters.Length == 3)
				{
					// It handles one specific case for OData
					if (func.Parameters[0] is SqlSearchCondition &&
					    func.Parameters[2] is SqlSearchCondition sc &&
					    func.Parameters[1].TryEvaluateExpression(context, out var v1) && v1 is null)
					{
						if (isTrue.IsNot)
							return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);
						return sc;
					}
				}
			}
			return isTrue;
		}

		ISqlPredicate OptimizeCase(NullabilityContext nullability, SqlPredicate.ExprExpr expr, EvaluationContext context)
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


			if (!nullability.IsEmpty && !expr.Expr1.CanBeNullable(nullability) && !expr.Expr2.CanBeNullable(nullability) && expr.Expr1.SystemType.IsSignedType() && expr.Expr2.SystemType.IsSignedType())
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


		public virtual ISqlPredicate OptimizePredicate(NullabilityContext nullability, ISqlPredicate predicate, EvaluationContext context, DataOptions dataOptions)
		{
			// Avoiding infinite recursion
			//
			if (predicate.ElementType == QueryElementType.ExprPredicate)
			{
				var exprPredicate = (SqlPredicate.Expr)predicate;
				if (exprPredicate.Expr1.ElementType == QueryElementType.SqlValue)
					return predicate;
			}

			if (predicate.ElementType != QueryElementType.SearchCondition
				&& predicate.TryEvaluateExpression(context, out var value) && value != null)
			{
				return new SqlPredicate.Expr(new SqlValue(value));
			}

			switch (predicate.ElementType)
			{
				case QueryElementType.SearchCondition:
					return SelectQueryOptimizer.OptimizeSearchCondition((SqlSearchCondition)predicate, context);

				case QueryElementType.IsTruePredicate:
				{
					var isTrue = (SqlPredicate.IsTrue)predicate;
					predicate = OptimizeCase(isTrue, context);
					break;
				}

				case QueryElementType.BetweenPredicate:
				{
					var between = (SqlPredicate.Between)predicate;
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Between) && between.Expr1 is SqlRow)
					{
						return ConvertBetweenPredicate(between);
					}

					break;
				}

				case QueryElementType.ExprExprPredicate:
				{
					var expr = (SqlPredicate.ExprExpr)predicate;

					if (expr.WithNull == null && (expr.Operator == SqlPredicate.Operator.Equal || expr.Operator == SqlPredicate.Operator.NotEqual))
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

					if (expr.Expr1.ElementType == QueryElementType.SqlRow)
						return OptimizeRowExprExpr(expr, context);

					switch (expr.Operator)
					{
						case SqlPredicate.Operator.Equal          :
						case SqlPredicate.Operator.NotEqual       :
						case SqlPredicate.Operator.Greater        :
						case SqlPredicate.Operator.GreaterOrEqual :
						case SqlPredicate.Operator.Less           :
						case SqlPredicate.Operator.LessOrEqual    :
							predicate = OptimizeCase(nullability, expr, context);
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
									return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.NotEqual, ee.Expr2, dataOptions.LinqOptions.CompareNullsAsValues ? true : null);

								if (ee.Operator == SqlPredicate.Operator.NotEqual)
									return new SqlPredicate.ExprExpr(ee.Expr1, SqlPredicate.Operator.Equal, ee.Expr2, dataOptions.LinqOptions.CompareNullsAsValues ? true : null);
							}
						}
					}

					break;
				}

				case QueryElementType.InListPredicate:
				{
					var inList = (SqlPredicate.InList)predicate;
					if (inList.Expr1.ElementType == QueryElementType.SqlRow)
						return OptimizeRowInList(inList);
					break;
				}

				case QueryElementType.IsDistinctPredicate:
				{
					var expr = (SqlPredicate.IsDistinct)predicate;

					if (nullability.IsEmpty)
						return expr;

					// Here, several optimisations would already have occured:
					// - If both expressions could be evaluated, Sql.IsDistinct would have been evaluated client-side.
					// - If both expressions could not be null, an Equals expression would have been used instead.

					// The only remaining case that we'd like to simplify is when one expression is the constant null.
					if (expr.Expr1.TryEvaluateExpression(context, out var value1) && value1 == null)
					{
						return expr.Expr2.CanBeNullable(nullability)
							? new SqlPredicate.IsNull(expr.Expr2, !expr.IsNot)
							: new SqlPredicate.Expr(new SqlValue(!expr.IsNot));
					}
					if (expr.Expr2.TryEvaluateExpression(context, out var value2) && value2 == null)
					{
						return expr.Expr1.CanBeNullable(nullability)
							? new SqlPredicate.IsNull(expr.Expr1, !expr.IsNot)
							: new SqlPredicate.Expr(new SqlValue(!expr.IsNot));
					}

					break;
				}
			}

			return predicate;
		}

		#region SqlRow

		protected ISqlPredicate OptimizeRowExprExpr(SqlPredicate.ExprExpr predicate, EvaluationContext context)
		{
			var op = predicate.Operator;
			var feature = op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual
				? RowFeature.Equality
				: op is SqlPredicate.Operator.Overlaps
					? RowFeature.Overlaps
					: RowFeature.Comparisons;

			switch (predicate.Expr2)
			{
				// ROW(a, b) IS [NOT] NULL
				case SqlValue { Value: null }:
					if (op is not (SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))
						throw new LinqException("Null SqlRow is only allowed in equality comparisons");
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.IsNull))
						return RowIsNullFallback((SqlRow)predicate.Expr1, op == SqlPredicate.Operator.NotEqual);
					break;

				// ROW(a, b) operator ROW(c, d)
				case SqlRow rhs:
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature))
						return RowComparisonFallback(op, (SqlRow)predicate.Expr1, rhs, context);
					break;

				// ROW(a, b) operator (SELECT c, d)
				case SelectQuery:
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(feature) ||
						!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.CompareToSelect))
						throw new LinqException("SqlRow comparisons to SELECT are not supported by this DB provider");
					break;

				default:
					throw new LinqException("Inappropriate SqlRow expression, only Sql.Row() and sub-selects are valid.");
			}

			// Default ExprExpr translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, predicate.Expr2, withNull: null);
		}

		protected virtual ISqlPredicate OptimizeRowInList(SqlPredicate.InList predicate)
		{
			if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.In))
			{
				var left    = predicate.Expr1;
				var op      = predicate.IsNot ? SqlPredicate.Operator.NotEqual : SqlPredicate.Operator.Equal;
				var isOr    = !predicate.IsNot;
				var rewrite = new SqlSearchCondition();
				foreach (var item in predicate.Values)
					rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(left, op, item, withNull: null), isOr));
				return rewrite;
			}

			// Default InList translation is ok
			// We always disable CompareNullsAsValues behavior when comparing SqlRow.
			return predicate.WithNull == null
				? predicate
				: new SqlPredicate.InList(predicate.Expr1, withNull: null, predicate.IsNot, predicate.Values);
		}

		protected ISqlPredicate RowIsNullFallback(SqlRow row, bool isNot)
		{
			var rewrite = new SqlSearchCondition();
			// (a, b) is null     => a is null     and b is null
			// (a, b) is not null => a is not null and b is not null
			foreach (var value in row.Values)
				rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(value, isNot)));
			return rewrite;
		}

		protected ISqlPredicate RowComparisonFallback(SqlPredicate.Operator op, SqlRow row1, SqlRow row2, EvaluationContext context)
		{
			var rewrite = new SqlSearchCondition();
						
			if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				// (a1, a2) =  (b1, b2) => a1 =  b1 and a2 = b2
				// (a1, a2) <> (b1, b2) => a1 <> b1 or  a2 <> b2
				bool isOr = op == SqlPredicate.Operator.NotEqual;
				var compares = row1.Values.Zip(row2.Values, (a, b) =>
				{
					// There is a trap here, neither `a` nor `b` should be a constant null value,
					// because ExprExpr reduces `a == null` to `a is null`,
					// which is not the same and not equivalent to the Row expression.
					// We use `a >= null` instead, which is equivalent (always evaluates to `unknown`) but is never reduced by ExprExpr.
					// Reducing to `false` is an inaccuracy that causes problems when composed in more complicated ways,
					// e.g. the NOT IN SqlRow tests fail.
					SqlPredicate.Operator nullSafeOp = a.TryEvaluateExpression(context, out var val) && val == null ||
					                                   b.TryEvaluateExpression(context, out     val) && val == null
						? SqlPredicate.Operator.GreaterOrEqual
						: op;
					return new SqlPredicate.ExprExpr(a, nullSafeOp, b, withNull: null);
				});
				foreach (var comp in compares)
					rewrite.Conditions.Add(new SqlCondition(false, comp, isOr));

				return rewrite;
			}

			if (op is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual or SqlPredicate.Operator.Less or SqlPredicate.Operator.LessOrEqual)
			{
				// (a1, a2, a3) >  (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >  b3)
				// (a1, a2, a3) >= (b1, b2, b3) => a1 > b1 or (a1 = b1 and a2 > b2) or (a1 = b1 and a2 = b2 and a3 >= b3)
				// (a1, a2, a3) <  (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <  b3)
				// (a1, a2, a3) <= (b1, b2, b3) => a1 < b1 or (a1 = b1 and a2 < b2) or (a1 = b1 and a2 = b2 and a3 <= b3)
				var strictOp = op is SqlPredicate.Operator.Greater or SqlPredicate.Operator.GreaterOrEqual ? SqlPredicate.Operator.Greater : SqlPredicate.Operator.Less;
				var values1 = row1.Values;
				var values2 = row2.Values;
				for (int i = 0; i < values1.Length; ++i)
				{
					for (int j = 0; j < i; j++)
						rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(values1[j], SqlPredicate.Operator.Equal, values2[j], withNull: null), isOr: false));
					rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(values1[i], i == values1.Length - 1 ? op : strictOp, values2[i], withNull: null), isOr: true));
				}

				return rewrite;
			}

			if (op is SqlPredicate.Operator.Overlaps)
			{
				//TODO: make it working if possible
				/*
				if (row1.Values.Length != 2 || row2.Values.Length != 2)
					throw new LinqException("Unsupported SqlRow conversion from operator: " + op);

				rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(row1.Values[0], SqlPredicate.Operator.LessOrEqual, row2.Values[1], withNull: false)));
				rewrite.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(row2.Values[0], SqlPredicate.Operator.LessOrEqual, row1.Values[1], withNull: false)));
				*/

				return rewrite;
			}

			throw new LinqException("Unsupported SqlRow operator: " + op);
		}

		public virtual ISqlPredicate ConvertBetweenPredicate(SqlPredicate.Between between)
		{
			var newPredicate = !between.IsNot
				? new SqlSearchCondition(
					new SqlCondition(false, new SqlPredicate.ExprExpr(between.Expr1, SqlPredicate.Operator.GreaterOrEqual, between.Expr2, withNull: false)),
					new SqlCondition(false, new SqlPredicate.ExprExpr(between.Expr1, SqlPredicate.Operator.LessOrEqual,    between.Expr3, withNull: false)))
				: new SqlSearchCondition(
					new SqlCondition(false, new SqlPredicate.ExprExpr(between.Expr1, SqlPredicate.Operator.Less,    between.Expr2, withNull: false), isOr: true),
					new SqlCondition(false, new SqlPredicate.ExprExpr(between.Expr1, SqlPredicate.Operator.Greater, between.Expr3, withNull: false)));

			return newPredicate;
		}

		#endregion

		private readonly SqlDataType _typeWrapper = new (default(DbDataType));
		public virtual IQueryElement OptimizeQueryElement(ConvertVisitor<RunOptimizationContext> visitor, IQueryElement element)
		{
			switch (element.ElementType)
			{
				case QueryElementType.Condition:
				{
					var condition = (SqlCondition)element;

					return SelectQueryOptimizer.OptimizeCondition(condition);
				}

				case QueryElementType.SqlValue:
				{
					var value = (SqlValue)element;

					if (value.Value is Sql.SqlID)
						break;

					// TODO:
					// this line produce insane amount of allocations
					// as currently we cannot change ValueConverter signatures, we use pre-created instance of type wrapper
					//var dataType = new SqlDataType(value.ValueType);
					_typeWrapper.Type = value.ValueType;

					if (!visitor.Context.MappingSchema.ValueToSqlConverter.CanConvert(_typeWrapper, visitor.Context.DataOptions, value.Value))
					{
						// we cannot generate SQL literal, so just convert to parameter
						var param = visitor.Context.OptimizationContext.SuggestDynamicParameter(value.ValueType, value.Value);
						return param;
					}

					break;
				}
			}

			return element;
		}

		public virtual ISqlExpression OptimizeBinaryExpression(SqlBinaryExpression be, EvaluationContext context)
		{
			switch (be.Operation)
			{
				case "+":
				{
					var v1 = be.Expr1.TryEvaluateExpression(context, out var value1);
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

					var v2 = be.Expr2.TryEvaluateExpression(context, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return be.Expr1;
							case int vi when
								be.Expr1    is SqlBinaryExpression be1 &&
								be1.Expr2.TryEvaluateExpression(context, out var be1v2) &&
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
								be1.Expr2.TryEvaluateExpression(context, out var be1v2) &&
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
					var v2 = be.Expr2.TryEvaluateExpression(context, out var value2);
					if (v2)
					{
						switch (value2)
						{
							case int vi when vi == 0 : return be.Expr1;
							case int vi when
								be.Expr1 is SqlBinaryExpression be1 &&
								be1.Expr2.TryEvaluateExpression(context, out var be1v2) &&
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

					if (v2 && be.Expr1.TryEvaluateExpression(context, out var value1))
					{
						if (value1 is int i1 && value2 is int i2) return CreateSqlValue(i1 - i2, be);
					}

					break;
				}

				case "*":
				{
					var v1 = be.Expr1.TryEvaluateExpression(context, out var value1);
					if (v1)
					{
						switch (value1)
						{
							case int i when i == 0 : return CreateSqlValue(0, be);
							case int i when i == 1 : return be.Expr2;
							case int i when
								be.Expr2    is SqlBinaryExpression be2 &&
								be2.Operation == "*"                   &&
								be2.Expr1.TryEvaluateExpression(context, out var be2v1)  &&
								be2v1 is int bi :
							{
								return new SqlBinaryExpression(be2.SystemType, CreateSqlValue(i * bi, be), "*", be2.Expr2);
							}
						}
					}

					var v2 = be.Expr2.TryEvaluateExpression(context, out var value2);
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

			if (be.Operation.In("+", "-") && be.Expr1.SystemType == be.Expr2.SystemType)
			{
				ISqlExpression? newExpr = be switch
				{
					// (binary + v)
					(SqlBinaryExpression binary, "+", var v) when v.CanBeEvaluated(context) =>
						binary switch
						{
							// (some + e) + v ===> some + (e + v)
							(var some, "+", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "+", v)),

							// (some - e) + v ===> some + (v - e)
							(var some, "-", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, v, "-", e)),

							// (e + some) + v ===> some + (e + v)
							(var e, "+", var some) when e.SystemType.IsNumericType() && e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "+", v)),

							// (e - some) + v ===> (e + v) - some
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, e, "+", v), "-", some),

							_ => null
						},

					// (binary - v)
					(SqlBinaryExpression binary, "-", var v) when v.CanBeEvaluated(context) =>
						binary switch
						{
							// (some + e) - v ===> some + (e - v)
							(var some, "+", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "-", v)),

							// (some - e) - v ===> some - (e + v)
							(var some, "-", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "+", v)),

							// (e + some) - v ===> some + (e - v)
							(var e, "+", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, some, "+", new SqlBinaryExpression(be.SystemType, e, "-", v)),

							// (e - some) - v ===> (e - v) - some
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, e, "-", v), "-", some),

							_ => null
						},

					// (v + binary)
					(var v, "+", SqlBinaryExpression binary) when v.CanBeEvaluated(context) =>
						binary switch
						{
							// v + (some + e) ===> (v + e) + some
							(var some, "+", var e) when e.SystemType.IsNumericType() && e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "+", some),

							// v + (some - e) + v ===> (v - e) + some
							(var some, "-", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "+", some),

							// v + (e + some) ===> (v + e) + some
							(var e, "+", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "+", some),

							// v + (e - some) ===> (v + e) - some
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "-", some),

							_ => null
						},

					// (v - binary)
					(var v, "+", SqlBinaryExpression binary) when v.CanBeEvaluated(context) =>
						binary switch
						{
							// v - (some + e) ===> (v - e) - some
							(var some, "+", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "-", some),

							// v - (some - e) + v ===> (v + e) - some
							(var some, "-", var e) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "+", e), "-", some),

							// v - (e + some) ===> (v - e) - some
							(var e, "+", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "-", some),

							// v - (e - some) ===> (v - e) + some
							(var e, "-", var some) when e.CanBeEvaluated(context) => new SqlBinaryExpression(be.SystemType, new SqlBinaryExpression(be.SystemType, v, "-", e), "+", some),

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

		[return: NotNullIfNotNull(nameof(element))]
		public virtual IQueryElement? ConvertElement(MappingSchema mappingSchema, DataOptions dataOptions, IQueryElement? element, OptimizationContext context, NullabilityContext nullability)
		{
			return OptimizeElement(mappingSchema, dataOptions, element, context, true, nullability);
		}

		public virtual ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
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
								var len = be.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(visitor.Context.MappingSchema.GetDataType(be.Expr2.SystemType).Type.DataType);

								if (len == null || len <= 0)
									len = 100;

								return new SqlBinaryExpression(
									be.SystemType,
									be.Expr1,
									be.Operation,
									ConvertExpressionImpl(PseudoFunctions.MakeConvert(new SqlDataType(DataType.VarChar, typeof(string), len.Value), new SqlDataType(be.Expr2.GetExpressionType()), be.Expr2), visitor),
									be.Precedence);
							}

							if (be.Expr1.SystemType != typeof(string) && be.Expr2.SystemType == typeof(string))
							{
								var len = be.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(visitor.Context.MappingSchema.GetDataType(be.Expr1.SystemType).Type.DataType);

								if (len == null || len <= 0)
									len = 100;

								return new SqlBinaryExpression(
									be.SystemType,
									ConvertExpressionImpl(PseudoFunctions.MakeConvert(new SqlDataType(DataType.VarChar, typeof(string), len.Value), new SqlDataType(be.Expr1.GetExpressionType()), be.Expr1), visitor),
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
					return ConvertFunction(visitor.Context.Nullability, (SqlFunction)expression);
				}
				#endregion

				case QueryElementType.SqlExpression   :
				{
					var se = (SqlExpression)expression;

					if (se.Expr == "{0}" && se.Parameters.Length == 1 && se.Parameters[0] != null && se.CanBeNull == se.Parameters[0].CanBeNullable(visitor.Context.Nullability))
						return se.Parameters[0];

					break;
				}
			}

			return expression;
		}

		protected virtual ISqlExpression ConvertFunction(NullabilityContext nullability, SqlFunction func)
		{
			switch (func.Name)
			{
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

				case PseudoFunctions.CONVERT:
					return ConvertConversion(func);

				case PseudoFunctions.TO_LOWER: return func.WithName("Lower");
				case PseudoFunctions.TO_UPPER: return func.WithName("Upper");
				case PseudoFunctions.REPLACE : return func.WithName("Replace");
				case PseudoFunctions.COALESCE: return func.WithName("Coalesce");
			}

			return func;
		}

		public readonly struct RunOptimizationContext
		{
			public RunOptimizationContext(
				OptimizationContext optimizationContext,
				BasicSqlOptimizer   optimizer,
				MappingSchema       mappingSchema,
                DataOptions         dataOptions,
				NullabilityContext  nullability,
				bool                register,
				Func<ConvertVisitor<RunOptimizationContext>, IQueryElement, IQueryElement> func)
			{
				OptimizationContext = optimizationContext;
				Optimizer           = optimizer;
				MappingSchema       = mappingSchema;
                DataOptions         = dataOptions;
				Nullability         = nullability;
				Register            = register;
				Func                = func;
			}

			public readonly OptimizationContext OptimizationContext;
			public readonly BasicSqlOptimizer   Optimizer;
			public readonly NullabilityContext  Nullability;
			public readonly bool                Register;
			public readonly MappingSchema       MappingSchema;
			public readonly DataOptions         DataOptions;

			public readonly Func<ConvertVisitor<RunOptimizationContext>,IQueryElement,IQueryElement> Func;
		}

		static IQueryElement RunOptimization(
			IQueryElement       element,
			OptimizationContext optimizationContext,
			BasicSqlOptimizer   optimizer,
			MappingSchema       mappingSchema,
			DataOptions         dataOptions,
			NullabilityContext  nullability,
			bool                register,
			Func<ConvertVisitor<RunOptimizationContext>,IQueryElement,IQueryElement> func)
		{
			var ctx = new RunOptimizationContext(optimizationContext, optimizer, mappingSchema, dataOptions, nullability, register, func);

			for (;;)
			{
				var newElement = optimizationContext.ConvertAll(
					ctx,
					element,
					static (visitor, e) =>
					{
						var prev = e;

						for (;;)
						{
							var ne = visitor.Context.Func(visitor, e);

							if (ReferenceEquals(ne, e))
								break;

							e = ne;
						}

						if (visitor.Context.Register)
							visitor.Context.OptimizationContext.RegisterOptimized(prev, e);

						return e;
					},
					static visitor =>
					{
						if (visitor.Context.OptimizationContext.IsOptimized(visitor.CurrentElement, out var expr))
						{
							visitor.CurrentElement = expr;
							return false;
						}
						return true;
					});

				if (ReferenceEquals(newElement, element))
					return element;

				element = newElement;
			}
		}

		public IQueryElement? OptimizeElement(MappingSchema mappingSchema, DataOptions dataOptions, IQueryElement? element, OptimizationContext optimizationContext, bool withConversion, NullabilityContext nullability)
		{
			if (element == null)
				return null;

			if (optimizationContext.IsOptimized(element, out var newElement))
				return newElement!;

			newElement = RunOptimization(element, optimizationContext, this, mappingSchema, dataOptions, nullability, !withConversion,
				static (visitor, e) =>
				{
					var ne = e;
					if (ne is ISqlExpression expr1)
						ne = visitor.Context.Optimizer.OptimizeExpression(expr1, visitor);

					if (ne is ISqlPredicate pred1)
						ne = visitor.Context.Optimizer.OptimizePredicate(visitor.Context.Nullability, pred1, visitor.Context.OptimizationContext.Context, visitor.Context.DataOptions);

					if (!ReferenceEquals(ne, e))
						return ne;

					ne = visitor.Context.Optimizer.OptimizeQueryElement(visitor, ne);

					return ne;
				});

			if (withConversion)
			{
				if (mappingSchema == null)
					throw new InvalidOperationException("MappingSchema is required for conversion");

				newElement = RunOptimization(newElement, optimizationContext, this, mappingSchema, dataOptions, nullability, true,
					static(visitor, e) =>
					{
						var ne = e;

						if (ne is ISqlExpression expr2)
							ne = visitor.Context.Optimizer.ConvertExpressionImpl(expr2, visitor);

						if (!ReferenceEquals(ne, e))
							return ne;

						if (ne is ISqlPredicate pred3)
							ne = visitor.Context.Optimizer.ConvertPredicateImpl(pred3, visitor);

						return ne;
					});

			}

			return newElement;
		}

		public virtual bool CanCompareSearchConditions => false;

		public virtual ISqlPredicate ConvertPredicateImpl(ISqlPredicate predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					var exprExpr = (SqlPredicate.ExprExpr)predicate;

					var reduced = exprExpr.Reduce(visitor.Context.Nullability, visitor.Context.OptimizationContext.Context);

					if (!ReferenceEquals(reduced, exprExpr))
					{
						return reduced;
					}

					if (!CanCompareSearchConditions && (exprExpr.Expr1.ElementType == QueryElementType.SearchCondition || exprExpr.Expr2.ElementType == QueryElementType.SearchCondition))
					{
						var expr1 = exprExpr.Expr1;
						if (expr1.ElementType == QueryElementType.SearchCondition)
							expr1 = ConvertBooleanExprToCase(expr1);

						var expr2 = exprExpr.Expr2;
						if (expr2.ElementType == QueryElementType.SearchCondition)
							expr2 = ConvertBooleanExprToCase(expr2);

						return new SqlPredicate.ExprExpr(expr1, exprExpr.Operator, expr2, exprExpr.WithNull);
					}

					break;
				}
				case QueryElementType.IsTruePredicate:
					return ((SqlPredicate.IsTrue)predicate).Reduce(visitor.Context.Nullability);
				case QueryElementType.IsNullPredicate:
					return ConvertIsNullPredicate(visitor.Context.Nullability, ((SqlPredicate.IsNull)predicate));
				case QueryElementType.LikePredicate:
					return ConvertLikePredicate(visitor.Context.MappingSchema, (SqlPredicate.Like)predicate, visitor.Context.OptimizationContext.Context);
				case QueryElementType.SearchStringPredicate:
					return ConvertSearchStringPredicate((SqlPredicate.SearchString)predicate, visitor);
				case QueryElementType.InListPredicate:
					return ConvertInListPredicate(visitor.Context.MappingSchema, (SqlPredicate.InList)predicate, visitor.Context.OptimizationContext.Context);
			}
			return predicate;
		}

		/// <summary>
		/// Escape sequence/character to escape special characters in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>).
		/// Default: <c>"~"</c>.
		/// </summary>
		public virtual string LikeEscapeCharacter         => "~";
		public virtual string LikeWildcardCharacter       => "%";
		public virtual bool   LikePatternParameterSupport => true;
		public virtual bool   LikeValueParameterSupport   => true;
		/// <summary>
		/// Should be <c>true</c> for provider with <c>LIKE ... ESCAPE</c> modifier support.
		/// Default: <c>true</c>.
		/// </summary>
		public virtual bool   LikeIsEscapeSupported       => true;

		protected static  string[] StandardLikeCharactersToEscape = {"%", "_", "?", "*", "#", "[", "]"};
		/// <summary>
		/// Characters with special meaning in LIKE predicate (defined by <see cref="LikeCharactersToEscape"/>) that should be escaped to be used as matched character.
		/// Default: <c>["%", "_", "?", "*", "#", "[", "]"]</c>.
		/// </summary>
		public virtual string[] LikeCharactersToEscape => StandardLikeCharactersToEscape;

		public virtual string EscapeLikeCharacters(string str, string escape)
		{
			var newStr = str;

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
			var result = PseudoFunctions.MakeReplace(expression, character, new SqlBinaryExpression(typeof(string), escapeCharacter, "+", character, Precedence.Additive));
			return result;
		}

		public static ISqlExpression GenerateEscapeReplacement(ISqlExpression expression, ISqlExpression character)
		{
			var result = PseudoFunctions.MakeReplace(
				expression,
				character,
				new SqlBinaryExpression(typeof(string), new SqlValue("["), "+",
					new SqlBinaryExpression(typeof(string), character, "+", new SqlValue("]"), Precedence.Additive),
					Precedence.Additive));
			return result;
		}

		/// <summary>
		/// Implements LIKE pattern escaping logic for provider without ESCAPE clause support (<see cref="LikeIsEscapeSupported"/> is <c>false</c>).
		/// Default logic prefix characters from <see cref="LikeCharactersToEscape"/> with <see cref="LikeEscapeCharacter"/>.
		/// </summary>
		/// <param name="str">Raw pattern value.</param>
		/// <returns>Escaped pattern value.</returns>
		protected virtual string EscapeLikePattern(string str)
		{
			foreach (var s in LikeCharactersToEscape)
				str = str.Replace(s, LikeEscapeCharacter + s);

			return str;
		}

		public virtual ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			var newExpr = expression;

			escape ??= new SqlValue(LikeEscapeCharacter);

			newExpr = GenerateEscapeReplacement(newExpr, escape, escape);

			var toEscape = LikeCharactersToEscape;
			foreach (var s in toEscape)
			{
				newExpr = GenerateEscapeReplacement(newExpr, new SqlValue(s), escape);
			}

			return newExpr;
		}

		public virtual ISqlPredicate ConvertIsNullPredicate(NullabilityContext nullability, SqlPredicate.IsNull isNull)
		{
			if (nullability.IsEmpty)
				return isNull;
			
			if (!nullability.CanBeNull(isNull.Expr1))
				return new SqlPredicate.Expr(new SqlValue(isNull.IsNot));
			return isNull;
		}

		/// <summary>
		/// LIKE predicate interceptor. Default implementation does nothing.
		/// </summary>
		/// <param name="mappingSchema">Current mapping schema.</param>
		/// <param name="predicate">LIKE predicate.</param>
		/// <param name="context">Parameters evaluation context for current query.</param>
		/// <returns>Preprocessed predicate.</returns>
		public virtual ISqlPredicate ConvertLikePredicate(
			MappingSchema     mappingSchema,
			SqlPredicate.Like predicate,
			EvaluationContext context)
		{
			return predicate;
		}

		protected ISqlPredicate ConvertSearchStringPredicateViaLike(
			SqlPredicate.SearchString              predicate,
			ConvertVisitor<RunOptimizationContext> visitor)
		{
			if (predicate.Expr2.TryEvaluateExpression(visitor.Context.OptimizationContext.Context, out var patternRaw)
				&& Converter.TryConvertToString(patternRaw, out var patternRawValue))
			{
				if (patternRawValue == null)
					return new SqlPredicate.IsTrue(new SqlValue(true), new SqlValue(true), new SqlValue(false), null, predicate.IsNot);

				var patternValue = LikeIsEscapeSupported
					? EscapeLikeCharacters(patternRawValue, LikeEscapeCharacter)
					: EscapeLikePattern(patternRawValue);

				patternValue = predicate.Kind switch
				{
					SqlPredicate.SearchString.SearchKind.StartsWith => patternValue + LikeWildcardCharacter,
					SqlPredicate.SearchString.SearchKind.EndsWith   => LikeWildcardCharacter + patternValue,
					SqlPredicate.SearchString.SearchKind.Contains   => LikeWildcardCharacter + patternValue + LikeWildcardCharacter,
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}")
				};

				var patternExpr = LikePatternParameterSupport
					? CreateSqlValue(patternValue, predicate.Expr2.GetExpressionType(), predicate.Expr2)
					: new SqlValue(patternValue);

				var valueExpr = predicate.Expr1;
				if (!LikeValueParameterSupport)
				{
					predicate.Expr1.VisitAll(static e =>
					{
						if (e is SqlParameter p)
							p.IsQueryParameter = false;
					});
				}

				return new SqlPredicate.Like(valueExpr, predicate.IsNot, patternExpr,
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
					_ => throw new InvalidOperationException($"Unexpected predicate kind: {predicate.Kind}")
				};

				patternExpr = OptimizeExpression(patternExpr, visitor);

				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, patternExpr, LikeIsEscapeSupported ? escape : null);
			}
		}

		public virtual ISqlPredicate ConvertSearchStringPredicate(
			SqlPredicate.SearchString              predicate,
			ConvertVisitor<RunOptimizationContext> visitor)
		{
			if (predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) == false)
			{
				predicate = new SqlPredicate.SearchString(
					PseudoFunctions.MakeToLower(predicate.Expr1),
					predicate.IsNot,
					PseudoFunctions.MakeToLower(predicate.Expr2),
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate, visitor);
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

				if (paramValue.ProviderValue == null)
					return new SqlPredicate.Expr(new SqlValue(p.IsNot));

				if (paramValue.ProviderValue is IEnumerable items)
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
								values.Add(mappingSchema.GetSqlValueFromObject(cd, item!));
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
									var field    = ExpectsUnderlyingField(key);
									var cd       = field.ColumnDescriptor;
									var sqlValue = mappingSchema.GetSqlValueFromObject(cd, item!);
									//TODO: review
									var cond = sqlValue.Value == null ?
										new SqlCondition(false, new SqlPredicate.IsNull  (field, false)) :
										new SqlCondition(false, new SqlPredicate.ExprExpr(field, SqlPredicate.Operator.Equal, sqlValue, null));

									itemCond.Conditions.Add(cond);
								}

								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
							}

							if (sc.Conditions.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							if (p.IsNot)
								return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);

							return new SqlPredicate.Expr(sc, Precedence.LogicalDisjunction);
						}
					}

					if (p.Expr1 is SqlObjectExpression expr)
					{
						var parameters = expr.InfoParameters;
						if (parameters.Length == 1)
						{
							var values = new List<ISqlExpression>();

							foreach (var item in items)
								values.Add(expr.GetSqlValue(item!, 0));

							if (values.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							return new SqlPredicate.InList(parameters[0].Sql, null, p.IsNot, values);
						}

						var sc = new SqlSearchCondition();

						foreach (var item in items)
						{
							var itemCond = new SqlSearchCondition();

							for (var i = 0; i < parameters.Length; i++)
							{
								var sql   = parameters[i].Sql;
								var value = expr.GetSqlValue(item!, i);
								var cond  = value == null ?
									new SqlCondition(false, new SqlPredicate.IsNull  (sql, false)) :
									new SqlCondition(false, new SqlPredicate.ExprExpr(sql, SqlPredicate.Operator.Equal, value, null));

								itemCond.Conditions.Add(cond);
							}

							sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
						}

						if (sc.Conditions.Count == 0)
							return new SqlPredicate.Expr(new SqlValue(p.IsNot));

						if (p.IsNot)
							return new SqlPredicate.NotExpr(sc, true, Precedence.LogicalNegation);

						return new SqlPredicate.Expr(sc, Precedence.LogicalDisjunction);
					}
				}
			}

			return p;
		}

		protected ISqlExpression ConvertCoalesceToBinaryFunc(SqlFunction func, string funcName, bool supportsParameters = true)
		{
			var last = func.Parameters[func.Parameters.Length - 1];
			if (!supportsParameters && last is SqlParameter p1)
				p1.IsQueryParameter = false;

			for (int i = func.Parameters.Length - 2; i >= 0; i--)
			{
				var param = func.Parameters[i];
				if (!supportsParameters && param is SqlParameter p2)
					p2.IsQueryParameter = false;

				last = new SqlFunction(func.SystemType, funcName, param, last);
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

		/// <summary>
		/// Implements <see cref="PseudoFunctions.CONVERT"/> function converter.
		/// </summary>
		protected virtual ISqlExpression ConvertConversion(SqlFunction func)
		{
			var from = (SqlDataType)func.Parameters[1];
			var to   = (SqlDataType)func.Parameters[0];

			if (!func.DoNotOptimize && (to.Type.SystemType == typeof(object) || from.Type.EqualsDbOnly(to.Type)))
				return func.Parameters[2];

			if (to.Type.Length > 0)
			{
				var maxLength = to.Type.SystemType == typeof(string) ? GetMaxDisplaySize(from) : GetMaxLength(from);
				var newLength = maxLength != null && maxLength >= 0 ? Math.Min(to.Type.Length ?? 0, maxLength.Value) : to.Type.Length;

				if (to.Type.Length != newLength)
					to = new SqlDataType(to.Type.WithLength(newLength));
			}
			else if (!func.DoNotOptimize && from.Type.SystemType == typeof(short) && to.Type.SystemType == typeof(int))
				return func.Parameters[2];

			return new SqlFunction(func.SystemType, "Convert", false, true, Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, to, func.Parameters[2]);
		}

		#endregion

		#region Alternative Builders

		protected ISqlExpression? AlternativeConvertToBoolean(SqlFunction func, DataOptions dataOptions, int paramNumber)
		{
			var par = func.Parameters[paramNumber];

			if (par.SystemType!.IsFloatType() || par.SystemType!.IsIntegerType())
			{
				var sc = new SqlSearchCondition();

				sc.Conditions.Add(
					new SqlCondition(false,
						new SqlPredicate.ExprExpr(par, SqlPredicate.Operator.NotEqual, new SqlValue(0),
							dataOptions.LinqOptions.CompareNullsAsValues ? false : null)));

				return new SqlFunction(func.SystemType, "CASE", false, true, sc, new SqlValue(true), new SqlValue(false))
				{
					CanBeNull = false,
				};
			}

			return null;
		}

		protected ISqlExpression ConvertBooleanExprToCase(ISqlExpression expression)
		{
			return new SqlFunction(typeof(bool), "CASE", expression, new SqlValue(true), new SqlValue(false))
			{
				CanBeNull = false,
				DoNotOptimize = true
			};
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

		protected static bool IsSmallDateTimeType(ISqlExpression expr, string typeName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.SmallDateTime,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == typeName,
				_ => false,
			};
		}

		protected static bool IsDateTime2Type(ISqlExpression expr, string typeName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.DateTime2,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == typeName,
				_ => false,
			};
		}

		protected static bool IsDateTimeType(ISqlExpression expr, string typeName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.DateTime,
				QueryElementType.SqlExpression => ((SqlExpression)expr).Expr == typeName,
				_ => false,
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

		protected SqlDeleteStatement GetAlternativeDelete(SqlDeleteStatement deleteStatement, DataOptions dataOptions)
		{
			if ((deleteStatement.SelectQuery.From.Tables.Count > 1 || deleteStatement.SelectQuery.From.Tables[0].Joins.Count > 0))
			{
				var table = deleteStatement.Table ?? deleteStatement.SelectQuery.From.Tables[0].Source as SqlTable;

				//TODO: probably we can improve this part
				if (table == null)
					throw new LinqToDBException("Could not deduce table for delete");

				if (deleteStatement.Output != null)
					throw new NotImplementedException("GetAlternativeDelete not implemented for delete with output");

				var sql = new SelectQuery { IsParameterDependent = deleteStatement.IsParameterDependent };

				var newDeleteStatement = new SqlDeleteStatement(sql);

				deleteStatement.SelectQuery.ParentSelect = sql;

				var copy      = new SqlTable(table) { Alias = null };
				var tableKeys = table.GetKeys(true);
				var copyKeys  = copy. GetKeys(true);

				if (deleteStatement.SelectQuery.Where.SearchCondition.Conditions.Any(static c => c.IsOr))
				{
					var sc1 = new SqlSearchCondition(deleteStatement.SelectQuery.Where.SearchCondition.Conditions);
					var sc2 = new SqlSearchCondition();

					for (var i = 0; i < tableKeys.Count; i++)
					{
						sc2.Conditions.Add(new SqlCondition(
							false,
							new SqlPredicate.ExprExpr(
								copyKeys[i],
								SqlPredicate.Operator.Equal,
								tableKeys[i],
								dataOptions.LinqOptions.CompareNullsAsValues ? true : null)));
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
				if (query.Where.Find(IsAggregationFunction) != null)
					return true;
			}

			return false;
		}

		static bool HasComparisonInCondition(SqlSearchCondition search, SqlTable table)
		{
			return null != search.Find(e => e is SqlField field && field.Table == table);
		}

		protected static bool HasComparisonInQuery(SelectQuery query, SqlTable table)
		{
			if (query.Select.HasModifier || !query.GroupBy.IsEmpty)
				return false;

			if (HasComparisonInCondition(query.Where.SearchCondition, table))
				return true;

			foreach (var ts in query.From.Tables)
			{
				if (ts.Source is SelectQuery sc)
				{
					if (HasComparisonInQuery(sc, table))
						return true;
				}

				foreach (var join in ts.Joins)
				{
					if (join.JoinType == JoinType.Inner)
					{
						if (HasComparisonInCondition(join.Condition, table))
							return true;

						if (join.Table.Source is SelectQuery jq)
						{
							if (HasComparisonInQuery(jq, table))
								return true;
						}
					}
				}
						
			}

			return false;
		}

		protected static SqlOutputClause? RemapToOriginalTable(SqlOutputClause? outputClause)
		{
			//remapping to table back
			if (outputClause != null)
			{
				outputClause = outputClause.Convert((object?)null, (_, e) =>
				{
					if (e is SqlAnchor { AnchorKind: SqlAnchor.AnchorKindEnum.Deleted } anchor)
						return anchor.SqlExpression;
					return e;
				}, true);
			}

			return outputClause;
		}

		protected static bool RemoveUpdateTableIfPossible(SelectQuery query, SqlTable table, out SqlTableSource? source)
		{
			source = null;

			if (query.Select.HasModifier || !query.GroupBy.IsEmpty)
				return false;
				
			for (int i = 0; i < query.From.Tables.Count; i++)
			{
				var ts = query.From.Tables[i];
				if (ts.Joins.All(j => j.JoinType == JoinType.Inner || j.JoinType == JoinType.Left))
				{
					if (ts.Source == table)
					{
						source = ts;

						query.From.Tables.RemoveAt(i);
						for (int j = 0; j < ts.Joins.Count; j++)
						{
							query.From.Tables.Insert(i + j, ts.Joins[j].Table);
							query.Where.EnsureConjunction().ConcatSearchCondition(ts.Joins[j].Condition);
						}

						source.Joins.Clear();

						return true;
					}

					for (int j = 0; j < ts.Joins.Count; j++)
					{
						var join = ts.Joins[j];
						if (join.Table.Source == table)
						{
							if (ts.Joins.Skip(j + 1).Any(sj => QueryHelper.IsDependsOnSource(sj, table)))
								return false;

							source = join.Table;

							ts.Joins.RemoveAt(j);
							query.Where.EnsureConjunction().ConcatSearchCondition(join.Condition);

							for (int sj = 0; j < join.Table.Joins.Count; j++)
							{
								ts.Joins.Insert(j + sj, join.Table.Joins[sj]);
							}

							source.Joins.Clear();

							return true;
						}
					}
				}
			}

			return false;
		}

		static SelectQuery CloneQuery(
			SelectQuery                                  query, 
			SqlTable?                                    exceptTable,
			out Dictionary<IQueryElement, IQueryElement> replaceTree)
		{
			replaceTree = new Dictionary<IQueryElement, IQueryElement>();
			var clonedQuery = query.Clone(exceptTable, replaceTree, static (ut, e) =>
			{
				if ((e is SqlTable table && table == ut) || (e is SqlField field && field.Table == ut))
				{
					return false;
				}

				return true;
			});

			replaceTree = CorrectReplaceTree(replaceTree, exceptTable);

			return clonedQuery;
		}

		protected static SqlTable CloneTable(
			SqlTable                                     tableToClone,
			out Dictionary<IQueryElement, IQueryElement> replaceTree)
		{
			replaceTree = new Dictionary<IQueryElement, IQueryElement>();
			var clonedQuery = tableToClone.Clone(tableToClone, replaceTree,
				static (t, e) => (e is SqlTable table && table == t) || (e is SqlField field && field.Table == t));

			return clonedQuery;
		}

		static Dictionary<IQueryElement, IQueryElement> CorrectReplaceTree(Dictionary<IQueryElement, IQueryElement> replaceTree, SqlTable? exceptTable)
		{
			replaceTree = replaceTree
				.Where(pair =>
				{
					if (pair.Key is SqlTable table)
						return table != exceptTable;
					if (pair.Key is SqlColumn)
						return true;
					if (pair.Key is SqlField field)
						return field.Table != exceptTable;
					return false;
				})
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			return replaceTree;
		}

		protected static TElement RemapCloned<TElement>(
			TElement                                  element,
			Dictionary<IQueryElement, IQueryElement>? mainTree,
			Dictionary<IQueryElement, IQueryElement>? innerTree = null,
			bool insideColumns = true)
		where TElement : class, IQueryElement
		{
			if (mainTree == null && innerTree == null) 
				return element;

			var newElement = element.Convert((mainTree, innerTree, insideColumns), static (v, expr) =>
			{
				var converted = v.Context.mainTree?.TryGetValue(expr, out var newValue) == true
					? newValue
					: expr;

				if (v.Context.innerTree != null)
				{
					converted = v.Context.innerTree.TryGetValue(converted, out newValue)
						? newValue
						: converted;
				}

				return converted;
			}, !insideColumns);

			return newElement;
		}

		protected SqlUpdateStatement GetAlternativeUpdate(SqlUpdateStatement updateStatement, DataOptions dataOptions)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException();

			if (!updateStatement.SelectQuery.Select.HasModifier && updateStatement.SelectQuery.From.Tables.Count == 1)
			{
				var sqlTableSource = updateStatement.SelectQuery.From.Tables[0];
				if (sqlTableSource.Source == updateStatement.Update.Table && sqlTableSource.Joins.Count == 0)
				{
					// Simple variant
					CorrectUpdateSetters(updateStatement);
					updateStatement.Update.TableSource = null;
					return updateStatement;
				}
			}

			var needsComparison = !HasComparisonInQuery(updateStatement.SelectQuery, updateStatement.Update.Table);

			if (!needsComparison)
			{
				// trying to simplify query
				RemoveUpdateTableIfPossible(updateStatement.SelectQuery, updateStatement.Update.Table, out _);
			}

			if (NeedsEnvelopingForUpdate(updateStatement.SelectQuery))
				updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, allowMutation: true);

			// It covers subqueries also. Simple subquery will have sourcesCount == 2
			if (QueryHelper.EnumerateAccessibleTableSources(updateStatement.SelectQuery).Any())
			{
				var sql = new SelectQuery { IsParameterDependent = updateStatement.IsParameterDependent  };

				var newUpdateStatement = new SqlUpdateStatement(sql);
				updateStatement.SelectQuery.ParentSelect = sql;

				Dictionary<IQueryElement, IQueryElement>? replaceTree = null;

				var clonedQuery = CloneQuery(updateStatement.SelectQuery, needsComparison ? null : updateStatement.Update.Table, out replaceTree);

				SqlTable? tableToCompare = null;
				if (replaceTree.TryGetValue(updateStatement.Update.Table, out var newTable))
				{
					tableToCompare = (SqlTable)newTable;
				}

				if (tableToCompare != null)
				{
					replaceTree = CorrectReplaceTree(replaceTree, updateStatement.Update.Table);

					ApplyUpdateTableComparison(clonedQuery, updateStatement.Update, tableToCompare, dataOptions);
				}

				CorrectUpdateSetters(updateStatement);

				clonedQuery.Select.Columns.Clear();
				var processUniversalUpdate = true;

				if (updateStatement.Update.Items.Count > 1 && SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Update))
				{
					// check that items depends just on update table
					//
					var isComplex = false;
					foreach (var item in updateStatement.Update.Items)
					{
						var usedSources = new HashSet<ISqlTableSource>();
						QueryHelper.GetUsedSources(item.Expression!, usedSources);
						usedSources.Remove(updateStatement.Update.Table);
						if (replaceTree?.TryGetValue(updateStatement.Update.Table, out var replaced) == true)
							usedSources.Remove((ISqlTableSource)replaced);

						if (usedSources.Count > 0)
						{
							isComplex = true;
							break;
						}
					}

					if (isComplex)
					{
						// generating Row constructor update

						processUniversalUpdate = false;

						var innerQuery = CloneQuery(clonedQuery, updateStatement.Update.Table, out var innerTree);
						innerQuery.Select.Columns.Clear();

						var rows = new List<(ISqlExpression, ISqlExpression)>(updateStatement.Update.Items.Count);
						foreach (var item in updateStatement.Update.Items)
						{
							if (item.Expression == null)
								continue;

							var ex = RemapCloned(item.Expression, replaceTree, innerTree);

							var newUpdateExpression = innerQuery.Select.AddNewColumn(ex);

							if (newUpdateExpression == null)
								throw new InvalidOperationException(
									$"Could not create column for expression '{item.Expression}'");

							rows.Add((item.Column, newUpdateExpression));
						}

						var sqlRow        = new SqlRow(rows.Select(r => r.Item1).ToArray());
						var newUpdateItem = new SqlSetExpression(sqlRow, innerQuery);

						newUpdateStatement.Update.Items.Clear();
						newUpdateStatement.Update.Items.Add(newUpdateItem);
					}
				}

				if (processUniversalUpdate)
				{
					foreach (var item in updateStatement.Update.Items)
					{
						if (item.Expression == null)
							continue;

						var usedSources = new HashSet<ISqlTableSource>();

						var ex = item.Expression;

						QueryHelper.GetUsedSources(ex, usedSources);
						usedSources.Remove(updateStatement.Update.Table);

						if (usedSources.Count > 0)
						{
							// it means that update value column depends on other tables and we have to generate more complicated query

							var innerQuery = CloneQuery(clonedQuery, updateStatement.Update.Table, out var iterationTree);

							ex = RemapCloned(ex, replaceTree, iterationTree);

							innerQuery.ParentSelect = sql;

							innerQuery.Select.Columns.Clear();

							innerQuery.Select.AddNew(ex);
							innerQuery.RemoveNotUnusedColumns();

							ex = innerQuery;
						}
						else
						{
							ex = RemapCloned(ex, replaceTree, null);
						}

						item.Expression = ex;
						newUpdateStatement.Update.Items.Add(item);
					}
				}

				if (updateStatement.Output != null)
				{
					newUpdateStatement.Output = RemapCloned(updateStatement.Output, replaceTree, null);
				}

				newUpdateStatement.Update.Table = updateStatement.Update.Table;
				newUpdateStatement.With         = updateStatement.With;

				clonedQuery.RemoveNotUnusedColumns();
				clonedQuery.OptimizeSelectQuery(updateStatement, SqlProviderFlags, dataOptions);
				newUpdateStatement.SelectQuery.Where.Exists(clonedQuery);

				updateStatement.Update.Items.Clear();

				updateStatement = newUpdateStatement;
			}

			var (tableSource, _) = FindTableSource(new Stack<IQueryElement>(), updateStatement.SelectQuery, updateStatement.Update.Table!);

			if (tableSource == null)
			{
				CorrectUpdateSetters(updateStatement);
			}

			return updateStatement;
		}

		protected static void CorrectUpdateSetters(SqlUpdateStatement updateStatement)
		{
			// remove current column wrapping
			foreach (var item in updateStatement.Update.Items)
			{
				if (item.Expression == null)
					continue;

				item.Expression = item.Expression.Convert(updateStatement.SelectQuery, (v, e) =>
				{
					if (e is SqlColumn column && column.Parent == v.Context)
						return column.Expression;
					return e;
				});
			}
		}

		void ReplaceTable(ISqlExpressionWalkable? element, SqlTable replacing, SqlTable withTable)
		{
			element?.Walk(WalkOptions.Default, (replacing, withTable), static (ctx, e) =>
			{
				if (e is SqlField field && field.Table == ctx.replacing)
					return ctx.withTable.FindFieldByMemberName(field.Name) ?? throw new LinqException($"Field {field.Name} not found in table {ctx.withTable}");

				return e;
			});
		}

		protected SqlTable? FindUpdateTable(SelectQuery selectQuery, SqlTable tableToFind)
		{
			foreach (var tableSource in selectQuery.From.Tables)
			{
				if (tableSource.Source is SqlTable table && QueryHelper.IsEqualTables(table, tableToFind))
				{
					return table;
				}
			}

			foreach (var tableSource in selectQuery.From.Tables)
			{
				foreach (var join in tableSource.Joins)
				{
					if (join.Table.Source is SqlTable table)
					{
						if (QueryHelper.IsEqualTables(table, tableToFind))
							return table;
					}
					else if (join.Table.Source is SelectQuery query)
					{
						var found = FindUpdateTable(query, tableToFind);
						if (found != null)
							return found;
					}
				}
			}

			return null;
		}

		class DetachUpdateTableVisitor : SqlQueryVisitor
		{
			SqlUpdateStatement? _updateStatement;
			SqlTable?           _newTable;

			public DetachUpdateTableVisitor(): base(VisitMode.Modify)
			{
			}

			public override IQueryElement VisitSqlTableSource(SqlTableSource element)
			{
				if (element.Source == _updateStatement?.Update.Table)
				{
					if (_newTable == null)
					{
						var objectTree = new Dictionary<IQueryElement, IQueryElement>();
						_newTable = _updateStatement.Update.Table.Clone(objectTree);
						AddReplacements(objectTree);

						element.Source =_newTable!;
					}
				}

				return base.VisitSqlTableSource(element);
			}

			public override IQueryElement VisitSqlUpdateClause(SqlUpdateClause element)
			{
				// Do nothing
				return element;
			}

			public override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
			{
				// Do nothing
				return element;
			}

			/*
			public override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
			{
				// Do nothing
				return expression;
			}

			*/
			public SqlTable? DetachUpdateTable(SqlUpdateStatement updateStatement)
			{
				_newTable        = null;
				_updateStatement = updateStatement;

				Visit(updateStatement);

				return _newTable;
			}
		}

		protected SqlUpdateStatement DetachUpdateTableFromUpdateQuery(SqlUpdateStatement updateStatement, DataOptions dataOptions)
		{
			var updateTable = updateStatement.Update.Table;
			if (updateTable == null)
				throw new InvalidOperationException();

			// correct columns
			foreach (var item in updateStatement.Update.Items)
			{
				if (item.Column is SqlColumn column)
				{
					var field = QueryHelper.GetUnderlyingField(column.Expression);
					if (field == null)
						throw new InvalidOperationException("Expression {column.Expression} cannot be used for update field");
					item.Column = field;
				}
			}

			var visitor     = new DetachUpdateTableVisitor();
			var clonedTable = visitor.DetachUpdateTable(updateStatement);

			ApplyUpdateTableComparison(updateStatement.SelectQuery, updateStatement.Update, clonedTable, dataOptions);

			return updateStatement;
		}

		protected SqlStatement GetAlternativeUpdatePostgreSqlite(SqlUpdateStatement statement, DataOptions dataOptions)
		{
			if (statement.SelectQuery.Select.HasModifier)
			{
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery, allowMutation: true);
			}

			var tableToUpdate  = statement.Update.Table!;

			var hasUpdateTableInQuery = QueryHelper.HasTableInQuery(statement.SelectQuery, tableToUpdate);

			if (hasUpdateTableInQuery)
			{
				if (RemoveUpdateTableIfPossible(statement.SelectQuery, tableToUpdate, out _))
					hasUpdateTableInQuery = false;
			}

			if (hasUpdateTableInQuery)
			{
				statement = DetachUpdateTableFromUpdateQuery(statement, dataOptions);
			}

			CorrectUpdateSetters(statement);

			statement.Update.Table = tableToUpdate;

			return statement;
		}

		/// <summary>
		/// Corrects situation when update table is located in JOIN clause.
		/// Usually it is generated by associations.
		/// </summary>
		/// <param name="statement">Statement to examine.</param>
		/// <returns>Corrected statement.</returns>
		protected SqlUpdateStatement CorrectUpdateTable(SqlUpdateStatement statement, DataOptions dataOptions)
		{
			statement = BasicCorrectUpdate(statement, dataOptions, false);
			var tableToUpdate = statement.Update.Table;
			if (tableToUpdate != null)
			{
				var firstTable = statement.SelectQuery.From.Tables[0];

				if (firstTable.Source != tableToUpdate)
				{
					SqlTableSource? removedTableSource = null;

					if (QueryHelper.HasTableInQuery(statement.SelectQuery, tableToUpdate))
					{
						if (!RemoveUpdateTableIfPossible(statement.SelectQuery, tableToUpdate, out removedTableSource))
						{
							statement = DetachUpdateTableFromUpdateQuery(statement, dataOptions);
						}
					}

					var ts = removedTableSource ?? new SqlTableSource(tableToUpdate,
						QueryHelper.SuggestTableSourceAlias(statement.SelectQuery, "u"));

					statement.SelectQuery.From.Tables.Insert(0, ts);
				}

				statement.Update.TableSource = statement.SelectQuery.From.Tables[0];
			}

			CorrectUpdateSetters(statement);

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
			statement.Visit(maxLen, static (maxLen, e) =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField     : ((SqlField)      e).Alias = SetAlias(((SqlField)      e).Alias, maxLen); break;
					case QueryElementType.SqlTable     : ((SqlTable)      e).Alias = SetAlias(((SqlTable)      e).Alias, maxLen); break;
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
			((ISqlExpressionWalkable) statement).Walk(WalkOptions.Default, statement, static (statement, element) =>
			{
				if (element is SelectQuery query)
					new JoinOptimizer().OptimizeJoins(statement, query);
				return element;
			});
		}

		#endregion

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

		public virtual bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element)
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
				case QueryElementType.MultiInsertStatement:
				{
					var statement = (SqlStatement)element;
					return statement.IsParameterDependent;
				}
				case QueryElementType.SqlValuesTable:
				{
					return ((SqlValuesTable)element).Rows == null;
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
					return element.IsMutable();
				}
				case QueryElementType.ExprPredicate:
				{
					var exprExpr = (SqlPredicate.Expr)element;

					if (exprExpr.Expr1.IsMutable())
						return true;
					return false;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var exprExpr = (SqlPredicate.ExprExpr)element;

					var isMutable1 = exprExpr.Expr1.IsMutable();
					var isMutable2 = exprExpr.Expr2.IsMutable();

					if (isMutable1 && isMutable2)
						return true;

					if (isMutable1 && exprExpr.Expr2.CanBeEvaluated(false))
						return true;

					if (isMutable2 && exprExpr.Expr1.CanBeEvaluated(false))
						return true;

					if (isMutable1 && exprExpr.Expr1.ShouldCheckForNull(nullability))
						return true;

					if (isMutable2 && exprExpr.Expr2.ShouldCheckForNull(nullability))
						return true;

					return false;
				}
				case QueryElementType.IsDistinctPredicate:
				{
					var expr = (SqlPredicate.IsDistinct)element;
					return expr.Expr1.IsMutable() || expr.Expr2.IsMutable();
				}
				case QueryElementType.IsTruePredicate:
				{
					var isTruePredicate = (SqlPredicate.IsTrue)element;

					if (isTruePredicate.Expr1.IsMutable())
						return true;
					return false;
				}
				case QueryElementType.InListPredicate:
				{
					return true;
				}
				case QueryElementType.SearchStringPredicate:
				{
					var searchString = (SqlPredicate.SearchString)element;
					if (searchString.Expr2.ElementType != QueryElementType.SqlValue)
						return true;

					return IsParameterDependedElement(nullability, searchString.CaseSensitive);
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

		public bool IsParameterDependent(NullabilityContext nullability, SqlStatement statement)
		{
			return null != statement.Find((optimizer : this, nullability),
				static (ctx, e) => ctx.optimizer.IsParameterDependedElement(ctx.nullability, e));
		}

		public virtual SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions)
		{
			var newStatement = TransformStatement(statement, dataOptions);
			newStatement = FinalizeUpdate(newStatement, dataOptions);

			if (SqlProviderFlags.IsParameterOrderDependent)
			{
				// ensure that parameters in expressions are well sorted
				newStatement = NormalizeExpressions(newStatement, context.ParameterValues == null);
			}

			return newStatement;
		}

		public SqlStatement OptimizeAggregates(SqlStatement statement)
		{
			var newStatement = QueryHelper.JoinRemoval(statement, statement, static (statement, currentStatement, join) =>
			{
				if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply)
				{
					if (join.Table.Source is SelectQuery query && query.Select.Columns.Count > 0)
					{
						var isAggregateQuery =
							query.Select.Columns.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));
						if (isAggregateQuery)
						{
							// remove unwanted join
							if (!QueryHelper.IsDependsOnSources(statement, new HashSet<ISqlTableSource> { query },
								new HashSet<IQueryElement> { join }))
								return true;
						}
					}
				}

				return false;
			});

			return newStatement;
		}

		public virtual void ConvertSkipTake(NullabilityContext nullability, MappingSchema mappingSchema, DataOptions dataOptions,
			SelectQuery selectQuery, OptimizationContext optimizationContext, out ISqlExpression? takeExpr,
			out ISqlExpression? skipExpr)
		{
			// make skip take as parameters or evaluate otherwise

			takeExpr = ConvertElement(mappingSchema, dataOptions, selectQuery.Select.TakeValue, optimizationContext, nullability) as ISqlExpression;
			skipExpr = ConvertElement(mappingSchema, dataOptions, selectQuery.Select.SkipValue, optimizationContext, nullability) as ISqlExpression;

			if (takeExpr != null)
			{
				var supportsParameter = SqlProviderFlags.GetAcceptsTakeAsParameterFlag(selectQuery);

				if (supportsParameter)
				{
					if (takeExpr.ElementType != QueryElementType.SqlParameter && takeExpr.ElementType != QueryElementType.SqlValue)
					{
						var takeValue = takeExpr.EvaluateExpression(optimizationContext.Context)!;
						var takeParameter = new SqlParameter(new DbDataType(takeValue.GetType()), "take", takeValue)
						{
							IsQueryParameter = dataOptions.LinqOptions.ParameterizeTakeSkip && !QueryHelper.NeedParameterInlining(takeExpr)
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
					if (skipExpr.ElementType != QueryElementType.SqlParameter && skipExpr.ElementType != QueryElementType.SqlValue)
					{
						var skipValue = skipExpr.EvaluateExpression(optimizationContext.Context)!;
						var skipParameter = new SqlParameter(new DbDataType(skipValue.GetType()), "skip", skipValue)
						{
							IsQueryParameter = dataOptions.LinqOptions.ParameterizeTakeSkip && !QueryHelper.NeedParameterInlining(skipExpr)
						};
						skipExpr = skipParameter;
					}
				}
				else if (skipExpr.ElementType != QueryElementType.SqlValue)
					skipExpr = new SqlValue(skipExpr.EvaluateExpression(optimizationContext.Context)!);
			}
		}

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
			return QueryHelper.WrapQuery(
				queryFilter,
				statement,
				static (queryFilter, q, _) => q.Select.IsDistinct && queryFilter(q),
				static (_, p, q) =>
				{
					p.Select.SkipValue = q.Select.SkipValue;
					p.Select.Take(q.Select.TakeValue, q.Select.TakeHints);

					q.Select.SkipValue = null;
					q.Select.Take(null, null);

					QueryHelper.MoveOrderByUp(p, q);
				},
				allowMutation: true,
				withStack: false);
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
			return ReplaceTakeSkipWithRowNumber(
				onlySubqueries,
				statement,
				static (onlySubqueries, query) => onlySubqueries && query.ParentSelect == null,
				supportsEmptyOrderBy);
		}

		/// <summary>
		/// Replaces pagination by Window function ROW_NUMBER().
		/// </summary>
		/// <param name="context"><paramref name="predicate"/> context object.</param>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="supportsEmptyOrderBy">Indicates that database supports OVER () syntax.</param>
		/// <param name="predicate">Indicates when the transformation is needed</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNumber<TContext>(TContext context, SqlStatement statement, Func<TContext, SelectQuery, bool> predicate, bool supportsEmptyOrderBy)
		{
			return QueryHelper.WrapQuery(
				(predicate, context, supportsEmptyOrderBy),
				statement,
				static (context, query, _) =>
				{
					if ((query.Select.TakeValue == null || query.Select.TakeHints != null) && query.Select.SkipValue == null)
						return 0;
					return context.predicate(context.context, query) ? 1 : 0;
				},
				static (context, queries) =>
				{
					var query = queries[queries.Count - 1];
					var processingQuery = queries[queries.Count - 2];

					IReadOnlyCollection<SqlOrderByItem>? orderByItems = null;
					if (!query.OrderBy.IsEmpty)
						orderByItems = query.OrderBy.Items;
					//else if (query.Select.Columns.Count > 0)
					//{
					//	orderByItems = query.Select.Columns
					//		.Select(static c => QueryHelper.NeedColumnForExpression(query, c, false))
					//		.Where(static e => e != null)
					//		.Take(1)
					//		.Select(static e => new SqlOrderByItem(e, false))
					//		.ToArray();
					//}

					if (orderByItems == null || orderByItems.Count == 0)
						orderByItems = context.supportsEmptyOrderBy ? Array<SqlOrderByItem>.Empty : new[] { new SqlOrderByItem(new SqlExpression("SELECT NULL"), false) };

					var orderBy = string.Join(", ",
						orderByItems.Select(static (oi, i) => oi.IsDescending ? $"{{{i}}} DESC" : $"{{{i}}}"));

					var parameters = orderByItems.Select(static oi => oi.Expression).ToArray();

					// careful here - don't clear it before orderByItems used
					query.OrderBy.Items.Clear();

					var rowNumberExpression = parameters.Length == 0
						? new SqlExpression(typeof(long), "ROW_NUMBER() OVER ()", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null)
						: new SqlExpression(typeof(long), $"ROW_NUMBER() OVER (ORDER BY {orderBy})", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters);

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

				},
				allowMutation: true,
				withStack: false);
		}

		/// <summary>
		/// Alternative mechanism how to prevent loosing sorting in Distinct queries.
		/// </summary>
		/// <param name="statement">Statement which may contain Distinct queries.</param>
		/// <param name="queryFilter">Query filter predicate to determine if query needs processing.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceDistinctOrderByWithRowNumber(SqlStatement statement, Func<SelectQuery, bool> queryFilter)
		{
			return QueryHelper.WrapQuery(
				queryFilter,
				statement,
				static (queryFilter, q, _) => (q.Select.IsDistinct && !q.Select.OrderBy.IsEmpty && queryFilter(q)) /*|| q.Select.TakeValue != null || q.Select.SkipValue != null*/,
				static (_, p, q) =>
				{
					var columnItems  = q.Select.Columns.Select(static c => c.Expression).ToList();
					var orderItems   = q.Select.OrderBy.Items.Select(static o => o.Expression).ToList();

					var projectionItemsCount = columnItems.Union(orderItems).Count();
					if (projectionItemsCount < columnItems.Count)
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

						var partitionBy = string.Join(", ", columnItems.Select(static (oi, i) => $"{{{i}}}"));

						var columns = new string[orderByItems.Count];
						for (var i = 0; i < columns.Length; i++)
							columns[i] = orderByItems[i].IsDescending
								? $"{{{i + columnItems.Count}}} DESC"
								: $"{{{i + columnItems.Count}}}";
						var orderBy = string.Join(", ", columns);

						var parameters = columnItems.Concat(orderByItems.Select(static oi => oi.Expression)).ToArray();

						var rnExpr = new SqlExpression(typeof(long),
							$"ROW_NUMBER() OVER (PARTITION BY {partitionBy} ORDER BY {orderBy})", Precedence.Primary,
							SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters);

						var additionalProjection = orderItems.Except(columnItems);
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
				},
				allowMutation: true,
				withStack: false);
		}

		#region Helper functions

		protected static ISqlExpression TryConvertToValue(ISqlExpression expr, EvaluationContext context)
		{
			if (expr.ElementType != QueryElementType.SqlValue)
			{
				if (expr.TryEvaluateExpression(context, out var value))
					expr = new SqlValue(expr.GetExpressionType(), value);
			}

			return expr;
		}

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
			if (func.Name == "CASE")
			{
				ISqlExpression[]? parameters = null;
				for (var i = 0; i < func.Parameters.Length; i++)
				{
					var p = func.Parameters[i];
					if (IsBooleanParameter(p, func.Parameters.Length, i))
					{
						if (parameters == null)
						{
							parameters = new ISqlExpression[func.Parameters.Length];
							for (var j = 0; j < i; j++)
								parameters[j] = func.Parameters[j];
						}
						parameters[i] = new SqlFunction(typeof(bool), "CASE", p, new SqlValue(true), new SqlValue(false))
						{
							CanBeNull     = false,
							DoNotOptimize = true
						};
					}
					else if (parameters != null)
						parameters[i] = p;
				}

				if (parameters != null)
					return new SqlFunction(
						func.SystemType,
						func.Name,
						false,
						func.Precedence,
						parameters);
			}

			return func;
		}

		#endregion
	}
}
