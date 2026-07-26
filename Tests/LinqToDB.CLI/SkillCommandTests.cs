using System;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class SkillCommandTests
	{
		[Test]
		public async Task SkillPrintsAgentMarkdown()
		{
			var result = await RunCli("skill");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();
				(result.Output.Length).ShouldBeGreaterThan(1000);
				(result.Output).ShouldStartWith("# linq2db CLI Agent Skill");
				(result.Output).ShouldContain("## Query Command");
				(result.Output).ShouldContain("## Supported Database Providers");
				(result.Output).ShouldContain("## MCP STDIO Command");
				(result.Output).ShouldContain("## Skill Command");
			}
		}

		[Test]
		public async Task SkillsAliasPrintsAgentMarkdown()
		{
			var result = await RunCli("skills");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();
				(result.Output).ShouldStartWith("# linq2db CLI Agent Skill");
			}
		}

		[Test]
		public async Task SkillRejectsArguments()
		{
			var result = await RunCli("skill", "query");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Error).ShouldContain("Command 'skill' doesn't accept arguments.");
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
