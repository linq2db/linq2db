using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The process runner against real helper processes (sh scripts on POSIX, cmd scripts on Windows): arguments, standard
	/// input, environment, exit codes, output caps, timeouts and the fail-fast window.
	/// </summary>
	[TestFixture]
	public sealed class CredentialHelperProcessTests
	{
		const string Secret = "top secret";

		string _directory = null!;

		[SetUp]
		public void SetUp()
		{
			_directory = CredentialHelperTestSupport.CreateTempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			CredentialHelperTestSupport.DeleteDirectory(_directory);
		}

		string Script(string name, string sh, string cmd)
		{
			return CredentialHelperTestSupport.WriteScript(_directory, name, sh, cmd);
		}

		[Test]
		public void HelperReceivesVerbStdinAndEnvironment()
		{
			var record = Path.Combine(_directory, "record.txt");
			var helper = Script(
				"echo-helper",
				$"{{ printf 'argc=%s\\n' \"$#\"; printf 'arg=%s\\n' \"$1\"; printf 'interactive=%s\\n' \"$LINQ2DB_CREDENTIAL_INTERACTIVE\"; cat; }} > '{record}'\nprintf 'username=u\\npassword=p\\n'\n",
				$"@echo off\r\n(echo arg=%1& echo extra=%2& echo interactive=%LINQ2DB_CREDENTIAL_INTERACTIVE%& more) > \"{record}\"\r\necho username=u\r\necho password=p\r\n");

			var result = CredentialHelperTestSupport.CreateRunner(helper).Run("get", CredentialHelperTestSupport.Request("protocol=1", "target=linq2db/a"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0);
			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldBe("username=u\npassword=p\n");

			var recorded = File.ReadAllText(record).Replace("\r", string.Empty, StringComparison.Ordinal);

			recorded.ShouldContain("arg=get\n");
			recorded.ShouldContain("interactive=0\n");
			recorded.ShouldContain("protocol=1\ntarget=linq2db/a\n");

			if (OperatingSystem.IsWindows())
				recorded.ShouldContain("extra=\n");
			else
				recorded.ShouldContain("argc=1\n");
		}

		[Test]
		public void InteractiveFlagIsSet()
		{
			var helper = Script(
				"flag-helper",
				"printf 'username=%s\\npassword=p\\n' \"$LINQ2DB_CREDENTIAL_INTERACTIVE\"\n",
				"@echo off\r\necho username=%LINQ2DB_CREDENTIAL_INTERACTIVE%\r\necho password=p\r\n");

			var result = CredentialHelperTestSupport.CreateRunner(helper, interactive: true).Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			Encoding.UTF8.GetString(result.Output).ShouldStartWith("username=1");
		}

		[Test]
		public void NonZeroExitReturnsErrorOutput()
		{
			var helper = Script(
				"fail-helper",
				"printf 'username=leak\\n'\nprintf '\\nthe keyring is locked\\nsecond\\n' >&2\nexit 3\n",
				"@echo off\r\necho username=leak\r\necho the keyring is locked 1>&2\r\nexit /b 3\r\n");

			var store = new HelperCredentialStore(CredentialHelperTestSupport.CreateRunner(helper), CredentialHelperProtocol.Linq2Db);

			store.TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe($"Credential helper '{helper}' failed with exit code 3: the keyring is locked");
		}

		[Test]
		public void EchoedPasswordIsRedacted()
		{
			// A helper that traces its input on failure: the password line is the first line of standard error.
			var helper = Script(
				"trace-helper",
				"sed -n '/^password=/p' >&2\nexit 1\n",
				"@echo off\r\nfindstr /b \"password=\" 1>&2\r\nexit /b 1\r\n");

			var store = new HelperCredentialStore(CredentialHelperTestSupport.CreateRunner(helper), CredentialHelperProtocol.Linq2Db);

			store.TryStore("a", "u", Secret, out var error).ShouldBeFalse();

			error.ShouldBe($"Credential helper '{helper}' failed with exit code 1: password=***");
		}

		[Test]
		public void LongErrorLineIsCapped()
		{
			var line   = new string('e', 300);
			var helper = Script("long-helper", $"printf '%s\\n' '{line}' >&2\nexit 1\n", $"@echo off\r\necho {line} 1>&2\r\nexit /b 1\r\n");

			var result = CredentialHelperTestSupport.CreateRunner(helper).Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			CredentialHelperProcessRunner.GetFirstLine(result.ErrorOutput).ShouldBe(new string('e', 200) + "...");
		}

		[Test]
		public void LargeErrorOutputDoesNotBlockTheHelper()
		{
			// 200 KB on standard error (far more than a pipe buffer and the kept error text), then a valid answer.
			var helper = Script(
				"noisy-helper",
				"i=0\nwhile [ $i -lt 2048 ]; do printf '%0100d\\n' 0 >&2; i=$((i + 1)); done\nprintf 'username=u\\npassword=p\\n'\n",
				"@echo off\r\nfor /l %%i in (1,1,2048) do @echo eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee 1>&2\r\necho username=u\r\necho password=p\r\n");

			var result = new CredentialHelperProcessRunner(new CredentialHelperSettings(helper, CredentialHelperProtocol.Linq2Db, null), false)
			{
				Timeout = TimeSpan.FromSeconds(30),
			}.Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0);
			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldBe("username=u\npassword=p\n");
		}

		[Test]
		public void FailFastWindowIsPerProgramAndArguments()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Arguments are not supported for the .cmd scripts these tests use on Windows.");

			var helper = Script("args-helper", "[ \"$1\" = slow ] && sleep 30\nprintf 'username=u\\npassword=p\\n'\n", string.Empty);

			var slow = new CredentialHelperProcessRunner(new CredentialHelperSettings(helper, CredentialHelperProtocol.Linq2Db, null) { Arguments = ["slow"] }, false)
			{
				Timeout = TimeSpan.FromSeconds(1),
			};
			var fast = new CredentialHelperProcessRunner(new CredentialHelperSettings(helper, CredentialHelperProtocol.Linq2Db, null) { Arguments = ["fast"] }, false)
			{
				Timeout = TimeSpan.FromSeconds(10),
			};

			slow.Run("get", CredentialHelperTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer within");
			slow.Run("get", CredentialHelperTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer recently");

			var result = fast.Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0);
		}

		[Test]
		public void EndlessOutputIsStoppedAtTheCap()
		{
			var helper = Script(
				"flood-helper",
				"while :; do printf 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\\n'; done\n",
				"@echo off\r\n:loop\r\necho xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\r\ngoto loop\r\n");

			var stopwatch = Stopwatch.StartNew();
			var result    = new CredentialHelperProcessRunner(
				new CredentialHelperSettings(helper, CredentialHelperProtocol.Linq2Db, null), false)
			{
				Timeout = TimeSpan.FromSeconds(60),
			}.Run("list", CredentialHelperTestSupport.Request("protocol=1"));

			stopwatch.Stop();

			result.Failure.ShouldNotBeNull().ShouldContain("wrote more than 1048576 bytes to standard output");
			stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30));
		}

		[Test]
		public void SlowHelperIsKilledAndThenFailsFast()
		{
			var marker = Path.Combine(_directory, "started.txt");
			var helper = Script(
				"slow-helper",
				$"printf x >> '{marker}'\nsleep 30\n",
				$"@echo off\r\necho x>> \"{marker}\"\r\nping -n 31 127.0.0.1 >nul\r\n");

			var clock     = new ManualClock(DateTimeOffset.UtcNow);
			var stopwatch = Stopwatch.StartNew();
			var runner    = new CredentialHelperProcessRunner(new CredentialHelperSettings(helper, CredentialHelperProtocol.Linq2Db, null), false)
			{
				Timeout      = TimeSpan.FromSeconds(2),
				TimeProvider = clock,
			};

			var result = runner.Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			stopwatch.Stop();

			result.Failure.ShouldNotBeNull().ShouldContain("did not answer within 2 seconds");
			stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(20));

			// The fail-fast window: the helper is not started again.
			File.Delete(marker);

			runner.Run("get", CredentialHelperTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer recently");
			File.Exists(marker).ShouldBeFalse();

			// After the window it is started again.
			clock.Advance(TimeSpan.FromSeconds(61));

			runner.Run("get", CredentialHelperTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer within");
			File.Exists(marker).ShouldBeTrue();
		}

		[Test]
		public void InteractiveRunWaitsLongerThanNonInteractive()
		{
			var helper = Script(
				"prompt-helper",
				"sleep 3\nprintf 'username=u\\npassword=p\\n'\n",
				"@echo off\r\nping -n 4 127.0.0.1 >nul\r\necho username=u\r\necho password=p\r\n");

			var settings = new CredentialHelperSettings(helper, CredentialHelperProtocol.Linq2Db, null);

			var interactive = new CredentialHelperProcessRunner(settings, true)
			{
				Timeout            = TimeSpan.FromSeconds(1),
				InteractiveTimeout = TimeSpan.FromSeconds(30),
			}.Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			interactive.Failure.ShouldBeNull();
			interactive.ExitCode.ShouldBe(0);

			var nonInteractive = new CredentialHelperProcessRunner(settings, false)
			{
				Timeout            = TimeSpan.FromSeconds(1),
				InteractiveTimeout = TimeSpan.FromSeconds(30),
			}.Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			nonInteractive.Failure.ShouldNotBeNull().ShouldContain("did not answer within 1 seconds");
		}

		[Test]
		public void MissingHelperIsReported()
		{
			var result = CredentialHelperTestSupport.CreateRunner(Path.Combine(_directory, "missing-helper.exe")).Run("get", []);

			result.Failure.ShouldNotBeNull().ShouldContain("was not found");
		}

		[Test]
		public void BatchHelperInDirectoryWithSpacesAndParentheses()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("cmd.exe quoting.");

			var directory = Directory.CreateDirectory(Path.Combine(_directory, "h (x) y")).FullName;
			var helper    = CredentialHelperTestSupport.WriteScript(directory, "helper & co", string.Empty, "@echo off\r\necho username=%1\r\necho password=p\r\n");

			var result = CredentialHelperTestSupport.CreateRunner(helper).Run("get", CredentialHelperTestSupport.Request("protocol=1"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0, result.ErrorOutput);
			Encoding.UTF8.GetString(result.Output).ShouldStartWith("username=get");
		}

		[Test]
		public void ShellScriptNeedsNoExecuteBit()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh scripts.");

			var helper = Script("noexec-helper", "printf 'username=u\\npassword=p\\n'\n", string.Empty);

			File.GetUnixFileMode(helper).HasFlag(UnixFileMode.UserExecute).ShouldBeFalse();

			CredentialHelperTestSupport.CreateRunner(helper).Run("get", []).ExitCode.ShouldBe(0);
		}

		[Test]
		public async System.Threading.Tasks.Task HelperInitIsRefusedOnWindows()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows behaviour.");

			var environment = new TestCliEnvironment();
			var exitCode    = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(["credentials", "helper", "init", "--backend", "pass", "-o", Path.Combine(_directory, "h.sh")], environment);

			exitCode.ShouldBe(-1);
			environment.ErrorOutput.ShouldContain("Starter helper scripts are POSIX sh scripts and are not generated on Windows.");
			File.Exists(Path.Combine(_directory, "h.sh")).ShouldBeFalse();
		}

		sealed class ManualClock(DateTimeOffset now) : TimeProvider
		{
			DateTimeOffset _now = now;

			public void Advance(TimeSpan delta)
			{
				_now += delta;
			}

			public override DateTimeOffset GetUtcNow()
			{
				return _now;
			}
		}
	}
}
