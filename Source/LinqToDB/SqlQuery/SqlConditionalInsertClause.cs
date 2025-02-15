namespace LinqToDB.SqlQuery
{
	public class SqlConditionalInsertClause : IQueryElement
	{
		public SqlInsertClause     Insert { get; private set; }
		public SqlSearchCondition? When   { get; private set; }

		public SqlConditionalInsertClause(SqlInsertClause insert, SqlSearchCondition? when)
		{
			Insert = insert;
			When   = when;
		}

		public void Modify(SqlInsertClause insert, SqlSearchCondition? when)
		{
			Insert = insert;
			When   = when;
		}

		#region IQueryElement

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		QueryElementType IQueryElement.ElementType => QueryElementType.ConditionalInsertClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (When != null)
			{
				writer
					.Append("WHEN ")
					.AppendElement(When)
					.AppendLine(" THEN");
			}

			writer.AppendElement(Insert);

			return writer;
		}

		#endregion
	}
}
