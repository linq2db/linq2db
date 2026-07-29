using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpSkillToolTests : McpTestBase
	{
		[Test]
		public async Task McpSkillReturnsEmbeddedSkillMarkdown()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=secret-skill.db;Password=l2db-leak-canary-9f3a");

			await server.Initialize();
			var response  = await server.CallTool("linq2db_skill", new JsonObject());
			var skillText = ReadToolText(response);

			using (Assert.EnterMultipleScope())
			{
				skillText.Length.ShouldBeGreaterThan(1000);
				skillText.       ShouldStartWith("# linq2db CLI Agent Skill");
				skillText.       ShouldNotContain("secret-skill.db");
				skillText.       ShouldNotContain("l2db-leak-canary-9f3a");
			}

			server.ExpectNoStandardError();
		}

		[Test]
		public async Task McpSkillDoesNotRequireValidDatabaseConfiguration()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=missing-skill.db;Password=secret");

			await server.Initialize();
			var response = await server.CallTool("linq2db_skill", new JsonObject());

			using (Assert.EnterMultipleScope())
			{
				ReadToolText(response).ShouldContain("Use `dotnet linq2db mcp`");
			}

			server.ExpectNoStandardError();
		}

	}
}
