using System;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data.RetryPolicy;

namespace Tests
{
	class TestRetryPolicy : IRetryPolicy
	{
		public TResult Execute<TResult>(Func<TResult> operation)
		{
			return operation();
		}

		public void Execute(Action operation)
		{
			operation();
		}

		public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default(CancellationToken))
		{
			return operation(cancellationToken);
		}

		public Task ExecuteAsync(Func<CancellationToken,Task> operation, CancellationToken cancellationToken = new CancellationToken())
		{
			return operation(cancellationToken);
		}
	}
}
