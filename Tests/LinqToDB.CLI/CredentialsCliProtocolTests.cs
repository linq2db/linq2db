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
	/// Protocol client rows over a scripted runner: request framing, the answer header and its strict acceptance, target and
	/// value rules, and secret hygiene. No process is started.
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliProtocolTests
	{
		const string Secret = "pa ss=wörd";

		sealed class FakeRunner : ICredentialsCliRunner
		{
			readonly Queue<CredentialsCliRunResult> _results = new();

			public List<(string Verb, string Request)> Calls { get; } = [];

			public string DisplayName => "fake-cli";

			public string Describe()
			{
				return "/fake/cli";
			}

			public FakeRunner Answer(string output, int exitCode = 0, string errorOutput = "")
			{
				return Answer(Encoding.UTF8.GetBytes(output), exitCode, errorOutput);
			}

			public FakeRunner Answer(byte[] output, int exitCode = 0, string errorOutput = "")
			{
				_results.Enqueue(new CredentialsCliRunResult(null, exitCode, output, errorOutput));
				return this;
			}

			public FakeRunner AnswerWithTruncatedErrors(int exitCode, string errorOutput)
			{
				_results.Enqueue(new CredentialsCliRunResult(null, exitCode, Encoding.UTF8.GetBytes("protocol=1\nstatus=error\n"), errorOutput) { ErrorOutputTruncated = true });
				return this;
			}

			public FakeRunner Fail(string failure)
			{
				_results.Enqueue(CredentialsCliRunResult.Failed(failure));
				return this;
			}

			public CredentialsCliRunResult Run(string verb, byte[] request)
			{
				Calls.Add((verb, Encoding.UTF8.GetString(request)));
				return _results.Dequeue();
			}
		}

		static CredentialsCliStore Store(FakeRunner runner)
		{
			return new CredentialsCliStore(runner);
		}

		const string Ok       = "protocol=1\nstatus=ok\n";
		const string NotFound = "protocol=1\nstatus=not-found\n";

		// Golden answers.

		[Test]
		public void GetFound()
		{
			var runner = new FakeRunner().Answer(Ok + $"username= reader \npassword={Secret}\nextra=ignored\n");

			Store(runner).TryRead("LINQ2DB/Project-A/Prod", out var user, out var password, out var error).ShouldBeTrue(error);

			user.    ShouldBe(" reader ");
			password.ShouldBe(Secret);
			runner.Calls.ShouldBe([("get", "protocol=1\nverb=get\ntarget=linq2db/project-a/prod\n")]);
		}

		[Test]
		public void GetNotFound()
		{
			var runner = new FakeRunner().Answer(NotFound + "\n");

			Store(runner).TryRead("linq2db/missing", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential target 'linq2db/missing' was not found by credentials CLI 'fake-cli'.");
		}

		[Test]
		public void GetError()
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=error\n", 1, "the keyring is locked\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: the keyring is locked");
		}

		[TestCase("not-a-protocol-line-but-a-raw-password\n", TestName = "RawPasswordOnFirstLineDoesNotRevealItsLength")]
		[TestCase("protocol=1\nstatus=raw-password-12345\n",   TestName = "RawPasswordAsStatusDoesNotRevealItsLength")]
		public void InvalidAnswerDoesNotRevealOutputLength(string output)
		{
			// A program that is not a credentials CLI (an existing script printing the password) must not leak anything
			// derived from its output, its length included: the message also reaches MCP tool responses.
			var runner = new FakeRunner().Answer(output);

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldNotContain("characters");
		}

		[Test]
		public void GetSendsForeignTargetVerbatim()
		{
			var runner = new FakeRunner().Answer(Ok + "username=u\npassword=p\n");

			Store(runner).TryRead("Vault/Path/Secret", out _, out _, out var error).ShouldBeTrue(error);

			runner.Calls.Single().Request.ShouldBe("protocol=1\nverb=get\ntarget=Vault/Path/Secret\n");
		}

		[Test]
		public void StoreOk()
		{
			var runner = new FakeRunner().Answer(Ok);

			Store(runner).TryStore("Project-A/Prod", "DOMAIN\\user", Secret, out var error).ShouldBeTrue(error);

			runner.Calls.ShouldBe([("store", $"protocol=1\nverb=store\ntarget=linq2db/project-a/prod\nusername=DOMAIN\\user\npassword={Secret}\n")]);
		}

		[Test]
		public void StoreError()
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=error\n", 1, "pass insert failed\n");

			Store(runner).TryStore("a", "u", "secret-pw", out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: pass insert failed");
		}

		[TestCase("protocol=1\nstatus=ok\n",        true,  TestName = "EraseOk")]
		[TestCase("protocol=1\nstatus=not-found\n", false, TestName = "EraseNotFound")]
		public void Erase(string output, bool expected)
		{
			var runner = new FakeRunner().Answer(output);

			Store(runner).TryRemove("A", out var removed, out var error).ShouldBeTrue(error);

			removed.ShouldBe(expected);
			runner.Calls.ShouldBe([("erase", "protocol=1\nverb=erase\ntarget=linq2db/a\n")]);
		}

		[Test]
		public void ListTwoRecordsKeepsOnlyLinq2DbTargets()
		{
			var runner = new FakeRunner().Answer(
				Ok +
				"\ntarget=linq2db/b\nusername=ub\n" +
				"\ntarget=vault/other\nusername=x\n" +
				"\ntarget=linq2db/a\nusername=ua\nextra=1\n" +
				"\ntarget=linq2db/c\n");

			Store(runner).TryList(out var profiles, out var diagnostics, out var error).ShouldBeTrue(error);

			profiles.ShouldBe([new CredentialProfile("a", "ua"), new CredentialProfile("b", "ub"), new CredentialProfile("c", "")]);
			diagnostics.ShouldBeEmpty();
			runner.Calls.ShouldBe([("list", "protocol=1\nverb=list\n")]);
		}

		[Test]
		public void ListEmpty()
		{
			var runner = new FakeRunner().Answer(Ok);

			Store(runner).TryList(out var profiles, out _, out var error).ShouldBeTrue(error);

			profiles.ShouldBeEmpty();
		}

		[TestCase("get")]
		[TestCase("store")]
		[TestCase("erase")]
		[TestCase("list")]
		public void UnsupportedVerb(string verb)
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=unsupported\n");
			var store  = Store(runner);
			string? error;

			var result = verb switch
			{
				"get"   => store.TryRead("linq2db/a", out _, out _, out error),
				"store" => store.TryStore("a", "u", "p", out error),
				"erase" => store.TryRemove("a", out _, out error),
				_       => store.TryList(out _, out _, out error),
			};

			result.ShouldBeFalse();
			error.ShouldNotBeNull().ShouldStartWith($"Credentials CLI 'fake-cli' does not support '{verb}'");
			runner.Calls.Single().Verb.ShouldBe(verb);
		}

		[Test]
		public void Protocol2Unsupported()
		{
			var runner = new FakeRunner().Answer("protocol=2\nstatus=unsupported\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' speaks protocol 2, linq2db-cli speaks 1.");
		}

		[Test]
		public void Protocol2WithOtherStatusIsInvalid()
		{
			var runner = new FakeRunner().Answer("protocol=2\nstatus=ok\nusername=u\npassword=p\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' returned an invalid answer to 'get': the first line is protocol=2, expected protocol=1.");
		}

		[Test]
		public void ByteOrderMarkAndCrLfAreAccepted()
		{
			var output = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("protocol=1\r\nstatus=ok\r\nusername=u\r\npassword=p\r\n")).ToArray();
			var runner = new FakeRunner().Answer(output);

			Store(runner).TryRead("linq2db/a", out var user, out var password, out var error).ShouldBeTrue(error);

			user.    ShouldBe("u");
			password.ShouldBe("p");
		}

		// Strict acceptance.

		[Test]
		public void EmptyOutputWithExitZeroIsInvalid()
		{
			var runner = new FakeRunner().Answer(string.Empty);

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' returned an invalid answer to 'get': stdout is empty.");
		}

		[Test]
		public void GarbageFirstLineIsInvalidAndNeverEchoed()
		{
			var runner = new FakeRunner().Answer($"username=u\npassword={Secret}\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' returned an invalid answer to 'get': the first line is not protocol=1.");
			error.ShouldNotBeNull().ShouldNotContain(Secret);
		}

		[Test]
		public void BuildWarningFirstLineIsReportedByItsCodeOnly()
		{
			var runner = new FakeRunner().Answer($"/home/u/secrethelper.cs(12,5): warning CS8321: The local function 'X' is declared but never used [{Secret}]\nprotocol=1\nstatus=ok\nusername=u\npassword={Secret}\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' returned an invalid answer to 'get': the first line looks like build output (diagnostic CS8321); a program run through dotnet run must build without warnings.");
			error.ShouldNotBeNull().ShouldNotContain("secrethelper.cs");
			error.ShouldNotContain(Secret);
		}

		[Test]
		public void BuildOutputShapedSecretIsNotEchoed()
		{
			var runner = new FakeRunner().Answer("TOPSECRET(1,1): error CS1000: suffix\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("(diagnostic CS1000)");
			error.ShouldNotContain("TOPSECRET");
		}

		[TestCase("protocol=1\nusername=u\n",                         "the second line is not status=",   TestName = "MissingStatusIsInvalid")]
		[TestCase("protocol=1\nstatus=found\n",                       "unknown status; expected ok",      TestName = "UnknownStatusIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nstatus=ok\nusername=u\npassword=p\n", "'status' appears more than once", TestName = "StatusRepeatedIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nprotocol=1\nusername=u\npassword=p\n", "'protocol' appears more than once", TestName = "ProtocolRepeatedIsInvalid")]
		[TestCase("protocol=1\nstatus=not-found\nusername=u\n",       "status=not-found must not be followed by other lines", TestName = "TrailingKeysAfterNotFoundAreInvalid")]
		[TestCase("protocol=1\nstatus=unsupported\n\nreason=x\n",     "status=unsupported must not be followed by other lines", TestName = "TrailingKeysAfterUnsupportedAreInvalid")]
		[TestCase("protocol=1\nstatus=error\nmessage=x\n",            "status=error must not be followed by other lines", TestName = "TrailingKeysAfterErrorAreInvalid")]
		[TestCase("protocol=1\nstatus=ok\npassword=p\n",              "exactly one 'username' and one 'password'", TestName = "GetWithoutUsernameIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nusername=u\n",              "exactly one 'username' and one 'password'", TestName = "GetWithoutPasswordIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nusername=u\nusername=v\npassword=p\n", "exactly one 'username' and one 'password'", TestName = "GetWithRepeatedUsernameIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nUsername=u\nPassword=p\n", "exactly one 'username' and one 'password'", TestName = "GetKeysAreCaseSensitive")]
		[TestCase("protocol=1\nstatus=ok\nusername=u\nnot a key value line\n", "a non-empty line has no '='", TestName = "LineWithoutSeparatorIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nusername=u\npassword=p\0q\n", "NUL",                          TestName = "NulIsInvalid")]
		[TestCase("protocol=1\nstatus=ok\nusername=\u001b[31mred\npassword=p\n", "a value contains a control character", TestName = "EscapeSequenceInValueIsInvalid")]
		public void InvalidGetAnswer(string output, string message)
		{
			var runner = new FakeRunner().Answer(output);

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldStartWith("Credentials CLI 'fake-cli' returned an invalid answer to 'get': ");
			error.ShouldNotBeNull().ShouldContain(message);
		}

		[Test]
		public void InvalidUtf8IsInvalid()
		{
			var runner = new FakeRunner().Answer([.. "protocol=1\nstatus=ok\nusername=u\npassword="u8, 0xC3, 0x28, (byte)'\n']);

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' returned an invalid answer to 'get': the output is not valid UTF-8.");
		}

		[Test]
		public void StatusErrorWithExitZeroIsAFailure()
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=error\n", 0, "vault: permission denied\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' reported an error: vault: permission denied");
		}

		[Test]
		public void StatusErrorWithoutMessage()
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=error\n");

			Store(runner).TryStore("a", "u", "p", out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' reported an error without a message on standard error.");
		}

		[Test]
		public void OkWithNonZeroExitIsAFailure()
		{
			var runner = new FakeRunner().Answer(Ok + $"username=u\npassword={Secret}\n", 1, "\n  partial failure  \r\nsecond line\n");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: partial failure");
		}

		[Test]
		public void NonZeroExitWithoutErrorLine()
		{
			var runner = new FakeRunner().Answer(string.Empty, 1);

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1.");
		}

		[TestCase("store")]
		[TestCase("list")]
		public void NotFoundIsNoAnswerToStoreOrList(string verb)
		{
			var runner = new FakeRunner().Answer(NotFound);
			var store  = Store(runner);
			string? error;

			var result = verb == "store" ? store.TryStore("a", "u", "p", out error) : store.TryList(out _, out _, out error);

			result.ShouldBeFalse();
			error.ShouldBe($"Credentials CLI 'fake-cli' returned an invalid answer to '{verb}': status=not-found is not an answer to '{verb}'.");
		}

		[TestCase("\ntarget=linq2db/a\nusername=u\nusername=v\n", "line 6 repeats a key of its record", TestName = "ListDuplicateUsernameIsInvalid")]
		[TestCase("\ntarget=linq2db/a\ntarget=linq2db/b\n",        "line 5 repeats a key of its record", TestName = "ListDuplicateTargetIsInvalid")]
		[TestCase("\ntarget=linq2db/a\nTOPSECRET=1\nTOPSECRET=2\n", "line 6 repeats a key of its record", TestName = "ListDuplicateUnknownKeyIsNotEchoed")]
		[TestCase("\ntarget=linq2db/a\n\nusername=orphan\n",       "a record has no 'target'",    TestName = "ListMissingTargetIsInvalid")]
		[TestCase("\ntarget=linq2db/a\nusername=\u001b]0;title\u0007\n", "a value contains a control character", TestName = "ListUsernameWithEscapeSequenceIsInvalid")]
		public void InvalidListAnswer(string records, string message)
		{
			var runner = new FakeRunner().Answer(Ok + records);

			Store(runner).TryList(out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldStartWith("Credentials CLI 'fake-cli' returned an invalid answer to 'list': ");
			error.ShouldNotBeNull().ShouldContain(message);
		}

		[Test]
		public void RunFailureIsReported()
		{
			var runner = new FakeRunner().Fail("did not answer");

			Store(runner).TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe("did not answer");
		}

		// Clear and count.

		[Test]
		public void ClearErasesOnlyLinq2DbTargets()
		{
			var runner = new FakeRunner()
				.Answer(Ok + "\ntarget=linq2db/a\nusername=u\n\ntarget=vault/keep\nusername=x\n\ntarget=linq2db/b\nusername=v\n")
				.Answer(Ok)
				.Answer(NotFound);

			Store(runner).TryClear(out var count, out var error).ShouldBeTrue(error);

			count.ShouldBe(1);
			runner.Calls.Select(static call => call.Verb).ShouldBe(["list", "erase", "erase"]);
			runner.Calls[1].Request.ShouldBe("protocol=1\nverb=erase\ntarget=linq2db/a\n");
			runner.Calls[2].Request.ShouldBe("protocol=1\nverb=erase\ntarget=linq2db/b\n");
		}

		[Test]
		public void InvalidRecordNamesAreNeitherListedNorErased()
		{
			// A user name with line breaks, stored in a shared store by someone else, can forge records; a store may resolve
			// linq2db/../outside to a path outside linq2db/, so clear must never send it.
			const string Forged = "\ntarget=linq2db/../outside\nusername=x\n\ntarget=linq2db/a//b\n\ntarget=linq2db/./c\n\ntarget=linq2db/d/\n";

			var runner = new FakeRunner()
				.Answer(Ok + "\ntarget=linq2db/ok\nusername=u\n" + Forged)
				.Answer(Ok + "\ntarget=linq2db/ok\nusername=u\n" + Forged)
				.Answer(Ok);

			Store(runner).TryList(out var profiles, out var diagnostics, out var error).ShouldBeTrue(error);

			profiles.ShouldBe([new CredentialProfile("ok", "u")]);
			diagnostics.ShouldHaveSingleItem().ShouldContain("listed 4 record(s) under 'linq2db/' whose names credentials set would refuse");

			Store(runner).TryClear(out var count, out error).ShouldBeTrue(error);

			count.ShouldBe(1);
			runner.Calls.Select(static call => call.Verb).ShouldBe(["list", "list", "erase"]);
			runner.Calls[2].Request.ShouldBe("protocol=1\nverb=erase\ntarget=linq2db/ok\n");
		}

		[Test]
		public void CountUsesList()
		{
			var runner = new FakeRunner().Answer(Ok + "\ntarget=linq2db/a\nusername=u\n\ntarget=other\nusername=x\n");

			Store(runner).TryGetCount(out var count, out var error).ShouldBeTrue(error);

			count.ShouldBe(1);
		}

		// Secrets in error output.

		[Test]
		public void StoreFailureRedactsEchoedPassword()
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=error\n", 1, $"+ password={Secret}\n");

			Store(runner).TryStore("a", "u", Secret, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: + password=***");
		}

		[Test]
		public void StatusErrorRedactsEchoedPassword()
		{
			var runner = new FakeRunner().Answer("protocol=1\nstatus=error\n", 0, $"rejected {Secret}\n");

			Store(runner).TryStore("a", "u", Secret, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' reported an error: rejected ***");
		}

		[Test]
		public void StoreFailureRedactsPasswordCrossingTheLengthCap()
		{
			var password = new string('s', 40);
			var runner   = new FakeRunner().Answer(string.Empty, 1, new string('x', 190) + password + "\n");

			Store(runner).TryStore("a", "u", password, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldNotContain("ssss");
			error.ShouldNotBeNull().ShouldContain(new string('x', 190) + "***");
		}

		[TestCase("TOPSEC",          TestName = "TruncatedErrorRedactsPasswordPrefixAtTheCut")]
		[TestCase("TOPSEC�",    TestName = "TruncatedErrorRedactsPasswordPrefixBeforeSplitCharacter")]
		public void TruncatedErrorRedactsPasswordPrefix(string tail)
		{
			// Standard error was cut at its length limit in the middle of the echoed password.
			var runner = new FakeRunner().AnswerWithTruncatedErrors(1, new string('\n', 4090) + tail);

			Store(runner).TryStore("a", "u", "TOPSECRETVALUE", out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: ***");
		}

		[Test]
		public void TruncatedLongFirstLineRedactsPasswordPrefix()
		{
			var runner = new FakeRunner().AnswerWithTruncatedErrors(1, "password=TOPSEC");

			Store(runner).TryStore("a", "u", "TOPSECRETVALUE", out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: password=***");
		}

		[Test]
		public void UntruncatedErrorKeepsOrdinaryTail()
		{
			var runner = new FakeRunner().Answer(string.Empty, 1, "failed at T");

			Store(runner).TryStore("a", "u", "TOPSECRETVALUE", out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: failed at T");
		}

		[TestCase("abc")]
		[TestCase("p")]
		public void ShortPasswordDropsStandardError(string password)
		{
			var runner = new FakeRunner().Answer(string.Empty, 1, $"rejected {password} for abc-db\n");

			Store(runner).TryStore("a", "u", password, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: (standard error is not shown: the password is too short to remove from it)");
		}

		[Test]
		public void StoreFailureRedactsWhitespaceEdgedPassword()
		{
			const string password = "  edge  ";
			var runner = new FakeRunner().Answer(string.Empty, 1, password + "\n");

			Store(runner).TryStore("a", "u", password, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI 'fake-cli' failed with exit code 1: ***");
		}

		[TestCase("u\nx", "secret-pw",   TestName = "StoreRefusesNewlineInUser")]
		[TestCase("u",    "secret\npw",  TestName = "StoreRefusesNewlineInPassword")]
		[TestCase("u",    "secret\rpw",  TestName = "StoreRefusesCarriageReturnInPassword")]
		[TestCase("u",    "secret\tpw",  TestName = "StoreRefusesTabInPassword")]
		[TestCase("u\0",  "secret-pw",   TestName = "StoreRefusesNulInUser")]
		public void StoreRefusesControlCharacters(string user, string password)
		{
			var runner = new FakeRunner();

			Store(runner).TryStore("a", user, password, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("must not contain line breaks or other control characters");
			error.ShouldNotBeNull().ShouldNotContain("secret");
			runner.Calls.ShouldBeEmpty();
		}

		[TestCase("",                    "must not be empty",             TestName = "TargetEmpty")]
		[TestCase("-rf",                 "must not start with '-'",       TestName = "TargetLeadingDash")]
		[TestCase("linq2db/a\nb",        "control characters",            TestName = "TargetNewline")]
		[TestCase("linq2db/",            "must name a record",            TestName = "TargetPrefixOnly")]
		[TestCase("linq2db//a",          "must name a record",            TestName = "TargetEmptySegment")]
		[TestCase("linq2db/a/",          "must name a record",            TestName = "TargetTrailingSlash")]
		[TestCase("linq2db/a/./b",       "must name a record",            TestName = "TargetDotSegment")]
		[TestCase("LINQ2DB/a/../b",      "must name a record",            TestName = "TargetDotDotSegment")]
		public void TargetValidation(string target, string message)
		{
			var runner = new FakeRunner();

			Store(runner).TryRead(target, out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain(message);
			runner.Calls.ShouldBeEmpty();
		}

		[TestCase("https://vault.example/v1/db")]
		[TestCase("op://Private/db//password")]
		public void ForeignUrlTargetsAreAllowed(string target)
		{
			CredentialTargets.TryNormalize(target, out var normalized, out var error).ShouldBeTrue(error);
			normalized.ShouldBe(target);
		}

		[Test]
		public void RedactIgnoresEmptySecret()
		{
			CredentialsCliStore.Redact("line", string.Empty).ShouldBe("line");
			CredentialsCliStore.Redact("line", null).ShouldBe("line");
		}
	}
}
