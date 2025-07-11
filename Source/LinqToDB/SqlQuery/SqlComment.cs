using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlComment : QueryElement
	{
		public override QueryElementType ElementType => QueryElementType.Comment;

		public List<string> Lines { get; }

		public SqlComment()
		{
			Lines = new List<string>();
		}

		internal SqlComment(List<string> lines)
		{
			Lines = lines;
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			foreach (var part in Lines)
				writer
					.Append("-- ")
					.AppendLine(part);
			return writer;
		}

		public override int GetElementHashCode()
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
