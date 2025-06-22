using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlComment : IQueryElement
	{
#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
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

		public int GetElementHashCode()
		{
			var hash = new HashCode();
			foreach (var line in Lines)
			{
				hash.Add(line);
			}
			return hash.ToHashCode();
		}
	}
}
