using System;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpCommandLineTests : McpTestBase
	{
		[Test]
		public async Task McpHelpShowsMcpBoundary()
		{
			var result = await RunCliProcess("help", "mcp");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Output).  ShouldContain   ("dotnet linq2db mcp <options>");
				(result.Output).  ShouldContain   ("run STDIO MCP server");
				(result.Output).  ShouldContain   ("default: json-table");
				(result.Output).  ShouldContain   ("--credentials");
				(result.Output).  ShouldContain   ("--config");
				(result.Output).  ShouldContain   ("--provider");
				(result.Output).  ShouldContain   ("--max-rows");
				(result.Output).  ShouldContain   ("--max-response-bytes");
				(result.Output).  ShouldNotContain("CSV output");
				(result.Output).  ShouldNotContain("--sql");
				(result.Output).  ShouldNotContain("--output-file");
				(result.Output).  ShouldNotContain("--allow-unsafe-sql");
			}
		}

		[Test]
		public async Task McpRejectsInvalidResponseLimit()
		{
			var result = await RunCliProcess("mcp", "--max-response-bytes", "0");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).  ShouldBeEmpty();
				(result.Error).   ShouldContain("Option '--max-response-bytes' must be a positive 32-bit integer.");
			}
		}

		[Test]
		public async Task McpRejectsQueryOnlyStartupOptions()
		{
			var result = await RunCliProcess("mcp", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).  ShouldBeEmpty();
				(result.Error).   ShouldContain("Unrecognized option: --sql");
			}
		}

		[Test]
		public async Task McpRejectsCsvStartupOutput()
		{
			var result = await RunCliProcess("mcp", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).  ShouldBeEmpty();
				(result.Error).   ShouldContain("Cannot parse option value (--output csv): unknown value 'csv'");
			}
		}
	}
}
