using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Constraints;

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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That(response["result"]?["tools"]?.AsArray().Count, Is.EqualTo(2));

				var queryTool = FindTool(response, "linq2db_query");
				var infoTool  = FindTool(response, "linq2db_info");
				var queryInputSchema = queryTool["inputSchema"]!.ToJsonString();
				var queryProperties  = queryTool["inputSchema"]!["properties"]!.AsObject();

				Assert.That((string?)queryTool["description"], Does.Contain("Call linq2db_info first"));
				Assert.That((bool?)queryTool["annotations"]?["openWorldHint"], Is.True);
				Assert.That(queryTool["annotations"]?["readOnlyHint"], Is.Null);
				Assert.That(queryTool["annotations"]?["idempotentHint"], Is.Null);
				Assert.That(queryTool["annotations"]?["destructiveHint"], Is.Null);
				Assert.That(queryTool["inputSchema"]?["required"]?.AsArray().ToJsonString(), Does.Contain("sql"));
				Assert.That(queryInputSchema, Does.Contain("allowUnsafeSql"));
				Assert.That(queryProperties.ContainsKey("provider"), Is.False);
				Assert.That(queryProperties.ContainsKey("connectionString"), Is.False);
				Assert.That(queryProperties.ContainsKey("password"), Is.False);
				Assert.That(queryProperties.ContainsKey("providerLocation"), Is.False);

				Assert.That((string?)infoTool["description"], Does.Contain("Returns non-secret linq2db MCP query configuration information"));
				Assert.That((bool?)infoTool["annotations"]?["readOnlyHint"], Is.True);
				Assert.That((bool?)infoTool["annotations"]?["idempotentHint"], Is.True);
				Assert.That((bool?)infoTool["annotations"]?["openWorldHint"], Is.False);
				Assert.That((bool?)infoTool["annotations"]?["destructiveHint"], Is.False);
			}
		}

		[Test]
		public async Task McpInfoReturnsStartupProfile()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite.MS", "--connection-string", "Data Source=secret.db;Password=hidden");

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());
			var info     = ReadToolJson(response);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That(response["result"]?["isError"], Is.Null);
				Assert.That((string?)info["server"]?["name"], Is.EqualTo("linq2db.cli"));
				Assert.That((string?)info["server"]?["command"], Is.EqualTo("mcp"));
				Assert.That((string?)info["defaultProfile"], Is.EqualTo("startup"));
				Assert.That((string?)info["profiles"]?[0]?["name"], Is.EqualTo("startup"));
				Assert.That((string?)info["profiles"]?[0]?["provider"], Is.EqualTo("SQLite.MS"));
				Assert.That((string?)info["profiles"]?[0]?["dialect"], Is.EqualTo("SQLite"));
				Assert.That((string?)info["profiles"]?[0]?["defaultOutput"], Is.EqualTo("json-table"));
				Assert.That((int?)info["profiles"]?[0]?["maxRows"], Is.EqualTo(1000));
				Assert.That((string?)info["profiles"]?[0]?["unsafeSqlPolicy"], Is.EqualTo("deny"));
				Assert.That((bool?)info["profiles"]?[0]?["impersonationEnabled"], Is.False);
				Assert.That(info["supportedOutputFormats"]?.AsArray().ToJsonString(), Does.Contain("json-table"));
				Assert.That((bool?)info["rules"]?["singleStatementOnly"], Is.True);
				Assert.That(info.ToJsonString(), Does.Not.Contain("secret.db"));
				Assert.That(info.ToJsonString(), Does.Not.Contain("hidden"));
				Assert.That(info["profiles"]?[0]?.AsObject().ContainsKey("connectionString"), Is.False);
				Assert.That(info["profiles"]?[0]?.AsObject().ContainsKey("password"), Is.False);
				Assert.That(info["profiles"]?[0]?.AsObject().ContainsKey("providerLocation"), Is.False);
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((string?)info["defaultProfile"], Is.EqualTo("sqlserver"));
				Assert.That(info["profiles"]?.AsArray().Count, Is.EqualTo(1));

				var sqlServer      = FindProfile(info, "sqlserver");

				Assert.That(ContainsProfile(info, "default"), Is.False);

				Assert.That((string?)sqlServer["description"], Does.Contain("T-SQL"));
				Assert.That((string?)sqlServer["provider"], Is.EqualTo("SqlServer"));
				Assert.That((string?)sqlServer["dialect"], Is.EqualTo("SQL Server T-SQL"));
				Assert.That((int?)sqlServer["maxRows"], Is.EqualTo(500));
				Assert.That((string?)sqlServer["unsafeSqlPolicy"], Is.EqualTo("confirm"));
				Assert.That((bool?)sqlServer["impersonationEnabled"], Is.True);

				Assert.That(info.ToJsonString(), Does.Not.Contain("dev-secret.db"));
				Assert.That(info.ToJsonString(), Does.Not.Contain("LINQ2DB_SQLSERVER_CONNECTION"));
				Assert.That(sqlServer.ContainsKey("connectionString"), Is.False);
				Assert.That(sqlServer.ContainsKey("connectionStringEnv"), Is.False);
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That(response["result"]?["isError"], Is.Null);
				Assert.That(ContainsProfile(info, "default"), Is.False);
				Assert.That(ContainsProfile(info, "sqlite"), Is.True);
				Assert.That(ContainsProfile(info, "incomplete"), Is.False);
			}

			server.ExpectStandardError(Does.Contain("Configuration profile 'incomplete' doesn't configure provider"));
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
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("no configured profiles with provider"));
			}

			server.ExpectStandardError(Does.Contain("Configuration profile 'default' doesn't configure provider"));
		}

		[Test]
		public async Task McpInfoReturnsToolErrorForMissingConfig()
		{
			await using var server = await McpServerProcess.Start("--config", "missing-query-config.json");

			await server.Initialize();
			var response = await server.CallTool("linq2db_info", new JsonObject());

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("Cannot load linq2db query configuration"));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That(response["result"]?["isError"], Is.Null);
				Assert.That(contentText, Does.Contain("\"columns\""));
				Assert.That(contentText, Does.Contain("\"rows\":[[\"1\"]]"));
				Assert.That(contentText, Does.Contain("\"rowCount\":1"));
				Assert.That(contentText, Does.Contain("\"truncated\":false"));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("Query is not read-only"));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("Only single SQL statement is allowed."));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("Cannot create database provider 'Oracle.19.Managed'."));
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("looks like a test data source alias"));
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("Oracle.Managed"));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That(contentText, Does.Contain("\"rowCount\":2"));
				Assert.That(contentText, Does.Contain("\"truncated\":false"));
				Assert.That(contentText, Does.Contain("\"2\""));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That(contentText, Does.Contain("\"name\":\"Value\""));
				Assert.That(contentText, Does.Contain("\"rows\":[[\"1\",\"2\"]]"));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("MCP query execution supports only 'json' and 'json-table' output."));
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((bool?)response["result"]?["isError"], Is.True);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("MCP query execution supports only 'json' and 'json-table' output."));
			}
		}

		[Test]
		public async Task McpRejectsUnknownTool()
		{
			await using var server = await McpServerProcess.Start();

			await server.Initialize();
			var response = await server.CallTool("unknown_tool", new JsonObject());

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["result"], Is.Null);
				Assert.That(response["error"], Is.Not.Null);
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

			using (Assert.EnterMultipleScope())
			{
				Assert.That(response["error"], Is.Null);
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("\"rowCount\":2"));
				Assert.That((string?)response["result"]?["content"]?[0]?["text"], Does.Contain("\"2\""));
			}
		}

		[Test]
		public async Task McpHelpShowsMcpBoundary()
		{
			var result = await RunCliProcess("help", "mcp");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output, Does.Contain("dotnet linq2db mcp <options>"));
				Assert.That(result.Output, Does.Contain("run STDIO MCP server"));
				Assert.That(result.Output, Does.Contain("default: json-table"));
				Assert.That(result.Output, Does.Contain("--config"));
				Assert.That(result.Output, Does.Contain("--provider"));
				Assert.That(result.Output, Does.Contain("--max-rows"));
				Assert.That(result.Output, Does.Not.Contain("CSV output"));
				Assert.That(result.Output, Does.Not.Contain("--sql"));
				Assert.That(result.Output, Does.Not.Contain("--output-file"));
				Assert.That(result.Output, Does.Not.Contain("--allow-unsafe-sql"));
			}
		}

		[Test]
		public async Task McpRejectsQueryOnlyStartupOptions()
		{
			var result = await RunCliProcess("mcp", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Output, Is.Empty);
				Assert.That(result.Error, Does.Contain("Unrecognized option: --sql"));
			}
		}

		[Test]
		public async Task McpRejectsCsvStartupOutput()
		{
			var result = await RunCliProcess("mcp", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Output, Is.Empty);
				Assert.That(result.Error, Does.Contain("Cannot parse option value (--output csv): unknown value 'csv'"));
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
			int         _nextId = 1;
			Constraint? _standardErrorExpectation;

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
				_standardErrorExpectation = Is.Empty;
			}

			public void ExpectStandardError(Constraint expectation)
			{
				_standardErrorExpectation = expectation;
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
						Assert.That((string?)message["jsonrpc"], Is.EqualTo("2.0"));
						Assert.That(message["result"] != null || message["error"] != null, Is.True);
					}

					if (_standardErrorExpectation != null)
						Assert.That(await _standardErrorTask.ConfigureAwait(false), _standardErrorExpectation);

					_process.Dispose();
				}
			}
		}
	}
}
