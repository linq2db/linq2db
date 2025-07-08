using System;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlWhereClause : ClauseBase<SqlWhereClause>
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
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(SearchCondition.GetElementHashCode());
			return hash.ToHashCode();
		}

		#endregion

		public void Cleanup()
		{
			SearchCondition.Predicates.Clear();
		}
	}
}
