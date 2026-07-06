using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

using NUnit.Framework;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class SkillCommandTests
	{
		[Test]
		public async Task SkillPrintsAgentMarkdown()
		{
			var result = await RunCli("skill");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(result.Output,   Does.StartWith("# linq2db CLI Agent Skill"));
				Assert.That(result.Output,   Does.Contain("## Query Command"));
				Assert.That(result.Output,   Does.Contain("## Scaffold Command"));
				Assert.That(result.Output,   Does.Contain("## Template Command"));
				Assert.That(result.Output,   Does.Contain("## Skill Command"));
				Assert.That(result.Output,   Does.Contain("Exactly one of `--sql` or `--sql-file` must be specified."));
				Assert.That(result.Output,   Does.Contain("Use `{0}` in the connection string for the user value and `{1}` for the password value."));
				Assert.That(result.Output,   Does.Contain("Value precedence is: command-line literal, command-line environment variable option"));
				Assert.That(result.Output,   Does.Contain("an agent can analyze your code together with data from your database"));
				Assert.That(result.Output,   Does.Contain("Prepare regression tests for bugs found in real data"));
				Assert.That(result.Output,   Does.Contain("Support synchronous development of code and a development database"));
				Assert.That(result.Output,   Does.Contain("An agent MUST ask the user for explicit confirmation before executing any unsafe SQL operation."));
				Assert.That(result.Output,   Does.Contain("Default policy is guarded mode"));
				Assert.That(result.Output,   Does.Contain("result-set-oriented database inspection command"));
				Assert.That(result.Output,   Does.Contain("does not change the `query` command contract into a general DDL/DML execution workflow"));
				Assert.That(result.Output,   Does.Contain("best-effort guardrails intended to help avoid agent mistakes"));
				Assert.That(result.Output,   Does.Contain("Query output reads database values using .NET `DbDataReader.GetProviderSpecificValue`"));
				Assert.That(result.Output,   Does.Contain("Provider-specific output formatting has special handling validated for SQL Server and Oracle provider-specific types"));
				Assert.That(result.Output,   Does.Contain("Binary values are emitted using SQL-style hexadecimal notation like `0x010203`"));
				Assert.That(result.Output,   Does.Contain("Connection timeout is intentionally not exposed as a separate query command option"));
				Assert.That(result.Output,   Does.Contain("The query command accepts exactly one user-provided SQL statement per invocation"));
				Assert.That(result.Output,   Does.Contain("The CLI may execute provider-specific setup commands internally"));
				Assert.That(result.Output,   Does.Contain("Timeout value `0` disables the corresponding timeout option"));
				Assert.That(result.Output,   Does.Contain("`maxRows` value `0` disables the row limit"));
				Assert.That(result.Output,   Does.Contain("Agent responsibility"));
				Assert.That(result.Output,   Does.Contain("Use `dotnet linq2db skill` or `dotnet linq2db skills`"));
				Assert.That(result.Output,   Does.Contain(@"dotnet linq2db skill > "".\.agents\skills\linq2db-cli\SKILL.md"""));
			}
		}

		[Test]
		public async Task SkillsAliasPrintsAgentMarkdown()
		{
			var result = await RunCli("skills");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(result.Output,   Does.StartWith("# linq2db CLI Agent Skill"));
			}
		}

		[Test]
		public async Task SkillRejectsArguments()
		{
			var result = await RunCli("skill", "query");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Command 'skill' doesn't accept arguments."));
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
