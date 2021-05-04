using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlComment : IQueryElement, ICloneableElement
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

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlComment();

			foreach (var part in Lines)
				clone.Lines.Add(part);

			objectTree.Add(this, clone);

			return clone;
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
