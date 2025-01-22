using System;

using Microsoft.Extensions.Logging;

namespace LinqToDB.EntityFrameworkCore.Tests.Logging
{
	/// <summary>
	/// Scope provider that does nothing.
	/// </summary>
	internal sealed class NullExternalScopeProvider : IExternalScopeProvider
	{
		private NullExternalScopeProvider()
		{
		}

		/// <summary>
		/// Returns a cached instance of <see cref="NullExternalScopeProvider"/>.
		/// </summary>
		public static IExternalScopeProvider Instance { get; } = new NullExternalScopeProvider();

		/// <inheritdoc />
		void IExternalScopeProvider.ForEachScope<TState>(Action<object?, TState> callback, TState state)
		{
		}

		/// <inheritdoc />
		IDisposable IExternalScopeProvider.Push(object? state)
		{
			return NullScope.Instance;
		}
	}
}
