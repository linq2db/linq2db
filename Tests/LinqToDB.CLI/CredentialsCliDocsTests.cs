using System;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The user-facing descriptions of credential stores and credentials CLIs: command help, the agent skill, and the
	/// protocol document with its example.
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliDocsTests
	{
		static async Task<string> Run(params string[] args)
		{
			var environment = new TestCliEnvironment();
			var exitCode    = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);

			exitCode.ShouldBe(0, environment.ErrorOutput);
			return environment.Output;
		}

		[Test]
		public async Task CredentialsHelpNamesTheStorePerOperatingSystem()
		{
			var help = await Run("help", "credentials");

			help.ShouldContain("the built-in local store on Linux and macOS");
			help.ShouldContain("Windows Credential Manager on Windows");
			help.ShouldContain("credentials-cli");
			help.ShouldContain("cli init");
			help.ShouldContain("keyring");
			help.ShouldContain("a configuration that sets credentialsCli runs that program as you");
			help.ShouldNotContain("helper");
		}

		[TestCase("query")]
		[TestCase("execute")]
		[TestCase("schema")]
		[TestCase("mcp")]
		[TestCase("config-init")]
		public async Task CommandHelpListsCredentialsCliOption(string command)
		{
			var help = await Run("help", command);

			help.ShouldContain("--credentials-cli");
			help.ShouldNotContain("credential-helper");
		}

		[Test]
		public async Task SkillDescribesCredentialStores()
		{
			var skill = await Run("skill");

			skill.ShouldContain("credentialsCli");
			skill.ShouldContain("--credentials-cli");
			skill.ShouldContain("credentials cli init");
			skill.ShouldContain("CREDENTIALS-CLI.md");
			skill.ShouldContain("@local");
			skill.ShouldContain("a configuration that sets credentialsCli runs that program as you");
			skill.ShouldNotContain("credentialHelper");
			skill.ShouldNotContain("credential helper");
		}

		[Test]
		public void ProtocolDocumentReferencesTheTestedExample()
		{
			var directory = Path.Combine(AppContext.BaseDirectory, "CredentialsCli");
			var document  = File.ReadAllText(Path.Combine(directory, "CREDENTIALS-CLI.md"));

			document.ShouldContain("(CredentialsCli/secrethelper.cs)");
			File.Exists(Path.Combine(directory, "secrethelper.cs")).ShouldBeTrue();

			foreach (var text in new[] { "`get`", "`store`", "`erase`", "`list`", "status=unsupported", "status=not-found", "protocol=2", "LINQ2DB_CREDENTIAL_INTERACTIVE", "@local", "@credential-manager", "@keyring", "@gpg", "@vault", "LINQ2DB_CREDENTIALS_DIR" })
				document.ShouldContain(text);

			document.ShouldNotContain("credential helper", Case.Insensitive);
			document.ShouldNotContain("docker", Case.Insensitive);
		}
	}
}
