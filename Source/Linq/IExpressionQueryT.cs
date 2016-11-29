﻿using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	public interface IExpressionQuery<
#if !SL4
		out
#endif
		T> : IOrderedQueryable<T>, IQueryProvider, IExpressionQuery
	{
		new Expression Expression { get; set; }
	}
}
