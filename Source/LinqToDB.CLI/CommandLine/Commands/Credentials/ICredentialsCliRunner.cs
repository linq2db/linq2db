using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Runs a credentials CLI with one verb and a request on standard input.
	/// </summary>
	internal interface ICredentialsCliRunner
	{
		/// <summary>The credentials CLI as configured, for messages.</summary>
		string DisplayName { get; }

		/// <summary>Resolved program path and arguments for diagnostics, or the configured value when it cannot be resolved.</summary>
		string Describe();

		CredentialsCliRunResult Run(string verb, byte[] request);
	}
}
