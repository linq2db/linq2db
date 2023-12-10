using System;

namespace LinqToDB.SqlQuery
{
	public class SqlWhereClause : ClauseBase<SqlWhereClause>, IQueryElement
	{
		internal SqlWhereClause(SelectQuery selectQuery) : base(selectQuery)
		{
			SearchCondition = new SqlSearchCondition();
		}

		internal SqlWhereClause(SqlSearchCondition searchCondition) : base(null)
		{
			SearchCondition = searchCondition;
		}

		public SqlSearchCondition SearchCondition { get; internal set; }

		public bool IsEmpty => SearchCondition.Predicates.Count == 0;

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
			if (SearchCondition.Predicates.Count == 0)
				return writer;

			writer
				.AppendLine()
				.AppendLine("WHERE");

			using (writer.WithScope())
				writer.AppendElement(SearchCondition);

			return writer;
		}

		#endregion

		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
