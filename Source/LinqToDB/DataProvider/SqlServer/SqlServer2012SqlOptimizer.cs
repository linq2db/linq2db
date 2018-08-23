namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2012SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			if (statement.IsUpdate())
				statement = ReplaceTakeSkipWithRowNumber(statement, false);
			else
			{
				statement = ReplaceTakeSkipWithRowNumber(statement, true);
				CorrectRootSkip(statement.SelectQuery);
			}

			return statement;
		}

		private void CorrectRootSkip(SelectQuery selectQuery)
		{
			if (selectQuery != null && selectQuery.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery) && selectQuery.OrderBy.IsEmpty)
			{
				if (selectQuery.Select.Columns.Count == 0)
				{
					var source = selectQuery.Select.From.Tables[0].Source;
					var keys = source.GetKeys(true);

					foreach (var key in keys)
					{
						selectQuery.Select.AddNew(key);
					}
				}

				for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
					selectQuery.OrderBy.ExprAsc(new SqlValue(i + 1));

				if (selectQuery.OrderBy.IsEmpty)
				{
					throw new LinqToDBException("Order by required for Skip operation.");
				}
			}
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
				return ConvertConvertFunction((SqlFunction)expr);

			return expr;
		}
	}
}
