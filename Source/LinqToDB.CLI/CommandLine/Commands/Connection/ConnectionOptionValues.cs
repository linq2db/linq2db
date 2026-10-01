using System;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Raw trusted connection option values collected by command and MCP adapters before profile resolution.
	/// </summary>
	internal sealed record ConnectionOptionValues(
		string? Config,
		string? Profile,
		string? Provider,
		string? ProviderLocation,
		string? ConnectionString,
		string? ConnectionStringEnv,
		string? User,
		string? UserEnv,
		string? Password,
		string? PasswordEnv,
		string? Credentials,
		bool?   Impersonate,
		string? ImpersonateMode,
		string? CommandTimeout,
		string? LockTimeout)
	{
		/// <summary>Credential helper command from the command line; overrides the profile's <c>credentialHelper</c>.</summary>
		public string? CredentialHelper { get; init; }
	}
}
