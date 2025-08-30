namespace LinqToDB.Internal.SqlQuery
{
	public abstract class ClauseBase : QueryElement
	{
		protected ClauseBase(SelectQuery? selectQuery)
		{
			SelectQuery = selectQuery!;
		}

		public SqlSelectClause  Select  => SelectQuery.Select;
		public SqlFromClause    From    => SelectQuery.From;
		public SqlWhereClause   Where   => SelectQuery.Where;
		public SqlGroupByClause GroupBy => SelectQuery.GroupBy;
		public SqlHavingClause  Having  => SelectQuery.Having;
		public SqlOrderByClause OrderBy => SelectQuery.OrderBy;

		protected internal SelectQuery SelectQuery { get; private set; } = null!;

		internal void SetSqlQuery(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}

	public abstract class ClauseBase<T1> : QueryElement
		where T1 : ClauseBase<T1>
	{
		protected ClauseBase(SelectQuery? selectQuery)
		{
			SelectQuery = selectQuery!;
		}

		public SqlSelectClause  Select  => SelectQuery.Select;
		public SqlFromClause    From    => SelectQuery.From;
		public SqlGroupByClause GroupBy => SelectQuery.GroupBy;
		public SqlHavingClause  Having  => SelectQuery.Having;
		public SqlOrderByClause OrderBy => SelectQuery.OrderBy;

		protected internal SelectQuery SelectQuery { get; private set; } = null!;

		internal void SetSqlQuery(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}
}
