using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Common
{
	public sealed class StackGuard
	{
		// How often (in recursion levels) Enter probes the remaining stack. Small enough that the stack
		// consumed between two probes stays well inside the margin TryEnsureSufficientExecutionStack
		// guarantees, so deep recursion always hops gracefully instead of overflowing between probes.
		const int StackProbeInterval = 8;

		// Use interlocked to be safe when execution switches threads.
		int _hopCount;
		int _internalDepth;

		int? _maxHops;

		[DebuggerStepThrough]
		public void Reset()
		{
			_maxHops       = default;
			_hopCount      = default;
			_internalDepth = default;
		}

		[DebuggerStepThrough]
		public TResult? Enter<T, TResult>(Func<T, TResult> action, T arg)
		{
			_internalDepth++;
			_maxHops ??= LinqToDB.Common.Configuration.TranslationThreadMaxHopCount;

			// Probe the stack every StackProbeInterval recursion levels (and on the very first call, so
			// an already-too-small starting stack is caught). The interval bounds how much stack can be
			// consumed between two probes: TryEnsureSufficientExecutionStack only guarantees a fixed
			// margin, so if (interval x per-level-stack) exceeds that margin the stack can overflow
			// BETWEEN probes - an uncatchable StackOverflowException instead of a graceful hop. At ~220
			// bytes/level (x64) the old interval of 64 used ~14KB between probes, which overran the
			// margin on tighter configs (net462 / Linux, larger frames) and produced a flaky hard SO.
			// 8 keeps it under ~2KB, comfortably inside the margin on every TFM.
			return _maxHops switch
			{
				>= 0 when _internalDepth % StackProbeInterval == 1 && !TryEnsureSufficientExecutionStack() =>
					RunOnEmptyStack(action, arg),

				_ => default,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[DebuggerStepThrough]
		public void Exit()
		{
			_internalDepth--;
		}

		[DebuggerStepThrough]
		TResult RunOnEmptyStack<T, TResult>(Func<T, TResult> action, T arg)
		{
			Interlocked.Increment(ref _hopCount);
			if (_hopCount > _maxHops)
			{
				throw new InsufficientExecutionStackException($"Too many stack hops (> {_maxHops.Value.ToString(CultureInfo.InvariantCulture)}). Recursion cannot safely continue.");
			}

			try
			{
#pragma warning disable LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
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
#pragma warning restore LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.

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
		[DebuggerStepThrough]
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
