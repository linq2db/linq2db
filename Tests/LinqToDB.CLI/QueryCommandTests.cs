using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.Data;

using NUnit.Framework;

#pragma warning disable JSON002 // Allow JSON in test code for config file content.

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class QueryCommandTests
	{
		[Test]
		public async Task QueryRequiresSqlOrSqlFile()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Either '--sql' or '--sql-file' option must be specified."));
			}
		}

		[Test]
		public async Task QueryRejectsSqlAndSqlFileTogether()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--sql-file' conflicts with other option(s): --sql"));
			}
		}

		[Test]
		public async Task QueryRequiresProvider()
		{
			var result = await RunCli("query", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--provider' must be specified."));
			}
		}

		[Test]
		public async Task QueryRequiresConnectionString()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--connection-string' must be specified."));
			}
		}

		[Test]
		public async Task QueryAcceptsSql()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\": 1"));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsDml()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "update Person set Name = 'test'");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query is not read-only: token 'UPDATE' is not allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsDdl()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query is not read-only: token 'DROP' is not allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsProcedureCalls()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "call DoWork()");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query is not read-only: token 'CALL' is not allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsMultipleStatements()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1; select 2");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Only single SQL statement is allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsMultipleStatementsEvenWhenUnsafeSqlAllowed()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "allow"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1; drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Only single SQL statement is allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsUnsafeSqlConfirmationWhenSafetyPolicyDenies()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--allow-unsafe-sql", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--allow-unsafe-sql' cannot be used because SQL safety policy is 'deny'."));
			}
		}

		[Test]
		public async Task QueryConfirmSafetyPolicyRequiresUnsafeSqlConfirmation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "confirm"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Unsafe SQL requires '--allow-unsafe-sql'"));
			}
		}

		[Test]
		public async Task QueryConfirmSafetyPolicyAllowsUnsafeSqlWithConfirmation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "confirm"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--allow-unsafe-sql", "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query execution failed"));
				Assert.That(result.Error,    Does.Not.Contain("token 'DROP' is not allowed"));
			}
		}

		[Test]
		public async Task QueryAllowSafetyPolicyAllowsUnsafeSqlWithoutConfirmation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "allow"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query execution failed"));
				Assert.That(result.Error,    Does.Not.Contain("token 'DROP' is not allowed"));
			}
		}

		[Test]
		public async Task QueryIgnoresForbiddenTokensInStringsAndComments()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 'drop table' as Value -- update table");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\": \"drop table\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public void QuerySafetyAllowsSqlServerSelect()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = QuerySafetyValidator.Validate(provider, "select 1 as Value");

			Assert.That(result.IsSafe, Is.True);
		}

		[Test]
		public void QuerySafetyRejectsSqlServerDml()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = QuerySafetyValidator.Validate(provider, "update dbo.Person set Name = 'test'");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.IsSafe, Is.False);
				Assert.That(result.Error,  Does.Contain("UpdateStatement"));
			}
		}

		[Test]
		public void QuerySafetyRejectsSqlServerExecute()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = QuerySafetyValidator.Validate(provider, "exec dbo.DoWork");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.IsSafe, Is.False);
				Assert.That(result.Error,  Does.Contain("EXECUTE is not allowed"));
			}
		}

		[Test]
		public void QuerySafetyRejectsSqlServerSelectInto()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = QuerySafetyValidator.Validate(provider, "select * into dbo.NewPerson from dbo.Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.IsSafe, Is.False);
				Assert.That(result.Error,  Does.Contain("SELECT INTO is not allowed"));
			}
		}

		[Test]
		public void QueryHonorsCancellationToken()
		{
			using var cancellation = new CancellationTokenSource();

			cancellation.Cancel();

			Assert.ThrowsAsync<OperationCanceledException>(async () =>
				await new LinqToDBCliController()
					.Execute(
						["query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1"],
						new TestCliEnvironment(),
						cancellation.Token)
					.AsTask()
					.ConfigureAwait(false));
		}

		[Test]
		public async Task QueryAcceptsSqlFile()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("query.sql", "select 'test' as Value");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\": \"test\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsMissingSqlFile()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("SQL file 'query.sql' not found."));
			}
		}

		[Test]
		public async Task QueryRejectsProfileWithoutConfig()
		{
			var result = await RunCli("query", "--profile", "uat", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--profile' requires option '--config'."));
			}
		}

		[Test]
		public async Task QueryAcceptsOutputOptions()
		{
			var environment = new TestCliEnvironment();
			var result      = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv", "--output-file", "query.csv", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["query.csv"], Is.EqualTo($"Value{Environment.NewLine}1{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryAcceptsDefaultConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "csv",
						"outputFile": "query.csv"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["query.csv"], Is.EqualTo($"1{Environment.NewLine}1{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryAcceptsNamedConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					},
					"uat": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			environment.Files.Add("query.sql", "select 2 as Value");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\": 2"));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryUsesDefaultValuesForMissingProfileValues()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "csv",
						"outputFile": "default.csv"
					},
					"uat": {
						"output": "json"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["default.csv"], Does.Contain("\"1\": 1"));
			}
		}

		[Test]
		public async Task QueryCliOptionsOverrideConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=default;",
						"output": "csv",
						"outputFile": "default.csv"
					},
					"uat": {
						"connectionString": "Data Source=uat;"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--connection-string", "Data Source=:memory:", "--output", "json", "--output-file", "cli.json", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["cli.json"], Does.Contain("\"Value\": 1"));
			}
		}

		[Test]
		public async Task QueryFormatsConnectionStringWithCliCredentials()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--user", ":memory:", "--password", "ignored", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\": 1"));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryFormatsConnectionStringWithConfigCredentials()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"user": ":memory:",
						"password": "ignored"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\": 1"));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryStillRequiresSqlOrSqlFileWithConfig()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Either '--sql' or '--sql-file' option must be specified."));
			}
		}

		[Test]
		public async Task QueryRejectsUnknownConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' doesn't contain 'uat' profile."));
			}
		}

		[Test]
		public async Task QueryRejectsSqlInConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"sql": "select 1"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' contains unknown property 'sql'."));
			}
		}

		[Test]
		public async Task QueryRejectsUnknownConfigOutput()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "xml"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'output' has unknown value 'xml'."));
			}
		}

		[Test]
		public async Task QueryRejectsUnknownConfigUnsafeSql()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "prompt"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'unsafeSql' has unknown value 'prompt'."));
			}
		}

		[Test]
		public async Task QueryHelpShowsSqlInputOptions()
		{
			var result = await RunCli("help", "query");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query [--config config] [--profile profile] [--provider provider] [--connection-string connection-string] [--user user] [--password password] [--allow-unsafe-sql] [--output output] [--output-file output-file] [--sql sql | --sql-file file]"));
				Assert.That(result.Output,   Does.Contain("--config"));
				Assert.That(result.Output,   Does.Contain("--profile"));
				Assert.That(result.Output,   Does.Contain("--provider"));
				Assert.That(result.Output,   Does.Contain("--connection-string"));
				Assert.That(result.Output,   Does.Contain("--user"));
				Assert.That(result.Output,   Does.Contain("--password"));
				Assert.That(result.Output,   Does.Contain("--allow-unsafe-sql"));
				Assert.That(result.Output,   Does.Contain("ask the user before using this option"));
				Assert.That(result.Output,   Does.Contain("agents can analyze code together with live database data"));
				Assert.That(result.Output,   Does.Contain("--output"));
				Assert.That(result.Output,   Does.Contain("--output-file"));
				Assert.That(result.Output,   Does.Contain("--sql"));
				Assert.That(result.Output,   Does.Contain("--sql-file"));
				Assert.That(result.Output,   Does.Contain("Examples:"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql \"select * from Person\""));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql-file query.sql"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --config query.json --profile uat --user readonly --password secret --sql-file query.sql"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --output csv --output-file result.csv --sql \"select * from Person\""));
			}
		}

		private static string AddConfigFile(TestCliEnvironment environment, string content)
		{
			var fileName = $"query-{environment.Files.Count + 1}.json";

			environment.Files.Add(fileName, content);

			return fileName;
		}

		private static async Task<CliResult> RunCli(params string[] arguments)
		{
			return await RunCli(new TestCliEnvironment(), arguments).ConfigureAwait(false);
		}

		private static async Task<CliResult> RunCli(TestCliEnvironment environment, params string[] arguments)
		{
			var exitCode = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);

		private sealed class TestCliEnvironment : ICliEnvironment
		{
			private readonly StringWriter _output = new();
			private readonly StringWriter _error  = new();

			public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

			public TextWriter Out   => _output;
			public TextWriter Error => _error;

			public int BufferWidth => 120;

			public string Output      => _output.ToString();
			public string ErrorOutput => _error .ToString();

			public bool FileExists(string path)
			{
				return Files.ContainsKey(path);
			}

			public string ReadAllText(string path)
			{
				return Files[path];
			}

			public void WriteAllText(string path, string contents)
			{
				Files[path] = contents;
			}
		}
	}
}
