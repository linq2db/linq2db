using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class McpSchemaToolTests : McpTestBase
	{
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
				var schema = ReadToolResult<McpTestSchemaResult>(response);
				var orders = FindSchemaTable(schema, "Orders");

				{
					schema.Provider.              ShouldBe("SQLite");
					schema.Dialect.               ShouldBe("SQLite");
					schema.Options.DetailLevel.   ShouldBe("full");
					schema.Options.GetProcedures. ShouldBe(false);
					schema.Options.GetForeignKeys.ShouldBe(false);
					schema.Options.FilterTables.  ShouldBe(["main.Orders"]);
					schema.Tables.Count.          ShouldBe(1);
					orders.Columns.Count.         ShouldBe(3);
					orders.ForeignKeys.Count.     ShouldBe(0);
					ReadToolText(response).       ShouldNotContain("Data Source=");
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
					var result = ReadResponseResult<McpTestCallToolResult>(response);

					result.IsError.        ShouldBe(true);
					result.Content[0].Text.ShouldContain("Table filter regex '^(a+)+$' timed out");
				}

				server.ExpectNoStandardError();
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task McpSchemaReturnsCompactObjectNames()
		{
			var database = CreateSqliteDatabase();

			try
			{
				await using var server = await McpServerProcess.Start("--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False");

				await server.Initialize();
				var response = await server.CallTool("linq2db_schema", new JsonObject
				{
					["detailLevel"] = "names",
					["filterTables"] = new JsonArray("main.Orders"),
				});
				var schema = ReadToolResult<McpTestSchemaNamesResult>(response);

				{
					schema.Provider.              ShouldBe("SQLite");
					schema.Options.DetailLevel.   ShouldBe("names");
					schema.Options.GetForeignKeys.ShouldBe(false);
					schema.Objects.Count.         ShouldBe(1);
					schema.Objects[0].Catalog.    ShouldBeNull();
					schema.Objects[0].Schema.     ShouldBe("main");
					schema.Objects[0].Name.       ShouldBe("Orders");
					schema.Objects[0].Kind.       ShouldBe("table");
					ReadToolText(response).       ShouldNotContain("\"tables\"");
					ReadToolText(response).       ShouldNotContain("\"columns\"");
				}

				server.ExpectNoStandardError();
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task McpSchemaRejectsResponseOverConfiguredLimit()
		{
			var database = CreateSqliteDatabase();

			try
			{
				await using var server = await McpServerProcess.Start(
					"--provider",
					"SQLite",
					"--connection-string",
					$"Data Source={database};Pooling=False",
					"--max-response-bytes",
					"512");

				await server.Initialize();
				var response = await server.CallTool("linq2db_schema", new JsonObject());
				var result   = ReadResponseResult<McpTestCallToolResult>(response);

				{
					result.IsError.        ShouldBe(true);
					result.Content[0].Text.ShouldContain("maximum response size of 512 bytes");
					result.Content[0].Text.ShouldContain("filterSchemas, filterCatalogs, or filterTables");
					result.Content[0].Text.ShouldNotContain("\"tables\"");
				}

				server.ExpectNoStandardError();
			}
			finally
			{
				File.Delete(database);
			}
		}

	}
}
