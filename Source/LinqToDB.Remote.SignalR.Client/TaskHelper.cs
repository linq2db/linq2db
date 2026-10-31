using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Remote.SignalR
{
	static class TaskHelper
	{
		/// <summary>
		/// Waits for a shared task with the caller's own token and timeout, without cancelling the task itself.
		/// </summary>
		public static async Task<T> WaitAsync<T>(Task<T> task, CancellationToken cancellationToken, TimeSpan? timeout = null)
		{
			await WaitAsync((Task)task, cancellationToken, timeout).ConfigureAwait(false);

			return await task.ConfigureAwait(false);
		}

		/// <inheritdoc cref="WaitAsync{T}(Task{T}, CancellationToken, TimeSpan?)"/>
		public static Task WaitAsync(Task task, CancellationToken cancellationToken, TimeSpan? timeout = null)
		{
#if NET8_0_OR_GREATER
			return task.WaitAsync(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
#else
			return WaitCoreAsync(task, cancellationToken, timeout);
#endif
		}

#if !NET8_0_OR_GREATER
		static async Task WaitCoreAsync(Task task, CancellationToken cancellationToken, TimeSpan? timeout)
		{
			if (task.IsCompleted || !cancellationToken.CanBeCanceled && timeout == null)
			{
				await task.ConfigureAwait(false);
				return;
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			if (timeout != null)
				cts.CancelAfter(timeout.Value);

			var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			using (cts.Token.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), stopped))
			{
				if (await Task.WhenAny(task, stopped.Task).ConfigureAwait(false) != task)
				{
					cancellationToken.ThrowIfCancellationRequested();
					throw new TimeoutException();
				}
			}

			await task.ConfigureAwait(false);
		}
#endif
	}
}
