using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Raw query execution option values collected by a command adapter before profile resolution.
	/// </summary>
	internal sealed record QueryExecutionOptionValues(
		string?            Config,
		string?            Profile,
		string?            Provider,
		string?            ProviderLocation,
		string?            ConnectionString,
		string?            ConnectionStringEnv,
		string?            User,
		string?            UserEnv,
		string?            Password,
		string?            PasswordEnv,
		string?            WindowsCredentials,
		bool?              Impersonate,
		string?            ImpersonateMode,
		string?            CommandTimeout,
		string?            LockTimeout,
		string?            MaxRows,
		string?            Output,
		string?            OutputFile,
		bool               UseConfiguredOutputFile,
		bool               Overwrite,
		QueryExecutionMode Mode,
		string?            Sql,
		string?            SqlFile,
		string             DefaultOutput);
}
