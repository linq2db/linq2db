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
	/// The process runner against real processes (sh scripts on POSIX, cmd scripts on Windows): arguments, standard
	/// input, environment, exit codes, output caps, timeouts and the fail-fast window.
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliProcessTests
	{
		const string Secret = "top secret";

		string _directory = null!;

		[SetUp]
		public void SetUp()
		{
			_directory = CredentialsCliTestSupport.CreateTempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			KillChildren();
			CredentialsCliTestSupport.DeleteDirectory(_directory);
		}

		string Script(string name, string sh, string cmd)
		{
			return CredentialsCliTestSupport.WriteScript(_directory, name, sh, cmd);
		}

		[Test]
		public void ProgramReceivesVerbStdinAndEnvironment()
		{
			var record = Path.Combine(_directory, "record.txt");
			var helper = Script(
				"echo-helper",
				$"{{ printf 'argc=%s\\n' \"$#\"; printf 'arg=%s\\n' \"$1\"; printf 'interactive=%s\\n' \"$LINQ2DB_CREDENTIAL_INTERACTIVE\"; printf 'nologo=%s\\n' \"$DOTNET_NOLOGO\"; cat; }} > '{record}'\nprintf 'protocol=1\\nstatus=ok\\nusername=u\\npassword=p\\n'\n",
				$"@echo off\r\n(echo arg=%1& echo extra=%2& echo interactive=%LINQ2DB_CREDENTIAL_INTERACTIVE%& echo nologo=%DOTNET_NOLOGO%& more) > \"{record}\"\r\necho protocol=1\r\necho status=ok\r\necho username=u\r\necho password=p\r\n");

			var result = CredentialsCliTestSupport.CreateRunner(helper).Run("get", CredentialsCliTestSupport.Request("protocol=1", "verb=get", "target=linq2db/a"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0);
			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldBe("protocol=1\nstatus=ok\nusername=u\npassword=p\n");

			var recorded = File.ReadAllText(record).Replace("\r", string.Empty, StringComparison.Ordinal);

			recorded.ShouldContain("arg=get\n");
			recorded.ShouldContain("interactive=0\n");
			recorded.ShouldContain("nologo=1\n");
			recorded.ShouldContain("protocol=1\nverb=get\ntarget=linq2db/a\n");

			if (OperatingSystem.IsWindows())
				recorded.ShouldContain("extra=\n");
			else
				recorded.ShouldContain("argc=1\n");
		}

		[TestCase(false, TestName = "ArgumentStringIsSplitByTheWindowsRulesForAScript")]
		[TestCase(true,  TestName = "ArgumentStringIsSplitByTheWindowsRulesForAnExecutable")]
		public void ArgumentStringIsSplitByTheWindowsRules(bool executable)
		{
			// The configured rest of the command line reaches the program split like a Windows command line (MSVCRT rules)
			// on every OS: whitespace separates, "..." groups, \" is a quote; the verb comes last. On Unix .NET splits the
			// string before it starts the program; on Windows an executable splits its own command line. cmd.exe, which
			// runs batch files, has no backslash escape: see BatchProgramGetsGroupedArguments.
			if (!executable && OperatingSystem.IsWindows())
				Assert.Ignore("Batch files: see BatchProgramGetsGroupedArguments.");

			var program = executable
				? BuildArgumentEchoProgram()
				: Script("argv-helper", "for a in \"$@\"; do printf '[%s]\\n' \"$a\"; done\n", string.Empty);

			var result = CredentialsCliTestSupport.CreateRunner(program, arguments: "\"a b\" c\\\"d e").Run("store", CredentialsCliTestSupport.Request("protocol=1", "verb=store"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0, result.ErrorOutput);
			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldBe("[a b]\n[c\"d]\n[e]\n[store]\n");
		}

		[Test]
		public void BatchProgramGetsGroupedArguments()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("cmd.exe batch files.");

			var helper = Script("argv-helper", string.Empty, "@echo off\r\necho [%~1]\r\necho [%~2]\r\necho [%~3]\r\n");

			var result = CredentialsCliTestSupport.CreateRunner(helper, arguments: "\"a b\" c").Run("store", CredentialsCliTestSupport.Request("protocol=1", "verb=store"));

			result.ExitCode.ShouldBe(0, result.ErrorOutput);
			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldBe("[a b]\n[c]\n[store]\n");
		}

		/// <summary>Builds a small program that prints each of its arguments as <c>[argument]</c> on a line.</summary>
		string BuildArgumentEchoProgram()
		{
			var source = Path.Combine(_directory, "argecho.cs");
			var output = Path.Combine(_directory, "argecho");

			File.WriteAllText(source, "#:property PublishAot=false\nforeach (var argument in args)\n\tSystem.Console.Out.Write(\"[\" + argument + \"]\\n\");\n");

			var build = new ProcessStartInfo(CredentialsCliTestSupport.DotnetCommand)
			{
				UseShellExecute        = false,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
			};

			foreach (var argument in new[] { "build", source, "-o", output })
				build.ArgumentList.Add(argument);

			build.Environment["DOTNET_NOLOGO"] = "1";

			using var process = Process.Start(build)!;

			var buildOutput = process.StandardOutput.ReadToEndAsync();
			var buildErrors = process.StandardError.ReadToEndAsync();

			process.WaitForExit(TimeSpan.FromSeconds(300)).ShouldBeTrue("dotnet build of the argument echo program timed out");
			process.ExitCode.ShouldBe(0, buildOutput.Result + buildErrors.Result);

			return Path.Combine(output, OperatingSystem.IsWindows() ? "argecho.exe" : "argecho");
		}

		[Test]
		public void InteractiveFlagIsSet()
		{
			var helper = Script(
				"flag-helper",
				"printf 'protocol=1\\nstatus=ok\\nusername=%s\\npassword=p\\n' \"$LINQ2DB_CREDENTIAL_INTERACTIVE\"\n",
				"@echo off\r\necho protocol=1\r\necho status=ok\r\necho username=%LINQ2DB_CREDENTIAL_INTERACTIVE%\r\necho password=p\r\n");

			var result = CredentialsCliTestSupport.CreateRunner(helper, interactive: true).Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldContain("username=1\n");
		}

		[Test]
		public void NonZeroExitReturnsErrorOutput()
		{
			var helper = Script(
				"fail-helper",
				"printf 'username=leak\\n'\nprintf '\\nthe keyring is locked\\nsecond\\n' >&2\nexit 3\n",
				"@echo off\r\necho username=leak\r\necho the keyring is locked 1>&2\r\nexit /b 3\r\n");

			var store = new CredentialsCliStore(CredentialsCliTestSupport.CreateRunner(helper));

			store.TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();

			error.ShouldBe($"Credentials CLI '{helper}' failed with exit code 3: the keyring is locked");
		}

		[Test]
		public void EchoedPasswordIsRedacted()
		{
			// A helper that traces its input on failure: the password line is the first line of standard error.
			var helper = Script(
				"trace-helper",
				"sed -n '/^password=/p' >&2\nexit 1\n",
				"@echo off\r\nfindstr /b \"password=\" 1>&2\r\nexit /b 1\r\n");

			var store = new CredentialsCliStore(CredentialsCliTestSupport.CreateRunner(helper));

			store.TryStore("a", "u", Secret, out var error).ShouldBeFalse();

			error.ShouldBe($"Credentials CLI '{helper}' failed with exit code 1: password=***");
		}

		[Test]
		public void PasswordCrossingTheErrorCapIsRedacted()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh helper.");

			// 4090 newlines, then the password: the 4096-byte cap cuts it after "TOPSEC".
			var helper = Script("cut-helper", "i=0\nwhile [ $i -lt 4090 ]; do printf '\\n' >&2; i=$((i + 1)); done\nsed -n 's/^password=//p' >&2\nexit 1\n", string.Empty);
			var store  = new CredentialsCliStore(CredentialsCliTestSupport.CreateRunner(helper));

			store.TryStore("a", "u", "TOPSECRETVALUE", out var error).ShouldBeFalse();

			error.ShouldBe($"Credentials CLI '{helper}' failed with exit code 1: ***");
		}

		[Test]
		public void PasswordInLongFirstErrorLineIsRedacted()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh helper.");

			// A 5 KB password echoed after a short prefix: the kept 4 KB hold only its start, and the message shows the
			// beginning of that line.
			var helper = Script("wide-helper", "printf 'echo: ' >&2\nsed -n 's/^password=//p' >&2\nexit 1\n", string.Empty);
			var store  = new CredentialsCliStore(CredentialsCliTestSupport.CreateRunner(helper));

			store.TryStore("a", "u", "TOPSECRET" + new string('v', 5000), out var error).ShouldBeFalse();

			error.ShouldBe($"Credentials CLI '{helper}' failed with exit code 1: echo: ***");
		}

		[Test]
		public void BackgroundProcessHoldingInputDoesNotHang()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh helper.");

			// The helper starts a child that keeps standard input open without reading it (an asynchronous command gets
			// /dev/null as standard input unless it is handed a copy, hence fd 3), answers, and exits; the request
			// is larger than a pipe buffer, so writing it can never complete.
			var pidFile = Path.Combine(_directory, "child.pid");
			var helper  = Script(
				"stdin-holder",
				$"exec 3<&0\nsleep 60 <&3 >/dev/null 2>&1 &\nexec 3<&-\necho $! > '{pidFile}'\nprintf 'username=u\\npassword=p\\n'\n",
				string.Empty);

			try
			{
				var stopwatch = Stopwatch.StartNew();
				var result    = new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(helper), false)
				{
					Timeout = TimeSpan.FromSeconds(30),
				}.Run("store", CredentialsCliTestSupport.Request("protocol=1", "target=linq2db/a", "username=u", "password=" + new string('p', 1024 * 1024)));

				stopwatch.Stop();

				result.Failure.ShouldNotBeNull().ShouldContain("without reading its whole request");
				stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(20));
			}
			finally
			{
				RecordChild(pidFile);
			}
		}

		[Test]
		public void BackgroundProcessHoldingOutputTripsTheFailFastWindow()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh helper.");

			var pidFile = Path.Combine(_directory, "child.pid");
			var helper  = Script("stdout-holder", $"sleep 60 </dev/null 2>/dev/null &\necho $! > '{pidFile}'\nprintf 'username=u\\npassword=p\\n'\n", string.Empty);
			var runner  = CredentialsCliTestSupport.CreateRunner(helper);

			try
			{
				runner.Run("get", CredentialsCliTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("left a background process holding its output open");
				RecordChild(pidFile);
				File.Delete(pidFile);

				runner.Run("get", CredentialsCliTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer recently");
				File.Exists(pidFile).ShouldBeFalse();
			}
			finally
			{
				RecordChild(pidFile);
			}
		}

		/// <summary>Background processes a test helper left behind (no longer in the helper's process tree).</summary>
		readonly List<int> _children = [];

		void RecordChild(string pidFile)
		{
			if (File.Exists(pidFile) && int.TryParse(File.ReadAllText(pidFile).Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var pid))
				_children.Add(pid);
		}

		void KillChildren()
		{
			foreach (var child in _children)
			{
				try
				{
					using var process = Process.GetProcessById(child);
					process.Kill();
				}
				catch (ArgumentException)
				{
				}
				catch (InvalidOperationException)
				{
				}
			}

			_children.Clear();
		}

		[Test]
		public void LongErrorLineIsCapped()
		{
			var line   = new string('e', 300);
			var helper = Script("long-helper", $"printf '%s\\n' '{line}' >&2\nexit 1\n", $"@echo off\r\necho {line} 1>&2\r\nexit /b 1\r\n");

			var result = CredentialsCliTestSupport.CreateRunner(helper).Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			CredentialsCliProcessRunner.GetFirstLine(result.ErrorOutput).ShouldBe(new string('e', 200) + "...");
		}

		[Test]
		public void LargeErrorOutputDoesNotBlockTheHelper()
		{
			// 200 KB on standard error (far more than a pipe buffer and the kept error text), then a valid answer.
			var helper = Script(
				"noisy-helper",
				"i=0\nwhile [ $i -lt 2048 ]; do printf '%0100d\\n' 0 >&2; i=$((i + 1)); done\nprintf 'username=u\\npassword=p\\n'\n",
				"@echo off\r\nfor /l %%i in (1,1,2048) do @echo eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee 1>&2\r\necho username=u\r\necho password=p\r\n");

			var result = new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(helper), false)
			{
				Timeout = TimeSpan.FromSeconds(30),
			}.Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0);
			Encoding.UTF8.GetString(result.Output).Replace("\r", string.Empty, StringComparison.Ordinal).ShouldBe("username=u\npassword=p\n");
		}

		[Test]
		public void FailFastWindowIsPerProgramAndArguments()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh helper.");

			var helper = Script("args-helper", "[ \"$1\" = slow ] && sleep 30\nprintf 'username=u\\npassword=p\\n'\n", string.Empty);

			var slow = new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(helper, "slow"), false)
			{
				Timeout = TimeSpan.FromSeconds(1),
			};
			var fast = new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(helper, "fast"), false)
			{
				Timeout = TimeSpan.FromSeconds(10),
			};

			slow.Run("get", CredentialsCliTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer within");
			slow.Run("get", CredentialsCliTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer recently");

			var result = fast.Run("get", CredentialsCliTestSupport.Request("protocol=1"));

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
			var result    = new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(helper), false)
			{
				Timeout = TimeSpan.FromSeconds(60),
			}.Run("list", CredentialsCliTestSupport.Request("protocol=1"));

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
			var runner    = new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(helper), false)
			{
				Timeout      = TimeSpan.FromSeconds(2),
				TimeProvider = clock,
			};

			var result = runner.Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			stopwatch.Stop();

			result.Failure.ShouldNotBeNull().ShouldContain("did not answer within 2 seconds");
			stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(20));

			// The fail-fast window: the helper is not started again.
			File.Delete(marker);

			runner.Run("get", CredentialsCliTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer recently");
			File.Exists(marker).ShouldBeFalse();

			// After the window it is started again.
			clock.Advance(TimeSpan.FromSeconds(61));

			runner.Run("get", CredentialsCliTestSupport.Request("protocol=1")).Failure.ShouldNotBeNull().ShouldContain("did not answer within");
			File.Exists(marker).ShouldBeTrue();
		}

		[Test]
		public void InteractiveRunWaitsLongerThanNonInteractive()
		{
			var helper = Script(
				"prompt-helper",
				"sleep 3\nprintf 'username=u\\npassword=p\\n'\n",
				"@echo off\r\nping -n 4 127.0.0.1 >nul\r\necho username=u\r\necho password=p\r\n");

			var settings = CredentialsCliTestSupport.Settings(helper);

			var interactive = new CredentialsCliProcessRunner(settings, true)
			{
				Timeout            = TimeSpan.FromSeconds(1),
				InteractiveTimeout = TimeSpan.FromSeconds(30),
			}.Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			interactive.Failure.ShouldBeNull();
			interactive.ExitCode.ShouldBe(0);

			var nonInteractive = new CredentialsCliProcessRunner(settings, false)
			{
				Timeout            = TimeSpan.FromSeconds(1),
				InteractiveTimeout = TimeSpan.FromSeconds(30),
			}.Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			nonInteractive.Failure.ShouldNotBeNull().ShouldContain("did not answer within 1 seconds");
		}

		[Test]
		public void MissingHelperIsReported()
		{
			var result = CredentialsCliTestSupport.CreateRunner(Path.Combine(_directory, "missing-helper.exe")).Run("get", []);

			result.Failure.ShouldNotBeNull().ShouldContain("was not found");
		}

		[Test]
		public void BatchProgramInDirectoryWithSpacesAndParenthesesGetsItsArguments()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("cmd.exe quoting.");

			var directory = Directory.CreateDirectory(Path.Combine(_directory, "h (x) y")).FullName;
			var helper    = CredentialsCliTestSupport.WriteScript(directory, "helper & co", string.Empty, "@echo off\r\necho protocol=1\r\necho status=ok\r\necho username=%~1-%2\r\necho password=p\r\n");

			var result = CredentialsCliTestSupport.CreateRunner(helper, arguments: "\"--vault x\"").Run("get", CredentialsCliTestSupport.Request("protocol=1"));

			result.Failure.ShouldBeNull();
			result.ExitCode.ShouldBe(0, result.ErrorOutput);
			Encoding.UTF8.GetString(result.Output).ShouldContain("username=--vault x-get");
		}

		[Test]
		public void ScriptWithoutExecuteBitIsExplained()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh scripts.");

			var helper = Script("noexec-helper", "printf 'protocol=1\\nstatus=ok\\n'\n", string.Empty);
			File.SetUnixFileMode(helper, UnixFileMode.UserRead | UnixFileMode.UserWrite);

			var result = CredentialsCliTestSupport.CreateRunner(helper).Run("get", []);

			result.Failure.ShouldNotBeNull().ShouldContain("A script needs the execute bit (chmod u+x) and a '#!' line");
		}

		[Test]
		public void ScriptWithoutShebangIsExplained()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX sh scripts.");

			var helper = Path.Combine(_directory, "noshebang.sh");
			File.WriteAllText(helper, "printf 'protocol=1\\nstatus=ok\\n'\n");
			File.SetUnixFileMode(helper, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

			var result = CredentialsCliTestSupport.CreateRunner(helper).Run("get", []);

			result.Failure.ShouldNotBeNull().ShouldContain("'#!' line");
		}

		[Test]
		public void PowerShellScriptRoundTripsNonAsciiPassword()
		{
			// The documented way to run a PowerShell script: pwsh -NoProfile -File <file> [arguments]; the script sets UTF-8
			// on both standard streams itself.
			var pwsh = FindOnPath(OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh");

			if (pwsh == null)
			{
				if (OperatingSystem.IsWindows() && (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null || Environment.GetEnvironmentVariable("TF_BUILD") != null))
					Assert.Fail("pwsh is required on Windows CI for the PowerShell credentials CLI row.");

				Assert.Ignore("pwsh is not installed.");
			}

			var directory = Directory.CreateDirectory(Path.Combine(_directory, "ps dir")).FullName;
			var script    = Path.Combine(directory, "store.ps1");
			var file      = Path.Combine(directory, "entries.json");

			File.WriteAllText(script, PowerShellCredentialsCli, new UTF8Encoding(false));

			var store = new CredentialsCliStore(new CredentialsCliProcessRunner(
				CredentialsCliTestSupport.Settings("pwsh", $"-NoProfile -File \"{script}\" -Store \"{file}\""),
				false)
			{
				Timeout = TimeSpan.FromSeconds(60),
			});

			const string password = "pä ss wörd ✓ 密码";

			store.TryStore("ps", "üser", password, out var error).ShouldBeTrue(error);
			store.TryRead("linq2db/ps", out var user, out var readPassword, out error).ShouldBeTrue(error);

			user.        ShouldBe("üser");
			readPassword.ShouldBe(password);

			store.TryRead("linq2db/other", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credentials CLI");
		}

		/// <summary>A PowerShell credentials CLI keeping entries in the JSON file named by its -Store argument.</summary>
		const string PowerShellCredentialsCli = """
			param([string] $Store, [string] $Verb)
			$utf8 = [System.Text.UTF8Encoding]::new($false)
			[Console]::InputEncoding  = $utf8
			[Console]::OutputEncoding = $utf8
			$request = @{}
			while ($null -ne ($line = [Console]::In.ReadLine())) {
				$i = $line.IndexOf('=')
				if ($i -gt 0) { $request[$line.Substring(0, $i)] = $line.Substring($i + 1) }
			}
			$entries = @{}
			if (Test-Path -LiteralPath $Store) {
				(Get-Content -LiteralPath $Store -Raw -Encoding utf8 | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $entries[$_.Name] = $_.Value }
			}
			$out = "protocol=1`n"
			switch ($request['verb']) {
				'store' {
					$entries[$request['target']] = @{ user = $request['username']; password = $request['password'] }
					$entries | ConvertTo-Json | Set-Content -LiteralPath $Store -Encoding utf8
					$out += "status=ok`n"
				}
				'get' {
					$entry = $entries[$request['target']]
					if ($null -eq $entry) { $out += "status=not-found`n" }
					else { $out += "status=ok`nusername=$($entry.user)`npassword=$($entry.password)`n" }
				}
				default { $out += "status=unsupported`n" }
			}
			[Console]::Out.Write($out)
			""";

		[Test]
		public void ExecutableInDirectoryWithSpaces()
		{
			// The example built once into a directory with spaces, run as an executable (an .exe on Windows).
			var output = Path.Combine(_directory, "built cli");
			var build  = new ProcessStartInfo(CredentialsCliTestSupport.DotnetCommand)
			{
				UseShellExecute        = false,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
			};

			foreach (var argument in new[] { "build", CredentialsCliTestSupport.GetSecretHelperSource(), "-o", output })
				build.ArgumentList.Add(argument);

			build.Environment["DOTNET_NOLOGO"] = "1";

			using (var process = Process.Start(build)!)
			{
				var buildOutput = process.StandardOutput.ReadToEndAsync();
				var buildErrors = process.StandardError.ReadToEndAsync();

				process.WaitForExit(TimeSpan.FromSeconds(300)).ShouldBeTrue("dotnet build of the example timed out");
				process.ExitCode.ShouldBe(0, buildOutput.Result + buildErrors.Result);
			}

			var executable = Path.Combine(output, OperatingSystem.IsWindows() ? "secrethelper.exe" : "secrethelper");
			var file       = Path.Combine(_directory, "built.json");

			CredentialsCliSettings.TryCreate($"\"{executable}\"", null, out var settings, out var error).ShouldBeTrue(error);

			var store = new CredentialsCliStore(new CredentialsCliProcessRunner(settings, false, new Dictionary<string, string?> { ["SECRETHELPER_FILE"] = file }));

			store.TryStore("built", "u", "p w", out error).ShouldBeTrue(error);
			store.TryRead("linq2db/built", out var user, out var password, out error).ShouldBeTrue(error);

			user.    ShouldBe("u");
			password.ShouldBe("p w");
		}

		static string? FindOnPath(string name)
		{
			foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
			{
				if (directory.Length > 0 && File.Exists(Path.Combine(directory, name)))
					return Path.Combine(directory, name);
			}

			return null;
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
