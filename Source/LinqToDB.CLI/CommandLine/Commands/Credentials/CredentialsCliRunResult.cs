using System;
using System.Security.Cryptography;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Result of one credentials CLI run. <see cref="Failure"/> is set when the run did not complete (the program could
	/// not be resolved, could not start, timed out or wrote too much output); otherwise
	/// <see cref="ExitCode"/>, <see cref="Output"/> and <see cref="ErrorOutput"/> (standard error, capped) describe the
	/// finished process.
	/// </summary>
	internal sealed record CredentialsCliRunResult(string? Failure, int ExitCode, byte[] Output, string ErrorOutput)
	{
		/// <summary>Whether standard error was longer than the part kept in <see cref="ErrorOutput"/>.</summary>
		public bool ErrorOutputTruncated { get; init; }

		public static CredentialsCliRunResult Failed(string failure)
		{
			return new CredentialsCliRunResult(failure, -1, [], string.Empty);
		}

		/// <summary>
		/// Clears the output buffer, which can contain secrets.
		/// </summary>
		public void ClearOutput()
		{
			CryptographicOperations.ZeroMemory(Output);
		}
	}
}
