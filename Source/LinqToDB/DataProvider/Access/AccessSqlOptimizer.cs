using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Access
{
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

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			statement = CorrectInnerJoins(statement);
			statement = CorrectExists(statement);

			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => CorrectAccessUpdate ((SqlUpdateStatement)statement, dataOptions),
				_                => statement,
			};
		}

		private SqlUpdateStatement CorrectAccessUpdate(SqlUpdateStatement statement, DataOptions dataOptions)
		{
			if (statement.SelectQuery.Select.HasModifier)
				throw new LinqToDBException("Access does not support update query limitation");

			statement = CorrectUpdateTable(statement, leaveUpdateTableInQuery: true, dataOptions);

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

		SqlStatement CorrectExists(SqlStatement statement)
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

							if (underlying is SqlPredicate.FuncLike { Function.Name: "EXISTS" } funcLike)
							{
								var existsQuery = (SelectQuery)funcLike.Function.Parameters[0];
								existsQuery.Select.Columns.Clear();

								var newSearch = new SqlSearchCondition();

								var countExpr = SqlFunction.CreateCount(typeof(int), existsQuery.From.Tables[0]);
								if (!isNot)
									newSearch.AddGreater(countExpr, new SqlValue(0), false);
								else
									newSearch.AddEqual(countExpr, new SqlValue(0), false);

								existsQuery.Select.AddColumn(newSearch);

								return existsQuery;
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
