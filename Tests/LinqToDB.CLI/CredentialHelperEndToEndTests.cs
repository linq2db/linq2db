using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The reference helper (<c>secrethelper.cs</c>, run as <c>dotnet run secrethelper.cs -- &lt;verb&gt;</c>) through the
	/// real client: credentials management, every command that resolves <c>credentials</c> targets (query, execute, schema,
	/// the MCP query and execute tools), configuration parsing, and the default store on operating systems without one.
	/// </summary>
	[TestFixture]
	public sealed class CredentialHelperEndToEndTests : McpTestBase
	{
		const string HelperConfig = "helper.json";

		string _directory  = null!;
		string _storeFile  = null!;
		string _helperJson = null!;

		[SetUp]
		public void SetUp()
		{
			_helperJson = CredentialHelperTestSupport.SecretHelperConfigJson();
			_directory  = CredentialHelperTestSupport.CreateTempDirectory();
			_storeFile  = Path.Combine(_directory, "secrets.json");
		}

		[TearDown]
		public void TearDown()
		{
			CredentialHelperTestSupport.DeleteDirectory(_directory);
		}

		/// <summary>
		/// A test environment whose helper runs use this test's store file, with <see cref="HelperConfig"/> configuring the
		/// reference helper (<c>dotnet run</c>) in its default profile.
		/// </summary>
		TestCliEnvironment CreateEnvironment()
		{
			var environment = new TestCliEnvironment();
			environment.HelperEnvironment["SECRETHELPER_FILE"] = _storeFile;
			environment.Files.Add(HelperConfig, $$"""{ "default": { "credentialHelper": {{_helperJson}} } }""");
			return environment;
		}

		/// <summary>A wrapper script running the reference helper on this test's store (for --credential-helper and out-of-process runs).</summary>
		string CreateWrapper()
		{
			return CredentialHelperTestSupport.WriteSecretHelperWrapper(_directory, _storeFile);
		}

		async Task Seed(string profile, string user, string password)
		{
			var environment = CreateEnvironment();
			environment.Secrets.Enqueue(password);
			environment.Secrets.Enqueue(password);

			var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--config", HelperConfig, "--profile", profile, "--user", user);

			exitCode.ShouldBe(0, error);
		}

		static async Task<(int ExitCode, string Output, string Error)> RunCli(TestCliEnvironment environment, params string[] args)
		{
			var exitCode = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);
			return (exitCode, environment.Output, environment.ErrorOutput);
		}

		static string JsonEscape(string value)
		{
			return value.Replace("\\", "\\\\", StringComparison.Ordinal);
		}

		[Test]
		public async Task CredentialsRoundTripThroughReferenceHelper()
		{
			var environment = CreateEnvironment();
			environment.Secrets.Enqueue(" s3cret = é ");
			environment.Secrets.Enqueue(" s3cret = é ");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "set", "--config", HelperConfig, "--profile", "Project-A/Read", "--user", "DOMAIN\\reader");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Stored credential profile 'Project-A/Read' as target 'linq2db/project-a/read'.");
			error. ShouldContain("Using credential helper '");
			error. ShouldContain(" run ");

			(exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "list", "--config", HelperConfig);

			exitCode.ShouldBe(0, error);
			output.ShouldContain("project-a/read");
			output.ShouldContain("DOMAIN\\reader");
			output.ShouldNotContain("s3cret");

			(exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "remove", "--config", HelperConfig, "--profile", "project-a/READ");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Removed credential profile 'project-a/READ'.");

			(exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "remove", "--config", HelperConfig, "--profile", "project-a/read");

			exitCode.ShouldBe(-3);
			error.ShouldContain("Credential profile 'project-a/read' was not found.");
		}

		[Test]
		public void ReferenceHelperConformance()
		{
			var source = CredentialHelperTestSupport.GetSecretHelperSource();
			var runner = new CredentialHelperProcessRunner(
				new CredentialHelperSettings("dotnet", CredentialHelperProtocol.Linq2Db, null)
				{
					Arguments = ["run", source, "--"],
					Timeout   = TimeSpan.FromSeconds(CredentialHelperTestSupport.SecretHelperTimeoutSeconds),
				},
				false,
				new System.Collections.Generic.Dictionary<string, string?>(StringComparer.Ordinal) { ["SECRETHELPER_FILE"] = _storeFile });
			var store = new HelperCredentialStore(runner, CredentialHelperProtocol.Linq2Db);

			const string user     = " DOMAIN\\o'neil \"x\" ";
			const string password = "  p'a\"s$HOME`id`$(id)=é  ";

			store.TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credential helper 'dotnet run ");

			store.TryStore("A", user, password, out error).ShouldBeTrue(error);
			store.TryStore("b", "other", string.Empty, out error).ShouldBeTrue(error);

			store.TryRead("LINQ2DB/a", out var readUser, out var readPassword, out error).ShouldBeTrue(error);
			readUser.    ShouldBe(user);
			readPassword.ShouldBe(password);

			store.TryRead("linq2db/b", out readUser, out readPassword, out error).ShouldBeTrue(error);
			readUser.    ShouldBe("other");
			readPassword.ShouldBe(string.Empty);

			store.TryList(out var profiles, out var diagnostics, out error).ShouldBeTrue(error);
			profiles.   ShouldBe([new CredentialProfile("a", user), new CredentialProfile("b", "other")]);
			diagnostics.ShouldBeEmpty();

			store.TryStore("a", "replaced", "p2", out error).ShouldBeTrue(error);
			store.TryRead("linq2db/a", out readUser, out readPassword, out error).ShouldBeTrue(error);
			readUser.    ShouldBe("replaced");
			readPassword.ShouldBe("p2");

			store.TryRemove("a", out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeTrue();
			store.TryRemove("a", out removed, out error).ShouldBeTrue(error);
			removed.ShouldBeFalse();

			store.TryClear(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(1);

			System.Text.Encoding.UTF8.GetString(runner.Run("rename", CredentialHelperTestSupport.Request("protocol=1")).Output).ShouldBe("unsupported=verb\n");
			System.Text.Encoding.UTF8.GetString(runner.Run("get",    CredentialHelperTestSupport.Request("protocol=2")).Output).ShouldBe("unsupported=protocol\n");
		}

		[Test]
		public async Task CredentialsClearThroughCommandLineHelper()
		{
			await Seed("a", "ua", "pa");
			await Seed("b", "ub", "pb");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "clear", "--credential-helper", CreateWrapper(), "--force");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Removed 2 linq2db credential profile(s).");
			File.ReadAllText(_storeFile).ShouldNotContain("linq2db/");
		}

		[Test]
		public async Task CredentialsListThroughCommandLineHelper()
		{
			await Seed("cfg", "reader", "secret");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "list", "--credential-helper", CreateWrapper());

			exitCode.ShouldBe(0, error);
			output.ShouldContain("cfg");
			output.ShouldNotContain("secret");
		}

		[Test]
		public async Task CredentialsConfigWithoutHelperFails()
		{
			var environment = CreateEnvironment();
			environment.Files.Add("no-helper.json", """{ "default": { "provider": "SQLite" } }""");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "list", "--config", "no-helper.json");

			exitCode.ShouldBe(-1);
			error.ShouldContain("Configuration file 'no-helper.json' profile 'default' doesn't set 'credentialHelper'.");
		}

		[Test]
		public async Task CredentialsHelperAndConfigCannotBeCombined()
		{
			var (exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "list", "--config", HelperConfig, "--credential-helper", "h");

			exitCode.ShouldBe(-1);
			error.ShouldContain("Options '--credential-helper' and '--config' cannot be combined.");
		}

		[Test]
		public async Task QueryReadsCredentialsThroughCommandLineHelper()
		{
			await Seed("memory", ":memory:", "ignored");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(),
				"query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/Memory", "--credential-helper", CreateWrapper(), "--sql", "select 1 as Value");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task QueryReadsCredentialsThroughDotnetRunHelperInheritedFromDefault()
		{
			await Seed("memory", ":memory:", "ignored");

			var environment = CreateEnvironment();
			environment.Files.Add("query.json", $$"""
				{
					"default": {
						"credentialHelper": {{_helperJson}}
					},
					"dev": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory"
					}
				}
				""");

			var (exitCode, output, error) = await RunCli(environment, "query", "--config", "query.json", "--profile", "dev", "--sql", "select 1 as Value");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task QueryCommandLineHelperOverridesConfiguredHelper()
		{
			await Seed("memory", ":memory:", "ignored");

			var environment = CreateEnvironment();
			environment.Files.Add("query.json", """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialHelper": "/nonexistent/helper"
					}
				}
				""");

			var (exitCode, output, error) = await RunCli(environment, "query", "--config", "query.json", "--credential-helper", CreateWrapper(), "--sql", "select 1 as Value");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task QueryWithDockerProtocolUsesDockerAdapter()
		{
			var environment = CreateEnvironment();
			environment.Files.Add("query.json", $$"""
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialHelper": { "command": "dotnet", "args": ["run", "{{JsonEscape(CredentialHelperTestSupport.GetSecretHelperSource())}}", "--"], "protocol": "docker", "timeout": 300 }
					}
				}
				""");

			var (exitCode, _, error) = await RunCli(environment, "query", "--config", "query.json", "--sql", "select 1 as Value");

			// The reference helper speaks protocol 1, so a Docker-style request (no protocol line) is answered with
			// "unsupported=protocol", which the Docker adapter cannot parse as JSON.
			exitCode.ShouldNotBe(0);
			error.ShouldContain("returned an invalid answer to 'get': expected a JSON object with 'Username' and 'Secret'.");
		}

		[Test]
		public async Task QueryReportsTargetNotFoundByHelper()
		{
			var wrapper = CreateWrapper();

			var (exitCode, _, error) = await RunCli(CreateEnvironment(),
				"query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/missing", "--credential-helper", wrapper, "--sql", "select 1");

			exitCode.ShouldBe(-1);
			error.ShouldContain($"Credential target 'linq2db/missing' was not found by credential helper '{wrapper}'.");
		}

		[Test]
		public async Task ExecuteReadsCredentialsThroughHelper()
		{
			await Seed("memory", ":memory:", "ignored");

			var environment = CreateEnvironment();
			environment.Files.Add("execute.json", $$"""
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialHelper": {{_helperJson}},
						"enableExecute": true
					}
				}
				""");

			var (exitCode, _, error) = await RunCli(environment, "execute", "--config", "execute.json", "--sql", "create table T (Id int)");

			exitCode.ShouldBe(0, error);
		}

		[Test]
		public async Task SchemaReadsCredentialsThroughHelper()
		{
			await Seed("memory", ":memory:", "ignored");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(),
				"schema", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/memory", "--credential-helper", CreateWrapper(), "--detail-level", "names");

			exitCode.ShouldBe(0, error);
			output.ShouldStartWith("{");
		}

		[TestCase("\"credentialHelper\": 1",                                              "must be string or object",                    TestName = "ConfigHelperWrongType")]
		[TestCase("\"credentialHelper\": \"\"",                                           "must specify a non-empty command",            TestName = "ConfigHelperEmpty")]
		[TestCase("\"credentialHelper\": { \"protocol\": \"docker\" }",                   "must specify a non-empty command",            TestName = "ConfigHelperMissingCommand")]
		[TestCase("\"credentialHelper\": { \"command\": \"h\", \"protocol\": \"git\" }",  "has unknown value 'git'",                     TestName = "ConfigHelperUnknownProtocol")]
		[TestCase("\"credentialHelper\": { \"command\": \"h\", \"env\": {} }",            "contains unknown property 'env'",             TestName = "ConfigHelperUnknownProperty")]
		[TestCase("\"credentialHelper\": { \"command\": 1 }",                             "'credentialHelper.command' must be string",   TestName = "ConfigHelperCommandWrongType")]
		[TestCase("\"credentialHelper\": { \"command\": \"h\", \"args\": \"run\" }",      "'credentialHelper.args' must be an array of strings", TestName = "ConfigHelperArgsNotArray")]
		[TestCase("\"credentialHelper\": { \"command\": \"h\", \"args\": [1] }",          "'credentialHelper.args' must be an array of strings", TestName = "ConfigHelperArgsNotStrings")]
		[TestCase("\"credentialHelper\": { \"command\": \"h\", \"timeout\": 0 }",         "'credentialHelper.timeout' must be a positive integer", TestName = "ConfigHelperTimeoutZero")]
		[TestCase("\"credentialHelper\": { \"command\": \"h\", \"timeout\": \"10\" }",    "'credentialHelper.timeout' must be a positive integer", TestName = "ConfigHelperTimeoutString")]
		public async Task ConfigHelperValidation(string property, string message)
		{
			var environment = CreateEnvironment();
			environment.Files.Add("bad.json", $$"""{ "default": { "provider": "SQLite", "connectionString": "Data Source=:memory:", {{property}} } }""");

			var (exitCode, _, error) = await RunCli(environment, "query", "--config", "bad.json", "--sql", "select 1");

			exitCode.ShouldBe(-1);
			error.ShouldContain(message);
		}

		[Test]
		public async Task OutOfProcessQueryReadsCredentialsThroughWrapperHelper()
		{
			await Seed("memory", ":memory:", "ignored");

			var result = await RunCliProcess(
				"query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/memory", "--credential-helper", CreateWrapper(), "--sql", "select 1 as Value");

			result.ExitCode.ShouldBe(0, result.Error);
			result.Output.  ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task OutOfProcessCredentialsWithoutHelperOffWindows()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Windows uses Credential Manager when no helper is configured.");

			var result = await RunCliProcess("credentials", "list");

			result.ExitCode.ShouldBe(-3);
			result.Error.ShouldContain("No credential store is configured.");
			result.Error.ShouldContain("credentialHelper");
			result.Error.ShouldContain("credentials helper init");
		}

		[Test]
		public async Task McpQueryAndExecuteToolsReadCredentialsThroughDotnetRunHelper()
		{
			await Seed("memory", ":memory:", "ignored");

			var config = Path.Combine(_directory, "mcp.json");

			await File.WriteAllTextAsync(config, $$"""
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialHelper": {{_helperJson}},
						"enableExecute": true
					}
				}
				""").ConfigureAwait(false);

			// The server, and the helper it starts with "dotnet run", inherit the store location. Tests run sequentially.
			var previous = Environment.GetEnvironmentVariable("SECRETHELPER_FILE");
			Environment.SetEnvironmentVariable("SECRETHELPER_FILE", _storeFile);

			try
			{
				await using var server = await McpServerProcess.Start("--config", config, "--enable-execute-tool");

				await server.Initialize();

				var query = ReadToolResult<McpTestJsonTableResult>(await server.CallTool("linq2db_query", new JsonObject { ["sql"] = "select 1 as Value" }));

				query.Rows.ShouldBe([["1"]]);

				var execute = await server.CallTool("linq2db_execute", new JsonObject { ["sql"] = "create table T (Id int)" });

				ReadToolText(execute).ShouldNotContain("Credential");
			}
			finally
			{
				Environment.SetEnvironmentVariable("SECRETHELPER_FILE", previous);
			}
		}

		[Test]
		public async Task McpServerHelperFromCommandLine()
		{
			await Seed("memory", ":memory:", "ignored");

			await using var server = await McpServerProcess.Start(
				"--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/memory", "--credential-helper", CreateWrapper());

			await server.Initialize();

			var query = ReadToolResult<McpTestJsonTableResult>(await server.CallTool("linq2db_query", new JsonObject { ["sql"] = "select 1 as Value" }));

			query.Rows.ShouldBe([["1"]]);
		}
	}
}
