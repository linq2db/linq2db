using System;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// Raw MCP startup options that define the connection/configuration boundary for tool calls.
	/// </summary>
	sealed record McpQueryStartupOptions(
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
		string? LockTimeout,
		string? MaxRows,
		string? Output,
		bool    EnableExecuteTool);
}
