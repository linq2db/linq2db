using System;
using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Default credential store on operating systems without a built-in one: every operation fails with guidance on
	/// configuring a credential helper. There is no silent fallback to another store.
	/// </summary>
	internal sealed class UnavailableCredentialStore : ICredentialStore
	{
		public const string Message =
			"No credential store is configured. On this operating system linq2db-cli uses an external credential helper: "
			+ "set 'credentialHelper' in the configuration profile (the 'default' profile for the credentials command) or pass '--credential-helper <command>'. "
			+ "'dotnet linq2db credentials helper init --backend secret-tool|pass' creates a starter helper. "
			+ "Alternatively, provide the user and password with 'userEnv'/'passwordEnv' or '--user-env'/'--password-env'.";

		public static ICredentialStore Instance { get; } = new UnavailableCredentialStore();

		UnavailableCredentialStore()
		{
		}

		/// <summary>Returns the platform default store: Credential Manager on Windows, otherwise this store.</summary>
		public static ICredentialStore GetDefault(bool isWindows)
		{
			return isWindows ? WindowsCredentialStore.Instance : Instance;
		}

		public bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;
			error    = Message;
			return false;
		}

		public bool TryStore(string profile, string user, string password, out string? error)
		{
			error = Message;
			return false;
		}

		public bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error)
		{
			profiles    = [];
			diagnostics = [];
			error       = Message;
			return false;
		}

		public bool TryGetCount(out int count, out string? error)
		{
			count = 0;
			error = Message;
			return false;
		}

		public bool TryRemove(string profile, out bool removed, out string? error)
		{
			removed = false;
			error   = Message;
			return false;
		}

		public bool TryClear(out int removedCount, out string? error)
		{
			removedCount = 0;
			error        = Message;
			return false;
		}
	}
}
