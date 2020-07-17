namespace LinqToDB.DataProvider.Access
{
	using SqlProvider;
	using SqlQuery;

	class AccessSqlOptimizer : BasicSqlOptimizer
	{
		public AccessSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => CorrectAccessUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		private SqlUpdateStatement CorrectAccessUpdate(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.HasModifier)
				throw new LinqToDBException("Access does not support update query limitation");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override bool ConvertCountSubQuery(SelectQuery subQuery)
		{
			return !subQuery.Where.IsEmpty;
		}
	}
}
