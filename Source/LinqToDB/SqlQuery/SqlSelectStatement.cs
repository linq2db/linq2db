namespace LinqToDB.SqlQuery
{
	public class SqlSelectStatement : SqlStatementWithQueryBase
	{
		public SqlSelectStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public SqlSelectStatement() : base(null)
		{
		}

		public override QueryType          QueryType  => QueryType.Select;
		public override QueryElementType   ElementType => QueryElementType.SelectStatement;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendTag(Tag);

			if (With?.Clauses.Count > 0)
			{
				writer
					.AppendElement(With)
					.AppendLine("--------------------------");
			}

			return writer.AppendElement(SelectQuery);
		}
	}
}
