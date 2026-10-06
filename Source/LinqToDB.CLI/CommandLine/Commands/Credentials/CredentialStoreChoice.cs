using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// The store a command uses and why: the configured value, where it came from, and what it resolves to.
	/// </summary>
	/// <param name="Kind">Store kind.</param>
	/// <param name="Source">Where the choice came from: "option", "profile 'x' of &lt;file&gt;", "default for this OS".</param>
	internal sealed record CredentialStoreChoice(CredentialStoreKind Kind, string Source)
	{
		/// <summary>The credentials directory (local store), when it is known.</summary>
		public string? Directory { get; init; }
		/// <summary>The credentials CLI to run.</summary>
		public CredentialsCliSettings? Cli { get; init; }
		/// <summary>The generated script a reserved <c>@keyring</c>/<c>@gpg</c> value names.</summary>
		public string? Script { get; init; }
		/// <summary>Whether nothing named the store: the OS default is used.</summary>
		public bool IsOsDefault { get; init; }

		/// <summary>The store's name in messages: "the local store", "Windows Credential Manager", "credentials CLI '...'".</summary>
		public string Name => Kind switch
		{
			CredentialStoreKind.Local             => "the local store",
			CredentialStoreKind.CredentialManager => "Windows Credential Manager",
			_                                     => $"credentials CLI '{Cli!.DisplayName}'",
		};

		/// <summary>The store with its location and source, for the "Using ..." line.</summary>
		public string Describe()
		{
			var location = Kind == CredentialStoreKind.Local ? Directory : Script;
			return location != null ? $"{Name} ({location}, {Source})" : $"{Name} ({Source})";
		}
	}
}
