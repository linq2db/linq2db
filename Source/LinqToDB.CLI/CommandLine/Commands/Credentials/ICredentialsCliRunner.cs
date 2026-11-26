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

		CredentialsCliRunResult Run(string verb, byte[] request);
	}
}
