using System;

using LinqToDB.CommandLine.Commands.QueryExecution;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Fully resolved trusted database connection settings shared by query and schema execution.
	/// </summary>
	internal sealed record ConnectionSettings(
		string  Profile,
		string  Provider,
		string? ProviderLocation,
		string? User,
		string? Password,
		string  ConnectionString,
		int?    CommandTimeout,
		int?    LockTimeout,
		string? ConfigDirectory,
		bool    Impersonate,
		WindowsImpersonationMode     ImpersonateMode,
		QueryExecutionConfiguration? Configuration);
}
