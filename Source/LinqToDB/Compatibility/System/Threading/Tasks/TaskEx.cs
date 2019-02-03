using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	static class TaskEx
	{
		public static Task<TResult> Run<TResult>(Func<TResult> function)
		{
			return Task.Run(function);
		}

		public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
		{
			return Task.Run(function, cancellationToken);
		}

		public static Task Run(Action function, CancellationToken cancellationToken)
		{
			return Task.Run(function, cancellationToken);
		}

		public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
		{
			return Task.Delay(delay, cancellationToken);
		}

#if NET45
		private static readonly Task _completedTask = Task.FromResult(false);
#endif
		public static Task CompletedTask
		{
			get
			{
#if NET45
				return _completedTask;
#else
				return Task.CompletedTask;
#endif
			}
		}
	}
}
