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
	/// <c>credentials cli init --store keyring|gpg|local</c>: the scripts and the local store it creates, what it records in
	/// a configuration, and that a script and the configuration that names it change together.
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliInitTests
	{
		const UnixFileMode Owner700 = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

		string _root      = null!;
		string _directory = null!;

		[SetUp]
		public void SetUp()
		{
			_root      = CredentialsCliTestSupport.CreateTempDirectory();
			_directory = Path.Combine(_root, "credentials");
		}

		[TearDown]
		public void TearDown()
		{
			CredentialsCliTestSupport.DeleteDirectory(_root);
		}

		TestCliEnvironment CreateEnvironment()
		{
			var environment = new TestCliEnvironment();
			environment.EnvironmentVariables[CredentialsDirectory.Variable] = _directory;
			environment.EnvironmentVariables["USERPROFILE"]                 = _root;
			environment.EnvironmentVariables["PATH"]                        = _root;
			return environment;
		}

		static async Task<(int ExitCode, string Output, string Error)> RunCli(TestCliEnvironment environment, params string[] args)
		{
			var exitCode = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);
			return (exitCode, environment.Output, environment.ErrorOutput);
		}

		static void RequirePosix()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("The keyring and gpg stores are POSIX sh scripts.");
		}

		static string RecordedValue(TestCliEnvironment environment, string config)
		{
			return JsonNode.Parse(environment.Files[config])!["default"]!["credentialsCli"]!.GetValue<string>();
		}

		[TestCase("keyring", "secret-tool")]
		[TestCase("gpg",     "pass")]
		public async Task ScriptStoreWritesTheDefaultScript(string store, string tool)
		{
			RequirePosix();

			var environment = CreateEnvironment();

			var (exitCode, output, error) = await RunCli(environment, "credentials", "cli", "init", "--store", store);

			var script = Path.Combine(_directory, $"credentials-{store}.sh");

			exitCode.ShouldBe(0, error);
			output.ShouldContain($"Created '{script}': a credentials CLI that keeps passwords");
			output.ShouldContain($"It is used only when named: --credentials-cli @{store}, or \"credentialsCli\": \"@{store}\" in a configuration profile.");
			output.ShouldContain($"Check it: dotnet linq2db credentials list --credentials-cli @{store}");
			error. ShouldContain($"Warning: '{tool}' was not found on PATH.");
			File.ReadAllText(script).ShouldBe(CredentialsCliTemplates.Get(store));
			File.GetUnixFileMode(script).    ShouldBe(Owner700);
			File.GetUnixFileMode(_directory).ShouldBe(Owner700);
			Directory.GetFiles(_directory, "*.tmp").ShouldBeEmpty();
		}

		[Test]
		public async Task ScriptStoreRecordsTheReservedValue()
		{
			RequirePosix();

			var environment = CreateEnvironment();
			environment.Files.Add("cfg.json", """
				{
					"default": { "maxRows": 10 },
					"dev": { "provider": "SQLite", "connectionString": "Data Source=:memory:" }
				}
				""");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "gpg", "--config", "cfg.json");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Recorded \"credentialsCli\": \"@gpg\" in profile 'default' of 'cfg.json'.");
			output.ShouldContain("Check it: dotnet linq2db credentials list --config cfg.json");

			var json = JsonNode.Parse(environment.Files["cfg.json"])!;
			json["default"]!["credentialsCli"]!.GetValue<string>().ShouldBe("@gpg");
			json["default"]!["maxRows"]!.GetValue<int>().ShouldBe(10);
			json["dev"]!["provider"]!.GetValue<string>().ShouldBe("SQLite");
		}

		[TestCase("h.sh",         false, TestName = "OutputPathIsRecordedAsWritten")]
		[TestCase("my tools/h.sh", true, TestName = "OutputPathWithSpacesIsRecordedQuoted")]
		public async Task OutputPathIsRecorded(string relative, bool quoted)
		{
			RequirePosix();

			var environment = CreateEnvironment();
			var script      = Path.Combine(_root, relative);

			var (exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "keyring", "-o", script, "--config", "new.json");

			exitCode.ShouldBe(0, error);
			RecordedValue(environment, "new.json").ShouldBe(quoted ? $"\"{script}\"" : script);
			File.GetUnixFileMode(script).ShouldBe(Owner700);
		}

		[TestCase("a\"b.sh")]
		[TestCase("a'b.sh")]
		public async Task OutputWithQuotesIsRefused(string name)
		{
			var (exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "gpg", "-o", Path.Combine(_root, name));

			exitCode.ShouldBe(-1);
			error.ShouldContain("must not contain quotes");
		}

		[Test]
		public async Task ExistingScriptIsReplacedOnlyWithForce()
		{
			RequirePosix();

			var script = Path.Combine(_directory, "credentials-gpg.sh");

			var (exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "gpg");
			exitCode.ShouldBe(0, error);

			File.WriteAllText(script, "edited");

			(exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "gpg");
			exitCode.ShouldBe(-3);
			error.ShouldContain($"'{script}' already exists. Use '--force' to replace it.");
			File.ReadAllText(script).ShouldBe("edited");

			string output;
			(exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "gpg", "--force");
			exitCode.ShouldBe(0, error);
			output.ShouldContain($"Replaced '{script}'");
			File.ReadAllText(script).ShouldBe(CredentialsCliTemplates.Get("gpg"));
			File.GetUnixFileMode(script).ShouldBe(Owner700);
		}

		[Test]
		public async Task DifferentConfiguredStoreIsReplacedOnlyWithForce()
		{
			RequirePosix();

			var environment = CreateEnvironment();
			environment.Files.Add("cfg.json", """{ "default": { "credentialsCli": "/opt/vault-cli" } }""");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "keyring", "--config", "cfg.json");

			exitCode.ShouldBe(-3);
			error.ShouldContain("already sets 'credentialsCli' to \"/opt/vault-cli\". Use '--force' to replace it.");
			File.Exists(Path.Combine(_directory, "credentials-keyring.sh")).ShouldBeFalse();
			RecordedValue(environment, "cfg.json").ShouldBe("/opt/vault-cli");

			(exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "keyring", "--config", "cfg.json", "--force");

			exitCode.ShouldBe(0, error);
			RecordedValue(environment, "cfg.json").ShouldBe("@keyring");
		}

		[TestCase(false, TestName = "ConfigWriteFailureLeavesNoNewScript")]
		[TestCase(true,  TestName = "ConfigWriteFailureKeepsTheOldScript")]
		public async Task ConfigWriteFailureLeavesTheScriptUntouched(bool existingScript)
		{
			RequirePosix();

			var script = Path.Combine(_directory, "credentials-gpg.sh");

			if (existingScript)
			{
				Directory.CreateDirectory(_directory, Owner700);
				File.WriteAllText(script, "old");
			}

			var environment = new TestCliEnvironment { WriteAllTextException = new IOException("disk full") };
			environment.EnvironmentVariables[CredentialsDirectory.Variable] = _directory;
			environment.Files.Add("cfg.json", """{ "default": { "maxRows": 10 } }""");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "gpg", "--config", "cfg.json", "--force");

			exitCode.ShouldBe(-3);
			error.ShouldContain("Cannot write configuration file 'cfg.json': disk full");

			if (existingScript)
				File.ReadAllText(script).ShouldBe("old");
			else
				File.Exists(script).ShouldBeFalse();

			Directory.GetFiles(_directory, ".*.tmp").ShouldBeEmpty();
			environment.Files["cfg.json"].ShouldContain("\"maxRows\": 10");
			environment.Files["cfg.json"].ShouldNotContain("credentialsCli");
		}

		[TestCase(false, TestName = "FinalRenameFailureRestoresTheNewConfig")]
		[TestCase(true,  TestName = "FinalRenameFailureRestoresTheConfigAndKeepsTheOldScript")]
		public async Task FinalRenameFailureRestoresTheConfiguration(bool existingScript)
		{
			RequirePosix();

			var script = Path.Combine(_directory, "credentials-gpg.sh");

			if (existingScript)
			{
				Directory.CreateDirectory(_directory, Owner700);
				File.WriteAllText(script, "old");
			}

			var environment = CreateEnvironment();
			environment.MoveFileFault = destination => string.Equals(destination, script, StringComparison.Ordinal) ? new IOException("device busy") : null;

			const string original = """{ "default": { "credentialsCli": "/opt/old-cli" } }""";
			environment.Files.Add("cfg.json", original);

			var (exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "gpg", "--config", "cfg.json", "--force");

			exitCode.ShouldBe(-3);
			error.ShouldContain($"Cannot write '{script}': device busy. The configuration file 'cfg.json' was restored; '{script}' is unchanged.");
			environment.Files["cfg.json"].ShouldBe(original);

			if (existingScript)
				File.ReadAllText(script).ShouldBe("old");
			else
				File.Exists(script).ShouldBeFalse();

			Directory.GetFiles(_directory, ".*.tmp").ShouldBeEmpty();
		}

		[Test]
		public async Task FinalRenameFailureRemovesAConfigurationItCreated()
		{
			RequirePosix();

			var script      = Path.Combine(_directory, "credentials-keyring.sh");
			var environment = CreateEnvironment();
			environment.MoveFileFault = destination => string.Equals(destination, script, StringComparison.Ordinal) ? new IOException("device busy") : null;

			var (exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "keyring", "--config", "new.json");

			exitCode.ShouldBe(-3);
			error.ShouldContain("was restored");
			environment.Files.ShouldNotContainKey("new.json");
			File.Exists(script).ShouldBeFalse();
		}

		[Test]
		public async Task LocalStoreCreatesDirectoryAndKey()
		{
			var environment = CreateEnvironment();

			var (exitCode, output, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "local");

			exitCode.ShouldBe(0, error);
			output.ShouldContain($"Created the local store in {_directory}: an encrypted file and its key, readable only by you; a copy of both opens it.");
			output.ShouldContain(OperatingSystem.IsWindows()
				? "It is used only when named: --credentials-cli @local"
				: "local is already the default store on this OS; nothing needs to name it.");
			File.Exists(Path.Combine(_directory, "credentials.key")).ShouldBeTrue();
			File.Exists(Path.Combine(_directory, "credentials.dat")).ShouldBeFalse();

			if (!OperatingSystem.IsWindows())
				File.GetUnixFileMode(_directory).ShouldBe(Owner700);

			var key = File.ReadAllBytes(Path.Combine(_directory, "credentials.key"));

			(exitCode, output, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "local", "--force");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("already exists; its key was kept.");
			File.ReadAllBytes(Path.Combine(_directory, "credentials.key")).ShouldBe(key);
		}

		[Test]
		public async Task LocalStoreRecordsTheReservedValue()
		{
			var environment = CreateEnvironment();

			var (exitCode, output, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "local", "--config", "cfg.json");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Recorded \"credentialsCli\": \"@local\" in profile 'default' of 'cfg.json'.");
			RecordedValue(environment, "cfg.json").ShouldBe("@local");
		}

		[Test]
		public async Task LocalStoreConfigWriteFailureKeepsTheKey()
		{
			var environment = new TestCliEnvironment { WriteAllTextException = new IOException("disk full") };
			environment.EnvironmentVariables[CredentialsDirectory.Variable] = _directory;
			environment.EnvironmentVariables["USERPROFILE"]                 = _root;

			var (exitCode, _, error) = await RunCli(environment, "credentials", "cli", "init", "--store", "local", "--config", "cfg.json");

			exitCode.ShouldBe(-3);
			error.ShouldContain("disk full");
			File.Exists(Path.Combine(_directory, "credentials.key")).ShouldBeTrue();
		}

		[Test]
		public async Task LocalStoreDataWithoutKeyIsRefused()
		{
			Directory.CreateDirectory(_directory);

			if (!OperatingSystem.IsWindows())
				File.SetUnixFileMode(_directory, Owner700);

			File.WriteAllBytes(Path.Combine(_directory, "credentials.dat"), [1, 2, 3]);

			var (exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "local");

			exitCode.ShouldBe(-3);
			error.ShouldContain("credentials.key' is missing");
			File.Exists(Path.Combine(_directory, "credentials.key")).ShouldBeFalse();
		}

		[Test]
		public async Task LocalStoreRefusesOutput()
		{
			var (exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", "local", "-o", Path.Combine(_root, "x"));

			exitCode.ShouldBe(-1);
			error.ShouldContain("'--output' is not supported with '--store local'");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public async Task ScriptStoresAreRefusedOnWindows(string store)
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows behaviour.");

			var (exitCode, _, error) = await RunCli(CreateEnvironment(), "credentials", "cli", "init", "--store", store);

			exitCode.ShouldBe(-1);
			error.ShouldContain("generated sh script for Linux and macOS");
			Directory.Exists(_directory).ShouldBeFalse();
		}

		[TestCase(new[] { "credentials", "cli", "init" },                                          "Option '--store' must be specified for credentials cli init: keyring", TestName = "InitRequiresStore")]
		[TestCase(new[] { "credentials", "cli", "init", "--store", "vault" },                      "unknown value 'vault'",                                           TestName = "InitRejectsUnknownStore")]
		[TestCase(new[] { "credentials", "cli", "init", "--store", "gpg", "--profile", "x" },      "accepts only '--store', '--output', '--config', and '--force'",    TestName = "InitRejectsProfile")]
		[TestCase(new[] { "credentials", "cli", "init", "--store", "gpg", "--credentials-cli", "x" }, "accepts only '--store', '--output', '--config', and '--force'", TestName = "InitRejectsCredentialsCli")]
		[TestCase(new[] { "credentials", "cli", "remove" },                                        "Unknown credentials cli operation 'remove'",                      TestName = "CliUnknownOperation")]
		public async Task ArgumentValidation(string[] args, string message)
		{
			var (exitCode, _, error) = await RunCli(CreateEnvironment(), args);

			exitCode.ShouldBe(-1);
			error.ShouldContain(message);
		}
	}
}
