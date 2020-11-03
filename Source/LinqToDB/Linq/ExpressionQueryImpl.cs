using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	class ExpressionQueryImpl<T> : ExpressionQuery<T>
	{
		public ExpressionQueryImpl(IDataContext dataContext, Expression? expression)
		{
			Init(dataContext, expression);
		}

		public override string ToString()
		{
			return SqlText;
		}
	}

	static class ExpressionQueryImpl
	{
		public static IQueryable CreateQuery(Type entityType, IDataContext dataContext, Expression? expression)
		{
			var queryType = typeof(ExpressionQueryImpl<>).MakeGenericType(entityType);
			var query     = (IQueryable)Activator.CreateInstance(queryType, dataContext, expression)!;
			return query;
		}
	}
}
