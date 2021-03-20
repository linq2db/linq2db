using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlComment : IQueryElement, ICloneableElement
	{
		public QueryElementType ElementType => QueryElementType.Comment;

		public List<string> Parts { get; private set; }

		public SqlComment() 
		{ 
			Parts = new List<string>(); 
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlComment();
			Parts.ForEach(p => clone.Parts.Add(p));

			objectTree.Add(this, clone);

			return clone;
		}

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb;
		}
	}
}
