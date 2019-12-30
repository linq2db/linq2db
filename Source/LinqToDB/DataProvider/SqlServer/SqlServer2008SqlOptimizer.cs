using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServer2008SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2008SqlOptimizer(SqlProviderFlags sqlProviderFlags) : this(sqlProviderFlags, SqlServerVersion.v2008)
		{
		}

		protected SqlServer2008SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			statement = SeparateDistinctFromPagination(statement);
			statement = ReplaceTakeSkipWithRowNumber(statement, false);

			CorrectRootSkip(statement.SelectQuery);

			return statement;
		}

		protected void CorrectRootSkip(SelectQuery selectQuery)
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

			if (expr is SqlFunction function)
				return ConvertConvertFunction(function);

			return expr;
		}
	}
}
