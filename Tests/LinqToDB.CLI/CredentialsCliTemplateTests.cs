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
	/// The scripts written by <c>credentials cli init --store keyring|gpg</c>, run by the real client against fake
	/// <c>secret-tool</c> and <c>pass</c> executables that keep their items in files and record every argument vector
	/// (POSIX only).
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliTemplateTests
	{
		// Leading/trailing spaces, quotes, $, backticks, a command substitution, '=', non-ASCII.
		const string HostileUser     = " DOMAIN\\o'neil \"x\" ";
		const string HostilePassword = "  p'a\"s$HOME`id`$(id)=é  ";
		const string HostileTarget   = "linq2db/it's a \"test\" $x";

		// A fake secret-tool: items are directories under $FAKE_STATE/items with one file per attribute. "search --all"
		// prints the item header and secret on stdout and the attributes on stderr, like the real tool; a "locked" file
		// makes the keyring locked (no secret lines, lookup/store fail). "search --unlock" unlocks it, as a user answering
		// the unlock prompt does.
		const string FakeSecretTool = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			cmd=$1; shift
			label=''
			if [ "$cmd" = search ] && [ "${1-}" = --all ]; then shift; fi
			if [ "$cmd" = search ] && [ "${1-}" = --unlock ]; then shift; rm -f "$FAKE_STATE/locked"; fi
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

			_directory = CredentialsCliTestSupport.CreateTempDirectory();
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
				CredentialsCliTestSupport.DeleteDirectory(_directory);
		}

		static void WriteExecutable(string path, string content)
		{
			File.WriteAllText(path, content.Replace("\r\n", "\n", StringComparison.Ordinal));
			File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}

		string WriteTemplate(string store)
		{
			var path = Path.Combine(_directory, $"credentials-{store}.sh");
			WriteExecutable(path, CredentialsCliTemplates.Get(store)!);
			return path;
		}

		CredentialsCliProcessRunner CreateRunner(string store, bool interactive = false)
		{
			var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				["PATH"]               = _bin + ":" + Environment.GetEnvironmentVariable("PATH"),
				["FAKE_STATE"]         = _state,
				["PASSWORD_STORE_DIR"] = _store,
			};

			return new CredentialsCliProcessRunner(CredentialsCliTestSupport.Settings(WriteTemplate(store)), interactive, environment);
		}

		CredentialsCliStore CreateStore(string store, bool interactive = false)
		{
			return new CredentialsCliStore(CreateRunner(store, interactive));
		}

		string ArgvLog => File.Exists(Path.Combine(_state, "argv.log")) ? File.ReadAllText(Path.Combine(_state, "argv.log")) : string.Empty;

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void RoundTripWithHostileValues(string store)
		{
			var cli = CreateStore(store);

			cli.TryStore(HostileTarget.Substring("linq2db/".Length), HostileUser, HostilePassword, out var error).ShouldBeTrue(error);

			cli.TryRead(HostileTarget, out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe(HostileUser);
			password.ShouldBe(HostilePassword);

			cli.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile(HostileTarget.Substring("linq2db/".Length), HostileUser)]);

			cli.TryRemove(HostileTarget.Substring("linq2db/".Length), out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeTrue();

			cli.TryRemove(HostileTarget.Substring("linq2db/".Length), out removed, out error).ShouldBeTrue(error);
			removed.ShouldBeFalse();

			cli.TryRead(HostileTarget, out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credentials CLI");

			ArgvLog.ShouldNotContain("p'a\"s");
			ArgvLog.ShouldNotContain("$(id)=é");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void StoreReplacesWithDifferentUser(string store)
		{
			var cli = CreateStore(store);

			cli.TryStore("a", "first",  "p1", out var error).ShouldBeTrue(error);
			cli.TryStore("a", "second", "p2", out error).ShouldBeTrue(error);

			cli.TryRead("linq2db/a", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("second");
			password.ShouldBe("p2");

			cli.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a", "second")]);
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void EmptyPasswordRoundTrips(string store)
		{
			var cli = CreateStore(store);

			cli.TryStore("empty", "u", string.Empty, out var error).ShouldBeTrue(error);

			cli.TryRead("linq2db/empty", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("u");
			password.ShouldBe(string.Empty);

			cli.TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(1);
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void ClearRemovesAll(string store)
		{
			var cli = CreateStore(store);

			cli.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			cli.TryStore("b", "u", "p", out error).ShouldBeTrue(error);

			cli.TryClear(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(2);

			cli.TryGetCount(out count, out error).ShouldBeTrue(error);
			count.ShouldBe(0);
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void PasswordLookingLikeUserMetadataStaysThePassword(string store)
		{
			// pass keeps the password on line 1 and "user: <name>" on line 2; a password of that shape must not be read as
			// the user name.
			var cli = CreateStore(store);

			cli.TryStore("a", "app=reader", "user: TOPSECRET", out var error).ShouldBeTrue(error);

			cli.TryRead("linq2db/a", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("app=reader");
			password.ShouldBe("user: TOPSECRET");

			cli.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a", "app=reader")]);
		}

		[Test]
		public void SecretToolLockedKeyringIsAFailureNotNotFound()
		{
			var cli = CreateStore("keyring");

			cli.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.WriteAllText(Path.Combine(_state, "locked"), string.Empty);

			cli.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: the keyring is locked");

			cli.TryRemove("a", out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the keyring is locked");

			cli.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the keyring is locked");

			cli.TryStore("b", "u", "p", out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1");

			// Nobody can answer an unlock prompt: none is raised.
			ArgvLog.ShouldNotContain("--unlock");
		}

		[Test]
		public void SecretToolLockedKeyringIsUnlockedWhenAUserCanAnswer()
		{
			// A login keyring stays locked after an automatic login until first use; in a terminal the unlock prompt is
			// raised (the fake unlocks on --unlock, as a user answering it does).
			var cli = CreateStore("keyring", interactive: true);

			cli.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.WriteAllText(Path.Combine(_state, "locked"), string.Empty);

			cli.TryRead("linq2db/a", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("u");
			password.ShouldBe("p");

			ArgvLog.ShouldContain("--unlock");
		}

		[Test]
		public void PassDecryptionFailureIsAFailureNotNotFound()
		{
			var cli = CreateStore("gpg");

			cli.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.WriteAllText(Path.Combine(_state, "fail"), string.Empty);

			cli.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			// The backend's own message comes first on standard error, before the script's summary line.
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: gpg: decryption failed: No pinentry");

			cli.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1");
		}

		[Test]
		public void PassStoreFailure()
		{
			var cli = CreateStore("gpg");

			File.WriteAllText(Path.Combine(_state, "readonly"), string.Empty);

			cli.TryStore("a", "u", "secret-value", out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: mkdir: Permission denied");
			error.ShouldNotBeNull().ShouldNotContain("secret-value");
		}

		[Test]
		public void PassNonInteractiveDisablesPinentry()
		{
			// The pass template adds --pinentry-mode=error to gpg when nobody can answer a prompt.
			var script = WriteTemplate("gpg");
			var text   = File.ReadAllText(script);

			text.ShouldContain("if [ \"${LINQ2DB_CREDENTIAL_INTERACTIVE-1}\" = 0 ]; then");
			text.ShouldContain("--pinentry-mode=error");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void UnsupportedVerbAndProtocol(string store)
		{
			var runner = CreateRunner(store);

			Encoding.UTF8.GetString(runner.Run("rename", CredentialsCliTestSupport.Request("protocol=1", "verb=rename")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
			Encoding.UTF8.GetString(runner.Run("get",    CredentialsCliTestSupport.Request("protocol=2", "verb=get")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void EveryAnswerStartsWithTheHeader(string store)
		{
			var runner = CreateRunner(store);

			string Answer(string verb, params string[] keys)
			{
				var result = runner.Run(verb, CredentialsCliTestSupport.Request(["protocol=1", $"verb={verb}", .. keys]));
				return Encoding.UTF8.GetString(result.Output);
			}

			Answer("get", "target=linq2db/a").ShouldBe("protocol=1\nstatus=not-found\n");
			Answer("erase", "target=linq2db/a").ShouldBe("protocol=1\nstatus=not-found\n");
			Answer("list").ShouldBe("protocol=1\nstatus=ok\n");
			Answer("store", "target=linq2db/a", "username=u", "password=p").ShouldBe("protocol=1\nstatus=ok\n");
			Answer("get", "target=linq2db/a").ShouldBe("protocol=1\nstatus=ok\nusername=u\npassword=p\n");
			Answer("store", "target=linq2db/b", "username=v", "password=q").ShouldBe("protocol=1\nstatus=ok\n");
			Answer("list").ShouldBe("protocol=1\nstatus=ok\n\ntarget=linq2db/a\nusername=u\n\ntarget=linq2db/b\nusername=v\n");
			Answer("erase", "target=linq2db/a").ShouldBe("protocol=1\nstatus=ok\n");

			// A failing backend: the answer is still framed, as an error.
			File.WriteAllText(Path.Combine(_state, store == "keyring" ? "locked" : "fail"), string.Empty);

			var failed = runner.Run("get", CredentialsCliTestSupport.Request("protocol=1", "verb=get", "target=linq2db/b"));

			failed.ExitCode.ShouldBe(1);
			Encoding.UTF8.GetString(failed.Output).ShouldBe("protocol=1\nstatus=error\n");

			failed = runner.Run("list", CredentialsCliTestSupport.Request("protocol=1", "verb=list"));

			failed.ExitCode.ShouldBe(1);
			Encoding.UTF8.GetString(failed.Output).ShouldBe("protocol=1\nstatus=error\n");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void ShellCheckIsClean(string store)
		{
			var shellcheck = FindOnPath("shellcheck");

			if (shellcheck == null)
			{
				if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null || Environment.GetEnvironmentVariable("TF_BUILD") != null)
					Assert.Fail("shellcheck is required on CI to check the generated credentials CLI scripts.");

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
			startInfo.ArgumentList.Add(WriteTemplate(store));

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
	}
}
