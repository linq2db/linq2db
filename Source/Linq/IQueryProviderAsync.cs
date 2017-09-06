using System;
using System.Linq;
using System.Linq.Expressions;

#if !SL4
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	public interface IQueryProviderAsync : IQueryProvider
	{
#if !NOASYNC && !SL4
		Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token);
#endif
	}
}
