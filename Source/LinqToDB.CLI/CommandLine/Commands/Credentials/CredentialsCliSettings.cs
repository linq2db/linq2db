using System;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// A credentials CLI to run: the program as written (resolved before every run), the rest of the configured command line
	/// passed unchanged as its argument string, and the directory a relative program path is resolved against.
	/// </summary>
	/// <param name="DisplayName">The configured value, for messages.</param>
	/// <param name="Program">The program: a file name looked up on <c>PATH</c>, or an absolute or relative path.</param>
	/// <param name="Arguments">The argument string after the program, passed unchanged before the verb.</param>
	/// <param name="BaseDirectory">
	/// Directory of the configuration file that names the program; <see langword="null"/> means the current directory.
	/// </param>
	public sealed record CredentialsCliSettings(string DisplayName, string Program, string Arguments, string? BaseDirectory)
	{
		/// <summary>
		/// Splits a configured command line into the program and the argument string (see
		/// <see cref="CredentialsCliCommandResolver.TryParseCommandLine"/>).
		/// </summary>
		public static bool TryCreate(string commandLine, string? baseDirectory, out CredentialsCliSettings settings, out string? error)
		{
			if (!CredentialsCliCommandResolver.TryParseCommandLine(commandLine, out var program, out var arguments, out error))
			{
				settings = null!;
				return false;
			}

			settings = new CredentialsCliSettings(commandLine, program, arguments, baseDirectory);
			return true;
		}
	}
}
