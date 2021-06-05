using System;
using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides deadlock-free task await helpers.
	/// </summary>
	internal static class SafeAwaiter
	{
#if NATIVE_ASYNC
		public static T Run<T>(Func<ValueTask<T>> task)
		{
			// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
			var awaitable = Task.Run(async () => await task().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext));
			awaitable.Wait();

			return awaitable.Result;
		}

		public static void Run(Func<ValueTask> task)
		{
			Task.Run(async () => await task().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).Wait();
		}
#endif
	}
}
