namespace LinqToDB.SqlQuery
{
	public class SqlComment : IQueryElement
	{
		public QueryElementType ElementType => QueryElementType.Comment;

		public List<string> Lines { get; }

		public SqlComment()
		{
			Lines = new List<string>();
		}

		internal SqlComment(List<string> lines)
		{
			Lines = lines;
		}

		public QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			foreach (var part in Lines)
				writer
					.Append("-- ")
					.AppendLine(part);
			return writer;
		}
	}
}
