using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Runs a credential helper with one verb and a request on standard input.
	/// </summary>
	internal interface ICredentialHelperRunner
	{
		/// <summary>Helper name for messages: the command as configured.</summary>
		string DisplayName { get; }

		/// <summary>Resolved helper path for diagnostics, or the configured command when it cannot be resolved.</summary>
		string Describe();

		CredentialHelperRunResult Run(string verb, byte[] request);
	}
}
