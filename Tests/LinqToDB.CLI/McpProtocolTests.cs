using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

#pragma warning disable JSON002

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpProtocolTests : McpTestBase
	{
		[Test]
		public async Task McpListsQueryTool()
		{
			await using var server = await McpServerProcess.Start();

			await server.Initialize();

			var response = await server.SendRequest("tools/list", new JsonObject());

			{
				var tools      = ReadResponseResult<McpTestToolsResult>(response);
				var queryTool  = FindTool(tools, "linq2db_query");
				var infoTool   = FindTool(tools, "linq2db_info");
				var schemaTool = FindTool(tools, "linq2db_schema");
				var skillTool  = FindTool(tools, "linq2db_skill");

				tools.Tools.Count.ShouldBe(4);

				queryTool.Description.                            ShouldContain("Call linq2db_info first");
				queryTool.Description.                            ShouldContain("Call linq2db_skill");
				queryTool.Annotations.OpenWorldHint.              ShouldBe(true);
				queryTool.Annotations.ReadOnlyHint.               ShouldBe(true);
				queryTool.Annotations.IdempotentHint.             ShouldBe(false);
				queryTool.Annotations.DestructiveHint.            ShouldBe(false);
				queryTool.InputSchema.Required!.                  ShouldContain("sql");
				queryTool.InputSchema.Properties.AllowUnsafeSql.  ShouldBeNull();
				queryTool.InputSchema.Properties.AllowExecute.    ShouldBeNull();
				queryTool.InputSchema.Properties.Provider.        ShouldBeNull();
				queryTool.InputSchema.Properties.ConnectionString.ShouldBeNull();
				queryTool.InputSchema.Properties.Password.        ShouldBeNull();
				queryTool.InputSchema.Properties.Credentials.     ShouldBeNull();
				queryTool.InputSchema.Properties.ProviderLocation.ShouldBeNull();

				infoTool.Description.                ShouldContain("Returns non-secret linq2db MCP query configuration information");
				infoTool.Description.                ShouldContain("Use linq2db_schema");
				infoTool.Description.                ShouldContain("Use linq2db_skill");
				infoTool.Annotations.ReadOnlyHint.   ShouldBe(true);
				infoTool.Annotations.IdempotentHint. ShouldBe(true);
				infoTool.Annotations.OpenWorldHint.  ShouldBe(false);
				infoTool.Annotations.DestructiveHint.ShouldBe(false);
				infoTool.InputSchema.Properties.     ShouldBe(new McpTestInputProperties());
				infoTool.InputSchema.Required.       ShouldBeNull();

				schemaTool.Description.                            ShouldContain("Returns provider-independent database schema metadata");
				schemaTool.Description.                            ShouldContain("Procedures and functions are not supported");
				schemaTool.Annotations.ReadOnlyHint.               ShouldBe(true);
				schemaTool.Annotations.IdempotentHint.             ShouldBe(true);
				schemaTool.Annotations.OpenWorldHint.              ShouldBe(true);
				schemaTool.Annotations.DestructiveHint.            ShouldBe(false);
				schemaTool.InputSchema.Properties.Provider.        ShouldBeNull();
				schemaTool.InputSchema.Properties.ConnectionString.ShouldBeNull();
				schemaTool.InputSchema.Properties.Password.        ShouldBeNull();
				schemaTool.InputSchema.Properties.ProviderLocation.ShouldBeNull();
				schemaTool.InputSchema.Properties.Credentials.     ShouldBeNull();
				schemaTool.InputSchema.Properties.Sql.             ShouldBeNull();
				schemaTool.InputSchema.Properties.OutputFile.      ShouldBeNull();
				schemaTool.InputSchema.Properties.FilterTables.    ShouldNotBeNull();
				schemaTool.InputSchema.Properties.ExcludeTables.   ShouldBeNull();
				schemaTool.InputSchema.Properties.IncludeTables.   ShouldBeNull();
				schemaTool.InputSchema.Properties.GetProcedures.   ShouldBeNull();
				schemaTool.InputSchema.Properties.UseSchemaOnly.   ShouldBeNull();

				skillTool.Description.                                     ShouldContain("Returns the full embedded linq2db CLI agent skill as Markdown");
				skillTool.Annotations.ReadOnlyHint.                        ShouldBe(true);
				skillTool.Annotations.IdempotentHint.                      ShouldBe(true);
				skillTool.Annotations.OpenWorldHint.                       ShouldBe(false);
				skillTool.Annotations.DestructiveHint.                     ShouldBe(false);
				skillTool.InputSchema.Properties.                          ShouldBe(new McpTestInputProperties());
				skillTool.InputSchema.Required.                            ShouldBeNull();
				tools.Tools.Exists(tool => tool.Name == "linq2db_execute").ShouldBe(false);
			}
		}

		[Test]
		public async Task McpInitializeReturnsDefaultServerMetadata()
		{
			await using var server = await McpServerProcess.Start();

			var response = await server.Initialize();

			{
				var result = ReadResponseResult<McpTestInitializeResult>(response);

				result.ServerInfo.Name.       ShouldBe("linq2db.cli");
				result.ServerInfo.Title.      ShouldBe("linq2db Database Tools");
				result.ServerInfo.Description.ShouldContain("database schema inspection");
				result.Instructions.          ShouldContain("Call linq2db_info first");
				result.Instructions.          ShouldContain("Call linq2db_skill for the full linq2db CLI/MCP usage guide");
				result.Instructions.          ShouldContain("Use linq2db_execute only when it is available");
			}
		}

		[Test]
		public async Task McpListsExecuteToolWhenEnabled()
		{
			await using var server = await McpServerProcess.Start("--enable-execute-tool");

			await server.Initialize();
			var response = await server.SendRequest("tools/list", new JsonObject());

			{
				var tools       = ReadResponseResult<McpTestToolsResult>(response);
				var executeTool = FindTool(tools, "linq2db_execute");

				tools.Tools.Count.                                ShouldBe(5);
				executeTool.Description.                          ShouldContain("explicit user approval");
				executeTool.Annotations.ReadOnlyHint.             ShouldBe(false);
				executeTool.Annotations.IdempotentHint.           ShouldBe(false);
				executeTool.Annotations.OpenWorldHint.            ShouldBe(true);
				executeTool.Annotations.DestructiveHint.          ShouldBe(true);
				executeTool.InputSchema.Properties.AllowUnsafeSql.ShouldBeNull();
				executeTool.InputSchema.Properties.AllowExecute.  ShouldBeNull();
				executeTool.InputSchema.Properties.Credentials.   ShouldBeNull();
			}
		}

		[Test]
		public async Task McpInfoDoesNotExposeCredentialTarget()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-credential-{Guid.NewGuid():N}.json");
			await File.WriteAllTextAsync(config,
				"""
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/project-a/production"
					}
				}
				""")
				.ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());

			using (Assert.EnterMultipleScope())
			{
				ReadToolText(response).ShouldNotContain("linq2db/project-a/production");
			}
		}

		[TestCase(
			"linq2db Development Databases",
			"Databases used for linq2db development and provider testing.",
			"Use this server only for linq2db development, diagnostics, and provider compatibility testing.")]
		[TestCase(
			"Audiobooks Database",
			"Application database containing audiobooks, authors, narrators, users, and listening history.",
			"Use this server for Audiobooks application data analysis. Inspect the schema before writing queries.")]
		public async Task McpInitializeReturnsConfiguredServerMetadata(string title, string description, string instructions)
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-server-{Guid.NewGuid():N}.json");
			await File.WriteAllTextAsync(config, $$"""
				{
					"mcp": {
						"title": {{JsonSerializer.Serialize(title)}},
						"description": {{JsonSerializer.Serialize(description)}},
						"instructions": {{JsonSerializer.Serialize(instructions)}}
					},
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			var initializeResponse = await server.Initialize();
			var infoResponse       = await server.CallTool("linq2db_info", new JsonObject());
			var info               = ReadToolResult<McpTestInfoResult>(infoResponse);

			{
				var initialize = ReadResponseResult<McpTestInitializeResult>(initializeResponse);

				initialize.ServerInfo.Name.       ShouldBe("linq2db.cli");
				initialize.ServerInfo.Title.      ShouldBe(title);
				initialize.ServerInfo.Description.ShouldBe(description);
				initialize.Instructions.          ShouldContain("Call linq2db_skill for the full linq2db CLI/MCP usage guide");
				initialize.Instructions.          ShouldEndWith(instructions);
				info.Profiles.Count.              ShouldBe(1);
				info.Profiles[0].Name.            ShouldBe("default");
			}
		}

	}
}
