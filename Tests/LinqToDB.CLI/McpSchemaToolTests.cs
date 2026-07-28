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

	}
}
