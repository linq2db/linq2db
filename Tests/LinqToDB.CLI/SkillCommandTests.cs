using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

using NUnit.Framework;

using Shouldly;

#nullable enable annotations
#nullable disable warnings

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

		private sealed class TestCliEnvironment : ICliEnvironment
		{
			private readonly StringWriter _output = new();
			private readonly StringWriter _error  = new();

			public TextWriter Out   => _output;
			public TextWriter Error => _error;

			public int BufferWidth => 120;

			public string Output      => _output.ToString();
			public string ErrorOutput => _error .ToString();

			public bool FileExists(string path)
			{
				return false;
			}

			public string ReadAllText(string path)
			{
				throw new KeyNotFoundException(path);
			}

			public void WriteAllText(string path, string contents)
			{
				throw new NotSupportedException();
			}

			public TextWriter CreateTextWriter(string path)
			{
				throw new NotSupportedException();
			}

			public string? GetEnvironmentVariable(string name)
			{
				throw new NotSupportedException();
			}
		}
	}
}
