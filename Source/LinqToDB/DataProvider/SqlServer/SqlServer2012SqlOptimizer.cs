namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;
	using SqlQuery;

	class SqlServer2012SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			var result = base.Finalize(statement);
			if (result.SelectQuery != null)
				CorrectSkip(result.SelectQuery);
			return result;
		}

		private void CorrectSkip(SelectQuery selectQuery)
		{
			((ISqlExpressionWalkable)selectQuery).Walk(false, e =>
			{
				var q = e as SelectQuery;
				if (q != null && q.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(q) && q.OrderBy.IsEmpty)
				{
					if (q.Select.Columns.Count == 0)
					{
						var source = q.Select.From.Tables[0].Source;
						var keys = source.GetKeys(true);

						foreach (var key in keys)
						{
							q.Select.AddNew(key);
						}
					}

					for (var i = 0; i < q.Select.Columns.Count; i++)
						q.OrderBy.ExprAsc(new SqlValue(i + 1));

					if (q.OrderBy.IsEmpty)
					{
						throw new LinqToDBException("Order by required for Skip operation.");
					}
				}
				return e;
			}
			);
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
