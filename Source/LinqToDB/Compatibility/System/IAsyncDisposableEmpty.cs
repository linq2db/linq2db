﻿#if NATIVE_ASYNC
using System;
using System.Threading.Tasks;

namespace LinqToDB
{
	/// <summary>
	/// Helps leverage of pain to work with <c>await using nullable_value</c> code.
	/// </summary>
	internal static class EmptyIAsyncDisposable
	{
		public static readonly IAsyncDisposable Instance = new NoopImplementation();

		private sealed class NoopImplementation : IAsyncDisposable
		{
			ValueTask IAsyncDisposable.DisposeAsync() => default;
		}
	}
}
#endif
