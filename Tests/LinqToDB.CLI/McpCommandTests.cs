using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;

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
				Assert.That(response["result"]?["tools"]?.AsArray().Count, Is.EqualTo(1));

				var tool = response["result"]!["tools"]![0]!;

				Assert.That((string?)tool["name"], Is.EqualTo("linq2db_query"));
				Assert.That((string?)tool["description"], Does.Contain("Executes a single read-oriented SQL query"));
				Assert.That((bool?)tool["annotations"]?["readOnlyHint"], Is.True);
				Assert.That((bool?)tool["annotations"]?["destructiveHint"], Is.False);
				Assert.That(tool["inputSchema"]?["required"]?.AsArray().ToJsonString(), Does.Contain("sql"));
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
			readonly List<JsonObject> _stdoutMessages = new();
			int _nextId = 1;

			McpServerProcess(Process process)
			{
				_process = process;
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

				var server = new McpServerProcess(process);

				await Task.Yield();

				return server;
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
					var error = await _process.StandardError.ReadToEndAsync().ConfigureAwait(false);
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

					_process.Dispose();
				}
			}
		}
	}
}
