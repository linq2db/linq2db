using System;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Async
{
	/// <summary>
	/// Provides deadlock-free task await helpers.
	/// </summary>
	internal static class SafeAwaiter
	{
		public static T Run<T>(Func<ValueTask<T>> task)
		{
			// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
			var awaitable = Task.Run(async () => await task().ConfigureAwait(false));
			awaitable.Wait();

			return awaitable.Result;
		}

		public static T Run<T>(Func<Task<T>> task)
		{
			// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
			var awaitable = Task.Run(async () => await task().ConfigureAwait(false));
			awaitable.Wait();

			return awaitable.Result;
		}

		public static void Run(Func<ValueTask> task)
		{
			Task.Run(async () => await task().ConfigureAwait(false)).Wait();
		}
	}
}
