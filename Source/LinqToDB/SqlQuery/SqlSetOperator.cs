using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSetOperator : IQueryElement
	{
		public SqlSetOperator()
		{
		}

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
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append(" \n");
			switch (Operation)
			{
				case SetOperation.Union        : sb.Append("UNION");         break;
				case SetOperation.UnionAll     : sb.Append("UNION ALL");     break;
				case SetOperation.Except       : sb.Append("EXCEPT");        break;
				case SetOperation.ExceptAll    : sb.Append("EXCEPT ALL");    break;
				case SetOperation.Intersect    : sb.Append("INTERSECT");     break;
				case SetOperation.IntersectAll : sb.Append("INTERSECT ALL"); break;
			}
			sb.Append('\n');
			return ((IQueryElement)SelectQuery).ToString(sb, dic);
		}
	}
}
