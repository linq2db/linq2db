using System;

namespace LinqToDB.Internal.Concurrency
{
	/// <summary>
	/// Declares which providers support single-row <c>UPDATE</c> ... <c>OUTPUT</c> / <c>RETURNING</c> of the new
	/// (post-update) values, used by <see cref="LinqToDB.Concurrency.ConcurrencyExtensions"/> to choose between the
	/// single-statement output path and the update-then-<c>SELECT</c> fallback.
	/// </summary>
	/// <remarks>
	/// This is a hand-maintained list, not an enforced capability flag. The
	/// <c>ConcurrencyRefreshTests.OutputSupportSurface</c> guard test probes each provider's actual support at runtime
	/// and fails when reality diverges from this declaration, signalling that the list (and any required handling) needs
	/// updating.
	/// </remarks>
	public static class ConcurrencyOutputSupport
	{
		/// <summary>
		/// Returns <see langword="true"/> if the provider supports <c>UPDATE</c> with single-row <c>OUTPUT</c> /
		/// <c>RETURNING</c> of the new (post-update) values.
		/// </summary>
		/// <param name="contextName"><see cref="IDataContext.ContextName"/> of the target context.</param>
		public static bool IsUpdateOutputSupported(string contextName)
		{
			ArgumentNullException.ThrowIfNull(contextName);

			return HasPrefix(contextName, "SqlServer")
				|| HasPrefix(contextName, "PostgreSQL")
				|| HasPrefix(contextName, "SQLite")
				|| HasPrefix(contextName, "Firebird")
				|| HasPrefix(contextName, "YDB");
		}

		private static bool HasPrefix(string contextName, string prefix)
			=> contextName.StartsWith(prefix, StringComparison.Ordinal);
	}
}
