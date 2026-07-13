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
		string? WindowsCredentials,
		bool?   Impersonate,
		string? ImpersonateMode,
		string? CommandTimeout,
		string? LockTimeout);
}
