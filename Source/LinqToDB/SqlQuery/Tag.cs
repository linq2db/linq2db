using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class Tag : IQueryElement, ICloneableElement
	{
		public QueryElementType ElementType => QueryElementType.Tag;

		public string? Value { get; set; }

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new Tag() { Value = Value };

			objectTree.Add(this, clone);

			return clone;
		}

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return BuildSql(sb, Value);
		}

		internal static StringBuilder BuildSql(StringBuilder sb, string? tagValue)
		{
			if (!string.IsNullOrEmpty(tagValue))
			{
				var escapedTag = tagValue!.Replace("\n", "\n-- ");
				sb.Append("-- ");
				sb.Append(escapedTag);
				sb.AppendLine();
			}

			return sb;
		}
	}
}
