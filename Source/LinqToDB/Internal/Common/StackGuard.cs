using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		// limit snapshot to avoid issues with option changes
		readonly int _maxHops = LinqToDB.Common.Configuration.TranslationThreadMaxHopCount;

		int _internalDepth;

		readonly struct StackScope : IDisposable
		{
			readonly StackGuard _guard;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public StackScope(StackGuard guard)
			{
				_guard = guard;
				_guard._internalDepth++;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
				_guard._internalDepth--;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IDisposable EnterScope()
		{
			return new StackScope(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryEnterOnCurrentStack()
		{
			return _maxHops < 0 || _internalDepth % 64 != 0 || StackProbe.TryEnsureSufficientExecutionStack();
		}

		public int Depth
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _internalDepth;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Enter()
		{
			_internalDepth++;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Exit()
		{
			_internalDepth--;
		}

		public T RunOnEmptyStack<T>(Func<T> action)
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

		internal static class StackProbe
		{
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
}
