using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	public interface IQueryProviderAsync : IQueryProvider
	{
		Task<IAsyncEnumerable<TResult>> ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken);

		Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);

		Expression Expression { get; }
	}
}
