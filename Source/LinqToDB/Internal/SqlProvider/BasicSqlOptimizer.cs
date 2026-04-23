using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

// ReSharper disable InconsistentNaming

namespace LinqToDB.Internal.SqlProvider
{
	public abstract class BasicSqlOptimizer : ISqlOptimizer
	{
		static readonly ObjectPool<CteCollectorVisitor> _cteCollectorVisitorPool = new ObjectPool<CteCollectorVisitor>(() => new CteCollectorVisitor(), v => v.Cleanup(), 100);

		#region Init

		protected BasicSqlOptimizer(SqlProviderFlags sqlProviderFlags)
		{
			SqlProviderFlags = sqlProviderFlags;
		}

		protected SqlProviderFlags SqlProviderFlags { get; }

		public virtual bool RequiresCastingParametersForSetOperations => true;
		public virtual bool RequiresCastingNullValueForSetOperations => false;

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
			statement = FixSetOperationValues(mappingSchema, statement);

			// provider specific query correction
			statement = FinalizeStatement(statement, evaluationContext, dataOptions, mappingSchema);

			return statement;
		}

		#endregion

		protected virtual SqlStatement FinalizeInsert(SqlStatement statement)
		{
			if (statement is SqlInsertStatement insertStatement)
			{
				var tables = insertStatement.SelectQuery.From.Tables;
				var isSelfInsert = tables.Count == 0
					|| (tables.Count == 1 && tables[0].Source == insertStatement.Insert.Into);

				if (isSelfInsert)
				{
					if (insertStatement.SelectQuery is { IsSimple: true } or { From.Tables.Count: 0 })
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

			var result = path.TrueForAll(e =>
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

		protected static bool IsCompatibleForUpdate(SelectQuery query, SqlTable updateTable)
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

		protected static void ApplyUpdateTableComparison(SqlSearchCondition searchCondition,
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
			ApplyUpdateTableComparison(updateQuery.Where.EnsureConjunction(), updateClause, inQueryTable, dataOptions);
		}

		protected virtual SqlUpdateStatement BasicCorrectUpdate(SqlUpdateStatement statement, DataOptions dataOptions, bool wrapForOutput)
		{
			if (statement.Update.Table != null)
			{
				var (tableSource, queryPath) = FindTableSource(new Stack<IQueryElement>(), statement.SelectQuery, statement.Update.Table);

				if (tableSource != null && queryPath != null)
				{
					statement.Update.TableSource = tableSource;

					var forceWrapping = wrapForOutput && statement.Output != null
						&& statement.SelectQuery.From.Tables is [{ Joins.Count: 0 }] or { Count: not 1 };

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
			return statement;
		}

		protected virtual SqlStatement FinalizeInsertOrUpdate(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (statement is SqlInsertOrUpdateStatement insertOrUpdateStatement)
			{
				// get from columns expression
				//

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

		sealed class SqlRowExpandVisitor : SqlQueryVisitor
		{
			SelectQuery? _updateSelect;

			public SqlRowExpandVisitor() : base(VisitMode.Modify, null)
			{
			}

			protected internal override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
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

			protected internal override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
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

			protected internal override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
			{
				var saveUpdateSelect = _updateSelect;
				_updateSelect = element.SelectQuery;

				var result = base.VisitSqlUpdateStatement(element);

				_updateSelect = saveUpdateSelect;
				return result;
			}
		}

		static void CorrelateValueTypes(bool castParameters, bool castNulls, ref ISqlExpression toCorrect, ISqlExpression reference)
		{
			if (toCorrect.ElementType == QueryElementType.Column)
			{
				var column     = (SqlColumn)toCorrect;
				var columnExpr = column.Expression;
				CorrelateValueTypes(castParameters, castNulls, ref columnExpr, reference);
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
							if (castNulls)
								toCorrect = new SqlCastExpression(toCorrect, suggested.Value, null, true);
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

								CorrelateValueTypes(ctx.RequiresCastingParametersForSetOperations, ctx.RequiresCastingNullValueForSetOperations, ref columnExpr, otherExpr);
								CorrelateValueTypes(ctx.RequiresCastingParametersForSetOperations, ctx.RequiresCastingNullValueForSetOperations, ref otherExpr, columnExpr);

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
										var newField = (originalTable as SqlTable)?.Fields.Find(f => string.Equals(f.PhysicalName, field.PhysicalName, StringComparison.Ordinal));
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

		sealed class CteCollectorVisitor : QueryElementVisitor
		{
			public sealed class CteDependencyHolder
			{
				public CteClause           CteClause { get; }
				public HashSet<CteClause>? DependsOn { get; private set; }

				public CteDependencyHolder(CteClause cteClause)
				{
					CteClause = cteClause;
				}

				public bool AddDependency(CteClause cteClause)
				{
					if (ReferenceEquals(CteClause, cteClause))
					{
						CteClause.IsRecursive = true;
						return false;
					}

					DependsOn ??= new HashSet<CteClause>();
					DependsOn.Add(cteClause);

					return true;
				}
			}

			Dictionary<CteClause, CteDependencyHolder>? _foundCtes;
			Stack<CteDependencyHolder>?                  _currentCteStack;

			public CteCollectorVisitor() : base(VisitMode.ReadOnly)
			{
			}

			public IDictionary<CteClause, CteDependencyHolder>? FindCtes(SqlStatement statement)
			{
				_foundCtes       = null;
				_currentCteStack = null;
				Visit(statement);
				return _foundCtes;
			}

			public override void Cleanup()
			{
				_foundCtes       = null;
				_currentCteStack = null;

				base.Cleanup();
			}

			protected internal override IQueryElement VisitSqlWithClause(SqlWithClause element)
			{
				return element;
			}

			protected internal override IQueryElement VisitSqlCteTable(SqlCteTable element)
			{
				var cteClause = element.Cte;

				CteDependencyHolder? holder    = null;

				if (cteClause != null)
				{
					_foundCtes ??= new();
					if (!_foundCtes.TryGetValue(cteClause, out holder))
					{
						cteClause.IsRecursive = false;
						holder                = new CteDependencyHolder(cteClause);
						_foundCtes.Add(cteClause, holder);
					}
				}

				_currentCteStack ??= new Stack<CteDependencyHolder>();
				if (holder != null)
				{
					foreach (var h in _currentCteStack)
					{
						// recursion found
						if (!h.AddDependency(holder.CteClause))
							return element;
					}

					_currentCteStack.Push(holder);
				}

				Visit(cteClause?.Body);

				if (holder != null)
					_currentCteStack.Pop();

				return element;
			}
		}

		protected void FinalizeCte(SqlStatement statement)
		{
			if (statement is not SqlStatementWithQueryBase select)
				return;

			IDictionary<CteClause, CteCollectorVisitor.CteDependencyHolder>? foundCtes;

			using (var cteCollector = _cteCollectorVisitorPool.Allocate())
			{
				foundCtes = cteCollector.Value.FindCtes(statement);
			}

			if (foundCtes == null)
			{
				select.With = null;
			}
			else
			{
				// TODO: Ideally if there is no recursive CTEs we can convert them to SubQueries
				if (!SqlProviderFlags.IsCommonTableExpressionsSupported)
					throw new LinqToDBException("DataProvider do not supports Common Table Expressions.");

				var ordered = TopoSorting.TopoSort(foundCtes.Keys, foundCtes, static (ctes, cteClause) => (ctes.TryGetValue(cteClause, out var h) ? h.DependsOn ?? [] : []))
					.ToList();

				Utils.MakeUniqueNames(ordered, null, static (n, a) => !ReservedWords.IsReserved(n), static c => c.Name, static (c, n, a) => c.Name = n,
					static c => string.IsNullOrEmpty(c.Name) ? "CTE_1" : c.Name, StringComparer.OrdinalIgnoreCase);

				select.With = new SqlWithClause();
				select.With.Clauses.AddRange(ordered);
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

						var changed = ctx.WriteableValue || !string.Equals(newExpr, expr.Expr, StringComparison.Ordinal);

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

		protected SqlDeleteStatement GetAlternativeDelete(SqlDeleteStatement deleteStatement)
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

			if (query.HasWhere)
			{
				if (QueryHelper.ContainsAggregationFunction(query.Where))
					return true;
			}

			return false;
		}

		protected bool RemoveUpdateTableIfPossible(SelectQuery query, SqlTable table, out SqlTableSource? source)
		{
			return RemoveUpdateTableIfPossible(query, table, allowLeftJoin: false, out source);
		}

		protected bool RemoveUpdateTableIfPossible(SelectQuery query, SqlTable table, bool allowLeftJoin, out SqlTableSource? source)
		{
			source = null;

			if (query.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported) ||
				!query.GroupBy.IsEmpty)
			{
				return false;
			}

			if (table.SqlQueryExtensions?.Count > 0)
				return false;

			for (var i = 0; i < query.From.Tables.Count; i++)
			{
				var ts = query.From.Tables[i];
				if (ts.Source == table)
				{
					if (!ts.Joins.TrueForAll(j => j.JoinType is JoinType.Inner or JoinType.Cross || (allowLeftJoin && j.JoinType is JoinType.Left or JoinType.OuterApply)))
						return false;

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

					if (join.JoinType is not (JoinType.Inner or JoinType.Cross or JoinType.Left))
						return false;

					if (join.Table.Source == table)
					{
						if (ts.Joins.Skip(j + 1).Any(sj => QueryHelper.IsDependsOnSource(sj, join.Table)))
							return false;

						source = join.Table;

						ts.Joins.RemoveAt(j);
						query.Where.ConcatSearchCondition(join.Condition);

						for (var sj = 0; sj < join.Table.Joins.Count; sj++)
						{
							ts.Joins.Insert(j + sj, join.Table.Joins[sj]);
						}

						source.Joins.Clear();

						return true;
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
					SqlCteTable                           => false,
					_                                     => true,
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
					return pair.Key switch
					{
						SqlTable table => table != exceptTable,
						SqlColumn => true,
						SqlField field => field.Table != exceptTable,
						_ => false,
					};
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

		protected SqlUpdateStatement GetAlternativeUpdate(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			// Rewrites a generic UPDATE + SELECT tree into a form acceptable by providers
			// without native UPDATE-FROM. Three output shapes:
			//   (1) Simple   : plain `UPDATE t SET ... [WHERE ...]` — no extra joins.
			//   (2) SubQuery : setters backed by correlated subqueries (see TryMoveUpdateToSubQuery).
			//   (3) Universal: `UPDATE t SET ... WHERE EXISTS (...)` (see MakeUniversalUpdate).
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException("Update.Table is required.");

			CorrectUpdateSetters(updateStatement);

			MoveOuterJoinsToUpdateSetters(updateStatement, dataOptions, mappingSchema);

			FlattenRowConstructors(updateStatement, dataOptions, mappingSchema);

			// Shape (1): already simple — nothing to lift. `IsSimpleForUpdate` covers both
			//   (a) HasNoTables           — emit UPDATE as-is, and
			//   (b) FROM = [Update.Table] — detach the target from FROM so the renderer emits
			//                               plain `UPDATE t SET ...` instead of `UPDATE t ... FROM t`.
			if (IsSimpleForUpdate(updateStatement))
			{
				var sq = updateStatement.SelectQuery;
				if (!sq.HasNoTables
					&& sq.IsSingleTableQueryWithoutJoins
					&& sq.From.Tables[0].Source == updateStatement.Update.Table)
				{
					updateStatement.Update.TableSource = null;
				}

				return updateStatement;
			}

			// Non-trivial: first ensure the outer query is a shape we can rewrite (no aggregates /
			// modifiers in WHERE), then prefer the sub-query form, falling back to universal EXISTS.
			if (NeedsEnvelopingForUpdate(updateStatement.SelectQuery))
			{
				updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, allowMutation: true);
			}

			if (!TryMoveUpdateToSubQuery(updateStatement, dataOptions, mappingSchema))
			{
				MakeUniversalUpdate(updateStatement, dataOptions, mappingSchema);
			}

			return updateStatement;
		}

		bool TryMoveUpdateToSubQuery(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (updateStatement.Update.Table == null)
				return false;

			// We are about to clear the outer From.Tables and fold them into a correlated
			// subquery. The outer query must not carry clauses that logically require a FROM
			// (GROUP BY / HAVING / DISTINCT / LIMIT / ORDER BY / set operators) — otherwise
			// the resulting `UPDATE t SET ... WHERE ...` (no FROM) would be invalid SQL.
			var sq = updateStatement.SelectQuery;
			if (sq.HasGroupBy || sq.HasHaving || sq.IsDistinct || sq.IsLimited || sq.HasOrderBy || sq.HasSetOperators)
				return false;

			if (sq.From.Tables.Count != 1)
				return false;

			var tableSource = updateStatement.SelectQuery.From.Tables[0];

			if (tableSource.Source != updateStatement.Update.Table)
				return false;

			// if update statement's where depends on outer sources, we cannot move it to subquery, because it will change semantics of the query
			if (IsDependedExceptedSource(updateStatement.SelectQuery.Where, updateStatement.Update.Table))
				return false;

			if (!tableSource.Joins.TrueForAll(IsLiftableJoin))
				return false;

			// Build a correlated subquery from the outer joins. The first join is folded into
			// `FROM <table> WHERE <cond>`, deliberately dropping its `JoinType`: the subquery is
			// consumed as a *scalar* SET expression, and a scalar subquery returning zero rows
			// yields NULL — which matches LEFT/OUTER-APPLY "no match" semantics. Subsequent
			// joins keep their original JoinType (they compose against the first table).
			//
			// For `JoinType.Inner && IsSubqueryExpression`, the original INNER semantics would
			// filter the outer UPDATE row when no match exists — but after this rewrite the
			// outer UPDATE row is kept and the setter becomes NULL. This is tolerated because
			// `IsSubqueryExpression` joins are synthesized from subquery-in-expression LINQ
			// which, combined with the `IsLimitedToOneRecord` check above, acts as the same
			// "at most one row" scalar-subquery contract — callers expecting this shape treat
			// NULL-on-no-match as equivalent to the original behavior.
			// TODO: revisit if the `IsSubqueryExpression` upstream contract ever relaxes to
			// allow shapes that could produce zero rows without a compensating `DefaultIfEmpty`.
			var subquery = new SelectQuery();

			foreach (var join in tableSource.Joins)
			{
				if (subquery.HasNoTables)
				{
					subquery.From.Tables.Add(join.Table);
					subquery.Where.ConcatSearchCondition(join.Condition);
				}
				else
				{
					subquery.From.Tables[0].Joins.Add(join);
				}
			}

			// check that provider can handle such move
			if (!SqlProviderHelper.IsValidQuery(subquery, updateStatement.SelectQuery, null, 0, SqlProviderFlags, out _))
				return false;

			tableSource.Joins.Clear();

			updateStatement.SelectQuery.From.Tables.Clear();

			if (SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Update))
				ProcessUpdateItemsWithRows(updateStatement, subquery, null);
			else
				ProcessUpdateItemsWithoutRows(updateStatement, subquery, null);

			updateStatement.SelectQuery.Select.Columns.Clear();

			OptimizeQueries(updateStatement, updateStatement, dataOptions, mappingSchema, new EvaluationContext());

			return true;
		}

		void ProcessUpdateItemsWithoutRows(SqlUpdateStatement updateStatement, SelectQuery subquery, Dictionary<IQueryElement, IQueryElement>? replaceTree)
		{
			if (updateStatement.Update.Table is null)
				throw new InvalidOperationException("Update table cannot be null");

			for (var index = 0; index < updateStatement.Update.Items.Count; index++)
			{
				var item = updateStatement.Update.Items[index];

				var itemExpression = item.Expression;
				if (itemExpression is SqlColumn exprColumn && exprColumn.Parent == updateStatement.SelectQuery)
				{
					itemExpression = exprColumn.Expression;
				}

				if (itemExpression == null)
					continue;

				if (!IsDependedExceptedSource(itemExpression, updateStatement.Update.Table))
					continue;

				var cloned = CloneQuery(subquery, updateStatement.Update.Table, out var innerReplaceTree);

				var newSetExpression = itemExpression.Convert(1, (x, e) =>
				{
					if (e is not ISqlExpression sqlExpr)
					{
						return e;
					}

					var source = QueryHelper.ExtractSqlSource(sqlExpr);

					if (source != null && source != updateStatement.Update.Table)
					{
						if (innerReplaceTree.TryGetValue(sqlExpr, out var newExpr))
						{
							return newExpr;
						}
					}

					if (replaceTree != null && replaceTree.TryGetValue(sqlExpr, out var newExpr2))
					{
						return newExpr2;
					}

					return e;
				});

				cloned.Select.Columns.Clear();
				cloned.Select.AddColumn(newSetExpression);

				item.Expression = cloned;
			}
		}

		void ProcessUpdateItemsWithRows(SqlUpdateStatement updateStatement, SelectQuery subquery, Dictionary<IQueryElement, IQueryElement>? replaceTree)
		{
			if (updateStatement.Update.Table is null)
				throw new InvalidOperationException("Update table cannot be null");

			// extracting values, which should use subquery

			var newItems           = new List<SqlSetExpression>();
			var dependentArguments = new List<SqlSetExpression>();

			foreach (var item in updateStatement.Update.Items)
			{
				if (item.Expression == null || !IsDependedExceptedSource(item.Expression, updateStatement.Update.Table))
				{
					newItems.Add(item);
					continue;
				}

				if (item.Column is SqlRowExpression columnsRow)
				{
					if (item.Expression is SqlRowExpression row)
					{
						// Split a row-setter `(c1, c2, ...) = (v1, v2, ...)` into:
						//   * dependentArguments  — pairs whose value references another table; these
						//                            will be produced by a correlated sub-query below.
						//   * independentPairs    — pairs usable as-is; re-emitted as a plain setter.
						var independentPairs = new List<(ISqlExpression column, ISqlExpression value)>();

						for (int i = 0; i < row.Values.Length; i++)
						{
							var value = row.Values[i];
							if (IsDependedExceptedSource(value, updateStatement.Update.Table))
								dependentArguments.Add(new SqlSetExpression(columnsRow.Values[i], value));
							else
								independentPairs.Add((columnsRow.Values[i], value));
						}

						if (independentPairs.Count > 1)
						{
							newItems.Add(new SqlSetExpression(
								new SqlRowExpression(independentPairs.Select(p => p.column).ToArray()),
								new SqlRowExpression(independentPairs.Select(p => p.value).ToArray())));
						}
						else if (independentPairs.Count == 1)
						{
							newItems.Add(new SqlSetExpression(independentPairs[0].column, independentPairs[0].value));
						}
					}
					else if (item.Expression is SelectQuery updateSubquery && updateSubquery.Select.Columns.Count == columnsRow.Values.Length)
					{
						for (int i = 0; i < columnsRow.Values.Length; i++)
						{
							var value = updateSubquery.Select.Columns[i].Expression;
							if (IsDependedExceptedSource(value, updateStatement.Update.Table))
								dependentArguments.Add(new SqlSetExpression(columnsRow.Values[i], value));
							else
								newItems.Add(new SqlSetExpression(columnsRow.Values[i], value));
						}
					}
					else
					{
						// The RHS depends on another source (guard above) but is neither an inline row
						// nor a SelectQuery with matching column count. Re-emitting the item verbatim
						// would dangle a reference to a source that TryMoveUpdateToSubQuery strips.
						throw new LinqToDBException(
							$"Cannot rewrite row setter for {item.Column}: RHS must be a SqlRowExpression or SelectQuery with {columnsRow.Values.Length} value(s), was {item.Expression?.GetType().Name ?? "null"}.");
					}
				}
				else
				{
					dependentArguments.Add(item);
				}
			}

			if (dependentArguments.Count > 0)
			{
				// generate columns
				subquery.Select.Columns.Clear();
				for (var i = 0; i < dependentArguments.Count; i++)
				{
					var item = dependentArguments[i];

					var expression = replaceTree == null
						? item.Expression!
						: RemapCloned(item.Expression!, replaceTree);

					subquery.Select.AddNew(expression);
				}

				var columnExpression = dependentArguments.Count == 1
					? dependentArguments[0].Column
					: new SqlRowExpression(dependentArguments.Select(i => i.Column).ToArray());

				newItems.Add(new SqlSetExpression(columnExpression, subquery));
			}

			updateStatement.Update.Items.Clear();
			updateStatement.Update.Items.AddRange(newItems);
		}

		bool IsSimpleForUpdate(SqlUpdateStatement updateStatement)
		{
			if (updateStatement.SelectQuery.Select.TakeValue != null && !(SqlProviderFlags.IsUpdateTakeSupported || SqlProviderFlags.IsUpdateSkipTakeSupported))
				return false;

			if (updateStatement.SelectQuery.Select.SkipValue != null && !SqlProviderFlags.IsUpdateSkipTakeSupported)
				return false;

			if (updateStatement.SelectQuery.HasNoTables)
				return true;

			if (updateStatement.SelectQuery.From.Tables is [{ HasJoins: false }])
			{
				var source = updateStatement.SelectQuery.From.Tables[0].Source;
				if (source == updateStatement.Update.Table)
					return true;
			}

			return false;
		}

		void MakeUniversalUpdate(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException("Update table cannot be null");

			CorrectUpdateColumns(updateStatement);

			// Two clones of the original SelectQuery are produced:
			//   * subquery     — supplies correlated values for setters that depend on other tables;
			//   * existsQuery  — forms the `WHERE EXISTS (...)` row qualifier on the outer UPDATE.
			// The update target is correlated back to the clones only when it's actually present
			// inside the source SelectQuery (same-table UPDATE). For `.Update(otherTarget, ...)`
			// the source query does not reference Update.Table at all, so the TryGetValue miss
			// is expected — no correlation clause is needed.
			var subquery = CloneQuery(updateStatement.SelectQuery, null, out var replaceTree);
			subquery.Select.Columns.Clear();

			if (replaceTree.TryGetValue(updateStatement.Update.Table, out var elementInSubquery))
				ApplyUpdateTableComparison(subquery, updateStatement.Update, (SqlTable)elementInSubquery, dataOptions);

			if (SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Update))
				ProcessUpdateItemsWithRows(updateStatement, subquery, replaceTree);
			else
				ProcessUpdateItemsWithoutRows(updateStatement, subquery, replaceTree);

			var existsQuery = CloneQuery(updateStatement.SelectQuery, null, out var existsReplaceTree);

			if (existsReplaceTree.TryGetValue(updateStatement.Update.Table, out var elementInExistsQuery))
				ApplyUpdateTableComparison(existsQuery, updateStatement.Update, (SqlTable)elementInExistsQuery, dataOptions);

			var updateQuery = new SelectQuery();
			updateStatement.SelectQuery = updateQuery;
			updateQuery.Where.SearchCondition.AddExists(existsQuery);

			OptimizeQueries(updateStatement, updateStatement, dataOptions, mappingSchema, new EvaluationContext());
		}

		static bool IsDependedExceptedSource(IQueryElement element, ISqlTableSource exceptSource)
		{
			return QueryHelper.IsDependsOnOuterSources(element, currentSources: [exceptSource]);
		}

		// Expands `SET (c1, c2, ...) = <rhs>` setters into individual `c_i = v_i` setters when
		// the provider doesn't support the rhs shape natively. Two rhs shapes are handled:
		//   - SelectQuery  → flattened when the provider lacks RowFeature.Update (subquery rhs).
		//                    The subquery is re-attached as OUTER APPLY so column references
		//                    stay resolvable.
		//   - SqlRowExpression → flattened when the provider lacks RowFeature.UpdateLiteral
		//                        (literal row rhs).
		// Shapes the provider supports natively are left alone. Any other shape is a logic
		// error upstream.
		void FlattenRowConstructors(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			var supportsUpdate        = SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Update);
			var supportsUpdateLiteral = SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.UpdateLiteral);

			var setters = updateStatement.Update.Items;

			for (var ui = 0; ui < setters.Count; ui++)
			{
				var item = setters[ui];

				if (item is not { Column: SqlRowExpression row })
					continue;

				if (item.Expression is SelectQuery updateSubquery && updateSubquery.Select.Columns.Count == row.Values.Length)
				{
					if (supportsUpdate)
						continue;

					setters.RemoveAt(ui);
					ui--;

					for (int i = 0; i < row.Values.Length; i++)
					{
						var rowValue = row.Values[i];
						// Use the projected SqlColumn (not its unwrapped Expression) so the
						// reference goes through updateSubquery's Select projection. Otherwise
						// the subsequent OptimizeQueries pass sees the apply's projection as
						// unused, eliminates the entire apply, and leaves the setters pointing
						// at inner-table SqlField instances that no longer exist in the tree.
						var updateValue = (ISqlExpression)updateSubquery.Select.Columns[i];

						var newUpdateItem = new SqlSetExpression(rowValue, updateValue);
						setters.Add(newUpdateItem);
					}

					var evaluationContext = new EvaluationContext();

					// removing dead references
					OptimizeQueries(updateStatement, updateStatement, dataOptions, mappingSchema, evaluationContext);

					// remove query from columns expression
					for (var ci = updateStatement.SelectQuery.Select.Columns.Count - 1; ci >= 0; ci--)
					{
						var selectColumn = updateStatement.SelectQuery.Select.Columns[ci];
						if (selectColumn.Expression is SqlRowExpression || selectColumn.Expression.Equals(updateSubquery))
						{
							updateStatement.SelectQuery.Select.Columns.RemoveAt(ci);
						}
					}

					if (!updateStatement.SelectQuery.HasElement(updateSubquery))
					{
						if (updateStatement.SelectQuery.HasNoTables)
						{
							updateStatement.SelectQuery.From.Table(updateSubquery);
						}
						else
						{
							updateStatement.SelectQuery.From.Tables[^1].Joins.Add(new SqlJoinedTable(JoinType.OuterApply, updateSubquery, null, false));
						}
					}

					// optimize apply
					OptimizeQueries(updateStatement, updateStatement, dataOptions, mappingSchema, evaluationContext);
				}
				else if (item.Expression is SqlRowExpression updateRow && updateRow.Values.Length == row.Values.Length)
				{
					if (supportsUpdateLiteral)
						continue;

					setters.RemoveAt(ui);
					ui--;

					for (int i = 0; i < row.Values.Length; i++)
					{
						var rowValue      = row.Values[i];
						var updateValue   = updateRow.Values[i];
						var newUpdateItem = new SqlSetExpression(rowValue, updateValue);
						setters.Add(newUpdateItem);
					}
				}
				else
				{
					throw new LinqToDBException(
						$"Cannot flatten row setter for {item.Column}: RHS must be a SelectQuery or SqlRowExpression with {row.Values.Length} value(s).");
				}
			}
		}

		// When a row-expression subquery (e.g. `(from ... select Sql.Row(...)).Single()`) is used
		// as an UPDATE rvalue, the builder attaches it as an OUTER/CROSS APPLY (or post-optimized
		// LEFT/INNER JOIN) on the update's FROM and wires each setter's Expression to a SqlColumn
		// of that apply. Lift the apply back into the setter as a proper scalar subquery:
		// fold the join condition into the inner query's WHERE, rewrite its single SqlRowExpression
		// column into one column per row value, detach the apply from the outer FROM, and set the
		// setter's Expression to that inner SelectQuery.
		//
		// Providers without UPDATE-ROW-subquery support (no RowFeature.UpdateLiteral) still
		// benefit: FlattenRowConstructors runs next and expands the lifted SelectQuery into
		// individual setters, same as it already does for directly-emitted SelectQuery RHS.
		void MoveOuterJoinsToUpdateSetters(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			var updateSq = updateStatement.SelectQuery;
			if (updateSq.From.Tables.Count == 0)
				return;

			var setters = updateStatement.Update.Items;

			for (var si = 0; si < setters.Count; si++)
			{
				var item = setters[si];

				if (item.Column is not SqlRowExpression rowColumn)
					continue;

				// Unwrap one layer if item.Expression is a projection column of the update's own
				// SelectQuery (matches the convention used in ProcessUpdateItemsWithoutRows).
				var rhs = item.Expression;
				if (rhs is SqlColumn outerCol && outerCol.Parent == updateSq)
					rhs = outerCol.Expression;

				if (rhs is not SqlColumn { Expression: SqlRowExpression colRow } innerCol)
					continue;

				if (colRow.Values.Length != rowColumn.Values.Length)
					continue;

				var innerQuery = innerCol.Parent;
				if (innerQuery is null)
					continue;

				// Find the join on the update's FROM whose source is innerQuery.
				SqlTableSource? hostTs = null;
				var joinIdx = -1;

				foreach (var ts in updateSq.From.Tables)
				{
					for (var j = 0; j < ts.Joins.Count; j++)
					{
						var jn = ts.Joins[j];

						if (!IsLiftableJoin(jn))
							continue;

						if (jn.Table.Source == innerQuery)
						{
							hostTs  = ts;
							joinIdx = j;
							break;
						}
					}

					if (hostTs != null)
						break;
				}

				if (hostTs is null)
					continue;

				var join = hostTs.Joins[joinIdx];

				// Skip if innerQuery is referenced anywhere in the statement other than
				// the setter we're rewriting and the join we're detaching.
				var ignore = new HashSet<IQueryElement> { item, join };
				if (QueryHelper.IsDependsOn(updateStatement, innerQuery, ignore))
					continue;

				// Fold the join's ON into innerQuery.Where so the correlation travels with the subquery.
				if (join.Condition.Predicates.Count > 0)
					innerQuery.Where.ConcatSearchCondition(join.Condition);

				// Rewrite innerQuery's columns: replace the single SqlRowExpression column with
				// one column per row value.
				innerQuery.Select.Columns.Clear();
				for (var vi = 0; vi < colRow.Values.Length; vi++)
					innerQuery.Select.AddNew(colRow.Values[vi]);

				hostTs.Joins.RemoveAt(joinIdx);

				item.Expression = innerQuery;
			}

			// Re-run standard optimization so dangling references and now-empty join lists
			// collapse cleanly.
			OptimizeQueries(updateStatement, updateStatement, dataOptions, mappingSchema, new EvaluationContext());
		}

		// True when a join's source is safe to lift into a scalar subquery. Accepts:
		//   - OuterApply / Left:          "no match → NULL" semantics match a scalar subquery.
		//   - Inner && IsSubqueryExpression: synthesized from subquery-in-expression LINQ, where
		//                                    the IsLimitedToOneRecord bound makes the "drop outer
		//                                    row on no match" divergence benign — see the long
		//                                    comment in TryMoveUpdateToSubQuery below.
		// CrossApply is intentionally NOT accepted: an unpaired CrossApply drops the outer row
		// on no match, and we can only tolerate that divergence under IsSubqueryExpression.
		static bool IsLiftableJoin(SqlJoinedTable join)
		{
			var typeOk = join.JoinType is JoinType.OuterApply or JoinType.Left
				|| (join.JoinType is JoinType.Inner && join.IsSubqueryExpression);

			return typeOk && QueryHelper.IsLimitedToOneRecord(join);
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

				if (item is { Column: SqlRowExpression, Expression: SelectQuery subQuery })
				{
					if (subQuery.HasNoTables)
					{
						if (SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.UpdateLiteral))
						{
							// Provider supports row literals — collapse `SELECT v1, v2` back to `Row(v1, v2)`.
							var rowValues = subQuery.Select.Columns.Select(c => c.Expression).ToArray();
							item.Expression = new SqlRowExpression(rowValues);
						}
						// else: leave as SelectQuery; FlattenRowConstructors will expand into individual setters.
					}
					else if (subQuery.Select.Columns is [var column])
					{
						// Lift a degenerate single-column wrapper into N row columns. Only safe when the
						// inner SelectQuery is purely a column carrier — no FROM, filtering, ordering, or
						// set operations to preserve.
						if (column.Expression is SelectQuery columnQuery
							&& columnQuery.HasNoTables
							&& !columnQuery.HasWhere
							&& !columnQuery.HasGroupBy
							&& !columnQuery.HasHaving
							&& !columnQuery.HasOrderBy
							&& !columnQuery.Select.HasModifier
							&& !columnQuery.HasSetOperators)
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

				ApplyUpdateTableComparison(updateStatement.SelectQuery, updateStatement.Update, clonedTable, dataOptions);
			}
			else
			{
				if (addNewSource)
				{
					updateStatement.SelectQuery.From.Tables.Insert(0, newSource);
				}

				ApplyUpdateTableComparison(updateStatement.SelectQuery, updateStatement.Update, clonedTable, dataOptions);
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
			if (statement.Update.Table == null)
				throw new InvalidOperationException("Update.Table is required.");

			// If the provider can't carry skip/take on the outer UPDATE, wrap the query so modifiers
			// live inside a subquery the WHERE clause can reference.
			if (statement.SelectQuery.Select.HasSomeModifiers(SqlProviderFlags.IsUpdateSkipTakeSupported, SqlProviderFlags.IsUpdateTakeSupported))
			{
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery, allowMutation: true);
			}

			var tableToUpdate   = statement.Update.Table!;
			var needsReoptimize = false;

			// Reconcile the update target with the SELECT's FROM:
			//   (a) not present   → nothing to do
			//   (b) removable     → inline it into WHERE (plain `UPDATE t SET ... WHERE ...`)
			//   (c) not removable → detach via a self-join comparison, then null out the
			//                       Update.TableSource so the renderer emits bare `UPDATE t`.
			// CorrectUpdateSetters must run before Detach — Detach replaces SelectQuery refs and
			// would leave setter columns pointing at the pre-replace parents otherwise.
			if (QueryHelper.HasTableInQuery(statement.SelectQuery, tableToUpdate))
			{
				if (RemoveUpdateTableIfPossible(statement.SelectQuery, tableToUpdate, out _))
				{
					CorrectUpdateSetters(statement);
					needsReoptimize = true;
				}
				else
				{
					CorrectUpdateSetters(statement);
					statement = DetachUpdateTableFromUpdateQuery(statement, dataOptions, moveToJoin: false, addNewSource: false, out _);
					statement.Update.TableSource = null;
					needsReoptimize = true;
				}
			}
			else
			{
				CorrectUpdateSetters(statement);
			}

			if (needsReoptimize)
				OptimizeQueries(statement, statement, dataOptions, mappingSchema, new EvaluationContext());

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

				var supportsParameter = SqlProviderFlags.GetIsSkipSupportedFlag(query.Select.TakeValue)
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

					if (param.Type.SystemType.IsNullableOrReferenceType)
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
					if (takeExpr.ElementType is not QueryElementType.SqlParameter and not QueryElementType.SqlValue)
					{
						var takeValue = takeExpr.EvaluateExpression(optimizationContext.EvaluationContext)!;
						var takeParameter = new SqlParameter(new DbDataType(takeValue.GetType()), "take", takeValue)
						{
							IsQueryParameter = dataOptions.LinqOptions.ParameterizeTakeSkip && !QueryHelper.NeedParameterInlining(takeExpr),
						};
						takeExpr = takeParameter;
					}
				}
				else if (takeExpr.ElementType != QueryElementType.SqlValue)
					takeExpr = new SqlValue(takeExpr.EvaluateExpression(optimizationContext.EvaluationContext)!);
			}

			if (skipExpr != null)
			{
				var supportsParameter = SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery.Select.TakeValue)
										&& SqlProviderFlags.AcceptsTakeAsParameter;

				if (supportsParameter)
				{
					if (skipExpr.ElementType is not QueryElementType.SqlParameter and not QueryElementType.SqlValue)
					{
						var skipValue = skipExpr.EvaluateExpression(optimizationContext.EvaluationContext)!;
						var skipParameter = new SqlParameter(new DbDataType(skipValue.GetType()), "skip", skipValue)
						{
							IsQueryParameter = dataOptions.LinqOptions.ParameterizeTakeSkip && !QueryHelper.NeedParameterInlining(skipExpr),
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
				allowMutation: true);
		}

		/// <summary>
		/// Replaces pagination by Window function ROW_NUMBER().
		/// </summary>
		/// <param name="context"><paramref name="predicate"/> context object.</param>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="supportsEmptyOrderBy">Indicates that database supports OVER () syntax.</param>
		/// <param name="predicate">Indicates when the transformation is needed</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when transformation has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNumber<TContext>(TContext context, SqlStatement statement, MappingSchema mappingSchema, Func<TContext, SelectQuery, bool> predicate, bool supportsEmptyOrderBy)
		{
			return QueryHelper.WrapQuery(
				(predicate, context, supportsEmptyOrderBy, statement, mappingSchema),
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
						orderByItems = context.supportsEmptyOrderBy ? [] : [new SqlOrderByItem(new SqlFragment("(SELECT NULL)"), false, false)];

					var orderBy = string.Join(", ",
						orderByItems.Select(static (oi, i) => oi.IsDescending ? string.Create(CultureInfo.InvariantCulture, $"{{{i}}} DESC") : string.Create(CultureInfo.InvariantCulture, $"{{{i}}}")));

					var parameters = orderByItems.Select(static oi => oi.Expression).ToArray();

					// careful here - don't clear it before orderByItems used
					query.OrderBy.Items.Clear();

					var rowNumberExpression = parameters.Length == 0
						? new SqlExpression(context.mappingSchema.GetDbDataType(typeof(long)), "ROW_NUMBER() OVER ()", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable)
						: new SqlExpression(context.mappingSchema.GetDbDataType(typeof(long)), $"ROW_NUMBER() OVER (ORDER BY {orderBy})", Precedence.Primary, SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, parameters);

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
				allowMutation: true);
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
						if (sqlQuery.From.Tables.Exists(t => t.Joins.Count > 0))
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

						if (allJoins.Exists(j => j.JoinType == JoinType.Cross) && allJoins.Exists(j => j.JoinType != JoinType.Cross))
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

			protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
			{
				if (_disableParameters)
					sqlParameter.IsQueryParameter = false;

				return base.VisitSqlParameter(sqlParameter);
			}
		}
		#endregion

	}
}
