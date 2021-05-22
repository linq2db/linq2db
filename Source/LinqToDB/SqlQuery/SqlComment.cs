using System.Collections.Generic;
using System.Text;

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

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			foreach (var part in Lines)
				sb
					.Append("-- ")
					.AppendLine(part);
			return sb;
		}
	}
}
