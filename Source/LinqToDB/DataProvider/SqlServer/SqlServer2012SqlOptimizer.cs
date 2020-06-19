using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlProvider;

	class SqlServer2012SqlOptimizer : SqlServerSqlOptimizer
	{
		public SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags) : this(sqlProviderFlags, SqlServerVersion.v2012)
		{
		}

		protected SqlServer2012SqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion version) : base(sqlProviderFlags, version)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			// SQL Server 2012 supports OFFSET/FETCH providing there is an ORDER BY
			// UPDATE queries do not directly support ORDER BY, TOP, OFFSET, or FETCH, but they are supported in subqueries

			if (statement.IsUpdate()) statement = WrapRootTakeSkipOrderBy(statement);
			statement = AddOrderByForSkip(statement);

			return statement;
		}

		/// <summary>
		/// Adds an ORDER BY clause to queries using OFFSET/FETCH, if none exists
		/// </summary>
		protected SqlStatement AddOrderByForSkip(SqlStatement statement)
		{
			ConvertVisitor.Convert(statement, (visitor, element) => {
				if (element is SelectQuery query && query.Select.SkipValue != null && query.OrderBy.IsEmpty)
					query.OrderBy.ExprAsc(new SqlValue(typeof(int), 1));
				return element;
			});
			return statement;
		}
	}
}
