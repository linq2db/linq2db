using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Protocol client rows over a scripted runner: request framing, answer validation, target and value rules, the docker
	/// adapter, and secret hygiene. No process is started.
	/// </summary>
	[TestFixture]
	public sealed class CredentialHelperProtocolTests
	{
		const string Secret = "pa ss=wörd";

		sealed class FakeRunner : ICredentialHelperRunner
		{
			readonly Queue<CredentialHelperRunResult> _results = new();

			public List<(string Verb, string Request)> Calls { get; } = [];

			public string DisplayName => "fake-helper";

			public string Describe()
			{
				return "/fake/helper";
			}

			public FakeRunner Answer(string output, int exitCode = 0, string errorOutput = "")
			{
				return Answer(Encoding.UTF8.GetBytes(output), exitCode, errorOutput);
			}

			public FakeRunner Answer(byte[] output, int exitCode = 0, string errorOutput = "")
			{
				_results.Enqueue(new CredentialHelperRunResult(null, exitCode, output, errorOutput));
				return this;
			}

			public FakeRunner Fail(string failure)
			{
				_results.Enqueue(CredentialHelperRunResult.Failed(failure));
				return this;
			}

			public CredentialHelperRunResult Run(string verb, byte[] request)
			{
				Calls.Add((verb, Encoding.UTF8.GetString(request)));
				return _results.Dequeue();
			}
		}

		static HelperCredentialStore Linq2Db(FakeRunner runner)
		{
			return new HelperCredentialStore(runner, CredentialHelperProtocol.Linq2Db);
		}

		static HelperCredentialStore Docker(FakeRunner runner)
		{
			return new HelperCredentialStore(runner, CredentialHelperProtocol.Docker);
		}

		[Test]
		public void GetSendsFramedRequestAndReadsAnswer()
		{
			var runner = new FakeRunner().Answer($"username= reader \npassword={Secret}\nextra=ignored\n");

			Linq2Db(runner).TryRead("LINQ2DB/Project-A/Prod", out var user, out var password, out var error).ShouldBeTrue(error);

			user.    ShouldBe(" reader ");
			password.ShouldBe(Secret);
			runner.Calls.ShouldBe([("get", "protocol=1\ntarget=linq2db/project-a/prod\n")]);
		}

		[Test]
		public void GetAcceptsCrLfAndByteOrderMark()
		{
			var output = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("username=u\r\npassword=p\r\n")).ToArray();
			var runner = new FakeRunner().Answer(output);

			Linq2Db(runner).TryRead("linq2db/a", out var user, out var password, out var error).ShouldBeTrue(error);

			user.    ShouldBe("u");
			password.ShouldBe("p");
		}

		[Test]
		public void GetSendsForeignTargetVerbatim()
		{
			var runner = new FakeRunner().Answer("username=u\npassword=p\n");

			Linq2Db(runner).TryRead("Vault/Path/Secret", out _, out _, out var error).ShouldBeTrue(error);

			runner.Calls.Single().Request.ShouldBe("protocol=1\ntarget=Vault/Path/Secret\n");
		}

		[TestCase("")]
		[TestCase("\n\n")]
		[TestCase("\r\n")]
		public void GetEmptyAnswerIsNotFound(string output)
		{
			var runner = new FakeRunner().Answer(output);

			Linq2Db(runner).TryRead("linq2db/missing", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential target 'linq2db/missing' was not found by credential helper 'fake-helper'.");
		}

		[TestCase("password=p\n",                          TestName = "GetWithoutUsernameFails")]
		[TestCase("username=u\n",                          TestName = "GetWithoutPasswordFails")]
		[TestCase("username=u\nusername=v\npassword=p\n",  TestName = "GetWithRepeatedUsernameFails")]
		[TestCase("username=u\npassword=p\npassword=q\n",  TestName = "GetWithRepeatedPasswordFails")]
		[TestCase("Username=u\nPassword=p\n",              TestName = "GetKeysAreCaseSensitive")]
		public void GetInvalidAnswer(string output)
		{
			var runner = new FakeRunner().Answer(output);

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("expected exactly one 'username' and one 'password' line");
		}

		[Test]
		public void LineWithoutSeparatorFails()
		{
			var runner = new FakeRunner().Answer("username=u\nnot a key value line\n");

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' returned an invalid answer to 'get': a non-empty line has no '='.");
		}

		[Test]
		public void InvalidUtf8Fails()
		{
			var runner = new FakeRunner().Answer([.. "username=u\npassword="u8, 0xC3, 0x28, (byte)'\n']);

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' returned an invalid answer to 'get': the output is not valid UTF-8.");
		}

		[TestCase("username=\u001b[31mred\npassword=p\n", TestName = "GetUsernameWithEscapeSequenceFails")]
		[TestCase("username=u\npassword=p\tq\n",           TestName = "GetPasswordWithTabFails")]
		public void ControlCharacterInAnswerValueFails(string output)
		{
			var runner = new FakeRunner().Answer(output);

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' returned an invalid answer to 'get': a value contains a control character.");
		}

		[Test]
		public void ListUsernameWithEscapeSequenceFails()
		{
			var runner = new FakeRunner().Answer("target=linq2db/a\nusername=\u001b]0;title\u0007\n");

			Linq2Db(runner).TryList(out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("a value contains a control character");
		}

		[Test]
		public void DockerValuesWithControlCharactersFail()
		{
			var get = new FakeRunner().Answer("""{"Username":"u\u001b[2J","Secret":"p"}""");

			Docker(get).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("a value contains a control character");

			var list = new FakeRunner().Answer("""{"linq2db/a":"u\u001b[2J"}""");

			Docker(list).TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("a value contains a control character");
		}

		[Test]
		public void NulFails()
		{
			var runner = new FakeRunner().Answer("username=u\npassword=p\0q\n");

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("NUL");
		}

		[TestCase("get")]
		[TestCase("store")]
		[TestCase("erase")]
		[TestCase("list")]
		public void UnsupportedVerbIsReported(string verb)
		{
			var runner = new FakeRunner().Answer("unsupported=verb\n");
			var store  = Linq2Db(runner);
			string? error;

			var result = verb switch
			{
				"get"   => store.TryRead("linq2db/a", out _, out _, out error),
				"store" => store.TryStore("a", "u", "p", out error),
				"erase" => store.TryRemove("a", out _, out error),
				_       => store.TryList(out _, out _, out error),
			};

			result.ShouldBeFalse();
			error.ShouldNotBeNull().ShouldStartWith($"Credential helper 'fake-helper' does not support '{verb}'");
			runner.Calls.Single().Verb.ShouldBe(verb);
		}

		[Test]
		public void UnsupportedProtocolIsReported()
		{
			var runner = new FakeRunner().Answer("unsupported=protocol\n");

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' does not support credential helper protocol version 1.");
		}

		[Test]
		public void NonZeroExitShowsErrorLineNeverOutput()
		{
			var runner = new FakeRunner().Answer($"username=u\npassword={Secret}\n", 2, "\n  keyring is locked  \r\nsecond line\n");

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 2: keyring is locked");
		}

		[Test]
		public void NonZeroExitWithoutErrorLine()
		{
			var runner = new FakeRunner().Answer(string.Empty, 1);

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 1.");
		}

		[Test]
		public void RunFailureIsReported()
		{
			var runner = new FakeRunner().Fail("did not answer");

			Linq2Db(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("did not answer");
		}

		[Test]
		public void StoreSendsCredentialAndFoldsTarget()
		{
			var runner = new FakeRunner().Answer(string.Empty);

			Linq2Db(runner).TryStore("Project-A/Prod", "DOMAIN\\user", Secret, out var error).ShouldBeTrue(error);

			runner.Calls.ShouldBe([("store", $"protocol=1\ntarget=linq2db/project-a/prod\nusername=DOMAIN\\user\npassword={Secret}\n")]);
		}

		[Test]
		public void StoreFailureRedactsEchoedPassword()
		{
			var runner = new FakeRunner().Answer(string.Empty, 1, $"+ password={Secret}\n");

			Linq2Db(runner).TryStore("a", "u", Secret, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 1: + password=***");
		}

		[Test]
		public void StoreFailureRedactsPasswordCrossingTheLengthCap()
		{
			var password = new string('s', 40);
			var runner   = new FakeRunner().Answer(string.Empty, 1, new string('x', 190) + password + "\n");

			Linq2Db(runner).TryStore("a", "u", password, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldNotContain("ssss");
			error.ShouldNotBeNull().ShouldContain(new string('x', 190) + "***");
		}

		[Test]
		public void StoreFailureRedactsWhitespaceEdgedPassword()
		{
			const string password = "  edge  ";
			var runner = new FakeRunner().Answer(string.Empty, 1, password + "\n");

			Linq2Db(runner).TryStore("a", "u", password, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 1: ***");
		}

		[TestCase("u\nx", "secret-pw",   TestName = "StoreRefusesNewlineInUser")]
		[TestCase("u",    "secret\npw",  TestName = "StoreRefusesNewlineInPassword")]
		[TestCase("u",    "secret\rpw",  TestName = "StoreRefusesCarriageReturnInPassword")]
		[TestCase("u",    "secret\tpw",  TestName = "StoreRefusesTabInPassword")]
		[TestCase("u\0",  "secret-pw",   TestName = "StoreRefusesNulInUser")]
		public void StoreRefusesControlCharacters(string user, string password)
		{
			var runner = new FakeRunner();

			Linq2Db(runner).TryStore("a", user, password, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("must not contain line breaks or other control characters");
			error.ShouldNotBeNull().ShouldNotContain("secret");
			runner.Calls.ShouldBeEmpty();
		}

		[TestCase("",                    "must not be empty",             TestName = "TargetEmpty")]
		[TestCase("-rf",                 "must not start with '-'",       TestName = "TargetLeadingDash")]
		[TestCase("linq2db/a\nb",        "control characters",            TestName = "TargetNewline")]
		[TestCase("linq2db/",            "must name a profile",           TestName = "TargetPrefixOnly")]
		[TestCase("linq2db//a",          "must name a profile",           TestName = "TargetEmptySegment")]
		[TestCase("linq2db/a/",          "must name a profile",           TestName = "TargetTrailingSlash")]
		[TestCase("linq2db/a/./b",       "must name a profile",           TestName = "TargetDotSegment")]
		[TestCase("LINQ2DB/a/../b",      "must name a profile",           TestName = "TargetDotDotSegment")]
		public void TargetValidation(string target, string message)
		{
			var runner = new FakeRunner();

			Linq2Db(runner).TryRead(target, out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain(message);
			runner.Calls.ShouldBeEmpty();
		}

		[TestCase("https://vault.example/v1/db")]
		[TestCase("op://Private/db//password")]
		public void ForeignUrlTargetsAreAllowed(string target)
		{
			HelperCredentialStore.TryNormalizeTarget(target, out var normalized, out var error).ShouldBeTrue(error);
			normalized.ShouldBe(target);
		}

		[TestCase("removed=true\n",  true)]
		[TestCase("removed=false\n", false)]
		[TestCase("",                true)]
		public void EraseReadsRemoved(string output, bool expected)
		{
			var runner = new FakeRunner().Answer(output);

			Linq2Db(runner).TryRemove("A", out var removed, out var error).ShouldBeTrue(error);

			removed.ShouldBe(expected);
			runner.Calls.ShouldBe([("erase", "protocol=1\ntarget=linq2db/a\n")]);
		}

		[TestCase("removed=maybe\n")]
		[TestCase("removed=true\nremoved=true\n")]
		public void EraseInvalidRemovedFails(string output)
		{
			var runner = new FakeRunner().Answer(output);

			Linq2Db(runner).TryRemove("a", out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("'removed' must be 'true' or 'false'");
		}

		[Test]
		public void ListKeepsOnlyLinq2DbTargets()
		{
			var runner = new FakeRunner().Answer(
				"target=linq2db/b\nusername=ub\n\n" +
				"target=vault/other\nusername=x\n\n" +
				"target=linq2db/a\nusername=ua\nextra=1\n\n" +
				"target=linq2db/c\n\n" +
				"username=orphan\n\n");

			Linq2Db(runner).TryList(out var profiles, out var diagnostics, out var error).ShouldBeTrue(error);

			profiles.ShouldBe([new CredentialProfile("a", "ua"), new CredentialProfile("b", "ub"), new CredentialProfile("c", "")]);
			diagnostics.ShouldHaveSingleItem().ShouldContain("without exactly one 'target' line");
			runner.Calls.ShouldBe([("list", "protocol=1\n")]);
		}

		[Test]
		public void ClearErasesOnlyLinq2DbTargets()
		{
			var runner = new FakeRunner()
				.Answer("target=linq2db/a\nusername=u\n\ntarget=vault/keep\nusername=x\n\ntarget=linq2db/b\nusername=v\n")
				.Answer("removed=true\n")
				.Answer("removed=false\n");

			Linq2Db(runner).TryClear(out var count, out var error).ShouldBeTrue(error);

			count.ShouldBe(1);
			runner.Calls.Select(static call => call.Verb).ShouldBe(["list", "erase", "erase"]);
			runner.Calls[1].Request.ShouldBe("protocol=1\ntarget=linq2db/a\n");
			runner.Calls[2].Request.ShouldBe("protocol=1\ntarget=linq2db/b\n");
		}

		[Test]
		public void CountUsesList()
		{
			var runner = new FakeRunner().Answer("target=linq2db/a\nusername=u\n\ntarget=other\nusername=x\n");

			Linq2Db(runner).TryGetCount(out var count, out var error).ShouldBeTrue(error);

			count.ShouldBe(1);
		}

		[Test]
		public void DockerGetReadsJson()
		{
			var runner = new FakeRunner().Answer("""{"ServerURL":"linq2db/a","Username":"DOMAIN\\u1","Secret":"  s p  "}""");

			Docker(runner).TryRead("LINQ2DB/A", out var user, out var password, out var error).ShouldBeTrue(error);

			user.    ShouldBe("DOMAIN\\u1");
			password.ShouldBe("  s p  ");
			runner.Calls.ShouldBe([("get", "linq2db/a")]);
		}

		[Test]
		public void DockerGetNotFound()
		{
			var runner = new FakeRunner().Answer("credentials not found in native keychain\n", 1);

			Docker(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential target 'linq2db/a' was not found by credential helper 'fake-helper' (or the keyring is locked).");
		}

		[Test]
		public void DockerFailureShowsStdoutMessage()
		{
			var runner = new FakeRunner().Answer("error getting credentials - err: exit status 1\n", 1);

			Docker(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 1: error getting credentials - err: exit status 1");
		}

		[Test]
		public void DockerGetInvalidJsonFails()
		{
			var runner = new FakeRunner().Answer("{\"Username\":\"u\"}");

			Docker(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("expected a JSON object with 'Username' and 'Secret'");
		}

		[Test]
		public void DockerStoreSendsJsonAndRedactsFailure()
		{
			var runner = new FakeRunner().Answer($"bad input {Secret}\n", 1);

			Docker(runner).TryStore("A", "u\"q", Secret, out var error).ShouldBeFalse();

			runner.Calls.Single().ShouldBe(("store", "{\"ServerURL\":\"linq2db/a\",\"Username\":\"u\\u0022q\",\"Secret\":\"pa ss=w\\u00F6rd\"}"));
			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 1: bad input ***");
		}

		[Test]
		public void DockerStoreFailureRedactsJsonEscapedPassword()
		{
			// The adapter sends the secret inside JSON; a helper that echoes the request shows it JSON-escaped.
			var runner = new FakeRunner().Answer("invalid request {\"Secret\":\"pa ss=w\\u00F6rd\"}\n", 1);

			Docker(runner).TryStore("a", "u", Secret, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'fake-helper' failed with exit code 1: invalid request {\"Secret\":\"***\"}");
		}

		[Test]
		public void DockerListStripsSecretServiceLabels()
		{
			var runner = new FakeRunner().Answer("""{"Registry credentials for linq2db/b":"ub","https://index.docker.io/v1/":"me","linq2db/a":"ua"}""");

			Docker(runner).TryList(out var profiles, out _, out var error).ShouldBeTrue(error);

			profiles.ShouldBe([new CredentialProfile("a", "ua"), new CredentialProfile("b", "ub")]);
		}

		[Test]
		public void DockerListFailsOnUnknownLabel()
		{
			var runner = new FakeRunner().Answer("""{"Some other label linq2db/a":"u"}""");

			Docker(runner).TryList(out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("in a format the docker adapter does not recognize");
		}

		[Test]
		public void DockerEraseMissingDoesNotCallErase()
		{
			var runner = new FakeRunner().Answer("""{"linq2db/other":"u"}""");

			Docker(runner).TryRemove("a", out var removed, out var error).ShouldBeTrue(error);

			removed.ShouldBeFalse();
			runner.Calls.Select(static call => call.Verb).ShouldBe(["list"]);
		}

		[Test]
		public void DockerEraseExisting()
		{
			var runner = new FakeRunner().Answer("""{"linq2db/a":"u"}""").Answer(string.Empty);

			Docker(runner).TryRemove("A", out var removed, out var error).ShouldBeTrue(error);

			removed.ShouldBeTrue();
			runner.Calls.ShouldBe([("list", ""), ("erase", "linq2db/a")]);
		}

		[Test]
		public void DockerClearErasesOnlyLinq2DbTargets()
		{
			var runner = new FakeRunner()
				.Answer("""{"Registry credentials for linq2db/a":"u","Registry credentials for https://index.docker.io/v1/":"me"}""")
				.Answer(string.Empty);

			Docker(runner).TryClear(out var count, out var error).ShouldBeTrue(error);

			count.ShouldBe(1);
			runner.Calls.ShouldBe([("list", ""), ("erase", "linq2db/a")]);
		}

		[Test]
		public void RedactIgnoresEmptySecret()
		{
			HelperCredentialStore.Redact("line", string.Empty).ShouldBe("line");
			HelperCredentialStore.Redact("line", null).ShouldBe("line");
		}
	}
}
