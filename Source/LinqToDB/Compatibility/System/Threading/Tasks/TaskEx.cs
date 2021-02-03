using System.Runtime.CompilerServices;
using LinqToDB.Common.Internal;

namespace System.Threading.Tasks
{
	static class TaskEx
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
		{
			return Task.Run(function, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
		{
			return Task.Delay(delay, cancellationToken);
		}

		public static Task CompletedTask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if NET45
				return TaskCache.False;
#else
				return Task.CompletedTask;
#endif
			}
		}
	}
}
