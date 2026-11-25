using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>Which kind of credential store a command uses.</summary>
	internal enum CredentialStoreKind
	{
		/// <summary>Windows Credential Manager.</summary>
		CredentialManager,
		/// <summary>The built-in local store.</summary>
		Local,
		/// <summary>A credentials CLI (a command line, or a generated <c>keyring</c>/<c>gpg</c> script).</summary>
		CredentialsCli,
	}
}
