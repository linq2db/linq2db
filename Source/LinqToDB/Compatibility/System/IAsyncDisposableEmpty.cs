using System;
using System.Threading.Tasks;

namespace LinqToDB.Compatibility.System
{
	using Common.Internal;

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
