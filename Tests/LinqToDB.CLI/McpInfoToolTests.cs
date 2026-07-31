using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

#pragma warning disable JSON002

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpInfoToolTests : McpTestBase
	{
		[Test]
		public async Task McpInfoReturnsStartupProfile()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite.MS", "--connection-string", "Data Source=secret.db;Password=hidden");

			await server.Initialize();

			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolResult<McpTestInfoResult>(response);
			var infoText = ReadToolText(response);

			using (Assert.EnterMultipleScope())
			{
				info.Server.Name.                                             ShouldBe("linq2db.cli");
				info.Server.Command.                                          ShouldBe("mcp");
				info.Server.ExecuteToolEnabled.                               ShouldBe(false);
				info.Server.MaxResponseBytes.                                 ShouldBe(8388608);
				info.DefaultProfile.                                          ShouldBe("startup");
				info.DefaultProfileUsable.                                    ShouldBe(true);
				info.Profiles[0].Name.                                        ShouldBe("startup");
				info.Profiles[0].Provider.                                    ShouldBe("SQLite.MS");
				info.Profiles[0].Dialect.                                     ShouldBe("SQLite");
				info.Profiles[0].DefaultOutput.                               ShouldBe("json-table");
				info.Profiles[0].DefaultOutputSupportedByMcp.                 ShouldBe(true);
				info.Profiles[0].MaxRows.                                     ShouldBe(1000);
				info.Profiles[0].EnableExecute.                               ShouldBe(false);
				info.Profiles[0].ImpersonationEnabled.                        ShouldBe(false);
				info.SupportedOutputFormats.                                  ShouldContain("json-table");
				info.SupportedOutputFormats.                                  ShouldNotContain("csv");
				info.QueryCommandOutputFormats.                               ShouldContain("csv");
				info.SupportedProviders.ConvertAll(provider => provider.Name).ShouldContain("SQL Server");
				info.SupportedProviders.ConvertAll(provider => provider.Name).ShouldContain("Microsoft Access");
				info.SupportedProviders.ConvertAll(provider => provider.Name).ShouldContain("IBM DB2");
				info.SupportedProviders.ConvertAll(provider => provider.Name).ShouldContain("IBM Informix");
				FindSupportedProvider(info, "IBM DB2").Notes!.                ShouldContain("IBM.Data.Db2.dll");
				FindSupportedProvider(info, "IBM DB2").Bundled.               ShouldBe(false);
				FindSupportedProvider(info, "IBM DB2").ProviderNames.         ShouldContain("DB2");
				FindSupportedProvider(info, "IBM Informix").ProviderNames.    ShouldBe(["Informix", "Informix.DB2"]);
				FindSupportedProvider(info, "IBM Informix").Notes!.           ShouldContain("IBM.Data.Informix.dll");
				FindSupportedProvider(info, "IBM Informix").Notes!.           ShouldContain("IBM.Data.Db2.dll");
				info.Rules.SingleStatementOnly.                               ShouldBe(true);
				info.Rules.SqlGuardIsSecurityBoundary.                        ShouldBe(false);
				info.Rules.SqlGuardWarning.                                   ShouldContain("not a security boundary");
				info.Rules.SqlGuardWarning.                                   ShouldContain("restricted database accounts");
				info.Rules.ConnectionStringPlaceholdersEscaped.               ShouldBe(false);
				info.Rules.ConnectionStringPlaceholderWarning.                ShouldContain("raw user/password values");
				info.Rules.ConnectionStringPlaceholderWarning.                ShouldContain("trusted startup/config sources");
				info.Rules.ProviderInputAllowedInToolCall.                    ShouldBe(false);
				info.Rules.ConnectionStringInputAllowedInToolCall.            ShouldBe(false);
				info.Rules.CredentialsInputAllowedInToolCall.                 ShouldBe(false);
				infoText.                                                     ShouldNotContain("secret.db");
				infoText.                                                     ShouldNotContain("hidden");
				info.Profiles[0].ConnectionString.                            ShouldBeNull();
				info.Profiles[0].Password.                                    ShouldBeNull();
				info.Profiles[0].ProviderLocation.                            ShouldBeNull();
			}
		}

		[Test]
		public async Task McpInfoReturnsConfigProfiles()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"description": "Use SQLite syntax for local development queries.",
						"connectionString": "Data Source=dev-secret.db",
						"maxRows": 1000,
						"enableExecute": false
					},
					"sqlserver": {
						"description": "Use T-SQL syntax. Prefer dbo schema qualification.",
						"provider": "SqlServer",
						"connectionStringEnv": "LINQ2DB_SQLSERVER_CONNECTION",
						"maxRows": 500,
						"enableExecute": true,
						"impersonate": true
					},
					"mysqlconnector": {
						"description": "Use MySQL syntax with MySqlConnector provider.",
						"provider": "MySqlConnector",
						"connectionStringEnv": "LINQ2DB_MYSQL_CONNECTION"
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config, "--profile", "sqlserver", "--output", "json-table");

			await server.Initialize();

			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolResult<McpTestInfoResult>(response);
			var infoText = ReadToolText(response);

			using (Assert.EnterMultipleScope())
			{
				info.DefaultProfile.      ShouldBe("sqlserver");
				info.DefaultProfileUsable.ShouldBe(true);
				info.Profiles.Count.      ShouldBe(2);

				var sqlServer      = FindProfile(info, "sqlserver");
				var mySqlConnector = FindProfile(info, "mysqlconnector");

				info.Profiles.Exists(profile => profile.Name == "default").ShouldBe(false);

				sqlServer.Description!.               ShouldContain("T-SQL");
				sqlServer.Provider.                   ShouldBe("SqlServer");
				sqlServer.Dialect.                    ShouldBe("SQL Server T-SQL");
				sqlServer.DefaultOutputSupportedByMcp.ShouldBe(true);
				sqlServer.MaxRows.                    ShouldBe(500);
				sqlServer.EnableExecute.              ShouldBe(true);
				sqlServer.ImpersonationEnabled.       ShouldBe(true);

				mySqlConnector.Provider.ShouldBe("MySqlConnector");
				mySqlConnector.Dialect. ShouldBe("MySQL");

				infoText.                     ShouldNotContain("dev-secret.db");
				infoText.                     ShouldNotContain("LINQ2DB_SQLSERVER_CONNECTION");
				infoText.                     ShouldNotContain("LINQ2DB_MYSQL_CONNECTION");
				sqlServer.ConnectionString.   ShouldBeNull();
				sqlServer.ConnectionStringEnv.ShouldBeNull();
			}
		}

		[Test]
		public async Task McpInfoMarksDefaultProfileUnusableWhenDefaultHasNoProvider()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"description": "Base profile only.",
						"maxRows": 100
					},
					"sqlite": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			await server.Initialize();

			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolResult<McpTestInfoResult>(response);

			using (Assert.EnterMultipleScope())
			{
				info.DefaultProfile.                                       ShouldBe("default");
				info.DefaultProfileUsable.                                 ShouldBe(false);
				info.Profiles.Exists(profile => profile.Name == "default").ShouldBe(false);
				info.Profiles.Exists(profile => profile.Name == "sqlite"). ShouldBe(true);
			}
		}

		[Test]
		public async Task McpInfoWarnsForNamedProfileWithoutProvider()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"description": "Base profile only."
					},
					"sqlite": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					},
					"incomplete": {
						"description": "This profile intentionally misses provider."
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolResult<McpTestInfoResult>(response);

			using (Assert.EnterMultipleScope())
			{
				info.Profiles.Exists(profile => profile.Name == "default").   ShouldBe(false);
				info.Profiles.Exists(profile => profile.Name == "sqlite").    ShouldBe(true);
				info.Profiles.Exists(profile => profile.Name == "incomplete").ShouldBe(false);
			}

			server.ExpectStandardError("Configuration profile 'incomplete' doesn't configure provider");
		}

		[Test]
		public async Task McpInfoReturnsToolErrorForOnlyDefaultProfileWithoutProvider()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"description": "Base profile only."
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());

			using (Assert.EnterMultipleScope())
			{
				var result = ReadResponseResult<McpTestCallToolResult>(response);

				result.IsError.        ShouldBe(true);
				result.Content[0].Text.ShouldContain("no configured profiles with provider");
			}

			server.ExpectStandardError("Configuration profile 'default' doesn't configure provider");
		}

		[Test]
		public async Task McpInfoMarksCsvProfileOutputUnsupportedByMcp()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "csv"
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			await server.Initialize();

			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolResult<McpTestInfoResult>(response);
			var infoText = ReadToolText(response);

			using (Assert.EnterMultipleScope())
			{
				info.Profiles[0].DefaultOutput.              ShouldBe("csv");
				info.Profiles[0].DefaultOutputSupportedByMcp.ShouldBe(false);
				info.SupportedOutputFormats.                 ShouldNotContain("csv");
				info.QueryCommandOutputFormats.              ShouldContain("csv");
				infoText.                                    ShouldNotContain("Data Source=:memory:");
			}
		}

		[Test]
		public async Task McpRejectsMissingConfigAtStartup()
		{
			var result = await RunCliProcess("mcp", "--config", "missing-query-config.json");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Output.  ShouldBeEmpty();
				result.Error.   ShouldContain("Configuration file 'missing-query-config.json' not found.");
			}
		}

		[Test]
		public async Task McpRejectsMissingEnvironmentVariableInConfigPathAtStartup()
		{
			var result = await RunCliProcess("mcp", "--config", $"%MISSING_MCP_CONFIG_DIR%{Path.DirectorySeparatorChar}query.json");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Output.  ShouldBeEmpty();
				result.Error.   ShouldContain("Environment variable 'MISSING_MCP_CONFIG_DIR' referenced by option '--config' is not set.");
				result.Error.   ShouldNotContain("Configuration file");
			}
		}

		[TestCase("SapHana", "SAP HANA SQL")]
		[TestCase("SqlCe",   "SQL Server Compact SQL")]
		public async Task McpInfoReportsStartupProviderDialect(string provider, string dialect)
		{
			await using var server = await McpServerProcess.Start("--provider", provider, "--connection-string", "unused");

			await server.Initialize();

			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolResult<McpTestInfoResult>(response);

			using (Assert.EnterMultipleScope())
			{
				info.Profiles[0].Provider.ShouldBe(provider);
				info.Profiles[0].Dialect. ShouldBe(dialect);
			}
		}

		[Test]
		public async Task McpInfoUsesResolvedEnvironmentVariableConfigPath()
		{
			var variableName = $"MCP_CONFIG_DIR_{Guid.NewGuid():N}";
			var config       = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""").ConfigureAwait(false);

			Environment.SetEnvironmentVariable(variableName, Path.GetDirectoryName(config));

			try
			{
				await using var server = await McpServerProcess.Start("--config", $"${{{variableName}}}/{Path.GetFileName(config)}");

				await server.Initialize();

				var response = await server.CallTool("linq2db_info", new JsonObject());
				var info     = ReadToolResult<McpTestInfoResult>(response);

				using (Assert.EnterMultipleScope())
				{
					info.Profiles[0].Provider.ShouldBe("SQLite");
				}
			}
			finally
			{
				Environment.SetEnvironmentVariable(variableName, null);
				File.Delete(config);
			}
		}

		[Test]
		public async Task McpCommandLineResponseLimitOverridesConfig()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-response-limit-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"mcp": {
						"maxResponseBytes": 2048
					},
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""").ConfigureAwait(false);

			await using (var server = await McpServerProcess.Start("--config", config))
			{
				await server.Initialize();

				var response = await server.CallTool("linq2db_info", new JsonObject());
				var info     = ReadToolResult<McpTestInfoResult>(response);

				info.Server.MaxResponseBytes.ShouldBe(2048);
			}

			await using var overrideServer = await McpServerProcess.Start("--config", config, "--max-response-bytes", "100000000");

			await overrideServer.Initialize();

			var overrideResponse = await overrideServer.CallTool("linq2db_info", new JsonObject());
			var overrideInfo     = ReadToolResult<McpTestInfoResult>(overrideResponse);

			using (Assert.EnterMultipleScope())
			{
				overrideInfo.Server.MaxResponseBytes.ShouldBe(100000000);
			}
		}

	}
}
