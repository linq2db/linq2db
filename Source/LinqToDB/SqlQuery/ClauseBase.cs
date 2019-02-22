namespace LinqToDB.SqlQuery
{
	public abstract class ClauseBase
	{
		protected ClauseBase(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}

		public SqlSelectClause  Select  => SelectQuery.Select;
		public SqlFromClause    From    => SelectQuery.From;
		public SqlWhereClause   Where   => SelectQuery.Where;
		public SqlGroupByClause GroupBy => SelectQuery.GroupBy;
		public SqlWhereClause   Having  => SelectQuery.Having;
		public SqlOrderByClause OrderBy => SelectQuery.OrderBy;
		public SelectQuery      End() { return SelectQuery; }

		protected internal SelectQuery SelectQuery { get; private set; }

		internal void SetSqlQuery(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}

	public abstract class ClauseBase<T1,T2> : ConditionBase<T1,T2>
		where T1 : ClauseBase<T1,T2>
	{
		protected ClauseBase(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}

		public SqlSelectClause  Select  => SelectQuery.Select;
		public SqlFromClause    From    => SelectQuery.From;
		public SqlGroupByClause GroupBy => SelectQuery.GroupBy;
		public SqlWhereClause   Having  => SelectQuery.Having;
		public SqlOrderByClause OrderBy => SelectQuery.OrderBy;
		public SelectQuery      End() { return SelectQuery; }

		protected internal SelectQuery SelectQuery { get; private set; }

		internal void SetSqlQuery(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}
	}
}
