using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The example credentials CLI (<c>secrethelper.cs</c>, run as <c>dotnet run --file secrethelper.cs -- &lt;verb&gt;</c>)
	/// through the real client: credentials management, every command that resolves <c>credentials</c> targets (query,
	/// execute, schema, the MCP query and execute tools), configuration parsing, and the default store on Linux and macOS.
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliEndToEndTests : McpTestBase
	{
		const string CliConfig = "cli.json";

		string _directory     = null!;
		string _storeFile     = null!;
		string _cliJson       = null!;

		[SetUp]
		public void SetUp()
		{
			_cliJson   = CredentialsCliTestSupport.SecretHelperConfigJson();
			_directory = CredentialsCliTestSupport.CreateTempDirectory();
			_storeFile = Path.Combine(_directory, "secrets.json");
		}

		[TearDown]
		public void TearDown()
		{
			CredentialsCliTestSupport.DeleteDirectory(_directory);
		}

		/// <summary>
		/// A test environment whose credentials CLI runs use this test's store file, with <see cref="CliConfig"/> naming the
		/// example (<c>dotnet run --file</c>) in its default profile.
		/// </summary>
		TestCliEnvironment CreateEnvironment()
		{
			var environment = new TestCliEnvironment();
			environment.CredentialsCliEnvironment["SECRETHELPER_FILE"] = _storeFile;
			environment.Files.Add(CliConfig, $$"""{ "default": { "credentialsCli": {{_cliJson}} }, "prod": { "credentials": "linq2db/Prod" } }""");
			return environment;
		}

		/// <summary>A wrapper script running the example on this test's store (for out-of-process runs).</summary>
		string CreateWrapper()
		{
			return CredentialsCliTestSupport.WriteSecretHelperWrapper(_directory, _storeFile);
		}

		async Task Seed(string record, string user, string password)
		{
			var environment = CreateEnvironment();
			environment.Secrets.Enqueue(password);
			environment.Secrets.Enqueue(password);

			var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--config", CliConfig, "--credentials", record, "--user", user);

			exitCode.ShouldBe(0, error);
		}

		static async Task<(int ExitCode, string Output, string Error)> RunCli(TestCliEnvironment environment, params string[] args)
		{
			var exitCode = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);
			return (exitCode, environment.Output, environment.ErrorOutput);
		}

		[Test]
		public async Task CredentialsRoundTripThroughTheExample()
		{
			var environment = CreateEnvironment();
			environment.Secrets.Enqueue(" s3cret = é ");
			environment.Secrets.Enqueue(" s3cret = é ");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "set", "--config", CliConfig, "--profile", "prod", "--user", "DOMAIN\\reader");

			exitCode.ShouldBe(0, error);
			output.ShouldBe($"Stored linq2db/prod for DOMAIN\\reader in credentials CLI '{CredentialsCliTestSupport.SecretHelperCommandLine()}' (profile 'prod').{Environment.NewLine}");
			error. ShouldContain($"Using credentials CLI '{CredentialsCliTestSupport.SecretHelperCommandLine()}' (profile 'default' of {CliConfig}).");

			(exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "list", "--config", CliConfig);

			exitCode.ShouldBe(0, error);
			output.ShouldContain("linq2db/prod");
			output.ShouldContain("DOMAIN\\reader");
			output.ShouldNotContain("s3cret");
			output.ShouldNotContain("Using");

			(exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "remove", "--config", CliConfig, "--credentials", "linq2db/PROD");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Removed linq2db/prod from credentials CLI");

			(exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "remove", "--config", CliConfig, "--profile", "prod");

			exitCode.ShouldBe(-3);
			error.ShouldContain("linq2db/prod was not found in credentials CLI");
		}

		[Test]
		public void ExampleConformance()
		{
			var runner = new CredentialsCliProcessRunner(
				new CredentialsCliSettings("example", "dotnet", $"run --file \"{CredentialsCliTestSupport.GetSecretHelperSource()}\" --", null),
				false,
				new Dictionary<string, string?>(StringComparer.Ordinal) { ["SECRETHELPER_FILE"] = _storeFile })
			{
				Timeout = TimeSpan.FromSeconds(120),
			};
			var store = new CredentialsCliStore(runner);

			const string user     = " DOMAIN\\o'neil \"x\" ";
			const string password = "  p'a\"s$HOME`id`$(id)=é  ";

			store.TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credentials CLI 'example'");

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

			System.Text.Encoding.UTF8.GetString(runner.Run("rename", CredentialsCliTestSupport.Request("protocol=1", "verb=rename")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
			System.Text.Encoding.UTF8.GetString(runner.Run("get",    CredentialsCliTestSupport.Request("protocol=2", "verb=get")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
		}

		[Test]
		public async Task OptionOverridesTheProfileStore()
		{
			await Seed("linq2db/a", "ua", "pa");
			await Seed("linq2db/b", "ub", "pb");

			var environment = CreateEnvironment();
			environment.Files.Add("other.json", """{ "default": { "credentialsCli": "/nonexistent/cli" } }""");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "clear", "--config", "other.json", "--credentials-cli", CredentialsCliTestSupport.SecretHelperCommandLine(), "--force");

			exitCode.ShouldBe(0, error);
			error. ShouldContain("(option).");
			output.ShouldContain("Removed 2 linq2db credential record(s)");
			File.ReadAllText(_storeFile).ShouldNotContain("linq2db/");
		}

		[Test]
		public async Task ListThroughTheOption()
		{
			await Seed("linq2db/cfg", "reader", "secret");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "list", "--credentials-cli", CredentialsCliTestSupport.SecretHelperCommandLine());

			exitCode.ShouldBe(0, error);
			output.ShouldContain("linq2db/cfg");
			output.ShouldNotContain("secret");
		}

		[Test]
		public async Task QueryReadsCredentialsThroughTheOption()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(),
				"query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/Memory", "--credentials-cli", CredentialsCliTestSupport.SecretHelperCommandLine(), "--sql", "select 1 as Value");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("\"Value\":\"1\"");
			error. ShouldNotContain("Using");
		}

		[Test]
		public async Task QueryReadsCredentialsThroughTheStoreInheritedFromDefault()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var environment = CreateEnvironment();
			environment.Files.Add("query.json", $$"""
				{
					"default": {
						"credentialsCli": {{_cliJson}}
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
		public async Task QueryOptionOverridesTheConfiguredStore()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var environment = CreateEnvironment();
			environment.Files.Add("query.json", """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialsCli": "/nonexistent/cli"
					}
				}
				""");

			var (exitCode, output, error) = await RunCli(environment, "query", "--config", "query.json", "--credentials-cli", CredentialsCliTestSupport.SecretHelperCommandLine(), "--sql", "select 1 as Value");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task QueryReportsTargetNotFound()
		{
			var (exitCode, _, error) = await RunCli(CreateEnvironment(),
				"query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/missing", "--credentials-cli", CredentialsCliTestSupport.SecretHelperCommandLine(), "--sql", "select 1");

			exitCode.ShouldBe(-1);
			error.ShouldContain($"Credential target 'linq2db/missing' was not found by credentials CLI '{CredentialsCliTestSupport.SecretHelperCommandLine()}'.");
		}

		[Test]
		public async Task ExecuteReadsCredentialsThroughTheStore()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var environment = CreateEnvironment();
			environment.Files.Add("execute.json", $$"""
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialsCli": {{_cliJson}},
						"enableExecute": true
					}
				}
				""");

			var (exitCode, _, error) = await RunCli(environment, "execute", "--config", "execute.json", "--sql", "create table T (Id int)");

			exitCode.ShouldBe(0, error);
		}

		[Test]
		public async Task SchemaReadsCredentialsThroughTheStore()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var (exitCode, output, error) = await RunCli(CreateEnvironment(),
				"schema", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/memory", "--credentials-cli", CredentialsCliTestSupport.SecretHelperCommandLine(), "--detail-level", "names");

			exitCode.ShouldBe(0, error);
			output.ShouldStartWith("{");
		}

		[TestCase("\"credentialsCli\": 1",                         "credentialsCli is a string: the program and its arguments", TestName = "ConfigCredentialsCliWrongType")]
		[TestCase("\"credentialsCli\": { \"command\": \"h\" }",    "credentialsCli is a string: the program and its arguments", TestName = "ConfigCredentialsCliObject")]
		[TestCase("\"credentialsCli\": \"  \"",                    "is empty",                                                  TestName = "ConfigCredentialsCliEmpty")]
		[TestCase("\"credentialsCli\": \"\\\"/opt/h --x\"",        "unterminated quote",                                        TestName = "ConfigCredentialsCliUnterminatedQuote")]
		[TestCase("\"credentialsCli\": \"@vault\"",                "Unknown credential store '@vault'",                         TestName = "ConfigCredentialsCliUnknownReservedValue")]
		[TestCase("\"credentialHelper\": \"h\"",                   "contains unknown property 'credentialHelper'",              TestName = "ConfigOldCredentialHelperKeyIsUnknown")]
		public async Task ConfigCredentialsCliValidation(string property, string message)
		{
			var environment = CreateEnvironment();
			environment.Files.Add("bad.json", $$"""{ "default": { "provider": "SQLite", "connectionString": "Data Source={0}", "credentials": "linq2db/x", {{property}} } }""");

			var (exitCode, _, error) = await RunCli(environment, "query", "--config", "bad.json", "--sql", "select 1");

			exitCode.ShouldBe(-1);
			error.ShouldContain(message);
		}

		[Test]
		public async Task ConfiguredValueIsNotExpanded()
		{
			var environment = CreateEnvironment();
			environment.EnvironmentVariables["TOOLS"] = _directory;
			environment.Files.Add("expand.json", """{ "default": { "provider": "SQLite", "connectionString": "Data Source={0}", "credentials": "linq2db/x", "credentialsCli": "${TOOLS}/cli" } }""");

			var (exitCode, _, error) = await RunCli(environment, "query", "--config", "expand.json", "--sql", "select 1");

			exitCode.ShouldBe(-1);
			error.ShouldContain("Credentials CLI program '${TOOLS}/cli' was not found");
			error.ShouldContain("are not expanded");
		}

		[Test]
		public async Task RelativeProgramResolvesAgainstTheConfigDirectory()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var configDirectory = Directory.CreateDirectory(Path.Combine(_directory, "cfg")).FullName;
			var wrapper         = CreateWrapper();
			var relative        = Path.GetRelativePath(configDirectory, wrapper).Replace('\\', '/');
			var config          = Path.Combine(configDirectory, "query.json");

			var environment = CreateEnvironment();
			environment.Files.Add(config, $$"""
				{ "default": { "provider": "SQLite", "connectionString": "Data Source={0}", "credentials": "linq2db/memory", "credentialsCli": "{{relative}}" } }
				""");

			var (exitCode, output, error) = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task OutOfProcessQueryReadsCredentialsThroughAWrapper()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var result = await RunCliProcess(
				"query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/memory", "--credentials-cli", CreateWrapper(), "--sql", "select 1 as Value");

			result.ExitCode.ShouldBe(0, result.Error);
			result.Output.  ShouldContain("\"Value\":\"1\"");
		}

		[Test]
		public async Task OutOfProcessUnconfiguredLinuxAndMacOSUseTheLocalStore()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Windows uses Credential Manager when nothing names a store.");

			var directory   = Path.Combine(_directory, "credentials");
			var environment = new Dictionary<string, string?>(StringComparer.Ordinal) { [CredentialsDirectory.Variable] = directory };

			var result = await RunCliProcess(environment, "credentials", "list");

			result.ExitCode.ShouldBe(0, result.Error);
			result.Error. ShouldContain($"Using the local store ({directory}, default for this OS).");
			result.Output.ShouldContain("No linq2db credential records in the local store.");
			Directory.Exists(directory).ShouldBeFalse();

			result = await RunCliProcess(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/missing", "--sql", "select 1");

			result.ExitCode.ShouldBe(-1);
			result.Error.ShouldContain($"Credential target 'linq2db/missing' was not found in the local store ({directory}).");
			Directory.Exists(directory).ShouldBeFalse();
		}

		[Test]
		public async Task McpQueryAndExecuteToolsReadCredentialsThroughTheConfiguredStore()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			var config = Path.Combine(_directory, "mcp.json");

			await File.WriteAllTextAsync(config, $$"""
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"credentials": "linq2db/memory",
						"credentialsCli": {{_cliJson}},
						"enableExecute": true
					}
				}
				""").ConfigureAwait(false);

			// The server, and the example it starts with "dotnet run", inherit the store location. Tests run sequentially.
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
		public async Task McpServerStoreFromTheCommandLine()
		{
			await Seed("linq2db/memory", ":memory:", "ignored");

			await using var server = await McpServerProcess.Start(
				"--provider", "SQLite", "--connection-string", "Data Source={0}", "--credentials", "linq2db/memory", "--credentials-cli", CreateWrapper());

			await server.Initialize();

			var query = ReadToolResult<McpTestJsonTableResult>(await server.CallTool("linq2db_query", new JsonObject { ["sql"] = "select 1 as Value" }));

			query.Rows.ShouldBe([["1"]]);
		}
	}
}
