using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IQueryProviderEx : IQueryProvider
	{
		Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken token, TaskCreationOptions options);
	}
}
