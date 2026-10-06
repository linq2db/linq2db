using System;
using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Reads and manages linq2db credential records in a credential store: Windows Credential Manager, the built-in local
	/// store, or a credentials CLI.
	/// </summary>
	public interface ICredentialStore
	{
		/// <summary>Reads credentials from an exact store target.</summary>
		bool TryRead(string target, out string? user, out string? password, out string? error);
		/// <summary>Stores the record <c>linq2db/&lt;name&gt;</c>.</summary>
		/// <param name="name">Record name without the <c>linq2db/</c> target prefix.</param>
		/// <param name="user">Database user name.</param>
		/// <param name="password">Database password.</param>
		/// <param name="error">Error message when the record could not be stored.</param>
		bool TryStore(string name, string user, string password, out string? error);
		/// <summary>Lists linq2db credential records.</summary>
		bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error);
		/// <summary>Gets the number of linq2db credential records without reading their values.</summary>
		bool TryGetCount(out int count, out string? error);
		/// <summary>Removes the record <c>linq2db/&lt;name&gt;</c>.</summary>
		/// <param name="name">Record name without the <c>linq2db/</c> target prefix.</param>
		/// <param name="removed">Whether the record existed and was removed.</param>
		/// <param name="error">Error message when the store failed.</param>
		bool TryRemove(string name, out bool removed, out string? error);
		/// <summary>Removes all linq2db credential records.</summary>
		bool TryClear(out int removedCount, out string? error);
	}
}
