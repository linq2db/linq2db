using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;
	using Linq;
	using LinqToDB.Common;

	class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override bool CanCompareSearchConditions => true;

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			return base.Finalize(statement);
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => PrepareUpdateStatement((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		void ReplaceTable(ISqlExpressionWalkable? element, SqlTable replacing, SqlTable withTable)
		{
			element?.Walk(WalkOptions.Default, (replacing, withTable), static (ctx, e) =>
			{
				if (e is SqlField field && field.Table == ctx.replacing)
					return ctx.withTable[field.Name] ?? throw new LinqException($"Field {field.Name} not found in table {ctx.withTable}");

				return e;
			});
		}

		SqlStatement PrepareUpdateStatement(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.HasModifier)
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery, allowMutation: true);

			var tableSource = GetMainTableSource(statement.SelectQuery);
			if (tableSource == null)
				throw new LinqToDBException("Invalid query for Update.");

			if (statement.SelectQuery.Select.HasModifier)
				statement = QueryHelper.WrapQuery(statement, statement.SelectQuery, allowMutation: true);

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
							ReplaceTable(statement, tableToUpdate, table);
						}
						tableToUpdate = table;
					}
					else
					{
						var processed    = false;
						var tableToCheck = tableToUpdate ?? table;
						var onSources    = new HashSet<ISqlTableSource> { tableToCheck };

						if (tableSource.Joins.All(j =>
							j.JoinType == JoinType.Inner && (j.Table.Source == tableToCheck || !QueryHelper.IsDependsOn(j.Table, onSources))))
						{
							// simplify all to FROM

							var tableToUpdateSource = table == tableToUpdate
								? tableSource
								: tableSource.Joins.FirstOrDefault(j => j.Table.Source == tableToCheck)?.Table;

							if (tableToUpdateSource != null)
							{
								processed = true;

								foreach (var j in tableSource.Joins)
								{
									statement.SelectQuery.From.Tables.Add(j.Table);
									statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Conditions
										.Add(new SqlCondition(false, j.Condition));
								}

								statement.SelectQuery.From.Tables.Remove(tableToUpdateSource);

								tableSource.Joins.Clear();
							}
						}


						if (!processed && (table == tableToUpdate || tableToUpdate == null))
						{
							processed = true;

							tableToUpdate ??= table;
							var joins = tableSource.Joins;

							if (joins.Count > 0)
							{
								if (joins.All(j => !QueryHelper.IsDependsOn(j, onSources)))
								{
									statement.SelectQuery.From.Tables.RemoveAt(0);

									var firstJoin = joins[0];
									statement.SelectQuery.From.Tables.Insert(0, firstJoin.Table);
									statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Conditions
										.Add(new SqlCondition(false, firstJoin.Condition));

									firstJoin.Table.Joins.InsertRange(0, joins.Skip(1));
								}
								else
								{
									// create clone
									var clonedTable = table.Clone();

									ReplaceTable(statement.Update, table, clonedTable);
									ReplaceTable(statement.Output, table, clonedTable);

									tableToCompare = table;
									tableToUpdate  = clonedTable;
								}
							}
						}

						if (!processed && tableToUpdate != null)
						{
							for (int i = 0; i < tableSource.Joins.Count; i++)
							{
								var join = tableSource.Joins[i];
								if (join.Table.Source == tableToUpdate)
								{
									var sources = new HashSet<ISqlTableSource> { join.Table.Source };

									if (tableSource.Joins.Skip(i + 1).Any(j => QueryHelper.IsDependsOn(j, sources)))
										break;

									processed = true;

									statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Conditions
										.Add(new SqlCondition(false, join.Condition));

									tableSource.Joins.RemoveAt(i);

									break;
								}
							}
						}

						if (!processed && tableToUpdate != null)
						{
							for (int i = 0; i < tableSource.Joins.Count; i++)
							{
								var join = tableSource.Joins[i];
								if (join.Table.Source is SqlTable currentTable &&
								    QueryHelper.IsEqualTables(currentTable, tableToUpdate))
								{
									processed = true;

									var sources = new HashSet<ISqlTableSource> { join.Table.Source };

									if (tableSource.Joins.Skip(i + 1).Any(j => QueryHelper.IsDependsOn(j, sources)))
									{
										tableToCompare = currentTable;
										break;
									}

									statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Conditions
										.Add(new SqlCondition(false, join.Condition));

									tableSource.Joins.RemoveAt(i);

									ReplaceTable(statement, tableToUpdate, currentTable);

									tableToUpdate = currentTable;

									break;
								}
							}
						}

						if (!processed)
						{
							if (QueryHelper.IsEqualTables(table, tableToCheck))
							{
								processed = true;

								var sources = new HashSet<ISqlTableSource> { tableSource.Source };

								if (tableSource.Joins.Any(j => QueryHelper.IsDependsOn(j, sources)))
								{
									tableToCompare = table;
									break;
								}

								var joins = tableSource.Joins;
								statement.SelectQuery.From.Tables.RemoveAt(0);
								if (joins.Count > 0)
								{
									var firstJoin = joins[0];
									statement.SelectQuery.From.Tables.Insert(0, firstJoin.Table);
									statement.SelectQuery.Where.SearchCondition.EnsureConjunction().Conditions
										.Add(new SqlCondition(false, firstJoin.Condition));

									firstJoin.Table.Joins.InsertRange(0, joins.Skip(1));
								}

								ReplaceTable(statement, tableToCheck, table);
								tableToUpdate = table;
							}
						}

						if (!processed)
							throw new LinqToDBException("Can not decide which table to update");
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

				for (var i = 0; i < statement.Update.Items.Count; i++)
				{
					var item = statement.Update.Items[i];
					var newItem = item.Convert((tableToCompare, tableToUpdate), static (v, e) =>
					{
						if (e is SqlField field && field.Table == v.Context.tableToCompare)
							return v.Context.tableToUpdate[field.Name] ?? throw new LinqException($"Field {field.Name} not found in table {v.Context.tableToUpdate}");

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

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate, visitor);

			if (false == predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) && searchPredicate is SqlPredicate.Like likePredicate)
			{
				searchPredicate = new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "^": return new SqlBinaryExpression(be.SystemType, be.Expr1, "#", be.Expr2);
					case "+": return be.SystemType == typeof(string) ? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Convert"   :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						// Another cast syntax
						//
						// rreturn new SqlExpression(func.SystemType, "{0}::{1}", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);

					case "CharIndex" :
						return func.Parameters.Length == 2
							? new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
								func.Parameters[0], func.Parameters[1])
							: Add<int>(
								new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
									func.Parameters[0],
									ConvertExpressionImpl(
										new SqlFunction(typeof(string), "Substring",
										func.Parameters[1],
										func.Parameters[2],
										Sub<int>(
											ConvertExpressionImpl(
													new SqlFunction(typeof(int), "Length", func.Parameters[1]), visitor), func.Parameters[2])),
										visitor)),
								Sub(func.Parameters[2], 1));
				}
			}

			return expression;
		}

		protected override SqlStatement FixSetOperationColumnTypes(SqlStatement statement)
		{
			statement = base.FixSetOperationColumnTypes(statement);

			// postgresql use more strict checks for sets in recursive CTEs, so we need to implement additional fixes for them.
			// List of known limitations:
			// - string types should be same
			statement.Visit(static e =>
			{
				if (e is CteClause cte && cte.Body?.HasSetOperators == true)
				{
					var query = cte.Body;

					for (int i = 0; i < query.Select.Columns.Count; i++)
					{
						var firstColumn = query.Select.Columns[i];

						var dataType = firstColumn.Expression.GetExpressionType().DataType;

						if (   dataType != DataType.NVarChar && dataType != DataType.VarChar
							&& dataType != DataType.NChar    && dataType != DataType.Char
							&& dataType != DataType.NText    && dataType != DataType.Text)
							continue;

						var field        = QueryHelper.GetUnderlyingField(firstColumn.Expression);
						var applyConvert = field == null;

						if (!applyConvert)
						{
							foreach (var setOperator in query.SetOperators)
							{
								var column   = setOperator.SelectQuery.Select.Columns[i];
								applyConvert = applyConvert || QueryHelper.GetUnderlyingField(column) != field;

								if (applyConvert)
									break;
							}
						}

						if (applyConvert)
						{
							// use text as varchar without length ignored in this place by pgsql for unknown reason
							var type = new DbDataType(typeof(string), DataType.Text);

							firstColumn.Expression = new SqlExpression(firstColumn.Expression.SystemType, "Cast({0} as {1})", Precedence.Primary, firstColumn.Expression, new SqlDataType(type));

							foreach (var setOperator in query.SetOperators)
							{
								var column        = setOperator.SelectQuery.Select.Columns[i];
								column.Expression = new SqlExpression(column.Expression.SystemType, "Cast({0} as {1})", Precedence.Primary, column.Expression, new SqlDataType(type));
							}
						}
					}
				}
			});

			return statement;
		}

	}
}
