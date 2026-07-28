using System;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class CredentialsCommandTests
	{
		[Test]
		public async Task CredentialsSetStoresProtectedProfileReference()
		{
			var environment = new TestCliEnvironment();
			environment.Secrets.Enqueue("secret");
			environment.Secrets.Enqueue("secret");

			var (exitCode, output, _) = await RunCli(environment, "credentials", "set", "--profile", "project-a/production-read", "--user", "ProjectReader");

			exitCode.ShouldBe(0);
			output.  ShouldContain("Stored credential profile 'project-a/production-read' as target 'linq2db/project-a/production-read'.");
			environment.Credentials["linq2db/project-a/production-read"].ShouldBe(("ProjectReader", "secret"));
		}

		[Test]
		public async Task CredentialsSetRejectsPasswordMismatch()
		{
			var environment = new TestCliEnvironment();
			environment.Secrets.Enqueue("secret");
			environment.Secrets.Enqueue("different");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "set", "--profile", "project-a/read", "--user", "ProjectReader");

			exitCode.ShouldBe(-1);
			error.   ShouldContain("Passwords do not match.");
			environment.Credentials.ShouldBeEmpty();
		}

		[Test]
		public async Task CredentialsListReturnsOnlyLinq2DbProfiles()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-b/write", ("Writer", "secret"));
			environment.Credentials.Add("linq2db/project-a/read",  ("Reader", "secret"));
			environment.Credentials.Add("unrelated-target",        ("Other",  "secret"));

			var (exitCode, output, _) = await RunCli(environment, "credentials", "list");

			exitCode.ShouldBe(0);
			output.  ShouldContain("project-a/read");
			output.  ShouldContain("Reader");
			output.  ShouldContain("project-b/write");
			output.  ShouldNotContain("unrelated-target");
			output.IndexOf("project-a/read", StringComparison.Ordinal).ShouldBeLessThan(output.IndexOf("project-b/write", StringComparison.Ordinal));
		}

		[Test]
		public async Task CredentialsRemoveDeletesSelectedProfile()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read",  ("Reader", "secret"));
			environment.Credentials.Add("linq2db/project-a/write", ("Writer", "secret"));

			var (exitCode, _, _) = await RunCli(environment, "credentials", "remove", "--profile", "project-a/read");

			exitCode.ShouldBe(0);
			environment.Credentials.ShouldNotContainKey("linq2db/project-a/read");
			environment.Credentials.ShouldContainKey("linq2db/project-a/write");
		}

		[Test]
		public async Task CredentialsClearRequiresConfirmation()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read", ("Reader", "secret"));
			environment.InputLines.Enqueue("n");

			var (exitCode, output, _) = await RunCli(environment, "credentials", "clear");

			exitCode.ShouldBe(0);
			output.  ShouldContain("Credential profiles were not removed.");
			environment.Credentials.ShouldContainKey("linq2db/project-a/read");
		}

		[Test]
		public async Task CredentialsClearForceDeletesOnlyLinq2DbProfiles()
		{
			var environment = new TestCliEnvironment();
			environment.Credentials.Add("linq2db/project-a/read", ("Reader", "secret"));
			environment.Credentials.Add("unrelated-target",       ("Other",  "secret"));

			var (exitCode, output, _) = await RunCli(environment, "credentials", "clear", "--force");

			exitCode.ShouldBe(0);
			output.  ShouldContain("Removed 1 linq2db credential profile(s).");
			environment.Credentials.ShouldNotContainKey("linq2db/project-a/read");
			environment.Credentials.ShouldContainKey("unrelated-target");
		}

		static async Task<(int ExitCode, string Output, string Error)> RunCli(TestCliEnvironment environment, params string[] args)
		{
			var exitCode = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);
			return (exitCode, environment.Output, environment.ErrorOutput);
		}
	}
}
