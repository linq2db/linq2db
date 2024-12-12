using System.Collections.Generic;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	class AccessSqlOptimizer : BasicSqlOptimizer
	{
		public AccessSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new AccessSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);
			statement = CorrectMultiTableQueries(statement);
			statement = CorrectInnerJoins(statement);
			statement = CorrectExistsAndIn(statement, dataOptions);

			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => CorrectAccessUpdate ((SqlUpdateStatement)statement, dataOptions, mappingSchema),
				_                => statement,
			};
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement = base.Finalize(mappingSchema, statement, dataOptions);

			statement = WrapParameters(statement);

			return statement;
		}

		private static SqlStatement WrapParameters(SqlStatement statement)
		{
			// System.Data.Odbc cannot handle types if they are not in a list of hardcoded types.
			// Here we try to avoid FromSqlType to fail when ODBC Access driver returns 0 for type
			// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Odbc/src/System/Data/Odbc/Odbc32.cs#L935
			//
			// This is a bug in Access ODBC driver where it returns no type information for NULL/parameter-based top-level column.
			// We wrap all NULL/parameter top level columns, because exact conditions for triggering error are not clear and even same query could fail and pass
			// in applications with different modules loaded
			//
			// Some related tests:
			// AccessTests.TestParametersWrapping
			// Distinct5/Distinct6 tests
			// some of Select_Ternary* tests

			// only SELECT query could return dataset in ACCESS
			if (statement.QueryType != QueryType.Select || statement.SelectQuery == null)
				return statement;

			var visitor = new WrapParametersVisitor(VisitMode.Modify);

			statement = (SqlStatement)visitor.WrapParameters(statement, WrapParametersVisitor.WrapFlags.All);

			return statement;
		}

		private SqlUpdateStatement CorrectAccessUpdate(SqlUpdateStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (statement.SelectQuery.Select.HasModifier)
				throw new LinqToDBException("Access does not support update query limitation");

			statement = CorrectUpdateTable(statement, leaveUpdateTableInQuery: true, dataOptions, mappingSchema);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		SqlStatement CorrectInnerJoins(SqlStatement statement)
		{
			statement.Visit(static e =>
			{
				if (e.ElementType == QueryElementType.SqlQuery)
				{
					var sqlQuery = (SelectQuery)e;

					for (var tIndex = 0; tIndex < sqlQuery.From.Tables.Count; tIndex++)
					{
						var t = sqlQuery.From.Tables[tIndex];
						for (int i = 0; i < t.Joins.Count; i++)
						{
							var join = t.Joins[i];
							if (join.JoinType == JoinType.Inner)
							{
								bool moveUp = false;

								if (join.Table.Joins.Count > 0 && join.Table.Joins[0].JoinType == JoinType.Inner)
								{
									// INNER JOIN Table1 t1
									//		INNER JOIN Table2 t2 ON ...
									// ON t1.Field = t2.Field
									//

									var usedSources = new HashSet<ISqlTableSource>();
									QueryHelper.GetUsedSources(join.Condition, usedSources);

									if (usedSources.Contains(join.Table.Joins[0].Table.Source))
									{
										moveUp = true;
									}
								}
								else
								{
									// Check for join with unbounded condition
									//
									// INNER JOIN Table1 t1 ON other.Field = 1
									//

									var usedSources = new HashSet<ISqlTableSource>();
									QueryHelper.GetUsedSources(join.Condition, usedSources);

									moveUp = usedSources.Count < 2;
								}

								if (moveUp)
								{
									// Convert to old style JOIN
									//
									sqlQuery.From.Tables.Insert(tIndex + 1, join.Table);
									sqlQuery.From.Where.ConcatSearchCondition(join.Condition);

									t.Joins.RemoveAt(i);
									--i;
								}
							}
						}
					}
				}

			});

			return statement;
		}

		SqlStatement CorrectExistsAndIn(SqlStatement statement, DataOptions dataOptions)
		{
			statement = statement.Convert(1, (_, e) =>
			{
				if (e is SelectQuery sq)
				{
					if (sq.From.Tables.Count == 0 && sq.Select.Columns.Count == 1)
					{
						var column = sq.Select.Columns[0];
						if (column.Expression is SqlSearchCondition sc && sc.Predicates.Count == 1)
						{
							QueryHelper.ExtractPredicate(sc.Predicates[0], out var underlying, out var isNot);

							if (underlying is SqlPredicate.Exists exists)
							{
								var existsQuery = exists.SubQuery;

								// note that it still will not work as we need to rewrite union queries
								// see ConcatInAny test
								if (existsQuery.HasSetOperators)
									return e;

								existsQuery.Select.Columns.Clear();

								var newSearch = new SqlSearchCondition();

								var countExpr = SqlFunction.CreateCount(typeof(int), existsQuery.From.Tables[0]);
								if (!isNot)
									newSearch.AddGreater(countExpr, new SqlValue(0), dataOptions.LinqOptions.CompareNulls);
								else
									newSearch.AddEqual(countExpr, new SqlValue(0), dataOptions.LinqOptions.CompareNulls);

								existsQuery.Select.AddColumn(newSearch);

								return existsQuery;
							}

							if (underlying is SqlPredicate.InSubQuery inSubQuery)
							{
								var subquery = inSubQuery.SubQuery;
								subquery.Where.EnsureConjunction()
									.AddEqual(subquery.Select.Columns[0].Expression, inSubQuery.Expr1, dataOptions.LinqOptions.CompareNulls);

								subquery.Select.Columns.Clear();

								var newSearch = new SqlSearchCondition();
								var countExpr = SqlFunction.CreateCount(typeof(int), subquery.From.Tables[0]);

								isNot = isNot != inSubQuery.IsNot;

								if (!isNot)
									newSearch.AddGreater(countExpr, new SqlValue(0), dataOptions.LinqOptions.CompareNulls);
								else
									newSearch.AddEqual(countExpr, new SqlValue(0), dataOptions.LinqOptions.CompareNulls);

								subquery.Select.AddColumn(newSearch);

								return subquery;
							}
						}
					}
				}

				return e;
			});

			return statement;
		}
	}
}
