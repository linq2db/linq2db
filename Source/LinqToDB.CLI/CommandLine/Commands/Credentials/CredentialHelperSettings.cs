using System;
using System.Collections.Generic;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Trusted credential helper configuration: the command as configured, its protocol, and the directory a relative
	/// command path is resolved against (the configuration file directory; <see langword="null"/> means the current directory).
	/// </summary>
	public sealed record CredentialHelperSettings(string Command, CredentialHelperProtocol Protocol, string? BaseDirectory)
	{
		/// <summary>
		/// Fixed arguments passed before the verb, for helpers started through another program, for example
		/// <c>dotnet run /path/helper.cs --</c>. Each is one argument; no shell parses them.
		/// </summary>
		public IReadOnlyList<string> Arguments { get; init; } = [];

		/// <summary>
		/// How long a run may take, overriding the defaults (10 seconds non-interactive, 60 seconds interactive); for
		/// helpers with a slow first start, such as a file-based app that is built on first use.
		/// </summary>
		public TimeSpan? Timeout { get; init; }

		/// <summary>Configuration name of <see cref="CredentialHelperProtocol.Linq2Db"/>.</summary>
		public const string Linq2DbProtocolName = "linq2db";
		/// <summary>Configuration name of <see cref="CredentialHelperProtocol.Docker"/>.</summary>
		public const string DockerProtocolName  = "docker";

		/// <summary>Parses a configured protocol name; <see langword="null"/> means the linq2db protocol.</summary>
		public static bool TryParseProtocol(string? value, out CredentialHelperProtocol protocol)
		{
			if (value == null || string.Equals(value, Linq2DbProtocolName, StringComparison.OrdinalIgnoreCase))
			{
				protocol = CredentialHelperProtocol.Linq2Db;
				return true;
			}

			if (string.Equals(value, DockerProtocolName, StringComparison.OrdinalIgnoreCase))
			{
				protocol = CredentialHelperProtocol.Docker;
				return true;
			}

			protocol = default;
			return false;
		}
	}
}
