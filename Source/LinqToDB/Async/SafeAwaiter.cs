using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides deadlock-free task await helpers.
	/// </summary>
	internal static class SafeAwaiter
	{
#if NATIVE_ASYNC
		public static T GetResult<T>(ValueTask<T> task)
		{
			if (task.IsCompleted)
				return task.Result;

			Task.Run(async () => await task.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).Wait();

			return task.Result;
		}

		public static void Await(ValueTask task)
		{
			if (task.IsCompleted)
			{
				if (!task.IsCompletedSuccessfully)
					task.AsTask().Wait();

				return;
			}

			Task.Run(async () => await task.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).Wait();
		}
#endif

		public static T GetResult<T>(Task<T> task)
		{
			if (task.IsCompleted)
				return task.Result;

			Task.Run(async () => await task.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).Wait();

			return task.Result;
		}

		public static void Await(Task task)
		{
			if (task.IsCompleted)
			{
				if (task.Status != TaskStatus.RanToCompletion)
					task.Wait();

				return;
			}

			Task.Run(async () => await task.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext)).Wait();
		}
	}
}
