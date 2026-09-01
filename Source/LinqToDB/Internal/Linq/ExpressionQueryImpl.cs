using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Linq
{
	sealed class ExpressionQueryImpl<T> : ExpressionQuery<T>
	{
		public ExpressionQueryImpl(IDataContext dataContext, Expression? expression)
		{
			Init(dataContext, expression);
		}

		public ExpressionQueryImpl(IDataContext dataContext, Expression? expression, object?[]? parameters)
		{
			Init(dataContext, expression);

			Parameters = parameters;
		}
	}

	static class ExpressionQueryImpl
	{
		public static IQueryable CreateQuery(Type entityType, IDataContext dataContext, Expression? expression)
		{
			var queryType = typeof(ExpressionQueryImpl<>).MakeGenericType(entityType);
			var query     = ActivatorExt.CreateInstance<IQueryable>(queryType, dataContext, expression);
			return query;
		}
	}
}
