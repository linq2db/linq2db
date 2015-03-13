using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class ExpressionQueryOldImpl<T> : ExpressionQueryOld<T>, IExpressionQuery
	{
		public ExpressionQueryOldImpl(IDataContextInfo dataContext, Expression expression)
		{
			Init(dataContext, expression);
		}

		public override string ToString()
		{
			return SqlText;
		}
	}
}
