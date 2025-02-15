using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Async;

namespace LinqToDB.Linq
{
	public interface IExpressionQuery<out T> : IOrderedQueryable<T>, IQueryProviderAsync, IExpressionQuery
	{
		new Expression Expression { get; }
	}
}
