using System;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlWhereClause : ClauseBase<SqlWhereClause>
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

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.WhereClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!IsEmpty)
			{
				writer
					//.DebugAppendUniqueId(this)
					.AppendLine()
					.AppendLine("WHERE");

				using (writer.IndentScope())
					writer.AppendElement(SearchCondition);

			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				ElementType,
				SearchCondition.GetElementHashCode()
			);
		}

		#endregion

		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
