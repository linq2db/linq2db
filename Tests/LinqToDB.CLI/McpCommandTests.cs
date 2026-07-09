using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Shouldly;

#nullable enable annotations
#nullable disable warnings

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpCommandTests
	{
		[Test]
		public async Task McpListsQueryTool()
		{
			await using var server = await McpServerProcess.Start();

			await server.Initialize();
			var response = await server.SendRequest("tools/list", new JsonObject());

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["tools"]?.AsArray().Count).ShouldBe(4);

				var queryTool = FindTool(response, "linq2db_query");
				var infoTool  = FindTool(response, "linq2db_info");
				var schemaTool = FindTool(response, "linq2db_schema");
				var skillTool = FindTool(response, "linq2db_skill");

				var queryInputSchema = queryTool["inputSchema"]!.ToJsonString();
				var queryProperties  = queryTool["inputSchema"]!["properties"]!.AsObject();
				var schemaProperties = schemaTool["inputSchema"]!["properties"]!.AsObject();

				((string?)queryTool["description"]).ShouldContain("Call linq2db_info first");
				((string?)queryTool["description"]).ShouldContain("Call linq2db_skill");
				((bool?)queryTool["annotations"]?["openWorldHint"]).ShouldBe(true);
				(queryTool["annotations"]?["readOnlyHint"]).ShouldBeNull();
				(queryTool["annotations"]?["idempotentHint"]).ShouldBeNull();
				(queryTool["annotations"]?["destructiveHint"]).ShouldBeNull();
				(queryTool["inputSchema"]?["required"]?.AsArray().ToJsonString()).ShouldContain("sql");
				(queryInputSchema).ShouldContain("allowUnsafeSql");
				(queryProperties.ContainsKey("provider")).ShouldBe(false);
				(queryProperties.ContainsKey("connectionString")).ShouldBe(false);
				(queryProperties.ContainsKey("password")).ShouldBe(false);
				(queryProperties.ContainsKey("providerLocation")).ShouldBe(false);

				((string?)infoTool["description"]).ShouldContain("Returns non-secret linq2db MCP query configuration information");
				((string?)infoTool["description"]).ShouldContain("Use linq2db_schema");
				((string?)infoTool["description"]).ShouldContain("Use linq2db_skill");
				((bool?)  infoTool["annotations"]?["readOnlyHint"]).ShouldBe(true);
				((bool?)  infoTool["annotations"]?["idempotentHint"]).ShouldBe(true);
				((bool?)  infoTool["annotations"]?["openWorldHint"]).ShouldBe(false);
				((bool?)  infoTool["annotations"]?["destructiveHint"]).ShouldBe(false);
				(infoTool["inputSchema"]?["properties"]?.AsObject().Count).ShouldBe(0);
				(infoTool["inputSchema"]?["required"]).ShouldBeNull();

				((string?)schemaTool["description"]).ShouldContain("Returns provider-independent database schema metadata");
				((string?)schemaTool["description"]).ShouldContain("Procedures and functions are not supported");
				((bool?)schemaTool["annotations"]?["readOnlyHint"]).ShouldBe(true);
				((bool?)schemaTool["annotations"]?["idempotentHint"]).ShouldBe(true);
				((bool?)schemaTool["annotations"]?["openWorldHint"]).ShouldBe(true);
				((bool?)schemaTool["annotations"]?["destructiveHint"]).ShouldBe(false);
				schemaProperties.ContainsKey("provider").ShouldBe(false);
				schemaProperties.ContainsKey("connectionString").ShouldBe(false);
				schemaProperties.ContainsKey("password").ShouldBe(false);
				schemaProperties.ContainsKey("providerLocation").ShouldBe(false);
				schemaProperties.ContainsKey("sql").ShouldBe(false);
				schemaProperties.ContainsKey("outputFile").ShouldBe(false);
				schemaProperties.ContainsKey("filterTables").ShouldBe(true);
				schemaProperties.ContainsKey("excludeTables").ShouldBe(false);
				schemaProperties.ContainsKey("includeTables").ShouldBe(false);
				schemaProperties.ContainsKey("getProcedures").ShouldBe(false);
				schemaProperties.ContainsKey("useSchemaOnly").ShouldBe(false);

				((string?)skillTool["description"]).ShouldContain("Returns the full embedded linq2db CLI agent skill as Markdown");
				((bool?)skillTool["annotations"]?["readOnlyHint"]).ShouldBe(true);
				((bool?)skillTool["annotations"]?["idempotentHint"]).ShouldBe(true);
				((bool?)skillTool["annotations"]?["openWorldHint"]).ShouldBe(false);
				((bool?)skillTool["annotations"]?["destructiveHint"]).ShouldBe(false);
				(skillTool["inputSchema"]?["properties"]?.AsObject().Count).ShouldBe(0);
				(skillTool["inputSchema"]?["required"]).ShouldBeNull();
			}
		}

		[Test]
		public async Task McpSkillReturnsEmbeddedSkillMarkdown()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=secret-skill.db;Password=hidden");

			await server.Initialize();
			var response  = await server.CallTool("linq2db_skill", new JsonObject());
			var skillText = (string?)response["result"]?["content"]?[0]?["text"];

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				skillText.ShouldNotBeNull();
				skillText.Length.ShouldBeGreaterThan(1000);
				(skillText).ShouldStartWith("# linq2db CLI Agent Skill");
				(skillText).ShouldNotContain("secret-skill.db");
				(skillText).ShouldNotContain("hidden");
			}

			server.ExpectNoStandardError();
		}

		[Test]
		public async Task McpSkillDoesNotRequireValidDatabaseConfiguration()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=missing-skill.db;Password=secret");

			await server.Initialize();
			var response = await server.CallTool("linq2db_skill", new JsonObject());

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Use `dotnet linq2db mcp`");
			}

			server.ExpectNoStandardError();
		}

		[Test]
		public async Task McpInfoReturnsStartupProfile()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite.MS", "--connection-string", "Data Source=secret.db;Password=hidden");

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolJson(response);

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				((string?)info["server"]?["name"]).ShouldBe("linq2db.cli");
				((string?)info["server"]?["command"]).ShouldBe("mcp");
				((string?)info["defaultProfile"]).ShouldBe("startup");
				((bool?)info["defaultProfileUsable"]).ShouldBe(true);
				((string?)info["profiles"]?[0]?["name"]).ShouldBe("startup");
				((string?)info["profiles"]?[0]?["provider"]).ShouldBe("SQLite.MS");
				((string?)info["profiles"]?[0]?["dialect"]).ShouldBe("SQLite");
				((string?)info["profiles"]?[0]?["defaultOutput"]).ShouldBe("json-table");
				((bool?)info["profiles"]?[0]?["defaultOutputSupportedByMcp"]).ShouldBe(true);
				((int?)info["profiles"]?[0]?["maxRows"]).ShouldBe(1000);
				((string?)info["profiles"]?[0]?["unsafeSqlPolicy"]).ShouldBe("deny");
				((bool?)info["profiles"]?[0]?["impersonationEnabled"]).ShouldBe(false);
				(info["supportedOutputFormats"]?.AsArray().ToJsonString()).ShouldContain("json-table");
				(info["supportedOutputFormats"]?.AsArray().ToJsonString()).ShouldNotContain("csv");
				(info["queryCommandOutputFormats"]?.AsArray().ToJsonString()).ShouldContain("csv");
				(info["supportedProviders"]?.AsArray().ToJsonString()).ShouldContain("SQL Server");
				(info["supportedProviders"]?.AsArray().ToJsonString()).ShouldContain("IBM DB2");
				(info["supportedProviders"]?.AsArray().ToJsonString()).ShouldContain("IBM Informix");
				(info["supportedProviders"]?.AsArray().ToJsonString()).ShouldContain("IBM.Data.Db2.dll");
				((bool?)FindSupportedProvider(info, "IBM DB2")["bundled"]).ShouldBe(false);
				(FindSupportedProvider(info, "IBM DB2")["providerNames"]?.AsArray().ToJsonString()).ShouldContain("DB2");
				((bool?)info["rules"]?["singleStatementOnly"]).ShouldBe(true);
				(info.ToJsonString()).ShouldNotContain("secret.db");
				(info.ToJsonString()).ShouldNotContain("hidden");
				(info["profiles"]?[0]?.AsObject().ContainsKey("connectionString")).ShouldBe(false);
				(info["profiles"]?[0]?.AsObject().ContainsKey("password")).ShouldBe(false);
				(info["profiles"]?[0]?.AsObject().ContainsKey("providerLocation")).ShouldBe(false);
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
						"unsafeSql": "deny"
					},
					"sqlserver": {
						"description": "Use T-SQL syntax. Prefer dbo schema qualification.",
						"provider": "SqlServer",
						"connectionStringEnv": "LINQ2DB_SQLSERVER_CONNECTION",
						"maxRows": 500,
						"unsafeSql": "confirm",
						"impersonate": true
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config, "--profile", "sqlserver", "--output", "json-table");

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolJson(response);

			{
				(response["error"]).ShouldBeNull();
				((string?)info["defaultProfile"]).ShouldBe("sqlserver");
				((bool?)info["defaultProfileUsable"]).ShouldBe(true);
				(info["profiles"]?.AsArray().Count).ShouldBe(1);

				var sqlServer      = FindProfile(info, "sqlserver");

				(ContainsProfile(info, "default")).ShouldBe(false);

				((string?)sqlServer["description"]).ShouldContain("T-SQL");
				((string?)sqlServer["provider"]).ShouldBe("SqlServer");
				((string?)sqlServer["dialect"]).ShouldBe("SQL Server T-SQL");
				((bool?)sqlServer["defaultOutputSupportedByMcp"]).ShouldBe(true);
				((int?)sqlServer["maxRows"]).ShouldBe(500);
				((string?)sqlServer["unsafeSqlPolicy"]).ShouldBe("confirm");
				((bool?)sqlServer["impersonationEnabled"]).ShouldBe(true);

				(info.ToJsonString()).ShouldNotContain("dev-secret.db");
				(info.ToJsonString()).ShouldNotContain("LINQ2DB_SQLSERVER_CONNECTION");
				(sqlServer.ContainsKey("connectionString")).ShouldBe(false);
				(sqlServer.ContainsKey("connectionStringEnv")).ShouldBe(false);
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
			var info     = ReadToolJson(response);

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				((string?)info["defaultProfile"]).ShouldBe("default");
				((bool?)info["defaultProfileUsable"]).ShouldBe(false);
				(ContainsProfile(info, "default")).ShouldBe(false);
				(ContainsProfile(info, "sqlite")).ShouldBe(true);
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
			var info     = ReadToolJson(response);

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				(ContainsProfile(info, "default")).ShouldBe(false);
				(ContainsProfile(info, "sqlite")).ShouldBe(true);
				(ContainsProfile(info, "incomplete")).ShouldBe(false);
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

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("no configured profiles with provider");
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
			var info     = ReadToolJson(response);

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				((string?)info["profiles"]?[0]?["defaultOutput"]).ShouldBe("csv");
				((bool?)info["profiles"]?[0]?["defaultOutputSupportedByMcp"]).ShouldBe(false);
				(info["supportedOutputFormats"]?.AsArray().ToJsonString()).ShouldNotContain("csv");
				(info["queryCommandOutputFormats"]?.AsArray().ToJsonString()).ShouldContain("csv");
				(info.ToJsonString()).ShouldNotContain("Data Source=:memory:");
			}
		}

		[Test]
		public async Task McpInfoReturnsToolErrorForMissingConfig()
		{
			await using var server = await McpServerProcess.Start("--config", "missing-query-config.json");

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Cannot load linq2db query configuration");
			}
		}

		[Test]
		public async Task McpExecutesSqlWithJsonTableDefault()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select 1 as Value",
			});

			var contentText = (string?)response["result"]?["content"]?[0]?["text"];

			{
				(response["error"]).ShouldBeNull();
				(response["result"]?["isError"]).ShouldBeNull();
				(contentText).ShouldContain("\"columns\"");
				(contentText).ShouldContain("\"rows\":[[\"1\"]]");
				(contentText).ShouldContain("\"rowCount\":1");
				(contentText).ShouldContain("\"truncated\":false");
			}

			server.ExpectNoStandardError();
		}

		[Test]
		public async Task McpReturnsToolErrorForUnsafeSql()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "drop table Person",
			});

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Query is not read-only");
			}
		}

		[Test]
		public async Task McpReturnsToolErrorForMultipleStatements()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select 1; select 2",
			});

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Only single SQL statement is allowed.");
			}
		}

		[Test]
		public async Task McpReturnsToolErrorForProviderAlias()
		{
			await using var server = await McpServerProcess.Start("--provider", "Oracle.19.Managed", "--connection-string", "Data Source=localhost/XE;User Id=test;Password=test");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select 1 from dual",
			});

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Cannot create database provider 'Oracle.19.Managed'.");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("looks like a test data source alias");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Oracle.Managed");
			}
		}

		[Test]
		public async Task McpAppliesToolMaxRowsOverride()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--max-rows", "1");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["maxRows"] = 0,
				["sql"]     = "select 1 as Value union all select 2",
			});

			var contentText = (string?)response["result"]?["content"]?[0]?["text"];

			{
				(response["error"]).ShouldBeNull();
				(contentText).ShouldContain("\"rowCount\":2");
				(contentText).ShouldContain("\"truncated\":false");
				(contentText).ShouldContain("\"2\"");
			}
		}

		[Test]
		public async Task McpJsonTablePreservesDuplicateColumns()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select 1 as Value, 2 as Value",
			});

			var contentText = (string?)response["result"]?["content"]?[0]?["text"];

			{
				(response["error"]).ShouldBeNull();
				(contentText).ShouldContain("\"name\":\"Value\"");
				(contentText).ShouldContain("\"rows\":[[\"1\",\"2\"]]");
			}
		}

		[Test]
		public async Task McpSchemaReturnsSqliteMetadata()
		{
			var database = CreateSqliteDatabase();

			try
			{
				await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False");

				await server.Initialize();
				var response = await server.CallTool("linq2db_schema", new JsonObject
				{
					["getForeignKeys"] = false,
					["filterTables"] = new JsonArray("main.Orders"),
				});
				var schema = ReadToolJson(response);
				var orders = FindSchemaTable(schema, "Orders");

				{
					(response["error"]).ShouldBeNull();
					(response["result"]?["isError"]).ShouldBeNull();
					((string?)schema["provider"]).ShouldBe("SQLite");
					((string?)schema["dialect"]).ShouldBe("SQLite");
					((bool?)schema["options"]?["getProcedures"]).ShouldBe(false);
					((bool?)schema["options"]?["getForeignKeys"]).ShouldBe(false);
					((string?)schema["options"]?["filterTables"]?[0]).ShouldBe("main.Orders");
					schema["tables"]!.AsArray().Count.ShouldBe(1);
					orders["columns"]!.AsArray().Count.ShouldBe(3);
					orders["foreignKeys"]!.AsArray().Count.ShouldBe(0);
					(response.ToJsonString()).ShouldNotContain("Data Source=");
				}

				server.ExpectNoStandardError();
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task McpSchemaReportsRegexFilterTimeout()
		{
			var database = CreateSqliteDatabase();

			try
			{
				await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False");

				await server.Initialize();
				var response = await server.CallTool("linq2db_schema", new JsonObject
				{
					["filterTables"] = new JsonArray("rx:^(a+)+$"),
				});

				{
					(response["error"]).ShouldBeNull();
					((bool?)response["result"]?["isError"]).ShouldBe(true);
					((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Table filter regex '^(a+)+$' timed out");
				}

				server.ExpectNoStandardError();
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task McpRejectsCsvToolOutput()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["output"] = "csv",
				["sql"]    = "select 1 as Value",
			});

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("MCP query execution supports only 'json' and 'json-table' output.");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("output='csv'");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Pass output='json-table' or output='json'");
			}
		}

		[Test]
		public async Task McpRejectsCsvConfigOutput()
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
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select 1 as Value",
			});

			{
				(response["error"]).ShouldBeNull();
				((bool?)response["result"]?["isError"]).ShouldBe(true);
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("MCP query execution supports only 'json' and 'json-table' output.");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("output='csv'");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("Pass output='json-table' or output='json'");
			}
		}

		[Test]
		public async Task McpRejectsUnknownTool()
		{
			await using var server = await McpServerProcess.Start();

			await server.Initialize();
			var response = await server.CallTool("unknown_tool", new JsonObject());

			{
				(response["result"]).ShouldBeNull();
				(response["error"]).ShouldNotBeNull();
			}
		}

		[Test]
		public async Task McpUsesProfileOverrideFromToolCall()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");
			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"maxRows": 1
					},
					"wide": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"maxRows": 0
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config);

			await server.Initialize();
			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["profile"] = "wide",
				["sql"]     = "select 1 as Value union all select 2",
			});

			{
				(response["error"]).ShouldBeNull();
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("\"rowCount\":2");
				((string?)response["result"]?["content"]?[0]?["text"]).ShouldContain("\"2\"");
			}
		}

		[Test]
		public async Task McpHelpShowsMcpBoundary()
		{
			var result = await RunCliProcess("help", "mcp");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Output).ShouldContain("dotnet linq2db mcp <options>");
				(result.Output).ShouldContain("run STDIO MCP server");
				(result.Output).ShouldContain("default: json-table");
				(result.Output).ShouldContain("--config");
				(result.Output).ShouldContain("--provider");
				(result.Output).ShouldContain("--max-rows");
				(result.Output).ShouldNotContain("CSV output");
				(result.Output).ShouldNotContain("--sql");
				(result.Output).ShouldNotContain("--output-file");
				(result.Output).ShouldNotContain("--allow-unsafe-sql");
			}
		}

		[Test]
		public async Task McpRejectsQueryOnlyStartupOptions()
		{
			var result = await RunCliProcess("mcp", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Unrecognized option: --sql");
			}
		}

		[Test]
		public async Task McpRejectsCsvStartupOutput()
		{
			var result = await RunCliProcess("mcp", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Cannot parse option value (--output csv): unknown value 'csv'");
			}
		}

		static JsonObject FindTool(JsonObject response, string name)
		{
			foreach (var tool in response["result"]!["tools"]!.AsArray())
			{
				if ((string?)tool?["name"] == name)
					return tool!.AsObject();
			}

			throw new InvalidOperationException($"Tool '{name}' not found.");
		}

		static JsonObject ReadToolJson(JsonObject response)
		{
			var contentText = (string?)response["result"]?["content"]?[0]?["text"];

			if (contentText == null)
				throw new InvalidOperationException("MCP tool response doesn't contain text content.");

			return JsonNode.Parse(contentText)?.AsObject()
				?? throw new InvalidOperationException("MCP tool response text content is not a JSON object.");
		}

		static JsonObject FindSchemaTable(JsonObject schema, string name)
		{
			foreach (var table in schema["tables"]!.AsArray())
			{
				if ((string?)table?["name"] == name)
					return table!.AsObject();
			}

			throw new InvalidOperationException($"Table '{name}' not found.");
		}

		static string CreateSqliteDatabase()
		{
			var fileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-schema-{Guid.NewGuid():N}.db");

			var dataProvider = DataConnection.GetDataProvider("SQLite", $"Data Source={fileName};Pooling=False");
			using var db     = new DataConnection(new DataOptions().UseConnectionString(dataProvider, $"Data Source={fileName};Pooling=False"));

			db.Execute("""
				create table Customers
				(
					Id   integer not null primary key,
					Name text    not null
				)
				""");

			db.Execute("""
				create table Orders
				(
					Id         integer not null primary key,
					CustomerId integer not null references Customers(Id),
					Amount     decimal(10, 2) null
				)
				""");

			db.Execute("""
				create table ChildOrders
				(
					Id      integer not null primary key,
					OrderId integer not null references Orders(Id)
				)
				""");

			var longName = new string('a', 512) + "b";
			db.Execute($"create table \"{longName}\" (Id integer not null primary key)");

			return fileName;
		}

		static JsonObject FindProfile(JsonObject info, string name)
		{
			foreach (var profile in info["profiles"]!.AsArray())
			{
				if ((string?)profile?["name"] == name)
					return profile!.AsObject();
			}

			throw new InvalidOperationException($"Profile '{name}' not found.");
		}

		static bool ContainsProfile(JsonObject info, string name)
		{
			foreach (var profile in info["profiles"]!.AsArray())
			{
				if ((string?)profile?["name"] == name)
					return true;
			}

			return false;
		}

		static JsonObject FindSupportedProvider(JsonObject info, string name)
		{
			foreach (var provider in info["supportedProviders"]!.AsArray())
			{
				if ((string?)provider?["name"] == name)
					return provider!.AsObject();
			}

			throw new InvalidOperationException($"Supported provider '{name}' not found.");
		}

		static async Task<CliProcessResult> RunCliProcess(params string[] arguments)
		{
			var cliAssembly = Path.Combine(AppContext.BaseDirectory, "dotnet-linq2db.dll");

			if (!File.Exists(cliAssembly))
				throw new FileNotFoundException("Cannot find CLI assembly for MCP process tests.", cliAssembly);

			var startInfo = new ProcessStartInfo("dotnet")
			{
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
			};

			startInfo.ArgumentList.Add(cliAssembly);

			foreach (var argument in arguments)
				startInfo.ArgumentList.Add(argument);

			using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start CLI process.");

			var outputTask = process.StandardOutput.ReadToEndAsync();
			var errorTask  = process.StandardError.ReadToEndAsync();

			await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);

			return new CliProcessResult(process.ExitCode, await outputTask.ConfigureAwait(false), await errorTask.ConfigureAwait(false));
		}

		sealed record CliProcessResult(int ExitCode, string Output, string Error);

		sealed class McpServerProcess : IAsyncDisposable
		{
			readonly Process          _process;
			readonly Task<string>     _standardErrorTask;
			readonly List<JsonObject> _stdoutMessages = new();
			int     _nextId = 1;
			bool    _expectNoStandardError;
			string? _standardErrorExpectation;

			McpServerProcess(Process process, Task<string> standardErrorTask)
			{
				_process           = process;
				_standardErrorTask = standardErrorTask;
			}

			public static async Task<McpServerProcess> Start(params string[] arguments)
			{
				var cliAssembly = Path.Combine(AppContext.BaseDirectory, "dotnet-linq2db.dll");

				if (!File.Exists(cliAssembly))
					throw new FileNotFoundException("Cannot find CLI assembly for MCP process tests.", cliAssembly);

				var startInfo = new ProcessStartInfo("dotnet")
				{
					RedirectStandardInput  = true,
					RedirectStandardOutput = true,
					RedirectStandardError  = true,
					UseShellExecute        = false,
				};

				startInfo.ArgumentList.Add(cliAssembly);
				startInfo.ArgumentList.Add("mcp");

				foreach (var argument in arguments)
					startInfo.ArgumentList.Add(argument);

				var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start MCP server process.");
				process.StandardInput.AutoFlush = true;

				var server = new McpServerProcess(process, process.StandardError.ReadToEndAsync());

				await Task.Yield();

				return server;
			}

			public void ExpectNoStandardError()
			{
				_expectNoStandardError = true;
			}

			public void ExpectStandardError(string expectedText)
			{
				_standardErrorExpectation = expectedText;
			}

			public async Task Initialize()
			{
				await SendRequest("initialize", new JsonObject
				{
					["protocolVersion"] = "2025-06-18",
					["capabilities"]    = new JsonObject(),
					["clientInfo"]      = new JsonObject
					{
						["name"]    = "linq2db-cli-tests",
						["version"] = "1.0",
					},
				}).ConfigureAwait(false);

				await SendNotification("notifications/initialized", new JsonObject()).ConfigureAwait(false);
			}

			public Task<JsonObject> CallTool(string name, JsonObject arguments)
			{
				return SendRequest("tools/call", new JsonObject
				{
					["name"]      = name,
					["arguments"] = arguments,
				});
			}

			public async Task<JsonObject> SendRequest(string method, JsonObject parameters)
			{
				var id = _nextId++;

				await WriteMessage(new JsonObject
				{
					["jsonrpc"] = "2.0",
					["id"]      = id,
					["method"]  = method,
					["params"]  = parameters,
				}).ConfigureAwait(false);

				while (true)
				{
					var response = await ReadMessage().ConfigureAwait(false);

					if ((int?)response["id"] == id)
						return response;
				}
			}

			async Task SendNotification(string method, JsonObject parameters)
			{
				await WriteMessage(new JsonObject
				{
					["jsonrpc"] = "2.0",
					["method"]  = method,
					["params"]  = parameters,
				}).ConfigureAwait(false);
			}

			async Task WriteMessage(JsonObject message)
			{
				await _process.StandardInput.WriteLineAsync(message.ToJsonString()).ConfigureAwait(false);
			}

			async Task<JsonObject> ReadMessage()
			{
				var line = await _process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);

				if (line == null)
				{
					var error = await _standardErrorTask.ConfigureAwait(false);
					throw new InvalidOperationException($"MCP server closed stdout before response. stderr: {error}");
				}

				JsonObject? message;

				try
				{
					message = JsonNode.Parse(line)?.AsObject();
				}
				catch (JsonException ex)
				{
					throw new InvalidOperationException($"MCP stdout line is not a JSON-RPC message: {line}", ex);
				}

				_stdoutMessages.Add(message ?? throw new InvalidOperationException($"MCP stdout line is not a JSON object: {line}"));
				return message!;
			}

			public async ValueTask DisposeAsync()
			{
				try
				{
					_process.StandardInput.Close();
					await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
				}
				catch
				{
					if (!_process.HasExited)
						_process.Kill(entireProcessTree: true);
				}
				finally
				{
					if (!_process.HasExited)
						_process.Kill(entireProcessTree: true);

					foreach (var message in _stdoutMessages)
					{
						((string?)message["jsonrpc"]).ShouldBe("2.0");
						(message["result"] != null || message["error"] != null).ShouldBe(true);
					}

					if (_expectNoStandardError)
						(await _standardErrorTask.ConfigureAwait(false)).ShouldBeEmpty();

					if (_standardErrorExpectation != null)
						(await _standardErrorTask.ConfigureAwait(false)).ShouldContain(_standardErrorExpectation);

					_process.Dispose();
				}
			}
		}
	}
}
