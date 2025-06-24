using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Expressions.ExpressionVisitors;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

// ReSharper disable InconsistentNaming

namespace LinqToDB.SqlProvider
{
	public class BasicSqlOptimizer : ISqlOptimizer
	{
		#region Init

		protected BasicSqlOptimizer(SqlProviderFlags sqlProviderFlags)
		{
			SqlProviderFlags = sqlProviderFlags;
		}

		protected SqlProviderFlags SqlProviderFlags { get; }

		public virtual bool RequiresCastingParametersForSetOperations => true;

		#endregion

		#region ISqlOptimizer Members

		public virtual SqlExpressionOptimizerVisitor CreateOptimizerVisitor(bool allowModify)
		{
			return new SqlExpressionOptimizerVisitor(allowModify);
		}

		public virtual SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlExpressionConvertVisitor(allowModify);
		}

		public virtual SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			FixEmptySelect(statement);
			FinalizeCte   (statement);

			var evaluationContext = new EvaluationContext(null);

			statement = (SqlStatement)OptimizeQueries(statement, statement, dataOptions, mappingSchema, evaluationContext);

			if (dataOptions.LinqOptions.OptimizeJoins)
			{
				statement = new JoinsOptimizer().Optimize(statement, evaluationContext);

				// Do it again after JOIN Optimization
				FinalizeCte(statement);
			}

			statement = FinalizeInsert(statement);
			statement = FinalizeSelect(statement);
			statement = CorrectUnionOrderBy(statement);
			statement = FixSetOperationValues(mappingSchema, statement);

			// provider specific query correction
			statement = FinalizeStatement(statement, evaluationContext, dataOptions, mappingSchema);

//statement.EnsureFindTables();

			return statement;
		}

		#endregion

		protected virtual SqlStatement FinalizeInsert(SqlStatement statement)
		{
			if (statement is SqlInsertStatement insertStatement)
			{
				var tables = insertStatement.SelectQuery.From.Tables;
				var isSelfInsert =
					tables.Count     == 0 ||
					tables.Count     == 1 &&
					tables[0].Source == insertStatement.Insert.Into;

				if (isSelfInsert)
				{
					if (insertStatement.SelectQuery.IsSimple || insertStatement.SelectQuery.From.Tables.Count == 0)
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
			return joinedTable.JoinType is JoinType.Inner or JoinType.Left or JoinType.Right;
		}

		static bool IsCompatibleForUpdate(List<IQueryElement> path)
		{
			if (path.Count > 2)
				return false;

			var result = path.All(e =>
			{
				return e switch
				{
					SelectQuery sc    => IsCompatibleForUpdate(sc),
					SqlJoinedTable jt => IsCompatibleForUpdate(jt),
					_                 => true,
				};
			});

			return result;
		}

		protected static bool IsCompatibleForUpdate(SelectQuery query, SqlTable updateTable, int level = 0)
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

		static ISqlExpression? PopulateNesting(List<SelectQuery> queryPath, ISqlExpression expression, int ignoreCount)
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

		protected static void ApplyUpdateTableComparison(SqlSearchCondition searchCondition, SelectQuery updateQuery,
			SqlUpdateClause updateClause, SqlTable inQueryTable, DataOptions dataOptions)
		{
			var compareKeys = inQueryTable.GetKeys(true);
			var tableKeys   = updateClause.Table!.GetKeys(true);

			var found = false;

			if (tableKeys != null && compareKeys != null)
			{
				for (var i = 0; i < tableKeys.Count; i++)
				{
					var tableKey = tableKeys[i];

					found = true;
					searchCondition.AddEqual(tableKey, compareKeys[i], dataOptions.LinqOptions.CompareNulls);
				}
			}

			if (!found)
				throw new LinqToDBException("Could not generate update statement.");
		}

		protected static void ApplyUpdateTableComparison(SelectQuery updateQuery, SqlUpdateClause updateClause, SqlTable inQueryTable, DataOptions dataOptions)
		{
			ApplyUpdateTableComparison(updateQuery.Where.EnsureConjunction(), updateQuery, updateClause, inQueryTable, dataOptions);
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
						var keys    = statement.Update.Table.GetKeys(true);

						if (!(keys?.Count > 0))
						{
							keys = queries[0].Select.Columns
								.Where(c => c.Expression is SqlField field && field.Table == statement.Update.Table)
								.Select(c => c.Expression)
								.ToList();
						}

						if (keys.Count == 0)
						{
							throw new LinqToDBException("Invalid update query.");
						}

						var keysColumns = new List<ISqlExpression>(keys.Count);
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

						for (var index = 0; index < keys.Count; index++)
						{
							var originalField = keys[index];

							if (!objectMap.TryGetValue(originalField, out var newField))
							{
								throw new InvalidOperationException();
							}

							var originalColumn = keysColumns[index];

							sc.AddEqual((ISqlExpression)newField, originalColumn, dataOptions.LinqOptions.CompareNulls);
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
							statement.SelectQuery.Where.ConcatSearchCondition(sc);
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

		protected virtual SqlStatement FinalizeUpdate(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
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

		protected virtual SqlStatement FinalizeInsertOrUpdate(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (statement is SqlInsertOrUpdateStatement insertOrUpdateStatement)
			{
				// get from columns expression
				//

				insertOrUpdateStatement.Insert.Items.ForEach(item =>
				{
					item.Expression = QueryHelper.SimplifyColumnExpression(item.Expression);
				});

				insertOrUpdateStatement.Update.Items.ForEach(item =>
				{
					item.Expression = QueryHelper.SimplifyColumnExpression(item.Expression);
				});

				CorrectSetters(insertOrUpdateStatement.Insert.Items, insertOrUpdateStatement.SelectQuery);
				CorrectSetters(insertOrUpdateStatement.Update.Items, insertOrUpdateStatement.SelectQuery);
			}

			return statement;
		}

		protected virtual SqlStatement FinalizeSelect(SqlStatement statement)
		{
			var expandVisitor = new SqlRowExpandVisitor();
			expandVisitor.ProcessElement(statement);

			return statement;
		}

		class SqlRowExpandVisitor : SqlQueryVisitor
		{
			SelectQuery? _updateSelect;

			public SqlRowExpandVisitor() : base(VisitMode.Modify, null)
			{
			}

			protected override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
			{
				var newElement = base.VisitSqlSelectClause(element);

				if (!ReferenceEquals(newElement, element))
					return Visit(newElement);

				if (_updateSelect == element.SelectQuery)
					return element;

				// When selecting a SqlRow, expand the row into individual columns.

				for (var i = 0; i < element.Columns.Count; i++)
				{
					var column    = element.Columns[i];
					var unwrapped = QueryHelper.UnwrapNullablity(column.Expression);
					if (unwrapped.ElementType == QueryElementType.SqlRow)
					{
						var row = (SqlRowExpression)unwrapped;
						element.Columns.RemoveAt(i);
						element.Columns.InsertRange(i, row.Values.Select(v => new SqlColumn(element.SelectQuery, v)));
					}
				}

				return element;
			}

			protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
			{
				base.VisitExprExprPredicate(predicate);

				// flip expressions when comparing a row to a query
				if (QueryHelper.UnwrapNullablity(predicate.Expr2).ElementType == QueryElementType.SqlRow && QueryHelper.UnwrapNullablity(predicate.Expr1).ElementType == QueryElementType.SqlQuery)
				{
					var newPredicate = new SqlPredicate.ExprExpr(predicate.Expr2, SqlPredicate.ExprExpr.SwapOperator(predicate.Operator), predicate.Expr1, predicate.UnknownAsValue);
					return newPredicate;
				}

				return predicate;
			}

			protected override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
			{
				var saveUpdateSelect = _updateSelect;
				_updateSelect = element.SelectQuery;

				var result = base.VisitSqlUpdateStatement(element);

				_updateSelect = saveUpdateSelect;
				return result;
			}
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

		static void CorrelateValueTypes(bool castParameters, ref ISqlExpression toCorrect, ISqlExpression reference)
		{
			if (toCorrect.ElementType == QueryElementType.Column)
			{
				var column     = (SqlColumn)toCorrect;
				var columnExpr = column.Expression;
				CorrelateValueTypes(castParameters, ref columnExpr, reference);
				column.Expression = columnExpr;
			}
			else
			{
				var unwrapped = QueryHelper.UnwrapNullablity(toCorrect);
				if (unwrapped.ElementType == QueryElementType.SqlValue)
				{
					var value = (SqlValue)unwrapped;
					if (value.Value == null)
					{
						var suggested = QueryHelper.SuggestDbDataType(reference);
						if (suggested != null)
						{
							toCorrect = new SqlValue(suggested.Value, null);
						}
					}
					else
					{
						var suggested = QueryHelper.SuggestDbDataType(reference);
						if (suggested == null)
							suggested = value.ValueType;
						toCorrect = new SqlCastExpression(value, suggested.Value, null, true);
					}
				}
				else if (castParameters && unwrapped.ElementType == QueryElementType.SqlParameter)
				{
					var parameter = (SqlParameter)unwrapped;
					var suggested = QueryHelper.SuggestDbDataType(reference);
					if (suggested == null)
						suggested = parameter.Type;
					toCorrect = new SqlCastExpression(parameter, suggested.Value, null, true);
				}
			}
		}

		protected virtual SqlStatement FixSetOperationValues(MappingSchema mappingSchema, SqlStatement statement)
		{
			statement.VisitParentFirst(this, static (ctx, e) =>
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

								CorrelateValueTypes(ctx.RequiresCastingParametersForSetOperations, ref columnExpr, otherExpr);
								CorrelateValueTypes(ctx.RequiresCastingParametersForSetOperations, ref otherExpr, columnExpr);

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
		/// <param name="mappingSchema"></param>
		/// <returns></returns>
		public virtual SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			CorrectOutputTables(statement);

			return statement;
		}

		protected virtual void CorrectOutputTables(SqlStatement statement)
		{
			SqlOutputClause CorrectOutputClause(SqlOutputClause output, ISqlTableSource? originalTable)
			{
				var result = output.Convert(1, (_, e) =>
				{
					if (e is SqlAnchor anchor)
					{
						if (anchor.AnchorKind is SqlAnchor.AnchorKindEnum.Inserted or SqlAnchor.AnchorKindEnum.Deleted)
						{
							var resultExpression = anchor.SqlExpression;

							if (anchor is { AnchorKind: SqlAnchor.AnchorKindEnum.Inserted })
							{
								if (QueryHelper.GetUnderlyingField(anchor.SqlExpression) is { } field)
								{
									resultExpression = field;
									if (field.Table != originalTable)
									{
										var newField = (originalTable as SqlTable)?.Fields.FirstOrDefault(f => f.PhysicalName == field.PhysicalName);
										if (newField != null)
										{
											resultExpression = newField;
										}
									}
								}
							}

							return resultExpression;
						}
					}

					return e;
				});
				return result;
			}

			if (!SqlProviderFlags.OutputDeleteUseSpecialTable && statement is SqlDeleteStatement { Output.HasOutput: true } deleteStatement)
			{
				deleteStatement.Output = CorrectOutputClause(deleteStatement.Output, deleteStatement.Table);
			}

			if (!SqlProviderFlags.OutputUpdateUseSpecialTables && statement is SqlUpdateStatement { Output.HasOutput: true } updateStatement)
			{
				updateStatement.Output = CorrectOutputClause(updateStatement.Output, updateStatement.Update.Table);
			}

			if (!SqlProviderFlags.OutputInsertUseSpecialTable && statement is SqlInsertStatement { Output.HasOutput: true } insertStatement)
			{
				insertStatement.Output = CorrectOutputClause(insertStatement.Output, null);
			}

			if (!SqlProviderFlags.OutputMergeUseSpecialTables && statement is SqlMergeStatement { Output.HasOutput: true } mergeStatement)
			{
				mergeStatement.Output = CorrectOutputClause(mergeStatement.Output, mergeStatement.Target);
			}
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
							newExpression = new SqlExpression(expr.Type, newExpr, expr.Precedence, expr.Flags, expr.NullabilityType, null, newExpressions.ToArray());

						return newExpression;
					}
				}

				return e;
			});

			return result;
		}

		#region Alternative Builders

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

				var copy      = new SqlTable(table) { Alias = null };
				var tableKeys = table.GetKeys(true);
				var copyKeys  = copy. GetKeys(true);

				var wsc = deleteStatement.SelectQuery.Where.EnsureConjunction();

				if (copyKeys == null || tableKeys == null)
				{
					throw new LinqToDBException("Could not generate comparison between tables.");
				}

				for (var i = 0; i < tableKeys.Count; i++)
					wsc.AddEqual(copyKeys[i], tableKeys[i], CompareNulls.LikeSql);

				newDeleteStatement.SelectQuery.From.Table(copy).Where.SearchCondition.AddExists(deleteStatement.SelectQuery);
				newDeleteStatement.With = deleteStatement.With;

				deleteStatement = newDeleteStatement;
			}

			return deleteStatement;
		}

		protected bool NeedsEnvelopingForUpdate(SelectQuery query)
		{
			if (query.Select.HasModifier || !query.GroupBy.IsEmpty)
				return true;

			if (!query.Where.IsEmpty)
			{
				if (QueryHelper.ContainsAggregationFunction(query.Where))
					return true;
			}

			return false;
		}

		bool MoveConditions(SqlTable table,
			IReadOnlyCollection<ISqlTableSource> currentSources,
			SqlSearchCondition       source,
			SqlSearchCondition       destination,
			SqlSearchCondition       common)
		{
			if (source.IsOr)
				return false;

			List<ISqlPredicate>? predicatesForDestination = null;
			List<ISqlPredicate>? predicatesCommon         = null;

			ISqlTableSource[] tableSources = { (ISqlTableSource)table };

			foreach (var p in source.Predicates)
			{
				if (QueryHelper.IsDependsOnOuterSources(p, currentSources : currentSources) &&
				    QueryHelper.IsDependsOnSources(p, tableSources))
				{
					predicatesForDestination ??= new();
					predicatesForDestination.Add(p);
				}
				else
				{
					predicatesCommon ??= new();
					predicatesCommon.Add(p);
				}
			}

			if (predicatesForDestination != null)
			{
				if (destination.IsOr)
					return false;
			}

			if (predicatesCommon != null)
			{
				if (common.IsOr)
					return false;
			}

			if (predicatesForDestination != null)
			{
				destination.AddRange(predicatesForDestination);
				foreach(var p in predicatesForDestination)
					source.Predicates.Remove(p);
			}

			if (predicatesCommon != null)
			{
				common.AddRange(predicatesCommon);
				foreach(var p in predicatesCommon)
					source.Predicates.Remove(p);
			}

			return true;
		}

		protected bool RemoveUpdateTableIfPossible(SelectQuery query, SqlTable table, out SqlTableSource? source)
		{
			source = null;

			if (query.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported) ||
				!query.GroupBy.IsEmpty)
				return false;

			if (table.SqlQueryExtensions?.Count > 0)
				return false;

			for (var i = 0; i < query.From.Tables.Count; i++)
			{
				var ts = query.From.Tables[i];
				if (ts.Joins.All(j => j.JoinType is JoinType.Inner or JoinType.Left or JoinType.Cross))
				{
					if (ts.Source == table)
					{
						source = ts;

						query.From.Tables.RemoveAt(i);
						for (var j = 0; j < ts.Joins.Count; j++)
						{
							query.From.Tables.Insert(i + j, ts.Joins[j].Table);
							query.Where.ConcatSearchCondition(ts.Joins[j].Condition);
						}

						source.Joins.Clear();

						return true;
					}

					for (var j = 0; j < ts.Joins.Count; j++)
					{
						var join = ts.Joins[j];
						if (join.Table.Source == table)
						{
							if (ts.Joins.Skip(j + 1).Any(sj => QueryHelper.IsDependsOnSource(sj, table)))
								return false;

							source = join.Table;

							ts.Joins.RemoveAt(j);
							query.Where.ConcatSearchCondition(join.Condition);

							for (var sj = 0; j < join.Table.Joins.Count; j++)
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
				return e switch
				{
					SqlTable table when table       == ut => false,
					SqlField field when field.Table == ut => false,
					_ => true,
				};
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

		static IEnumerable<(ISqlExpression target, ISqlExpression source, SelectQuery? query)> GenerateRows(
			ISqlExpression                            target,
			ISqlExpression                            source)
		{
			if (target is SqlRowExpression targetRow)
			{
				if (source is SqlRowExpression sourceRow)
				{
					if (targetRow.Values.Length != sourceRow.Values.Length)
						throw new InvalidOperationException("Target and Source SqlRows are different");

					for (var i = 0; i < targetRow.Values.Length; i++)
					{
						var targetRowValue = targetRow.Values[i];
						var sourceRowValue = sourceRow.Values[i];

						foreach (var r in GenerateRows(targetRowValue, sourceRowValue))
							yield return r;
					}

					yield break;
				}
				else if (source is SqlColumn { Expression: SelectQuery selectQuery })
				{
					for (var i = 0; i < targetRow.Values.Length; i++)
					{
						var targetRowValue = targetRow.Values[i];
						var sourceRowValue = selectQuery.Select.Columns[i].Expression;

						foreach (var r in GenerateRows(targetRowValue, sourceRowValue))
							yield return (r.target, r.source, selectQuery);
					}

					yield break;
				}
			}

			yield return (target, source, null);
		}

		static IEnumerable<(ISqlExpression, ISqlExpression)> GenerateRows(
			ISqlExpression                            target,
			ISqlExpression                            source,
			Dictionary<IQueryElement, IQueryElement>? mainTree,
			Dictionary<IQueryElement, IQueryElement>? innerTree,
			SelectQuery                               selectQuery)
		{
			if (target is SqlRowExpression targetRow && source is SqlRowExpression sourceRow)
			{
				if (targetRow.Values.Length != sourceRow.Values.Length)
					throw new InvalidOperationException("Target and Source SqlRows are different");

				for (var i = 0; i < targetRow.Values.Length; i++)
				{
					var tagetRowValue  = targetRow.Values[i];
					var sourceRowValue = sourceRow.Values[i];

					foreach (var r in GenerateRows(tagetRowValue, sourceRowValue, mainTree, innerTree, selectQuery))
						yield return r;
				}
			}
			else
			{
				var ex         = RemapCloned(source, mainTree, innerTree);
				var columnExpr = selectQuery.Select.AddNewColumn(ex);

				yield return (target, columnExpr);
			}
		}

		protected SqlUpdateStatement GetAlternativeUpdate(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException();

			if (!updateStatement.SelectQuery.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported)
				&& updateStatement.SelectQuery.From.Tables.Count == 1)
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

			SelectQuery?                              clonedQuery = null;
			Dictionary<IQueryElement, IQueryElement>? replaceTree = null;

			var needsComparison = !updateStatement.Update.HasComparison;

			CorrectUpdateSetters(updateStatement);

			if (NeedsEnvelopingForUpdate(updateStatement.SelectQuery))
			{
				updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, allowMutation : true);
			}

			needsComparison = false;

			if (!needsComparison)
			{
				// clone earlier, we need table before remove
				clonedQuery = CloneQuery(updateStatement.SelectQuery, null, out replaceTree);

				// trying to simplify query
				RemoveUpdateTableIfPossible(updateStatement.SelectQuery, updateStatement.Update.Table!, out _);
			}

			// It covers subqueries also. Simple subquery will have sourcesCount == 2
			if (QueryHelper.EnumerateAccessibleTableSources(updateStatement.SelectQuery).Any())
			{
				var sql = new SelectQuery { IsParameterDependent = updateStatement.IsParameterDependent  };

				var newUpdateStatement = new SqlUpdateStatement(sql);

				if (clonedQuery == null)
					clonedQuery = CloneQuery(updateStatement.SelectQuery, null, out replaceTree);

				SqlTable? tableToCompare = null;
				if (replaceTree!.TryGetValue(updateStatement.Update.Table!, out var newTable))
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
						if (item.Column is SqlRowExpression)
							continue;

						var usedSources = new HashSet<ISqlTableSource>();
						QueryHelper.GetUsedSources(item.Expression!, usedSources);
						usedSources.Remove(updateStatement.Update.Table!);
						if (replaceTree?.TryGetValue(updateStatement.Update.Table!, out var replaced) == true)
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

							rows.AddRange(GenerateRows(item.Column, item.Expression, replaceTree, innerTree, innerQuery));
						}

						var sqlRow        = new SqlRowExpression(rows.Select(r => r.Item1).ToArray());
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
						usedSources.Remove(updateStatement.Update.Table!);

						if (usedSources.Count > 0)
						{
							// it means that update value column depends on other tables and we have to generate more complicated query

							var innerQuery = CloneQuery(clonedQuery, updateStatement.Update.Table, out var iterationTree);

							ex = RemapCloned(ex, replaceTree, iterationTree);

							innerQuery.Select.Columns.Clear();

							innerQuery.Select.AddNew(ex);

							ex = innerQuery;
						}
						else
						{
							ex = RemapCloned(ex, replaceTree, null);
						}

						item.Expression = ex;
						newUpdateStatement.Update.Items.Add(item);
					}

					foreach (var setExpression in newUpdateStatement.Update.Items)
					{
						var column = setExpression.Column;
						if (column is SqlRowExpression)
							continue;

						var field = QueryHelper.GetUnderlyingField(column);
						if (field == null)
							throw new LinqToDBException($"Expression {column} cannot be used for update field");

						setExpression.Column = field;
					}
				}

				if (updateStatement.Output != null)
				{
					newUpdateStatement.Output = RemapCloned(updateStatement.Output, replaceTree, null);
				}

				newUpdateStatement.Update.Table = updateStatement.Update.Table;
				newUpdateStatement.With         = updateStatement.With;

				newUpdateStatement.SelectQuery.Where.SearchCondition.AddExists(clonedQuery);

				updateStatement.Update.Items.Clear();

				updateStatement = newUpdateStatement;

				OptimizeQueries(updateStatement, updateStatement, dataOptions, mappingSchema, new EvaluationContext());
			}

			var (tableSource, _) = FindTableSource(new Stack<IQueryElement>(), updateStatement.SelectQuery, updateStatement.Update.Table!);

			if (tableSource == null)
			{
				CorrectUpdateSetters(updateStatement);
			}

			return updateStatement;
		}

		protected void CorrectSetters(List<SqlSetExpression> setters, SelectQuery query)
		{
			// remove current column wrapping
			foreach (var item in setters)
			{
				if (item.Expression == null)
					continue;

				item.Expression = item.Expression.Convert(query, (v, e) =>
				{
					if (e is SqlColumn column && column.Parent == v.Context)
					{
						if (QueryHelper.UnwrapNullablity(column.Expression) is SqlRowExpression rowExpression)
						{
							if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.UpdateLiteral))
							{
								var rowSubquery = new SelectQuery();

								foreach (var expr in rowExpression.Values)
								{
									rowSubquery.Select.AddNew(expr);
								}

								return rowSubquery;
							}
						}

						return column.Expression;
					}

					return e;
				});

				if (item.Column is SqlRowExpression && item.Expression is SelectQuery subQuery)
				{
					if (subQuery.Select.Columns is [var column])
					{
						if (column.Expression is SelectQuery { From.Tables: [] } columnQuery)
						{
							subQuery.Select.Columns.Clear();
							foreach (var c in columnQuery.Select.Columns)
							{
								subQuery.Select.AddNew(c.Expression);
							}
						}
						else if (column.Expression is SqlRowExpression rowExpression)
						{
							subQuery.Select.Columns.Clear();
							foreach (var value in rowExpression.Values)
							{
								subQuery.Select.AddNew(value);
							}
						}
					}
				}
			}
		}

		protected void CorrectUpdateSetters(SqlUpdateStatement updateStatement)
		{
			CorrectSetters(updateStatement.Update.Items, updateStatement.SelectQuery);
		}

		protected static SqlUpdateStatement DetachUpdateTableFromUpdateQuery(SqlUpdateStatement updateStatement, DataOptions dataOptions, bool moveToJoin, bool addNewSource, out SqlTableSource newSource)
		{
			var updateTable = updateStatement.Update.Table;
			var alias       = updateStatement.Update.TableSource?.Alias;
			if (updateTable == null)
				throw new InvalidOperationException();

			CorrectUpdateColumns(updateStatement);

			var replacements = new Dictionary<IQueryElement, IQueryElement>();
			var clonedTable = updateTable.Clone(replacements);
			//replacements.Remove(updateTable);

			updateStatement.SelectQuery.Replace(replacements);

			newSource                          = new SqlTableSource(updateTable, alias ?? "u");
			updateStatement.Update.Table       = updateTable;
			updateStatement.Update.TableSource = newSource;

			if (moveToJoin)
			{
				var currentSource = updateStatement.SelectQuery.From.Tables[0];
				var join          = new SqlJoinedTable(JoinType.Inner, currentSource, false);

				updateStatement.SelectQuery.From.Tables.Clear();
				updateStatement.SelectQuery.From.Tables.Add(newSource);

				newSource.Joins.Add(join);

				ApplyUpdateTableComparison(join.Condition, updateStatement.SelectQuery, updateStatement.Update,
					clonedTable, dataOptions);
			}
			else
			{
				if (addNewSource)
				{
					updateStatement.SelectQuery.From.Tables.Insert(0, newSource);
				}

				ApplyUpdateTableComparison(updateStatement.SelectQuery, updateStatement.Update, clonedTable,
					dataOptions);
			}

			return updateStatement;
		}

		static void CorrectUpdateColumns(SqlUpdateStatement updateStatement)
		{
			// correct columns
			foreach (var item in updateStatement.Update.Items)
			{
				if (item.Column is SqlColumn column)
				{
					var field = QueryHelper.GetUnderlyingField(column.Expression);
					if (field == null)
						throw new InvalidOperationException($"Expression {column.Expression} cannot be used for update field");
					item.Column = field;
				}
			}
		}

		protected SqlStatement GetAlternativeUpdatePostgreSqlite(SqlUpdateStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (statement.SelectQuery.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported))
			{
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery, allowMutation: true);
			}

			var tableToUpdate = statement.Update.Table!;
			var tableSource   = statement.Update.TableSource;

			var isModified            = false;
			var hasUpdateTableInQuery = QueryHelper.HasTableInQuery(statement.SelectQuery, tableToUpdate);

			if (hasUpdateTableInQuery)
			{
				if (RemoveUpdateTableIfPossible(statement.SelectQuery, tableToUpdate, out _))
				{
					isModified            = true;
					hasUpdateTableInQuery = false;
				}
			}

			CorrectUpdateSetters(statement);

			if (hasUpdateTableInQuery)
			{
				statement     = DetachUpdateTableFromUpdateQuery(statement, dataOptions, moveToJoin: false, addNewSource: false, out tableSource);
				tableToUpdate = statement.Update.Table!;
				tableSource = null;

				isModified = true;
			}

			if (isModified)
				OptimizeQueries(statement, statement, dataOptions, mappingSchema, new EvaluationContext());

			statement.Update.Table       = tableToUpdate;
			statement.Update.TableSource = tableSource;

			return statement;
		}

		/// <summary>
		/// Corrects situation when update table is located in JOIN clause.
		/// Usually it is generated by associations.
		/// </summary>
		/// <param name="statement">Statement to examine.</param>
		/// <returns>Corrected statement.</returns>
		protected SqlUpdateStatement CorrectUpdateTable(SqlUpdateStatement statement, bool leaveUpdateTableInQuery, DataOptions dataOptions, MappingSchema mappingSchema)
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
							statement = DetachUpdateTableFromUpdateQuery(statement, dataOptions, moveToJoin: false, addNewSource: leaveUpdateTableInQuery, out var newTableSource);
							statement.Update.TableSource = newTableSource;
						}
						else
						{
							statement.Update.TableSource = removedTableSource;
							statement.SelectQuery.From.Tables.Insert(0, removedTableSource!);
						}

						OptimizeQueries(statement, statement, dataOptions, mappingSchema, new EvaluationContext());
					}
					else if (leaveUpdateTableInQuery)
					{
						var ts = new SqlTableSource(tableToUpdate, "u");
						statement.Update.TableSource = ts;
						statement.SelectQuery.From.Tables.Insert(0, ts);
					}
				}
				else
				{
					statement.Update.TableSource = firstTable;
				}
			}

			CorrectUpdateSetters(statement);

			return statement;
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

		public virtual bool IsParameterDependedElement(NullabilityContext nullability, IQueryElement element, DataOptions dataOptions, MappingSchema mappingSchema)
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
					var param = (SqlParameter)element;
					if (!param.IsQueryParameter)
						return true;
					if (param.NeedsCast)
						return true;

					if (param.Type.SystemType.IsNullableType())
						return true;

					return false;
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

					return IsParameterDependedElement(nullability, searchString.CaseSensitive, dataOptions, mappingSchema);
				}
				case QueryElementType.SqlCase:
				{
					var sqlCase = (SqlCaseExpression)element;

					if (sqlCase.Cases.Any(c => c.Condition.CanBeEvaluated(true)))
						return true;

					return false;
				}
				case QueryElementType.SqlCondition:
				{
					var sqlCondition = (SqlConditionExpression)element;

					if (sqlCondition.Condition.CanBeEvaluated(true))
						return true;

					return false;
				}
				case QueryElementType.SqlFunction:
				{
					var sqlFunc = (SqlFunction)element;
					switch (sqlFunc.Name)
					{
						case PseudoFunctions.LENGTH:
						{
							if (sqlFunc.Parameters[0].CanBeEvaluated(true))
								return true;
							break;
						}
					}

					break;
				}
				case QueryElementType.SqlInlinedExpression:
				case QueryElementType.SqlInlinedToSqlExpression:
					return true;
			}

			return false;
		}

		public bool IsParameterDependent(NullabilityContext nullability, MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			return null != statement.Find((optimizer : this, nullability, dataOptions, mappingSchema),
				static (ctx, e) => ctx.optimizer.IsParameterDependedElement(ctx.nullability, e, ctx.dataOptions, ctx.mappingSchema));
		}

		public virtual SqlStatement FinalizeStatement(SqlStatement statement, EvaluationContext context, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			var newStatement = TransformStatement(statement, dataOptions, mappingSchema);
			newStatement = FinalizeUpdate(newStatement, dataOptions, mappingSchema);
			newStatement = FinalizeInsertOrUpdate(newStatement, dataOptions, mappingSchema);

			if (SqlProviderFlags.IsParameterOrderDependent)
			{
				// ensure that parameters in expressions are well sorted
				newStatement = NormalizeExpressions(newStatement, context.ParameterValues == null);
			}

			return newStatement;
		}

		public virtual void ConvertSkipTake(NullabilityContext nullability, MappingSchema mappingSchema, DataOptions dataOptions,
			SelectQuery selectQuery, OptimizationContext optimizationContext, out ISqlExpression? takeExpr,
			out ISqlExpression? skipExpr)
		{
			// make skip take as parameters or evaluate otherwise

			takeExpr = optimizationContext.Optimize(selectQuery.Select.TakeValue, nullability, false);
			skipExpr = optimizationContext.Optimize(selectQuery.Select.SkipValue, nullability, false);

			if (takeExpr != null)
			{
				var supportsParameter = SqlProviderFlags.GetAcceptsTakeAsParameterFlag(selectQuery);

				if (supportsParameter)
				{
					if (takeExpr.ElementType != QueryElementType.SqlParameter && takeExpr.ElementType != QueryElementType.SqlValue)
					{
						var takeValue = takeExpr.EvaluateExpression(optimizationContext.EvaluationContext)!;
						var takeParameter = new SqlParameter(new DbDataType(takeValue.GetType()), "take", takeValue)
						{
							IsQueryParameter = dataOptions.LinqOptions.ParameterizeTakeSkip && !QueryHelper.NeedParameterInlining(takeExpr)
						};
						takeExpr = takeParameter;
					}
				}
				else if (takeExpr.ElementType != QueryElementType.SqlValue)
					takeExpr = new SqlValue(takeExpr.EvaluateExpression(optimizationContext.EvaluationContext)!);
			}

			if (skipExpr != null)
			{
				var supportsParameter = SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery.Select.TakeValue, selectQuery.Select.SkipValue)
										&& SqlProviderFlags.AcceptsTakeAsParameter;

				if (supportsParameter)
				{
					if (skipExpr.ElementType != QueryElementType.SqlParameter && skipExpr.ElementType != QueryElementType.SqlValue)
					{
						var skipValue = skipExpr.EvaluateExpression(optimizationContext.EvaluationContext)!;
						var skipParameter = new SqlParameter(new DbDataType(skipValue.GetType()), "skip", skipValue)
						{
							IsQueryParameter = dataOptions.LinqOptions.ParameterizeTakeSkip && !QueryHelper.NeedParameterInlining(skipExpr)
						};
						skipExpr = skipParameter;
					}
				}
				else if (skipExpr.ElementType != QueryElementType.SqlValue)
					skipExpr = new SqlValue(skipExpr.EvaluateExpression(optimizationContext.EvaluationContext)!);
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
		/// <param name="context"><paramref name="predicate"/> context object.</param>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="supportsEmptyOrderBy">Indicates that database supports OVER () syntax.</param>
		/// <param name="predicate">Indicates when the transformation is needed</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNumber<TContext>(TContext context, SqlStatement statement, Func<TContext, SelectQuery, bool> predicate, bool supportsEmptyOrderBy)
		{
			return QueryHelper.WrapQuery(
				(predicate, context, supportsEmptyOrderBy, statement),
				statement,
				static (context, query, _) =>
				{
					if ((query.Select.TakeValue == null || query.Select.TakeHints != null) && query.Select.SkipValue == null)
						return 0;
					return context.predicate(context.context, query) ? 1 : 0;
				},
				static (context, queries) =>
				{
					if (context.statement.SelectQuery == queries[^1])
					{
						// move orderby to root
						for (var i = queries.Count - 1; i > 0; i--)
						{
							var innerQuery = queries[i];
							var outerQuery = queries[i - 1];
							foreach (var item in innerQuery.Select.OrderBy.Items)
							{
								foreach (var c in innerQuery.Select.Columns)
								{
									if (c.Expression.Equals(item.Expression))
									{
										outerQuery.OrderBy.Items.Add(new SqlOrderByItem(c, item.IsDescending, item.IsPositioned));
										break;
									}
								}
							}
						}

						// cleanup unnecessary intermediate copy to have ordering only on root query
						for (var i = 1; i < queries.Count - 1; i++)
							queries[i].OrderBy.Items.Clear();
					}

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
						orderByItems = context.supportsEmptyOrderBy ? [] : new[] { new SqlOrderByItem(new SqlFragment("(SELECT NULL)"), false, false) };

					var orderBy = string.Join(", ",
						orderByItems.Select(static (oi, i) => oi.IsDescending ? FormattableString.Invariant($"{{{i}}} DESC") : FormattableString.Invariant($"{{{i}}}")));

					var parameters = orderByItems.Select(static oi => oi.Expression).ToArray();

					// careful here - don't clear it before orderByItems used
					query.OrderBy.Items.Clear();

					var rowNumberExpression = parameters.Length == 0
						? new SqlExpression(typeof(long), "ROW_NUMBER() OVER ()", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable)
						: new SqlExpression(typeof(long), $"ROW_NUMBER() OVER (ORDER BY {orderBy})", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, parameters);

					var rowNumberColumn = query.Select.AddNewColumn(rowNumberExpression);
					rowNumberColumn.Alias = "RN";

					if (query.Select.SkipValue != null)
					{
						processingQuery.Where.EnsureConjunction().AddGreater(rowNumberColumn, query.Select.SkipValue, CompareNulls.LikeSql);

						if (query.Select.TakeValue != null)
							processingQuery.Where.SearchCondition.AddLessOrEqual(
								rowNumberColumn,
								new SqlBinaryExpression(
									query.Select.SkipValue.SystemType!,
									query.Select.SkipValue,
									"+",
									query.Select.TakeValue),
								CompareNulls.LikeSql);
					}
					else
					{
						processingQuery.Where.EnsureConjunction().AddLessOrEqual(rowNumberColumn, query.Select.TakeValue!, CompareNulls.LikeSql);
					}

					query.Select.SkipValue = null;
					query.Select.Take(null, null);

				},
				allowMutation: true,
				withStack: false);
		}

		protected IQueryElement OptimizeQueries(IQueryElement startFrom, IQueryElement root, DataOptions dataOptions, MappingSchema mappingSchema, EvaluationContext evaluationContext)
		{
			using var visitor = QueryHelper.SelectOptimizer.Allocate();

#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var sqlText = startFrom.DebugText;

			if (startFrom is SqlSelectStatement statementBefore)
			{

			}
#endif

			var result = visitor.Value.Optimize(startFrom, root, SqlProviderFlags, true, dataOptions, mappingSchema, evaluationContext);

#if DEBUG
			// ReSharper disable once NotAccessedVariable
			var newSqlText = result.DebugText;

			if (startFrom is SqlSelectStatement statementAfter)
			{

			}
#endif

			return result;
		}

		protected SqlStatement CorrectMultiTableQueries(SqlStatement statement)
		{
			var isModified = false;

			statement.Visit(e =>
			{
				if (e.ElementType == QueryElementType.SqlQuery)
				{
					var sqlQuery = (SelectQuery)e;

					if (sqlQuery.From.Tables.Count > 1)
					{
						// if multitable query has joins, we need to move tables to subquery and left joins on the current level
						//
						if (sqlQuery.From.Tables.Any(t => t.Joins.Count > 0))
						{
							var sub = new SelectQuery { DoNotRemove = true };

							sub.From.Tables.AddRange(sqlQuery.From.Tables);

							var restJoins = sqlQuery.From.Tables.SelectMany(t => t.Joins).ToArray();

							sqlQuery.From.Tables.Clear();

							sqlQuery.From.Tables.Add(new SqlTableSource(sub, "sub", restJoins));

							sub.From.Tables.ForEach(t => t.Joins.Clear());

							isModified = true;
						}
					}

					if (SqlProviderFlags.IsCrossJoinSupported)
					{
						var allJoins = sqlQuery.From.Tables.SelectMany(t => t.Joins).ToList();

						if (allJoins.Any(j => j.JoinType == JoinType.Cross) && allJoins.Any(j => j.JoinType != JoinType.Cross))
						{
							var sub = new SelectQuery { DoNotRemove = true };

							sub.From.Tables.AddRange(sqlQuery.From.Tables);
							sub.From.Tables.AddRange(allJoins.Where(j => j.JoinType == JoinType.Cross).Select(j => j.Table));

							sqlQuery.From.Tables.Clear();

							sqlQuery.From.Tables.Add(new SqlTableSource(sub, "sub", allJoins.Where(j => j.JoinType != JoinType.Cross).ToArray()));

							sub.From.Tables.ForEach(t => t.Joins.Clear());

							isModified = true;
						}
					}
				}
			});

			if (isModified)
			{
				var corrector = new SqlQueryColumnNestingCorrector();
				corrector.CorrectColumnNesting(statement);
			}

			return statement;
		}

		public virtual ISqlExpressionFactory CreateSqlExpressionFactory(MappingSchema mappingSchema, DataOptions dataOptions)
			=> new SqlExpressionFactory(mappingSchema, dataOptions);

		#region Visitors
		protected sealed class ClearColumParametersVisitor : SqlQueryVisitor
		{
			bool _disableParameters;

			public ClearColumParametersVisitor() : base(VisitMode.Modify, null)
			{
			}

			protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
			{
				var old            = _disableParameters;
				_disableParameters = true;

				var result         = base.VisitSqlColumnExpression(column, expression);

				_disableParameters = old;

				return result;
			}

			protected override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				if (_disableParameters)
					sqlParameter.IsQueryParameter = false;

				return base.VisitSqlParameter(sqlParameter);
			}
		}
		#endregion

	}
}
