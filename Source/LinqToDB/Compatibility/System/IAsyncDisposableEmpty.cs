using System;
using System.Threading.Tasks;

namespace LinqToDB
{
	using Common.Internal;

	/// <summary>
	/// Helps leverage of pain to work with <c>await using nullable_value</c> code.
	/// </summary>
	internal static class EmptyIAsyncDisposable
	{
#if NATIVE_ASYNC
		public static readonly IAsyncDisposable Instance = new NoopImplementation();

		private sealed class NoopImplementation : IAsyncDisposable
		{
			ValueTask IAsyncDisposable.DisposeAsync() => default;
		}
#else
		public static readonly Async.IAsyncDisposable Instance = new NoopImplementation();

		private sealed class NoopImplementation : Async.IAsyncDisposable
		{
			Task Async.IAsyncDisposable.DisposeAsync() => TaskCache.CompletedTask;
		}
#endif
	}
}
