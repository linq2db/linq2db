using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class SchemaCommandTests : CliProcessTestBase
	{
		[Test]
		public async Task SchemaHelpShowsSchemaCommand()
		{
			var result = await RunCliProcess("help", "schema");

			result.ExitCode.ShouldBe(0);
			result.Output.  ShouldContain("dotnet linq2db schema <options>");
			result.Output.  ShouldContain("--get-foreign-keys");
			result.Output.  ShouldContain("--detail-level");
			result.Output.  ShouldContain("--filter-schema");
			result.Output.  ShouldContain("--filter-table");
			result.Output.  ShouldNotContain("--exclude-table");
			result.Output.  ShouldNotContain("--get-procedures");
			result.Output.  ShouldNotContain("--use-schema-only");
		}

		[Test]
		public async Task SchemaReturnsCompactObjectNames()
		{
			var database = CreateSqliteDatabase();

			try
			{
				var result = await RunCliProcess(
					"schema",
					"--provider", "SQLite",
					"--connection-string", $"Data Source={database};Pooling=False",
					"--detail-level", "names",
					"--filter-table", "main.Orders");

				result.ExitCode.ShouldBe(0);
				result.Error.   ShouldBeEmpty();

				var schema  = JsonNode.Parse(result.Output)!.AsObject();
				var objects = schema["objects"]!.AsArray();

				((string?)schema["options"]?["detailLevel"]). ShouldBe("names");
				((bool?)schema["options"]?["getForeignKeys"]).ShouldBe(false);
				objects.Count.                                ShouldBe(1);
				((string?)objects[0]?["name"]).                ShouldBe("Orders");
				((string?)objects[0]?["kind"]).                ShouldBe("table");
				schema["tables"].                              ShouldBeNull();
				result.Output.                                ShouldNotContain("\"columns\"");
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task SchemaRejectsQueryOnlyOptions()
		{
			var result = await RunCliProcess("schema", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			result.ExitCode.ShouldBe(-1);
			result.Error.   ShouldContain("Unrecognized option: --sql");
		}

		[Test]
		public async Task SchemaRejectsUnsupportedOutput()
		{
			var result = await RunCliProcess("schema", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv");

			result.ExitCode.ShouldBe(-1);
			result.Error.   ShouldContain("Cannot parse option value (--output csv)");
		}

		[Test]
		public async Task SchemaReturnsSqliteMetadata()
		{
			var database = CreateSqliteDatabase();

			try
			{
				var result = await RunCliProcess(
					"schema",
					"--provider", "SQLite",
					"--connection-string", $"Data Source={database};Pooling=False",
					"--get-foreign-keys", "true");

				result.ExitCode.ShouldBe(0);
				result.Error.   ShouldBeEmpty();

				var schema = JsonNode.Parse(result.Output)!.AsObject();

				((string?)schema["provider"]).               ShouldBe("SQLite");
				((string?)schema["dialect"]).                ShouldBe("SQLite");
				((bool?)schema["options"]?["getProcedures"]).ShouldBe(false);

				var orders    = FindTable(schema, "Orders");
				var customers = FindTable(schema, "Customers");

				orders["columns"]!.AsArray().Count.                               ShouldBe(3);
				((string?)orders["primaryKey"]?["columns"]?[0]?["name"]).         ShouldBe("Id");
				((string?)orders["foreignKeys"]?[0]?["name"]).                    ShouldBe("FK_Orders_0");
				((string?)orders["foreignKeys"]?[0]?["referencedTable"]?["name"]).ShouldBe("Customers");
				((string?)orders["foreignKeys"]?[0]?["columns"]?[0]).             ShouldBe("CustomerId");
				customers["foreignKeys"]!.AsArray().                              ShouldBeEmpty();
				result.Output.                                                    ShouldNotContain("secret");
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task SchemaFiltersTables()
		{
			var database = CreateSqliteDatabase();

			try
			{
				var result = await RunCliProcess(
					"schema",
					"--provider", "SQLite",
					"--connection-string", $"Data Source={database};Pooling=False",
					"--filter-table", "main.Orders,rx:^Child");

				result.ExitCode.ShouldBe(0);
				result.Error.   ShouldBeEmpty();

				var schema = JsonNode.Parse(result.Output)!.AsObject();

				((string?)schema["options"]?["filterTables"]?[0]).ShouldBe("main.Orders");
				((string?)schema["options"]?["filterTables"]?[1]).ShouldBe("rx:^Child");
				schema["tables"]!.AsArray().Count.                ShouldBe(2);
				ContainsTable(schema, "Orders").                  ShouldBe(true);
				ContainsTable(schema, "ChildOrders").             ShouldBe(true);
				ContainsTable(schema, "Customers").               ShouldBe(false);
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task SchemaRespectsGetForeignKeysFalse()
		{
			var database = CreateSqliteDatabase();

			try
			{
				var result = await RunCliProcess(
					"schema",
					"--provider", "SQLite",
					"--connection-string", $"Data Source={database};Pooling=False",
					"--get-foreign-keys", "false");

				result.ExitCode.ShouldBe(0);
				result.Error.   ShouldBeEmpty();

				var schema = JsonNode.Parse(result.Output)!.AsObject();
				var orders = FindTable(schema, "Orders");

				((bool?)schema["options"]?["getForeignKeys"]).ShouldBe(false);
				orders["foreignKeys"]!.AsArray().Count.       ShouldBe(0);
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task SchemaReportsRegexFilterTimeout()
		{
			var database = CreateSqliteDatabase();

			try
			{
				var result = await RunCliProcess(
					"schema",
					"--provider", "SQLite",
					"--connection-string", $"Data Source={database};Pooling=False",
					"--filter-table", "rx:^(a+)+$");

				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("Table filter regex '^(a+)+$' timed out");
			}
			finally
			{
				File.Delete(database);
			}
		}

		static bool ContainsTable(JsonObject schema, string name)
		{
			foreach (var table in schema["tables"]!.AsArray())
			{
				if ((string?)table?["name"] == name)
					return true;
			}

			return false;
		}

		static JsonObject FindTable(JsonObject schema, string name)
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
			return CreateCliSqliteDatabase("schema", seedCustomers: false);
		}
	}
}
