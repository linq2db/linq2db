using System;
using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Reads and manages credential profiles in a platform or externally provided credential store.
	/// </summary>
	public interface ICredentialStore
	{
		/// <summary>Reads credentials from an exact store target.</summary>
		bool TryRead(string target, out string? user, out string? password, out string? error);
		/// <summary>Stores an linq2db credential profile.</summary>
		bool TryStore(string profile, string user, string password, out string? error);
		/// <summary>Lists linq2db credential profiles.</summary>
		bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error);
		/// <summary>Gets the number of linq2db credential profiles without reading their values.</summary>
		bool TryGetCount(out int count, out string? error);
		/// <summary>Removes an linq2db credential profile.</summary>
		bool TryRemove(string profile, out bool removed, out string? error);
		/// <summary>Removes all linq2db credential profiles.</summary>
		bool TryClear(out int removedCount, out string? error);
	}
}
