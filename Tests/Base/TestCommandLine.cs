using System;
using System.Collections.Generic;

namespace Tests
{
	/// <summary>
	/// Parses linq2db-specific test-run command-line options out of the process arguments.
	/// <para>
	/// The options are declared to Microsoft.Testing.Platform via <see cref="TestRunCommandLineProvider"/>
	/// (so the NUnit MTP runner accepts them and lists them under <c>--help</c>); their values are read here
	/// directly from <see cref="Environment.GetCommandLineArgs"/> rather than from an MTP service, because the
	/// values are needed during type initialization / NUnit test discovery — before any MTP service provider
	/// is resolvable. Reading the raw process arguments is also independent of how the run was launched
	/// (<c>dotnet test</c> vs the test executable directly) and of the target framework.
	/// </para>
	/// </summary>
	public static class TestCommandLine
	{
		/// <summary>Name of the <c>--provider</c> option (without the leading dashes).</summary>
		public const string ProviderOption = "provider";

		/// <summary>Name of the <c>--test-progress</c> option (without the leading dashes).</summary>
		public const string TestProgressOption = "test-progress";

		/// <summary>
		/// Provider configuration names passed via one or more <c>--provider</c> options (each value may be
		/// space- or comma-separated). Empty when the option was not supplied.
		/// </summary>
		public static IReadOnlyList<string> Providers { get; }

		/// <summary>
		/// Value of the <c>--test-progress</c> option: <see langword="null"/> when the option is absent, an empty
		/// string when it is present without a value, otherwise the supplied directory or <c>.json</c> path.
		/// </summary>
		public static string? TestProgress { get; }

		static TestCommandLine()
		{
			string[] args;

			try
			{
				args = Environment.GetCommandLineArgs();
			}
			catch
			{
				// Never let argument parsing break a test run.
				args = [];
			}

			Providers    = GetValues(args, ProviderOption);
			TestProgress = GetSingle(args, TestProgressOption);
		}

		// All values that follow each "--<option>" token until the next "--" token, split on ','.
		static IReadOnlyList<string> GetValues(string[] args, string option)
		{
			var token  = "--" + option;
			var result = new List<string>();

			for (var i = 0; i < args.Length; i++)
			{
				if (!string.Equals(args[i], token, StringComparison.OrdinalIgnoreCase))
					continue;

				for (var j = i + 1; j < args.Length && !args[j].StartsWith("--", StringComparison.Ordinal); j++)
					foreach (var part in args[j].Split(','))
						if (!string.IsNullOrWhiteSpace(part))
							result.Add(part.Trim());
			}

			return result;
		}

		// The first value following "--<option>", "" if the option is present without a value, null if absent.
		static string? GetSingle(string[] args, string option)
		{
			var token = "--" + option;

			for (var i = 0; i < args.Length; i++)
			{
				if (!string.Equals(args[i], token, StringComparison.OrdinalIgnoreCase))
					continue;

				return i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
					? args[i + 1]
					: "";
			}

			return null;
		}
	}
}
