using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

#pragma warning disable JSON002

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpQueryExecutionToolTests : McpTestBase
	{
		[Test]
		public async Task McpExecutesSqlWithJsonTableDefault()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();

			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select 1 as Value",
			});
			var result = ReadToolResult<McpTestJsonTableResult>(response);

			using (Assert.EnterMultipleScope())
			{
				result.Columns[0].Name.ShouldBe("Value");
				result.Rows.           ShouldBe([["1"]]);
				result.RowCount.       ShouldBe(1);
				result.Truncated.      ShouldBe(false);
			}

			server.ExpectNoStandardError();
		}

		[TestCase("json")]
		[TestCase("json-table")]
		public async Task McpTruncatesResponseAtWholeRowBoundary(string output)
		{
			await using var server = await McpServerProcess.Start(
				"--provider",
				"SQLite",
				"--connection-string",
				"Data Source=:memory:",
				"--max-response-bytes",
				"2048");

			await server.Initialize();

			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["output"] = output,
				["sql"]    = """
					with recursive numbers(Value) as
					(
						select 1
						union all
						select Value + 1 from numbers where Value < 100
					)
					select printf('%0500d', Value) as Value from numbers
					""",
			});

			var content      = ReadResponseResult<McpTestCallToolResult>(response).Content;
			var tableResult  = output == "json-table" ? ReadToolResult<McpTestJsonTableResult>(response) : null;
			var objectResult = output == "json" ? ReadToolResult<List<McpTestValueRow>>(response) : null;
			var returnedRows = tableResult?.Rows.Count ?? objectResult!.Count;
			var warning      = content[1].Text;

			{
				returnedRows. ShouldBeGreaterThan(0);
				returnedRows. ShouldBeLessThan(100);
				content.Count.ShouldBe(2);
				warning.      ShouldContain($"{returnedRows} row(s)");
				warning.      ShouldContain("pagination");
				warning.      ShouldContain("2048 bytes");
			}

			if (output == "json-table")
			{
				tableResult!.RowCount.       ShouldBe(returnedRows);
				tableResult.Truncated.       ShouldBe(true);
				tableResult.TruncationReason.ShouldBe("maxOutputBytes");
				tableResult.MaxOutputBytes.  ShouldBe(2048);
			}
		}

		[Test]
		public async Task McpTruncatesOversizedBlobWithoutMaterializingIt()
		{
			await using var server = await McpServerProcess.Start(
				"--provider",
				"SQLite",
				"--connection-string",
				"Data Source=:memory:",
				"--max-response-bytes",
				"2048");

			await server.Initialize();

			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "select zeroblob(100000000) as Value",
			});
			var content = ReadResponseResult<McpTestCallToolResult>(response).Content;
			var result  = ReadToolResult<McpTestJsonTableResult>(response);

			{
				result.RowCount.        ShouldBe(0);
				result.Rows.            ShouldBeEmpty();
				result.Truncated.       ShouldBe(true);
				result.TruncationReason.ShouldBe("maxOutputBytes");
				result.MaxOutputBytes.  ShouldBe(2048);
				content.Count.          ShouldBe(2);
				content[1].Text.        ShouldContain("pagination");
			}
		}

		[Test]
		public async Task McpQueryReturnsToolErrorForWriteSql()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();

			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["sql"] = "drop table Person",
			});

			using (Assert.EnterMultipleScope())
			{
				ReadToolErrorText(response).ShouldContain("Query is not read-only");
			}
		}

		[Test]
		public async Task McpExecuteToolIsUnavailableByDefault()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();

			var response = await server.CallTool("linq2db_execute", new JsonObject
			{
				["sql"] = "drop table Person",
			});

			using (Assert.EnterMultipleScope())
			{
				response["result"]. ShouldBeNull();
				(response["error"]).ShouldNotBeNull();
			}
		}

		[Test]
		public async Task McpExecuteRequiresProfileEnableExecute()
		{
			var config = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			await File.WriteAllTextAsync(config, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""").ConfigureAwait(false);

			await using var server = await McpServerProcess.Start("--config", config, "--enable-execute-tool");

			await server.Initialize();

			var response = await server.CallTool("linq2db_execute", new JsonObject
			{
				["sql"] = "drop table Person",
			});

			using (Assert.EnterMultipleScope())
			{
				ReadToolErrorText(response).ShouldContain("Profile 'default' doesn't enable execute mode.");
			}
		}

		[Test]
		public async Task McpExecuteRunsWriteSqlWhenEnabled()
		{
			var database = CreateSqliteDatabase();

			var config   = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mcp-query-{Guid.NewGuid():N}.json");

			try
			{
				await File.WriteAllTextAsync(config, $$"""
					{
						"default": {
							"provider": "SQLite",
							"connectionString": "Data Source={{database.Replace("\\", "\\\\", StringComparison.Ordinal)}};Pooling=False",
							"enableExecute": true
						}
					}
					""").ConfigureAwait(false);

				await using var server = await McpServerProcess.Start("--config", config, "--enable-execute-tool");

				await server.Initialize();

				var response = await server.CallTool("linq2db_execute", new JsonObject
				{
					["sql"] = "update Customers set Name = 'updated' where Id = 1",
				});
				var result = ReadToolResult<McpTestJsonTableResult>(response);

				using (Assert.EnterMultipleScope())
				{
					result.RecordsAffected.ShouldBe(1);
				}

				server.ExpectStandardError("Executing write-capable SQL because profile 'default' has enableExecute=true. Provider: SQLite.");
			}
			finally
			{
				File.Delete(database);
				File.Delete(config);
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
				ReadToolErrorText(response).ShouldContain("Only single SQL statement is allowed.");
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
				var error = ReadToolErrorText(response);

				error.ShouldContain("Cannot create database provider 'Oracle.19.Managed'.");
				error.ShouldContain("looks like a test data source alias");
				error.ShouldContain("Oracle.Managed");
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
			var result = ReadToolResult<McpTestJsonTableResult>(response);

			{
				result.RowCount.  ShouldBe(2);
				result.Truncated. ShouldBe(false);
				result.Rows[1][0].ShouldBe("2");
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
			var result = ReadToolResult<McpTestJsonTableResult>(response);

			using (Assert.EnterMultipleScope())
			{
				result.Columns.ConvertAll(column => column.Name).ShouldBe(["Value", "Value"]);
				result.Rows.                                     ShouldBe([["1", "2"]]);
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
				var error = ReadToolErrorText(response);

				error.ShouldContain("MCP query execution supports only 'json' and 'json-table' output.");
				error.ShouldContain("output='csv'");
				error.ShouldContain("Pass output='json-table' or output='json'");
			}
		}

		[Test]
		public async Task McpNormalizesToolOutputFormat()
		{
			await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			await server.Initialize();

			var response = await server.CallTool("linq2db_query", new JsonObject
			{
				["output"] = "JSON-TABLE",
				["sql"]    = "select 1 as Value",
			});
			var result = ReadToolResult<McpTestJsonTableResult>(response);

			using (Assert.EnterMultipleScope())
			{
				result.Columns[0].Name.ShouldBe("Value");
				result.RowCount.       ShouldBe(1);
				result.Rows[0][0].     ShouldBe("1");
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
				var error = ReadToolErrorText(response);

				error.ShouldContain("MCP query execution supports only 'json' and 'json-table' output.");
				error.ShouldContain("output='csv'");
				error.ShouldContain("Pass output='json-table' or output='json'");
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
				response["result"]. ShouldBeNull();
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
			var result = ReadToolResult<McpTestJsonTableResult>(response);

			{
				result.RowCount.  ShouldBe(2);
				result.Rows[1][0].ShouldBe("2");
			}
		}

	}
}
