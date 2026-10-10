using System;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The <c>credentials set|list|remove|clear</c> commands over the in-memory store that stands for the OS default
	/// (Windows Credential Manager on Windows, the local store elsewhere), plus store and record selection.
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCommandTests
	{
		static string DefaultStoreName => OperatingSystem.IsWindows() ? "Windows Credential Manager" : "the local store";

		[Test]
		public async Task CredentialsSetStoresRecord()
		{
			var environment = new TestCliEnvironment();
			environment.Secrets.Enqueue("secret");
			environment.Secrets.Enqueue("secret");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "set", "--credentials", "linq2db/project-a/production-read", "--user", "ProjectReader");

			exitCode.ShouldBe(0, error);
			output.  ShouldBe($"Stored linq2db/project-a/production-read for ProjectReader in {DefaultStoreName}.{Environment.NewLine}");
			error.   ShouldContain($"Using {DefaultStoreName} (");
			error.   ShouldContain("default for this OS).");
			environment.Credentials["linq2db/project-a/production-read"].ShouldBe(("ProjectReader", "secret"));
		}

		[Test]
		public async Task CredentialsSetTakesTheRecordFromTheProfile()
		{
			var environment = new TestCliEnvironment();
			environment.Files.Add("q.json", """{ "default": { "provider": "SQLite" }, "prod": { "credentials": "linq2db/prod" } }""");
			environment.Secrets.Enqueue("secret");
			environment.Secrets.Enqueue("secret");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "set", "--config", "q.json", "--profile", "prod", "--user", "app_reader");

			exitCode.ShouldBe(0, error);
			output.  ShouldBe($"Stored linq2db/prod for app_reader in {DefaultStoreName} (profile 'prod').{Environment.NewLine}");
			environment.Credentials["linq2db/prod"].ShouldBe(("app_reader", "secret"));
		}

		[Test]
		public async Task CredentialsOptionOverridesTheProfileRecord()
		{
			var environment = new TestCliEnvironment();
			environment.Files.Add("q.json", """{ "prod": { "credentials": "linq2db/prod" } }""");
			environment.Secrets.Enqueue("secret");
			environment.Secrets.Enqueue("secret");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--config", "q.json", "--profile", "prod", "--credentials", "linq2db/other", "--user", "u");

			exitCode.ShouldBe(0, error);
			environment.Credentials.ShouldContainKey("linq2db/other");
			environment.Credentials.ShouldNotContainKey("linq2db/prod");
		}

		[Test]
		public async Task CredentialsSetWithoutRecordNamesWhatToAdd()
		{
			var environment = new TestCliEnvironment();
			environment.Files.Add("q.json", """{ "prod": { "provider": "SQLite" } }""");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--config", "q.json", "--profile", "prod", "--user", "u");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("profile 'prod' has no 'credentials'; add \"credentials\": \"linq2db/prod\" or pass --credentials");
		}

		[Test]
		public async Task CredentialsSetWithoutConfigOrRecord()
		{
			var (exitCode, _, error) = await RunCli(new TestCliEnvironment(), "credentials", "set", "--user", "u");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("Option '--credentials linq2db/<name>' must be specified");
		}

		[Test]
		public async Task ProfileRequiresConfig()
		{
			var (exitCode, _, error) = await RunCli(new TestCliEnvironment(), "credentials", "list", "--profile", "prod");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("Option '--profile' requires option '--config'.");
		}

		[TestCase("set",    TestName = "SetRefusesForeignRecord")]
		[TestCase("remove", TestName = "RemoveRefusesForeignRecord")]
		public async Task ForeignRecordIsRefused(string operation)
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("vault/db", ("Other", "secret"));

			var (exitCode, _, error) = operation == "set"
				? await RunCli(environment, "credentials", "set", "--credentials", "vault/db", "--user", "u")
				: await RunCli(environment, "credentials", "remove", "--credentials", "vault/db");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("is not a linq2db record");
			environment.Credentials.ShouldContainKey("vault/db");
		}

		[Test]
		public async Task CredentialsListContinuesAfterUnreadableRecord()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read",   ("Reader", "secret"));
			environment.Credentials.Add("linq2db/project-b/broken", ("Writer", "secret"));
			environment.UnreadableCredentialTargets.Add("linq2db/project-b/broken");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "list");

			using (Assert.EnterMultipleScope())
			{
				exitCode.ShouldBe(0);
				output.  ShouldContain("linq2db/project-a/read");
				output.  ShouldContain("Reader");
				output.  ShouldNotContain("project-b/broken");
				error.   ShouldContain("linq2db/project-b/broken");
			}
		}

		[Test]
		public async Task CredentialsSetRejectsPasswordMismatch()
		{
			var environment = new TestCliEnvironment();
			environment.Secrets.Enqueue("secret");
			environment.Secrets.Enqueue("different");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--credentials", "linq2db/project-a/read", "--user", "ProjectReader");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("Passwords do not match.");
			environment.Credentials.ShouldBeEmpty();
		}

		[Test]
		public async Task CredentialsListReturnsOnlyLinq2DbRecordsAndNeverTheUsingLine()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-b/write", ("Writer", "secret"));
			environment.Credentials.Add("linq2db/project-a/read",  ("Reader", "secret"));
			environment.Credentials.Add("unrelated-target",        ("Other",  "secret"));

			var (exitCode, output, error) = await RunCli(environment, "credentials", "list");

			exitCode.ShouldBe(0);
			output.  ShouldStartWith("RECORD");
			output.  ShouldContain("linq2db/project-a/read");
			output.  ShouldContain("Reader");
			output.  ShouldContain("linq2db/project-b/write");
			output.  ShouldNotContain("unrelated-target");
			output.  ShouldNotContain("Using");
			error.   ShouldContain("Using ");
			output.IndexOf("project-a/read", StringComparison.Ordinal).ShouldBeLessThan(output.IndexOf("project-b/write", StringComparison.Ordinal));
		}

		[TestCase("--credentials", "linq2db/x")]
		[TestCase("--user",        "u")]
		[TestCase("--force",       null)]
		public async Task CredentialsListRejectsRecordOptions(string option, string? value)
		{
			var (exitCode, _, error) = value != null
				? await RunCli(new TestCliEnvironment(), "credentials", "list", option, value)
				: await RunCli(new TestCliEnvironment(), "credentials", "list", option);

			exitCode.ShouldBe(-1);
			error.   ShouldContain("Credentials list does not accept");
		}

		[Test]
		public async Task CredentialsRemoveDeletesSelectedRecord()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read",  ("Reader", "secret"));
			environment.Credentials.Add("linq2db/project-a/write", ("Writer", "secret"));

			var (exitCode, output, _) = await RunCli(environment, "credentials", "remove", "--credentials", "linq2db/project-a/read");

			exitCode.ShouldBe(0);
			output.  ShouldContain($"Removed linq2db/project-a/read from {DefaultStoreName}.");
			environment.Credentials.ShouldNotContainKey("linq2db/project-a/read");
			environment.Credentials.ShouldContainKey("linq2db/project-a/write");
		}

		[Test]
		public async Task CredentialsRemoveReportsMissingRecord()
		{
			var (exitCode, _, error) = await RunCli(new TestCliEnvironment(), "credentials", "remove", "--credentials", "linq2db/missing");

			exitCode.ShouldBe(-3);
			error.   ShouldContain($"linq2db/missing was not found in {DefaultStoreName}.");
		}

		[Test]
		public async Task CredentialsClearRequiresConfirmation()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read", ("Reader", "secret"));
			environment.InputLines.Enqueue("n");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "clear");

			exitCode.ShouldBe(0);
			error.   ShouldContain($"Remove all 1 linq2db credential records from {DefaultStoreName}? [y/N]");
			output.  ShouldContain("Credential records were not removed.");
			environment.Credentials.ShouldContainKey("linq2db/project-a/read");
		}

		[Test]
		public async Task CredentialsClearForceDeletesOnlyLinq2DbRecords()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read", ("Reader", "secret"));
			environment.Credentials.Add("unrelated-target",       ("Other",  "secret"));

			var (exitCode, output, _) = await RunCli(environment, "credentials", "clear", "--force");

			exitCode.ShouldBe(0);
			output.  ShouldContain("Removed 1 linq2db credential record(s)");
			environment.Credentials.ShouldNotContainKey("linq2db/project-a/read");
			environment.Credentials.ShouldContainKey("unrelated-target");
		}

		[Test]
		public async Task CredentialsClearDeletesUnreadableRecords()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read",   ("Reader", "secret"));
			environment.Credentials.Add("linq2db/project-b/broken", ("Writer", "secret"));
			environment.UnreadableCredentialTargets.Add("linq2db/project-b/broken");

			var (exitCode, output, _) = await RunCli(environment, "credentials", "clear", "--force");

			using (Assert.EnterMultipleScope())
			{
				exitCode.ShouldBe(0);
				output.  ShouldContain("Removed 2 linq2db credential record(s)");
				environment.Credentials.ShouldNotContainKey("linq2db/project-a/read");
				environment.Credentials.ShouldNotContainKey("linq2db/project-b/broken");
			}
		}

		[Test]
		public async Task ProfilesSelectTheirOwnStores()
		{
			// One configuration, two profiles, two stores: the real local store and the in-memory Windows Credential Manager
			// stand-in (Windows), or the local store and a credentials CLI script (Linux, macOS).
			var directory = CredentialsCliTestSupport.CreateTempDirectory();

			try
			{
				var environment = new TestCliEnvironment { UseRealLocalStore = true };
				environment.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = Path.Combine(directory, "store");
				environment.EnvironmentVariables["USERPROFILE"]             = directory;

				string other;

				if (OperatingSystem.IsWindows())
				{
					other = "\"@credential-manager\"";
				}
				else
				{
					// A minimal credentials CLI that answers every list with one record.
					var script = CredentialsCliTestSupport.WriteScript(directory, "one", "printf 'protocol=1\\nstatus=ok\\n\\ntarget=linq2db/from-cli\\nusername=cli-user\\n'\n", string.Empty);
					other = System.Text.Json.JsonSerializer.Serialize(script);
				}

				environment.Files.Add("q.json", $$"""{ "local": { "credentialsCli": "@local" }, "other": { "credentialsCli": {{other}} } }""");
				environment.Credentials.Add("linq2db/from-cm", ("cm-user", "secret"));
				environment.Secrets.Enqueue("p");
				environment.Secrets.Enqueue("p");

				var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--config", "q.json", "--profile", "local", "--credentials", "linq2db/in-local", "--user", "local-user");
				exitCode.ShouldBe(0, error);

				var local = new TestCliEnvironment { UseRealLocalStore = true };
				local.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = Path.Combine(directory, "store");
				local.EnvironmentVariables["USERPROFILE"]             = directory;
				local.Files.Add("q.json", environment.Files["q.json"]);

				var (_, output, _) = await RunCli(local, "credentials", "list", "--config", "q.json", "--profile", "local");
				output.ShouldContain("linq2db/in-local");
				output.ShouldNotContain("from-");

				var otherEnvironment = new TestCliEnvironment();
				otherEnvironment.Credentials.Add("linq2db/from-cm", ("cm-user", "secret"));
				otherEnvironment.Files.Add("q.json", environment.Files["q.json"]);

				(exitCode, output, error) = await RunCli(otherEnvironment, "credentials", "list", "--config", "q.json", "--profile", "other");
				exitCode.ShouldBe(0, error);
				output.ShouldContain(OperatingSystem.IsWindows() ? "linq2db/from-cm" : "linq2db/from-cli");
				output.ShouldNotContain("in-local");
				error. ShouldContain("(profile 'other' of q.json)");
			}
			finally
			{
				CredentialsCliTestSupport.DeleteDirectory(directory);
			}
		}

		[Test]
		public async Task UnconfiguredLinuxAndMacOSUseTheLocalStore()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Windows uses Credential Manager when nothing names a store.");

			var directory = CredentialsCliTestSupport.CreateTempDirectory();

			try
			{
				var store       = Path.Combine(directory, "store");
				var environment = new TestCliEnvironment { UseRealLocalStore = true };
				environment.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = store;
				environment.Secrets.Enqueue("p");
				environment.Secrets.Enqueue("p");

				var (exitCode, output, error) = await RunCli(environment, "credentials", "set", "--credentials", "linq2db/a", "--user", "u");

				exitCode.ShouldBe(0, error);
				output.  ShouldBe($"Stored linq2db/a for u in the local store.{Environment.NewLine}");
				error.   ShouldContain($"Using the local store ({store}, default for this OS).");
				error.   ShouldContain($"Created the local store in {store}: an encrypted file and its key, readable only by you; a copy of both opens it. For the desktop keyring or GPG: dotnet linq2db credentials cli init --store keyring|gpg");
				File.Exists(Path.Combine(store, "credentials.key")).ShouldBeTrue();

				// The notice is shown once: the key exists now.
				environment = new TestCliEnvironment { UseRealLocalStore = true };
				environment.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = store;
				environment.Secrets.Enqueue("p");
				environment.Secrets.Enqueue("p");

				(exitCode, _, error) = await RunCli(environment, "credentials", "set", "--credentials", "linq2db/b", "--user", "u");

				exitCode.ShouldBe(0, error);
				error.ShouldNotContain("Created the local store");

				// A generated script next to the default store is only a note: it is used only when named.
				File.WriteAllText(Path.Combine(store, "credentials-gpg.sh"), "#!/bin/sh\n");

				environment = new TestCliEnvironment { UseRealLocalStore = true };
				environment.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = store;

				(exitCode, output, error) = await RunCli(environment, "credentials", "list");

				exitCode.ShouldBe(0, error);
				output.ShouldContain("linq2db/a");
				output.ShouldContain("linq2db/b");
				error. ShouldContain($"note: {Path.Combine(store, "credentials-gpg.sh")} is used only when named (--credentials-cli @gpg or \"credentialsCli\": \"@gpg\").");
			}
			finally
			{
				CredentialsCliTestSupport.DeleteDirectory(directory);
			}
		}

		[Test]
		public async Task NamedLocalStoreShowsNoDefaultNotices()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("The notices are about the Linux/macOS default.");

			var directory = CredentialsCliTestSupport.CreateTempDirectory();

			try
			{
				var store       = Path.Combine(directory, "store");
				var environment = new TestCliEnvironment { UseRealLocalStore = true };
				environment.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = store;
				environment.Secrets.Enqueue("p");
				environment.Secrets.Enqueue("p");

				var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--credentials-cli", "@local", "--credentials", "linq2db/a", "--user", "u");

				exitCode.ShouldBe(0, error);
				error.ShouldContain($"Using the local store ({store}, option).");
				error.ShouldNotContain("Created the local store");
			}
			finally
			{
				CredentialsCliTestSupport.DeleteDirectory(directory);
			}
		}

		[Test]
		public async Task LocalStoreInsideAGitWorkTreeIsWarnedAbout()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Uses a POSIX credentials directory.");

			var directory = CredentialsCliTestSupport.CreateTempDirectory();

			try
			{
				Directory.CreateDirectory(Path.Combine(directory, ".git"));

				var environment = new TestCliEnvironment { UseRealLocalStore = true };
				environment.EnvironmentVariables["LINQ2DB_CREDENTIALS_DIR"] = Path.Combine(directory, "store");

				var (exitCode, _, error) = await RunCli(environment, "credentials", "list");

				exitCode.ShouldBe(0, error);
				error.ShouldContain("is inside a git working tree; never commit credentials.key or credentials.dat.");
			}
			finally
			{
				CredentialsCliTestSupport.DeleteDirectory(directory);
			}
		}

		[TestCase(new[] { "credentials" },                               "must be one of: set, list, remove, clear, cli init", TestName = "OperationMissing")]
		[TestCase(new[] { "credentials", "rename" },                     "Unknown credentials operation 'rename'",               TestName = "OperationUnknown")]
		[TestCase(new[] { "credentials", "list", "--store", "gpg" },     "supported only by credentials cli init",               TestName = "StoreOnlyForInit")]
		[TestCase(new[] { "credentials", "set", "--credentials", "linq2db/a" }, "Option '--user' must be specified",             TestName = "SetRequiresUser")]
		[TestCase(new[] { "credentials", "clear", "--user", "u" },       "Credentials clear does not accept",                    TestName = "ClearRejectsUser")]
		[TestCase(new[] { "credentials", "list", "--credentials-cli", "@credential-manager" }, "", TestName = "CredentialManagerOption")]
		public async Task ArgumentValidation(string[] args, string message)
		{
			var (exitCode, _, error) = await RunCli(new TestCliEnvironment(), args);

			if (message.Length == 0)
			{
				// @credential-manager is valid on Windows only.
				if (OperatingSystem.IsWindows())
				{
					exitCode.ShouldBe(0, error);
				}
				else
				{
					exitCode.ShouldBe(-1);
					error.ShouldContain("available only on Windows");
				}

				return;
			}

			exitCode.ShouldBe(-1);
			error.   ShouldContain(message);
		}

		[TestCase("set",    TestName = "OldProfileSpellingForSetNamesTheCredentialsOption")]
		[TestCase("remove", TestName = "OldProfileSpellingForRemoveNamesTheCredentialsOption")]
		public async Task OldProfileSpellingNamesTheCredentialsOption(string operation)
		{
			// A 6.5 command line: --profile named the record linq2db/<name>.
			var environment = new TestCliEnvironment();

			var (exitCode, _, error) = operation == "set"
				? await RunCli(environment, "credentials", "set", "--profile", "project-a/production", "--user", "u")
				: await RunCli(environment, "credentials", "remove", "--profile", "project-a/production");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("--credentials linq2db/");
		}

		[TestCase("linq2db/a//b",    TestName = "CredentialManagerRecordWithEmptySegmentCanBeRemoved")]
		[TestCase("linq2db/../prod", TestName = "CredentialManagerRecordWithDotDotSegmentCanBeRemoved")]
		[TestCase("linq2db/a/./b",   TestName = "CredentialManagerRecordWithDotSegmentCanBeRemoved")]
		public async Task CredentialManagerRecordFromVersion65CanBeRemoved(string target)
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Before 6.6 records were stored only in Windows Credential Manager.");

			// 6.5 accepted these names (credentials set --profile a//b); the record must stay manageable one by one.
			var environment = new TestCliEnvironment();
			environment.Credentials.Add(target, ("u", "p"));
			environment.Credentials.Add("linq2db/other", ("u", "p"));

			var (exitCode, _, error) = await RunCli(environment, "credentials", "remove", "--credentials", target);

			exitCode.ShouldBe(0, error);
			environment.Credentials.ShouldNotContainKey(target);
			environment.Credentials.ShouldContainKey("linq2db/other");
		}

		static async Task<(int ExitCode, string Output, string Error)> RunCli(TestCliEnvironment environment, params string[] args)
		{
			var exitCode = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);
			return (exitCode, environment.Output, environment.ErrorOutput);
		}
	}
}
