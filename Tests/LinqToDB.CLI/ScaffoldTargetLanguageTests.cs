using System;
using System.IO;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class ScaffoldTargetLanguageTests
	{
		[TestCase("--add-init-context")]
		[TestCase("--emit-schema-errors")]
		public async Task FSharpRejectsCSharpOnlyOption(string option)
		{
			var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "--target-language", "f#", option, "true");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Error.   ShouldContain($"Option '{option}' is not supported for target language F#.");
			}
		}

		[Test]
		public async Task FSharpRejectsT4Mode()
		{
			var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "--target-language", "f#", "-t", "t4");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Error.   ShouldContain("F# generation (--target-language f#) is not supported together with the T4 template mode (-t t4).");
			}
		}

		[Test]
		public async Task CSharpAcceptsCSharpOnlyOption()
		{
			var output = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"scaffold-{Guid.NewGuid():N}");

			try
			{
				var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "-o", output, "--target-language", "c#", "--add-init-context", "true");

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0);
					result.Error.   ShouldNotContain("is not supported for target language");
				}
			}
			finally
			{
				if (Directory.Exists(output))
					Directory.Delete(output, true);
			}
		}

		[Test]
		public async Task HelpShowsSupportedLanguages()
		{
			var result = await RunCli("help", "scaffold");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0);
				result.Output.  ShouldContain("Supported in: C#");
				result.Output.  ShouldNotContain("Supported in: F#");
			}
		}

		private static async Task<CliResult> RunCli(params string[] arguments)
		{
			var environment = new TestCliEnvironment();
			var exitCode    = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);
	}
}
