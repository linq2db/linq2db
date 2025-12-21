namespace LinqToDB.Internal.Common
{
	using System;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using System.Threading.Tasks;

	internal sealed class StackGuard
	{
		private const int MaxHops = 256;

		// Use interlocked to be safe when execution switches threads.
		private int _hopCount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryEnterOnCurrentStack()
		{
			if (Volatile.Read(ref _hopCount) >= MaxHops)
				throw new InsufficientExecutionStackException(
					$"Too many stack hops (>{MaxHops}). Recursion cannot safely continue.");

			return StackProbe.TryEnsureSufficientExecutionStack();
		}

		public T RunOnEmptyStack<T>(Func<T> action)
		{
			Interlocked.Increment(ref _hopCount);
			try
			{
				var task = Task.Factory.StartNew(
					action,
					CancellationToken.None,
					TaskCreationOptions.DenyChildAttach,
					TaskScheduler.Default); // ThreadPool

				// Avoid Task.Wait/Result (AggregateException and potential inlining quirks).
				((IAsyncResult)task).AsyncWaitHandle.WaitOne();

				// Rethrows original exception type (no AggregateException).
				return task.GetAwaiter().GetResult();
			}
			finally
			{
				Interlocked.Decrement(ref _hopCount);
			}
		}

		internal static class StackProbe
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static bool TryEnsureSufficientExecutionStack()
			{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
		        // Available on netstandard/.NET (not on .NET Framework)
		        return RuntimeHelpers.TryEnsureSufficientExecutionStack();
#else
				// .NET Framework: only EnsureSufficientExecutionStack() exists
				try
				{
					RuntimeHelpers.EnsureSufficientExecutionStack();
					return true;
				}
				catch (InsufficientExecutionStackException)
				{
					return false;
				}
#endif
			}
		}
	}
}
