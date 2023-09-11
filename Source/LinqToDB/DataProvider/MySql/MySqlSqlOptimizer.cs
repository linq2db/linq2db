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

			statement = CorrectUpdateTable(statement, dataOptions);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			CorrectUpdateSetters(statement);

			return statement;
		}

	}
}
