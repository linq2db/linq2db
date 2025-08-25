using System;
using System.Threading;
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

			return awaitable.GetAwaiter().GetResult();
		}

		public static T Run<T>(Func<CancellationToken, ValueTask<T>> task)
		{
			// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
			var awaitable = Task.Run(async () => await task(default).ConfigureAwait(false));

			return awaitable.GetAwaiter().GetResult();
		}

		public static T Run<T>(Func<Task<T>> task)
		{
			// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
			var awaitable = Task.Run(async () => await task().ConfigureAwait(false));

			return awaitable.GetAwaiter().GetResult();
		}

		public static T Run<T>(Func<CancellationToken, Task<T>> task)
		{
			// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
			var awaitable = Task.Run(async () => await task(default).ConfigureAwait(false));

			return awaitable.GetAwaiter().GetResult();
		}

		public static void Run(Func<CancellationToken, ValueTask> task)
		{
			Task.Run(async () => await task(default).ConfigureAwait(false)).GetAwaiter().GetResult();
		}

		public static void Run(Func<CancellationToken, Task> task)
		{
			Task.Run(async () => await task(default).ConfigureAwait(false)).GetAwaiter().GetResult();
		}

		public static void Run(Func<ValueTask> task)
		{
			Task.Run(async () => await task().ConfigureAwait(false)).GetAwaiter().GetResult();
		}
	}
}
