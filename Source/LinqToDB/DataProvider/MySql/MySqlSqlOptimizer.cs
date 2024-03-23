using System;

namespace LinqToDB.DataProvider.MySql
{
	using SqlProvider;
	using SqlQuery;

	sealed class MySqlSqlOptimizer : BasicSqlOptimizer
	{
		public MySqlSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new MySqlSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			return statement.QueryType switch
			{
				QueryType.Update => CorrectMySqlUpdate((SqlUpdateStatement)statement, dataOptions),
				QueryType.Delete => PrepareDelete     ((SqlDeleteStatement)statement),
				_                => statement,
			};
		}

		static SqlStatement PrepareDelete(SqlDeleteStatement statement)
		{
			var tables = statement.SelectQuery.From.Tables;

			if (statement.Output != null && tables.Count == 1 && tables[0].Joins.Count == 0)
				tables[0].Alias = "$";

			return statement;
		}

		private SqlUpdateStatement CorrectMySqlUpdate(SqlUpdateStatement statement, DataOptions dataOptions)
		{
			if (statement.SelectQuery.Select.SkipValue != null)
				throw new LinqToDBException("MySql does not support Skip in update query");

			statement = CorrectUpdateTable(statement, leaveUpdateTableInQuery: true, dataOptions);

			// Mysql do not allow Update table usage in FROM clause. Moving it to subquery
			// https://stackoverflow.com/a/14302701/10646316
			// See UpdateIssue319Regression test
			var changed = false;
			statement.SelectQuery.VisitParentFirst(e =>
			{
				// Skip root query FROM clause
				if (ReferenceEquals(e, statement.SelectQuery.From))
				{
					return false;
				}

				if (e is SqlTableSource ts)
				{
					if (ts.Source is SqlTable table && !ReferenceEquals(table, statement.Update.Table) && QueryHelper.IsEqualTables(table, statement.Update.Table))
					{
						var subQuery = new SelectQuery();
						subQuery.DoNotRemove = true;
						subQuery.From.Tables.Add(new SqlTableSource(table, ts.Alias));
						ts.Source = subQuery;
						changed = true;

						return false;
					}
				}

				return true;
			});

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			CorrectUpdateSetters(statement);

			if (changed)
			{
				var corrector = new SqlQueryColumnNestingCorrector();
				corrector.CorrectColumnNesting(statement);
			}

			return statement;
		}

	}
}
