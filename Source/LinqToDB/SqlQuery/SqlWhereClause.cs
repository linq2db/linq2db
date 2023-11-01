using System;

namespace LinqToDB.SqlQuery
{
	public class SqlWhereClause : ClauseBase<SqlWhereClause,SqlWhereClause.Next>, IQueryElement
	{
		public class Next : ClauseBase
		{
			internal Next(SqlWhereClause parent) : base(parent.SelectQuery)
			{
				_parent = parent;
			}

			readonly SqlWhereClause _parent;

			public SqlWhereClause Or  => _parent.SetOr(true);
			public SqlWhereClause And => _parent.SetOr(false);
		}

		internal SqlWhereClause(SelectQuery selectQuery) : base(selectQuery)
		{
			SearchCondition = new SqlSearchCondition();
		}

		internal SqlWhereClause(SqlSearchCondition searchCondition) : base(null)
		{
			SearchCondition = searchCondition;
		}

		public SqlSearchCondition SearchCondition { get; internal set; }

		public bool IsEmpty => SearchCondition.Conditions.Count == 0;

		protected override SqlSearchCondition Search => SearchCondition;

		protected override Next GetNext()
		{
			return new Next(this);
		}

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.WhereClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (Search.Conditions.Count == 0)
				return writer;

			writer
				.AppendLine()
				.AppendLine("WHERE");

			using (writer.WithScope())
				writer.AppendElement(Search);

			return writer;
		}

		#endregion

		public void Cleanup()
		{
			SearchCondition.Conditions.Clear();
		}
	}
}
