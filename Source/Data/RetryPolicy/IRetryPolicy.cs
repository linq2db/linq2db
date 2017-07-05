using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace LinqToDB.Data.RetryPolicy
{
	public interface IRetryPolicy
	{
		/// <summary>
		///     Executes the specified operation and returns the result.
		/// </summary>
		/// <param name="operation">
		///     A delegate representing an executable operation that returns the result of type <typeparamref name="TResult" />.
		/// </param>
		/// <typeparam name="TResult"> The return type of <paramref name="operation" />. </typeparam>
		/// <returns> The result from the operation. </returns>
		TResult Execute<TResult>([NotNull] Func<TResult> operation);

		void Execute([NotNull] Action operation);

#if !NOASYNC
		/// <summary>
		///     Executes the specified asynchronous operation and returns the result.
		/// </summary>
		/// <param name="operation">
		///     A function that returns a started task of type <typeparamref name="TResult" />.
		/// </param>
		/// <param name="cancellationToken">
		///     A cancellation token used to cancel the retry operation, but not operations that are already in flight
		///     or that already completed successfully.
		/// </param>
		/// <typeparam name="TResult">
		///   The result type of the <see cref="System.Threading.Tasks.Task{T}" /> returned by <paramref name="operation" />.
		/// </typeparam>
		/// <returns>
		///     A task that will run to completion if the original task completes successfully (either the
		///     first time or after retrying transient failures). If the task fails with a non-transient error or
		///     the retry limit is reached, the returned task will become faulted and the exception must be observed.
		/// </returns>
		Task<TResult> ExecuteAsync<TResult>(
			[NotNull] Func<CancellationToken, Task<TResult>> operation,
			CancellationToken cancellationToken = default(CancellationToken));

		Task ExecuteAsync(
			[NotNull] Func<CancellationToken,Task> operation,
			CancellationToken cancellationToken = default(CancellationToken));
#endif
	}
}