using System;
using System.Diagnostics;
using System.IO;
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
	public sealed class SchemaCommandTests
	{
		[Test]
		public async Task SchemaHelpShowsSchemaCommand()
		{
			var result = await RunCliProcess("help", "schema");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Output).ShouldContain("dotnet linq2db schema <options>");
				(result.Output).ShouldContain("--get-foreign-keys");
				(result.Output).ShouldContain("--filter-schema");
				(result.Output).ShouldContain("--filter-table");
				(result.Output).ShouldNotContain("--exclude-table");
				(result.Output).ShouldNotContain("--get-procedures");
				(result.Output).ShouldNotContain("--use-schema-only");
			}
		}

		[Test]
		public async Task SchemaRejectsQueryOnlyOptions()
		{
			var result = await RunCliProcess("schema", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Error).ShouldContain("Unrecognized option: --sql");
			}
		}

		[Test]
		public async Task SchemaRejectsUnsupportedOutput()
		{
			var result = await RunCliProcess("schema", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Error).ShouldContain("Cannot parse option value (--output csv)");
			}
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

				var schema = JsonNode.Parse(result.Output)!.AsObject();

				{
					(result.ExitCode).ShouldBe(0);
					(result.Error).ShouldBeEmpty();
					((string?)schema["provider"]).ShouldBe("SQLite");
					((string?)schema["dialect"]).ShouldBe("SQLite");
					((bool?)schema["options"]?["getProcedures"]).ShouldBe(false);

					var orders = FindTable(schema, "Orders");

					orders["columns"]!.AsArray().Count.ShouldBe(3);
					((string?)orders["primaryKey"]?["columns"]?[0]?["name"]).ShouldBe("Id");
					((string?)orders["foreignKeys"]?[0]?["referencedTable"]?["name"]).ShouldBe("Customers");
					((string?)orders["foreignKeys"]?[0]?["columns"]?[0]).ShouldBe("CustomerId");
					result.Output.ShouldNotContain("secret");
				}
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

				var schema = JsonNode.Parse(result.Output)!.AsObject();

				{
					(result.ExitCode).ShouldBe(0);
					((string?)schema["options"]?["filterTables"]?[0]).ShouldBe("main.Orders");
					((string?)schema["options"]?["filterTables"]?[1]).ShouldBe("rx:^Child");
					schema["tables"]!.AsArray().Count.ShouldBe(2);
					ContainsTable(schema, "Orders").ShouldBe(true);
					ContainsTable(schema, "ChildOrders").ShouldBe(true);
					ContainsTable(schema, "Customers").ShouldBe(false);
				}
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

				var schema = JsonNode.Parse(result.Output)!.AsObject();
				var orders = FindTable(schema, "Orders");

				{
					(result.ExitCode).ShouldBe(0);
					((bool?)schema["options"]?["getForeignKeys"]).ShouldBe(false);
					orders["foreignKeys"]!.AsArray().Count.ShouldBe(0);
				}
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
			var fileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"schema-{Guid.NewGuid():N}.db");

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

			return fileName;
		}

		static async Task<CliProcessResult> RunCliProcess(params string[] arguments)
		{
			var cliAssembly = Path.Combine(AppContext.BaseDirectory, "dotnet-linq2db.dll");

			if (!File.Exists(cliAssembly))
				throw new FileNotFoundException("Cannot find CLI assembly for schema command tests.", cliAssembly);

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
	}
}
