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

		int? _maxHops;

		public void Reset()
		{
			_maxHops       = default;
			_hopCount      = default;
			_internalDepth = default;
		}

		public TResult? Enter<T, TResult>(Func<T, TResult> action, T arg)
		{
			_internalDepth++;
			_maxHops ??= LinqToDB.Common.Configuration.TranslationThreadMaxHopCount;

			// test _internalDepth for 1 so we can trigger hop before doing 64 calls and fail
			// when starting stack is already too small
			return _maxHops switch
			{
				>= 0 when _internalDepth % 64 == 1 && !TryEnsureSufficientExecutionStack() =>
					RunOnEmptyStack(action, arg),

				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Exit()
		{
			_internalDepth--;
		}

		TResult RunOnEmptyStack<T, TResult>(Func<T, TResult> action, T arg)
		{
			Interlocked.Increment(ref _hopCount);
			if (_hopCount > _maxHops)
			{
				throw new InsufficientExecutionStackException($"Too many stack hops (> {_maxHops.Value.ToString(CultureInfo.InvariantCulture)}). Recursion cannot safely continue.");
			}

			try
			{
				var task = Task.Factory.StartNew(
					static x =>
					{
						var (action, arg) = (ValueTuple<Func<T, TResult>, T>)x!;
						return action(arg);
					},
					(action, arg),
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
					throw new InsufficientExecutionStackException($"Too many stack hops (> {_maxHops!.Value.ToString(CultureInfo.InvariantCulture)}). Recursion cannot safely continue.", ex);
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
#if NET
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
