﻿using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;

namespace LinqToDB.Linq
{
	sealed class ExpressionQueryImpl<T> : ExpressionQuery<T>
	{
		public ExpressionQueryImpl(IDataContext dataContext, Expression? expression)
		{
			Init(dataContext, expression);
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
