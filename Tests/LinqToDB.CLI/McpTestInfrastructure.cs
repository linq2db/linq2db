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

#pragma warning disable JSON002

namespace Tests.LinqToDB.CLI
{
	public abstract partial class McpTestBase : CliProcessTestBase
	{
		protected static string CreateSqliteDatabase()
		{
			return CreateCliSqliteDatabase("mcp-schema", seedCustomers: true);
		}

		protected sealed class McpServerProcess : IAsyncDisposable
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

			public async Task<JsonObject> Initialize()
			{
				var response = await SendRequest("initialize", new JsonObject
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
				return response;
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
