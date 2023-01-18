namespace LinqToDB.SqlQuery
{
	public class SqlSetOperator : IQueryElement
	{
		public SqlSetOperator(SelectQuery selectQuery, SetOperation operation)
		{
			SelectQuery = selectQuery;
			Operation   = operation;
		}

		public SelectQuery  SelectQuery { get; }
		public SetOperation Operation   { get; }

		public QueryElementType ElementType => QueryElementType.SetOperator;

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer.AppendLine(" ");

			switch (Operation)
			{
				case SetOperation.Union        : writer.Append("UNION");         break;
				case SetOperation.UnionAll     : writer.Append("UNION ALL");     break;
				case SetOperation.Except       : writer.Append("EXCEPT");        break;
				case SetOperation.ExceptAll    : writer.Append("EXCEPT ALL");    break;
				case SetOperation.Intersect    : writer.Append("INTERSECT");     break;
				case SetOperation.IntersectAll : writer.Append("INTERSECT ALL"); break;
			}

			writer.AppendLine();

			using(writer.WithScope())
				writer.AppendElement(SelectQuery);

			return writer;
		}
	}
}
