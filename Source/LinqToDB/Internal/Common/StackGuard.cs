using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Common
{
	public sealed class StackGuard
	{
		// Use interlocked to be safe when execution switches threads.
		int _hopCount;
		int _internalDepth;

		// limit snapshot to avoid issues with option changes
		int _maxHops = LinqToDB.Common.Configuration.TranslationThreadMaxHopCount;

		public void Reset()
		{
			_maxHops = LinqToDB.Common.Configuration.TranslationThreadMaxHopCount;
			_hopCount = default;
			_internalDepth = default;
		}

		public T? Enter<T>(Func<T, T> action, T arg)
			where T : class
		{
			_internalDepth++;

			// test _internalDepth for 1 so we can trigger hop before doing 64 calls and fail
			// when starting stack is already too small
			if (_maxHops >= 0 && _internalDepth % 64 == 1 && !TryEnsureSufficientExecutionStack())
				return RunOnEmptyStack(() => action(arg));

			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Exit()
		{
			_internalDepth--;
		}

		T RunOnEmptyStack<T>(Func<T> action)
		{
			Interlocked.Increment(ref _hopCount);
			if (_hopCount > _maxHops)
			{
				throw new InsufficientExecutionStackException($"Too many stack hops (> {_maxHops.ToString(CultureInfo.InvariantCulture)}). Recursion cannot safely continue.");
			}

			try
			{
				var task = Task.Factory.StartNew(
					action,
					CancellationToken.None,
					TaskCreationOptions.DenyChildAttach,
					TaskScheduler.Default); // ThreadPool

				// Avoid Task.Wait/Result (AggregateException and potential inlining quirks).
				((IAsyncResult)task).AsyncWaitHandle.WaitOne();

				try
				{
					// Rethrows result or original exception type
					return task.GetAwaiter().GetResult();
				}
				catch (InsufficientExecutionStackException ex)
				{
					throw new InsufficientExecutionStackException($"Too many stack hops (> {_maxHops.ToString(CultureInfo.InvariantCulture)}). Recursion cannot safely continue.", ex);
				}
			}
			finally
			{
				Interlocked.Decrement(ref _hopCount);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryEnsureSufficientExecutionStack()
		{
#if !NETFRAMEWORK
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
