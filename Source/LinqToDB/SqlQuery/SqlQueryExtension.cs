using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlQueryExtension : IQueryElement, ISqlExpressionWalkable
	{
		public QueryElementType ElementType => QueryElementType.SqlQueryExtension;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb;
		}

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			return null;
		}
	}
}
