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
			try
			{
				// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
				var awaitable = Task.Run(async () => await task().ConfigureAwait(false));
				awaitable.Wait();

				return awaitable.Result;
			}
			catch (AggregateException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}

		public static T Run<T>(Func<CancellationToken, ValueTask<T>> task)
		{
			try
			{
				// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
				var awaitable = Task.Run(async () => await task(default).ConfigureAwait(false));
				awaitable.Wait();
				return awaitable.Result;
			}
			catch (AggregateException ex) when(ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}

		public static T Run<T>(Func<Task<T>> task)
		{
			try
			{
				// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
				var awaitable = Task.Run(async () => await task().ConfigureAwait(false));
				awaitable.Wait();

				return awaitable.Result;
			}
			catch (AggregateException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}

		public static T Run<T>(Func<CancellationToken, Task<T>> task)
		{
			try
			{
				// awaited ValueTask retrieved in Task.Run context as doing it in main thread could cause deadlock too
				var awaitable = Task.Run(async () => await task(default).ConfigureAwait(false));
				awaitable.Wait();

				return awaitable.Result;
			}
			catch (AggregateException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}

		public static void Run(Func<CancellationToken, ValueTask> task)
		{
			try
			{
				Task.Run(async () => await task(default).ConfigureAwait(false)).Wait();
			}
			catch (AggregateException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}

		public static void Run(Func<CancellationToken, Task> task)
		{
			try
			{
				Task.Run(async () => await task(default).ConfigureAwait(false)).Wait();
			}
			catch (AggregateException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}

		public static void Run(Func<ValueTask> task)
		{
			try
			{
				Task.Run(async () => await task().ConfigureAwait(false)).Wait();
			}
			catch (AggregateException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}
	}
}
