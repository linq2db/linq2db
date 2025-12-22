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

		public readonly struct StackScope : IDisposable
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
		public StackScope EnterScope()
		{
			return new StackScope(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryEnterOnCurrentStack()
		{
			return _internalDepth % 64 != 0 || StackProbe.TryEnsureSufficientExecutionStack();
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
			if (_hopCount > LinqToDB.Common.Configuration.TranslationThreadMaxHopCount)
			{
				throw new InsufficientExecutionStackException($"Too many stack hops (>{LinqToDB.Common.Configuration.TranslationThreadMaxHopCount.ToString(CultureInfo.InvariantCulture)}). Recursion cannot safely continue.");
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
