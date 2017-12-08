using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlUnion : IQueryElement
	{
		public SqlUnion()
		{
		}

		public SqlUnion(SelectQuery selectQuery, bool isAll)
		{
			SelectQuery = selectQuery;
			IsAll       = isAll;
		}

		public SelectQuery SelectQuery { get; }
		public bool IsAll              { get; }

		public QueryElementType ElementType => QueryElementType.Union;

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append(" \nUNION").Append(IsAll ? " ALL" : "").Append(" \n");
			return ((IQueryElement)SelectQuery).ToString(sb, dic);
		}
	}
}
