using System;
using System.Security.Cryptography;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Result of one credential helper run. <see cref="Failure"/> is set when the run did not complete (the helper could
	/// not be resolved, could not start, timed out or wrote too much output); otherwise
	/// <see cref="ExitCode"/>, <see cref="Output"/> and <see cref="ErrorOutput"/> (standard error, capped) describe the
	/// finished process.
	/// </summary>
	internal sealed record CredentialHelperRunResult(string? Failure, int ExitCode, byte[] Output, string ErrorOutput)
	{
		public static CredentialHelperRunResult Failed(string failure)
		{
			return new CredentialHelperRunResult(failure, -1, [], string.Empty);
		}

		/// <summary>
		/// Clears the helper output buffer, which can contain secrets.
		/// </summary>
		public void ClearOutput()
		{
			CryptographicOperations.ZeroMemory(Output);
		}
	}
}
