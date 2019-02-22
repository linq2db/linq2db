using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Async;

	public interface IExpressionQuery<out T> : IOrderedQueryable<T>, IQueryProviderAsync, IExpressionQuery
	{
		new Expression Expression { get; set; }
	}
}
