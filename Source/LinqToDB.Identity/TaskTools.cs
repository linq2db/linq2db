using System.Threading.Tasks;

namespace LinqToDB.Identity
{
	internal static class TaskCache<T>
	{
		/// <summary>
		///     Gets a completed <see cref="Task" /> with the value of <c>default(T)</c>.
		/// </summary>
		public static Task<T> DefaultCompletedTask { get; } = Task.FromResult(default(T));
	}

	internal static class TaskCache
	{
		/// <summary>
		///     A <see cref="Task" /> that's already completed successfully.
		/// </summary>
		/// <remarks>
		///     We're caching this in a static readonly field to make it more inlinable and avoid the volatile lookup done
		///     by <c>Task.CompletedTask</c>.
		/// </remarks>
		public static readonly Task CompletedTask = Task.FromResult(0);
	}
}