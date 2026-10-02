using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.Data;

using NUnit.Framework;

using Shouldly;

#pragma warning disable JSON002 // Allow JSON in test code for config file content.

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Pins the Windows impersonation boundary: configuration, provider loading, SQL validation and code
	/// loading run under the original process account; only connection opening and SQL execution run in
	/// the impersonation scope. The scope is the test environment's recorder, so these run on any OS.
	/// </summary>
	[TestFixture]
	public sealed class ImpersonationScopeTests
	{
		const string ScriptDomAssembly = "Microsoft.SqlServer.TransactSql.ScriptDom";

		// Nothing listens on port 1, so the connection attempt inside the scope fails fast.
		//
		const string UnreachableSqlServer = "Server=127.0.0.1,1;Database=master;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=2;ConnectRetryCount=0";

		[Test]
		public async Task QueryRejectsWriteSqlBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SqlServer.2022", "--connection-string", UnreachableSqlServer, "--user", "DOMAIN\\user", "--password", "secret", "--impersonate", "--sql", "delete from Person");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.               ShouldBe(-3);
				result.Error.                  ShouldContain("Query is not read-only: DeleteStatement is not allowed.");
				environment.ImpersonatedRuns.  ShouldBeEmpty();
			}
		}

		[Test]
		public async Task QueryRejectsMultipleStatementsBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--user", "user", "--password", "secret", "--impersonate", "--sql", "select 1; select 2");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.             ShouldBe(-3);
				result.Error.                ShouldContain("Only single SQL statement is allowed.");
				environment.ImpersonatedRuns.ShouldBeEmpty();
			}
		}

		[Test]
		public async Task ExecuteRejectsMultipleStatementsBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"user": "user",
						"password": "secret",
						"impersonate": true,
						"enableExecute": true
					}
				}
				""");

			var result = await RunCli(environment, "execute", "--config", config, "--sql", "select 1; drop table Person");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.             ShouldBe(-3);
				result.Error.                ShouldContain("Only single SQL statement is allowed.");
				environment.ImpersonatedRuns.ShouldBeEmpty();
			}
		}

		[Test]
		public async Task QueryValidatesSqlServerSqlBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SqlServer.2022", "--connection-string", UnreachableSqlServer, "--user", "DOMAIN\\user", "--password", "secret", "--impersonate", "--impersonate-mode", "new-credentials", "--sql", "select 1 as Value");

			var run = environment.ImpersonatedRuns.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				// The connection is the only work inside the scope, and it cannot be opened here.
				//
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("SQL execution failed:");

				run.User.        ShouldBe("DOMAIN\\user");
				run.Password.    ShouldBe("secret");
				run.Mode.        ShouldBe(WindowsImpersonationMode.NewCredentials);
				run.LoadedAtEntry.ShouldContain(ScriptDomAssembly);
				run.LoadedInside. ShouldNotContain(ScriptDomAssembly);
			}
		}

		[Test]
		public async Task QueryLoadsReferencedAssembliesBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "user", "--password", "secret", "--impersonate", "--output", "json", "--sql", "select Id, Name from Person");

				var run = environment.ImpersonatedRuns.ShouldHaveSingleItem();

				var referencedAssemblies = typeof(LinqToDBCliController).Assembly
					.GetReferencedAssemblies()
					.Select(static a => a.Name!)
					.ToHashSet(StringComparer.Ordinal);

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldBe("[{\"Id\":\"1\",\"Name\":\"original\"}]");

					run.Mode.ShouldBe(WindowsImpersonationMode.NetworkCleartext);
					run.LoadedInside.Where(referencedAssemblies.Contains).ShouldBeEmpty();
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task ExecuteWritesNoticeBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var config = AddConfigFile(environment, """
					{
						"default": {
							"provider": "SQLite",
							"connectionString": "Data Source=%DATABASE%;Pooling=False",
							"user": "user",
							"password": "secret",
							"impersonate": true,
							"enableExecute": true
						}
					}
					""".Replace("%DATABASE%", database.Replace("\\", "\\\\", StringComparison.Ordinal), StringComparison.Ordinal));

				var result = await RunCli(environment, "execute", "--config", config, "--output", "json-table", "--sql", "update Person set Name = 'updated' where Id = 1");

				var run = environment.ImpersonatedRuns.ShouldHaveSingleItem();

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldContain("\"recordsAffected\":1");

					run.ErrorOutputAtEntry.ShouldContain("Executing write-capable SQL because profile 'default' has enableExecute=true. Provider: SQLite.");
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task SchemaReadsDatabaseInsideImpersonation()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var result = await RunCli(environment, "schema", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "user", "--password", "secret", "--impersonate", "--impersonate-mode", "interactive");

				var run = environment.ImpersonatedRuns.ShouldHaveSingleItem();

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldContain("\"Person\"");

					run.Mode.ShouldBe(WindowsImpersonationMode.Interactive);
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task QueryWithoutImpersonateDoesNotEnterScope()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--user", "user", "--password", "secret", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.             ShouldBe(0, result.Error);
				environment.ImpersonatedRuns.ShouldBeEmpty();
			}
		}

		private static string CreateSqliteDatabase()
		{
			var fileName     = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"impersonation-{Guid.NewGuid():N}.db");
			var dataProvider = DataConnection.GetDataProvider("SQLite", $"Data Source={fileName};Pooling=False")!;
			using var db     = new DataConnection(new DataOptions().UseConnectionString(dataProvider, $"Data Source={fileName};Pooling=False"));

			db.Execute("""
				create table Person
				(
					Id   integer not null primary key,
					Name text    not null
				)
				""");

			db.Execute("insert into Person (Id, Name) values (1, 'original')");

			return fileName;
		}

		private static string AddConfigFile(TestCliEnvironment environment, string content)
		{
			var fileName = $"impersonation-{environment.Files.Count + 1}.json";

			environment.Files.Add(fileName, content);

			return fileName;
		}

		private static async Task<CliResult> RunCli(TestCliEnvironment environment, params string[] arguments)
		{
			var exitCode = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);
	}
}
