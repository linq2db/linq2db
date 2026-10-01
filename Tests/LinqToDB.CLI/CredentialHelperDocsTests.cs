using System;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The user-facing descriptions of credential helpers: command help, the agent skill, and the protocol document with its
	/// example helper.
	/// </summary>
	[TestFixture]
	public sealed class CredentialHelperDocsTests
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

			help.ShouldContain("Windows Credential Manager on Windows");
			help.ShouldContain("other operating systems need an external credential helper");
			help.ShouldContain("credential-helper");
			help.ShouldContain("helper init");
			help.ShouldContain("secret-tool");
		}

		[TestCase("query")]
		[TestCase("execute")]
		[TestCase("schema")]
		[TestCase("mcp")]
		[TestCase("config-init")]
		public async Task CommandHelpListsCredentialHelperOption(string command)
		{
			(await Run("help", command)).ShouldContain("--credential-helper");
		}

		[Test]
		public async Task SkillDescribesCredentialHelpers()
		{
			var skill = await Run("skill");

			skill.ShouldContain("credentialHelper");
			skill.ShouldContain("--credential-helper");
			skill.ShouldContain("credentials helper init");
			skill.ShouldContain("CREDENTIAL-HELPERS.md");
		}

		[Test]
		public void ProtocolDocumentReferencesTheTestedExampleHelper()
		{
			var directory = Path.Combine(AppContext.BaseDirectory, "CredentialHelpers");
			var document  = File.ReadAllText(Path.Combine(directory, "CREDENTIAL-HELPERS.md"));

			document.ShouldContain("(CredentialHelpers/secrethelper.cs)");
			File.Exists(Path.Combine(directory, "secrethelper.cs")).ShouldBeTrue();

			foreach (var verb in new[] { "`get`", "`store`", "`erase`", "`list`", "unsupported=verb", "unsupported=protocol", "LINQ2DB_CREDENTIAL_INTERACTIVE" })
				document.ShouldContain(verb);
		}
	}
}
