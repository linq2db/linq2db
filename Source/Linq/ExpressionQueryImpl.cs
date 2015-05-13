using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class ExpressionQueryImpl<T> : ExpressionQuery<T>, IExpressionQuery
	{
		public ExpressionQueryImpl(IDataContext dataContext, Expression expression)
			: base(dataContext, expression)
		{
		}

		public override string ToString()
		{
			return SqlText;
		}
	}
}
