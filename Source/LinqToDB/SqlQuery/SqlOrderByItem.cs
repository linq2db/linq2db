﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlOrderByItem : IQueryElement
	{
		public SqlOrderByItem(ISqlExpression expression, bool isDescending)
		{
			Expression   = expression;
			IsDescending = isDescending;
		}

		public ISqlExpression Expression   { get; internal set; }
		public bool           IsDescending { get; }

		internal void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Expression = Expression.Walk(options, func)!;
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.OrderByItem;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			Expression.ToString(sb, dic);

			if (IsDescending)
				sb.Append(" DESC");

			return sb;
		}

		#endregion
	}
}
