﻿namespace LinqToDB.SqlQuery
{
	public class SqlHavingClause : ClauseBase<SqlHavingClause>
	{
		internal SqlHavingClause(SelectQuery selectQuery) : base(selectQuery)
		{
			SearchCondition = new SqlSearchCondition();
		}

		internal SqlHavingClause(SqlSearchCondition searchCondition) : base(null)
		{
			SearchCondition = searchCondition;
		}

		public SqlSearchCondition SearchCondition { get; internal set; }

		public bool IsEmpty => SearchCondition.Predicates.Count == 0;

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.HavingClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!IsEmpty)
			{
				writer
					.DebugAppendUniqueId(this)
					.AppendLine()
					.AppendLine("HAVING");

				using (writer.IndentScope())
					writer.AppendElement(SearchCondition);

			}

			return writer;
		}

		#endregion

		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
