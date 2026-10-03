using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The starter scripts written by <c>credentials helper init</c>, run by the real client against fake <c>secret-tool</c>
	/// and <c>pass</c> executables that keep their items in files and record every argument vector (POSIX only), plus the
	/// <c>helper init</c> command itself.
	/// </summary>
	[TestFixture]
	public sealed class CredentialHelperTemplateTests
	{
		// Leading/trailing spaces, quotes, $, backticks, a command substitution, '=', non-ASCII.
		const string HostileUser     = " DOMAIN\\o'neil \"x\" ";
		const string HostilePassword = "  p'a\"s$HOME`id`$(id)=é  ";
		const string HostileTarget   = "linq2db/it's a \"test\" $x";

		// A fake secret-tool: items are directories under $FAKE_STATE/items with one file per attribute. "search --all"
		// prints the item header and secret on stdout and the attributes on stderr, like the real tool; a "locked" file
		// makes the keyring locked (no secret lines, lookup/store fail).
		const string FakeSecretTool = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			cmd=$1; shift
			label=''
			if [ "$cmd" = search ] && [ "${1-}" = --all ]; then shift; fi
			case ${1-} in --label=*) label=${1#--label=}; shift ;; esac
			ws=''; wt=''; wu=''; hs=0; ht=0; hu=0
			while [ $# -ge 2 ]; do
				case $1 in
					service) ws=$2; hs=1 ;;
					target)  wt=$2; ht=1 ;;
					user)    wu=$2; hu=1 ;;
				esac
				shift 2
			done
			mkdir -p "$FAKE_STATE/items"
			matches() {
				[ $hs = 0 ] || [ "$(cat "$1/service")" = "$ws" ] || return 1
				[ $ht = 0 ] || [ "$(cat "$1/target")" = "$wt" ] || return 1
				[ $hu = 0 ] || [ "$(cat "$1/user")" = "$wu" ] || return 1
				return 0
			}
			locked() { [ -e "$FAKE_STATE/locked" ]; }
			case $cmd in
				store)
					locked && { echo 'secret-tool: Cannot create an item in a locked collection' >&2; exit 1; }
					secret=$(cat; printf x); secret=${secret%x}
					item=''
					for d in "$FAKE_STATE"/items/*; do [ -d "$d" ] && matches "$d" && item=$d; done
					[ -n "$item" ] || { item=$FAKE_STATE/items/$(date +%s%N); mkdir "$item"; }
					printf '%s' "$ws" > "$item/service"; printf '%s' "$wt" > "$item/target"; printf '%s' "$wu" > "$item/user"
					printf '%s' "$secret" > "$item/secret"; printf '%s' "$label" > "$item/label"
					;;
				lookup)
					locked && { echo 'secret-tool: Cannot get secret of a locked object' >&2; exit 1; }
					for d in "$FAKE_STATE"/items/*; do
						if [ -d "$d" ] && matches "$d"; then cat "$d/secret"; exit 0; fi
					done
					exit 1
					;;
				clear)
					found=1
					for d in "$FAKE_STATE"/items/*; do
						if [ -d "$d" ] && matches "$d"; then rm -r "$d"; found=0; fi
					done
					exit $found
					;;
				search)
					n=0
					for d in "$FAKE_STATE"/items/*; do
						[ -d "$d" ] && matches "$d" || continue
						n=$((n + 1))
						printf '[/%s]\nlabel = %s\n' "$n" "$(cat "$d/label")"
						locked || printf 'secret = %s\n' "$(cat "$d/secret")"
						printf 'attribute.service = %s\nattribute.target = %s\nattribute.user = %s\n' "$(cat "$d/service")" "$(cat "$d/target")" "$(cat "$d/user")" >&2
					done
					;;
			esac
			""";

		// A fake pass: entries are plain files under $PASSWORD_STORE_DIR (no gpg); a "fail" file makes every decryption
		// fail like gpg without a pinentry.
		const string FakePass = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			cmd=$1; shift
			while [ $# -gt 0 ]; do case $1 in -m|-f) shift ;; --) shift; break ;; *) break ;; esac; done
			file=$PASSWORD_STORE_DIR/$1.gpg
			case $cmd in
				insert)
					[ -e "$FAKE_STATE/readonly" ] && { echo 'mkdir: Permission denied' >&2; exit 1; }
					mkdir -p "$(dirname "$file")"; cat > "$file" ;;
				show)
					[ -e "$FAKE_STATE/fail" ] && { echo 'gpg: decryption failed: No pinentry' >&2; exit 2; }
					[ -f "$file" ] || { echo "Error: $1 is not in the password store." >&2; exit 1; }
					cat "$file" ;;
				rm)
					[ -f "$file" ] || { echo "Error: $1 is not in the password store." >&2; exit 1; }
					rm "$file" ;;
			esac
			""";

		string _directory = null!;
		string _state     = null!;
		string _bin       = null!;
		string _store     = null!;

		[SetUp]
		public void SetUp()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("The starter scripts are POSIX sh scripts.");

			_directory = CredentialHelperTestSupport.CreateTempDirectory();
			_state     = Directory.CreateDirectory(Path.Combine(_directory, "state")).FullName;
			_bin       = Directory.CreateDirectory(Path.Combine(_directory, "bin")).FullName;
			_store     = Path.Combine(_directory, "password-store");

			WriteExecutable(Path.Combine(_bin, "secret-tool"), FakeSecretTool);
			WriteExecutable(Path.Combine(_bin, "pass"),        FakePass);
		}

		[TearDown]
		public void TearDown()
		{
			if (_directory != null)
				CredentialHelperTestSupport.DeleteDirectory(_directory);
		}

		static void WriteExecutable(string path, string content)
		{
			File.WriteAllText(path, content.Replace("\r\n", "\n", StringComparison.Ordinal));
			File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}

		string WriteTemplate(string backend)
		{
			var path = Path.Combine(_directory, backend + ".sh");
			CredentialHelperTemplates.TryWrite(path, CredentialHelperTemplates.Get(backend)!, false, out var error).ShouldBeTrue(error);
			return path;
		}

		HelperCredentialStore CreateStore(string backend, bool interactive = false)
		{
			var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				["PATH"]               = _bin + ":" + Environment.GetEnvironmentVariable("PATH"),
				["FAKE_STATE"]         = _state,
				["PASSWORD_STORE_DIR"] = _store,
			};

			var runner = new CredentialHelperProcessRunner(new CredentialHelperSettings(WriteTemplate(backend), CredentialHelperProtocol.Linq2Db, null), interactive, environment);

			return new HelperCredentialStore(runner, CredentialHelperProtocol.Linq2Db);
		}

		string ArgvLog => File.Exists(Path.Combine(_state, "argv.log")) ? File.ReadAllText(Path.Combine(_state, "argv.log")) : string.Empty;

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void RoundTripWithHostileValues(string backend)
		{
			var store = CreateStore(backend);

			store.TryStore(HostileTarget.Substring("linq2db/".Length), HostileUser, HostilePassword, out var error).ShouldBeTrue(error);

			store.TryRead(HostileTarget, out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe(HostileUser);
			password.ShouldBe(HostilePassword);

			store.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile(HostileTarget.Substring("linq2db/".Length), HostileUser)]);

			store.TryRemove(HostileTarget.Substring("linq2db/".Length), out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeTrue();

			store.TryRemove(HostileTarget.Substring("linq2db/".Length), out removed, out error).ShouldBeTrue(error);
			removed.ShouldBeFalse();

			store.TryRead(HostileTarget, out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credential helper");

			ArgvLog.ShouldNotContain("p'a\"s");
			ArgvLog.ShouldNotContain("$(id)=é");
		}

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void StoreReplacesWithDifferentUser(string backend)
		{
			var store = CreateStore(backend);

			store.TryStore("a", "first",  "p1", out var error).ShouldBeTrue(error);
			store.TryStore("a", "second", "p2", out error).ShouldBeTrue(error);

			store.TryRead("linq2db/a", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("second");
			password.ShouldBe("p2");

			store.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a", "second")]);
		}

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void EmptyPasswordRoundTrips(string backend)
		{
			var store = CreateStore(backend);

			store.TryStore("empty", "u", string.Empty, out var error).ShouldBeTrue(error);

			store.TryRead("linq2db/empty", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("u");
			password.ShouldBe(string.Empty);

			store.TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(1);
		}

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void ClearRemovesAll(string backend)
		{
			var store = CreateStore(backend);

			store.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			store.TryStore("b", "u", "p", out error).ShouldBeTrue(error);

			store.TryClear(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(2);

			store.TryGetCount(out count, out error).ShouldBeTrue(error);
			count.ShouldBe(0);
		}

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void PasswordLookingLikeUserMetadataStaysThePassword(string backend)
		{
			// pass keeps the password on line 1 and "user: <name>" on line 2; a password of that shape must not be read as
			// the user name.
			var store = CreateStore(backend);

			store.TryStore("a", "app=reader", "user: TOPSECRET", out var error).ShouldBeTrue(error);

			store.TryRead("linq2db/a", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("app=reader");
			password.ShouldBe("user: TOPSECRET");

			store.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a", "app=reader")]);
		}

		[Test]
		public void SecretToolLockedKeyringIsAFailureNotNotFound()
		{
			var store = CreateStore("secret-tool");

			store.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.WriteAllText(Path.Combine(_state, "locked"), string.Empty);

			store.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: the keyring is locked");

			store.TryRemove("a", out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the keyring is locked");

			store.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the keyring is locked");

			store.TryStore("b", "u", "p", out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1");
		}

		[Test]
		public void PassDecryptionFailureIsAFailureNotNotFound()
		{
			var store = CreateStore("pass");

			store.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.WriteAllText(Path.Combine(_state, "fail"), string.Empty);

			store.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			// The backend's own message comes first on standard error, before the script's summary line.
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: gpg: decryption failed: No pinentry");

			store.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1");
		}

		[Test]
		public void PassStoreFailure()
		{
			var store = CreateStore("pass");

			File.WriteAllText(Path.Combine(_state, "readonly"), string.Empty);

			store.TryStore("a", "u", "secret-value", out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: mkdir: Permission denied");
			error.ShouldNotBeNull().ShouldNotContain("secret-value");
		}

		[Test]
		public void PassNonInteractiveDisablesPinentry()
		{
			// The pass template adds --pinentry-mode=error to gpg when nobody can answer a prompt.
			var script = WriteTemplate("pass");
			var text   = File.ReadAllText(script);

			text.ShouldContain("if [ \"${LINQ2DB_CREDENTIAL_INTERACTIVE-1}\" = 0 ]; then");
			text.ShouldContain("--pinentry-mode=error");
		}

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void UnsupportedVerbAndProtocol(string backend)
		{
			var runner = new CredentialHelperProcessRunner(
				new CredentialHelperSettings(WriteTemplate(backend), CredentialHelperProtocol.Linq2Db, null),
				false,
				new Dictionary<string, string?>(StringComparer.Ordinal) { ["PATH"] = _bin + ":/usr/bin:/bin", ["FAKE_STATE"] = _state, ["PASSWORD_STORE_DIR"] = _store });

			Encoding.UTF8.GetString(runner.Run("rename", CredentialHelperTestSupport.Request("protocol=1")).Output).ShouldBe("unsupported=verb\n");
			Encoding.UTF8.GetString(runner.Run("get",    CredentialHelperTestSupport.Request("protocol=2")).Output).ShouldBe("unsupported=protocol\n");
		}

		[TestCase("secret-tool")]
		[TestCase("pass")]
		public void ShellCheckIsClean(string backend)
		{
			var shellcheck = FindOnPath("shellcheck");

			if (shellcheck == null)
			{
				if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null || Environment.GetEnvironmentVariable("TF_BUILD") != null)
					Assert.Fail("shellcheck is required on CI to check the starter credential helper scripts.");

				Assert.Ignore("shellcheck is not installed.");
			}

			var startInfo = new ProcessStartInfo(shellcheck)
			{
				UseShellExecute        = false,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
			};

			startInfo.ArgumentList.Add("-s");
			startInfo.ArgumentList.Add("sh");
			startInfo.ArgumentList.Add(WriteTemplate(backend));

			using var process = Process.Start(startInfo)!;
			var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
			process.WaitForExit();

			process.ExitCode.ShouldBe(0, output);
		}

		static string? FindOnPath(string name)
		{
			foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':'))
			{
				if (directory.Length > 0 && File.Exists(Path.Combine(directory, name)))
					return Path.Combine(directory, name);
			}

			return null;
		}

		// credentials helper init

		static async Task<(int ExitCode, string Output, string Error)> RunCli(TestCliEnvironment environment, params string[] args)
		{
			var exitCode = await new global::LinqToDB.CommandLine.LinqToDBCliController().Execute(args, environment);
			return (exitCode, environment.Output, environment.ErrorOutput);
		}

		[Test]
		public async Task InitWritesOwnerOnlyScriptUnderXdgConfigHome()
		{
			var environment = new TestCliEnvironment();
			var configHome  = Path.Combine(_directory, "xdg");
			environment.EnvironmentVariables["XDG_CONFIG_HOME"] = configHome;
			environment.EnvironmentVariables["PATH"]            = _bin;

			var (exitCode, output, error) = await RunCli(environment, "credentials", "helper", "init", "--backend", "secret-tool");

			var script = Path.Combine(configHome, "linq2db", "helpers", "secret-tool.sh");

			exitCode.ShouldBe(0, error);
			output.ShouldContain($"Created credential helper script '{script}' for backend 'secret-tool'.");
			output.ShouldContain($"Use it with '--credential-helper {script}'");
			error.ShouldNotBeNull().ShouldNotContain("Warning");
			File.ReadAllText(script).ShouldBe(CredentialHelperTemplates.Get("secret-tool"));
			File.GetUnixFileMode(script).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			File.GetUnixFileMode(Path.GetDirectoryName(script)!).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}

		[Test]
		public async Task InitDefaultsToHomeConfigAndWarnsAboutMissingBackend()
		{
			var environment = new TestCliEnvironment();
			environment.EnvironmentVariables["HOME"] = _directory;
			environment.EnvironmentVariables["PATH"] = Path.Combine(_directory, "empty-bin");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "helper", "init", "--backend", "pass");

			exitCode.ShouldBe(0, error);
			File.Exists(Path.Combine(_directory, ".config", "linq2db", "helpers", "pass.sh")).ShouldBeTrue();
			error.ShouldNotBeNull().ShouldContain("Warning: 'pass' was not found on PATH.");
		}

		[Test]
		public async Task InitRefusesToOverwriteWithoutForce()
		{
			var script = Path.Combine(_directory, "custom", "h.sh");

			var (exitCode, _, error) = await RunCli(new TestCliEnvironment(), "credentials", "helper", "init", "--backend", "pass", "-o", script);
			exitCode.ShouldBe(0, error);

			File.WriteAllText(script, "edited");

			(exitCode, _, error) = await RunCli(new TestCliEnvironment(), "credentials", "helper", "init", "--backend", "pass", "-o", script);
			exitCode.ShouldBe(-3);
			error.ShouldNotBeNull().ShouldContain($"Credential helper script '{script}' already exists. Use '--force' to replace it.");
			File.ReadAllText(script).ShouldBe("edited");

			(exitCode, _, error) = await RunCli(new TestCliEnvironment(), "credentials", "helper", "init", "--backend", "pass", "-o", script, "--force");
			exitCode.ShouldBe(0, error);
			File.ReadAllText(script).ShouldBe(CredentialHelperTemplates.Get("pass"));
			File.GetUnixFileMode(script).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}

		[Test]
		public async Task InitRecordsHelperInConfigDefaultProfile()
		{
			var environment = new TestCliEnvironment();
			var script      = Path.Combine(_directory, "h.sh");

			environment.Files.Add("cfg.json", """
				{
					"default": { "maxRows": 10 },
					"dev": { "provider": "SQLite", "connectionString": "Data Source=:memory:" }
				}
				""");

			var (exitCode, output, error) = await RunCli(environment, "credentials", "helper", "init", "--backend", "secret-tool", "-o", script, "--config", "cfg.json");

			exitCode.ShouldBe(0, error);
			output.ShouldContain("Recorded it as 'credentialHelper' in profile 'default' of 'cfg.json'.");

			var json = System.Text.Json.Nodes.JsonNode.Parse(environment.Files["cfg.json"])!;
			json["default"]!["credentialHelper"]!.GetValue<string>().ShouldBe(script);
			json["default"]!["maxRows"]!.GetValue<int>().ShouldBe(10);
			json["dev"]!["provider"]!.GetValue<string>().ShouldBe("SQLite");

			// A different helper is not replaced silently.
			(exitCode, _, error) = await RunCli(environment, "credentials", "helper", "init", "--backend", "pass", "-o", Path.Combine(_directory, "p.sh"), "--config", "cfg.json");

			exitCode.ShouldBe(-3);
			error.ShouldNotBeNull().ShouldContain("already sets 'credentialHelper'. Use '--force' to replace it.");
			File.Exists(Path.Combine(_directory, "p.sh")).ShouldBeFalse();
		}

		[Test]
		public async Task InitCreatesConfigWhenMissing()
		{
			var environment = new TestCliEnvironment();
			var script      = Path.Combine(_directory, "h.sh");

			var (exitCode, _, error) = await RunCli(environment, "credentials", "helper", "init", "--backend", "pass", "-o", script, "--config", "new.json");

			exitCode.ShouldBe(0, error);
			System.Text.Json.Nodes.JsonNode.Parse(environment.Files["new.json"])!["default"]!["credentialHelper"]!.GetValue<string>().ShouldBe(script);
		}

		[TestCase(new[] { "credentials", "helper", "init" },                                          "Option '--backend' must be specified",              TestName = "InitRequiresBackend")]
		[TestCase(new[] { "credentials", "helper", "init", "--backend", "pass", "--profile", "x" },   "accepts only '--backend', '--output', '--config', and '--force'", TestName = "InitRejectsProfile")]
		[TestCase(new[] { "credentials", "helper", "remove" },                                        "Unknown credentials helper operation 'remove'",     TestName = "HelperUnknownOperation")]
		[TestCase(new[] { "credentials", "list", "--backend", "pass" },                               "supported only by credentials helper init",         TestName = "BackendOnlyForInit")]
		public async Task InitArgumentValidation(string[] args, string message)
		{
			var (exitCode, _, error) = await RunCli(new TestCliEnvironment(), args);

			exitCode.ShouldBe(-1);
			error.ShouldNotBeNull().ShouldContain(message);
		}
	}
}
