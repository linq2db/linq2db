using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

using NUnit.Framework;

using Shouldly;

#nullable enable annotations
#nullable disable warnings

#pragma warning disable JSON002 // Allow JSON in test code for config file content.

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class ConfigInitCommandTests
	{
		[Test]
		public async Task ConfigInitCreatesDefaultConfig()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "config-init", "--provider", "SQLite", "--connection-string", "Data Source=data.db");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();
				(result.Output).ShouldContain("Created configuration profile 'default' in '.agents/linq2db-query.json'.");
				environment.Directories.ShouldContain(".agents");

				using var json = JsonDocument.Parse(environment.Files[".agents/linq2db-query.json"]);
				var profile = json.RootElement.GetProperty("default");
				profile.GetProperty("provider").GetString().ShouldBe("SQLite");
				profile.GetProperty("connectionString").GetString().ShouldBe("Data Source=data.db");
				profile.GetProperty("maxRows").GetInt32().ShouldBe(1000);
				profile.GetProperty("output").GetString().ShouldBe("json-table");
				profile.GetProperty("enableExecute").GetBoolean().ShouldBe(false);
			}
		}

		[Test]
		public async Task ConfigInitCreatesNamedProfileWithDefaultProfile()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(
				environment,
				"config-init",
				"--config",
				"query.json",
				"--profile",
				"dev",
				"--description",
				"Development database",
				"--provider",
				"SqlServer",
				"--provider-location",
				"providers\\custom.dll",
				"--connection-string-env",
				"LINQ2DB_DEV_CONNECTION",
				"--max-rows",
				"42",
				"--output",
				"csv");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();

				using var json = JsonDocument.Parse(environment.Files["query.json"]);
				var defaultProfile = json.RootElement.GetProperty("default");
				defaultProfile.GetProperty("maxRows").GetInt32().ShouldBe(1000);
				defaultProfile.GetProperty("output").GetString().ShouldBe("json-table");
				defaultProfile.GetProperty("enableExecute").GetBoolean().ShouldBe(false);

				var profile = json.RootElement.GetProperty("dev");
				profile.GetProperty("description").GetString().ShouldBe("Development database");
				profile.GetProperty("provider").GetString().ShouldBe("SqlServer");
				profile.GetProperty("providerLocation").GetString().ShouldBe("providers\\custom.dll");
				profile.GetProperty("connectionStringEnv").GetString().ShouldBe("LINQ2DB_DEV_CONNECTION");
				profile.GetProperty("maxRows").GetInt32().ShouldBe(42);
				profile.GetProperty("output").GetString().ShouldBe("csv");
				profile.GetProperty("enableExecute").GetBoolean().ShouldBe(false);
			}
		}

		[Test]
		public async Task ConfigInitAddsProfileToExistingConfig()
		{
			var environment = new TestCliEnvironment();
			environment.Files["query.json"] = """
				{
				  "default": {
				    "provider": "SQLite",
				    "connectionString": "Data Source=default.db"
				  },
				  "old": {
				    "provider": "SQLite",
				    "connectionString": "Data Source=old.db"
				  }
				}
				""";

			var result = await RunCli(environment, "config-init", "--config", "query.json", "--profile", "dev", "--provider", "SQLite", "--connection-string", "Data Source=dev.db");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();

				using var json = JsonDocument.Parse(environment.Files["query.json"]);
				json.RootElement.GetProperty("old").GetProperty("connectionString").GetString().ShouldBe("Data Source=old.db");
				json.RootElement.GetProperty("dev").GetProperty("connectionString").GetString().ShouldBe("Data Source=dev.db");
			}
		}

		[Test]
		public async Task ConfigInitRejectsExistingProfileByDefault()
		{
			var environment = new TestCliEnvironment();
			var original = """
				{
				  "default": {
				    "provider": "SQLite",
				    "connectionString": "Data Source=old.db"
				  }
				}
				""";
			environment.Files["query.json"] = original;

			var result = await RunCli(environment, "config-init", "--config", "query.json", "--provider", "SQLite", "--connection-string", "Data Source=new.db");

			{
				(result.ExitCode).ShouldBe(-3);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Configuration profile 'default' already exists in 'query.json'.");
				environment.Files["query.json"].ShouldBe(original);
			}
		}

		[Test]
		public async Task ConfigInitSkipsExistingProfile()
		{
			var environment = new TestCliEnvironment();
			var original = """
				{
				  "dev": {
				    "provider": "SQLite",
				    "connectionString": "Data Source=old.db"
				  }
				}
				""";
			environment.Files["query.json"] = original;

			var result = await RunCli(environment, "config-init", "--config", "query.json", "--profile", "dev", "--provider", "SQLite", "--connection-string", "Data Source=new.db", "--if-exists", "skip");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();
				(result.Output).ShouldContain("Configuration profile 'dev' already exists in 'query.json'. Skipped.");
				environment.Files["query.json"].ShouldBe(original);
			}
		}

		[Test]
		public async Task ConfigInitReplacesExistingProfile()
		{
			var environment = new TestCliEnvironment();
			environment.Files["query.json"] = """
				{
				  "default": {
				    "provider": "SQLite",
				    "connectionString": "Data Source=old.db"
				  }
				}
				""";

			var result = await RunCli(environment, "config-init", "--config", "query.json", "--provider", "PostgreSQL", "--connection-string-env", "LINQ2DB_CONNECTION", "--if-exists", "replace");

			{
				(result.ExitCode).ShouldBe(0);
				(result.Error).ShouldBeEmpty();
				(result.Output).ShouldContain("Updated configuration profile 'default' in 'query.json'.");

				using var json = JsonDocument.Parse(environment.Files["query.json"]);
				var profile = json.RootElement.GetProperty("default");
				profile.GetProperty("provider").GetString().ShouldBe("PostgreSQL");
				profile.GetProperty("connectionStringEnv").GetString().ShouldBe("LINQ2DB_CONNECTION");
				profile.TryGetProperty("connectionString", out _).ShouldBeFalse();
			}
		}

		[Test]
		public async Task ConfigInitRequiresProvider()
		{
			var result = await RunCli(new TestCliEnvironment(), "config-init", "--connection-string", "Data Source=data.db");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Option '--provider' must be specified.");
			}
		}

		[Test]
		public async Task ConfigInitRequiresExactlyOneConnectionStringSource()
		{
			var noConnection = await RunCli(new TestCliEnvironment(), "config-init", "--provider", "SQLite");
			var twoConnections = await RunCli(new TestCliEnvironment(), "config-init", "--provider", "SQLite", "--connection-string", "Data Source=data.db", "--connection-string-env", "LINQ2DB_CONNECTION");

			{
				(noConnection.ExitCode).ShouldBe(-1);
				(noConnection.Error).ShouldContain("Exactly one of '--connection-string' or '--connection-string-env' must be specified.");
				(twoConnections.ExitCode).ShouldBe(-1);
				(twoConnections.Error).ShouldContain("Exactly one of '--connection-string' or '--connection-string-env' must be specified.");
			}
		}

		[Test]
		public async Task ConfigInitRejectsNegativeMaxRows()
		{
			var result = await RunCli(new TestCliEnvironment(), "config-init", "--provider", "SQLite", "--connection-string", "Data Source=data.db", "--max-rows", "-1");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Option '--max-rows' must be a non-negative integer row count.");
			}
		}

		[Test]
		public async Task ConfigInitRejectsInvalidJson()
		{
			var environment = new TestCliEnvironment();
			environment.Files["query.json"] = "{";

			var result = await RunCli(environment, "config-init", "--config", "query.json", "--provider", "SQLite", "--connection-string", "Data Source=data.db");

			{
				(result.ExitCode).ShouldBe(-3);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Configuration file 'query.json' is not valid JSON:");
				environment.Files["query.json"].ShouldBe("{");
			}
		}

		[Test]
		public async Task ConfigInitRejectsInvalidOutput()
		{
			var result = await RunCli(new TestCliEnvironment(), "config-init", "--provider", "SQLite", "--connection-string", "Data Source=data.db", "--output", "xml");

			{
				(result.ExitCode).ShouldBe(-1);
				(result.Output).ShouldBeEmpty();
				(result.Error).ShouldContain("Cannot parse option value (--output xml): unknown value 'xml'");
			}
		}

		static async Task<CliResult> RunCli(TestCliEnvironment environment, params string[] arguments)
		{
			var exitCode = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		sealed record CliResult(int ExitCode, string Output, string Error);

		sealed class TestCliEnvironment : ICliEnvironment
		{
			readonly StringWriter _output = new();
			readonly StringWriter _error  = new();

			public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);
			public HashSet<string> Directories { get; } = new(StringComparer.Ordinal);

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

			public TextWriter CreateTextWriter(string path)
			{
				return new StringWriter();
			}

			public void CreateDirectory(string path)
			{
				Directories.Add(path);
			}

			public string? GetEnvironmentVariable(string name)
			{
				return null;
			}
		}
	}
}
