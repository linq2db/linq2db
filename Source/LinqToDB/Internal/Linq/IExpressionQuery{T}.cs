using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Async;

namespace LinqToDB.Internal.Linq
{
	public interface IExpressionQuery<out T> : IOrderedQueryable<T>, IQueryProviderAsync, IExpressionQuery
	{
		new Expression Expression { get; }
	}
}
