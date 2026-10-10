using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The scripts written by <c>credentials cli init --store keyring|gpg|vault</c>, run by the real client against fake
	/// <c>secret-tool</c>, <c>pass</c> and <c>vault</c> executables that keep their items in files and record every
	/// argument vector (POSIX only).
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliTemplateTests
	{
		// Leading/trailing spaces, quotes, $, backticks, a command substitution, '=', non-ASCII.
		const string HostileUser     = " DOMAIN\\o'neil \"x\" ";
		const string HostilePassword = "  p'a\"s$HOME`id`$(id)=é  ";
		const string HostileTarget   = "linq2db/it's a \"test\" $x";

		// A fake secret-tool: items are directories under $FAKE_STATE/items with one file per attribute, and a "collection"
		// file (missing: the default collection). "search --all" prints the item header and secret on stdout and the
		// attributes on stderr, like the real tool. A collection is locked by a file: "locked" for the default one,
		// "locked.<name>" for another; a locked item has no secret line and lookup fails. "search --unlock" unlocks the
		// collections of the items found, and "store" (which writes to the default collection) unlocks that one, as libsecret
		// does with a prompt that a user answers; "store" also leaves a "prompted" file, since nobody may be there to answer.
		const string FakeSecretTool = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			cmd=$1; shift
			label=''; unlock=0
			if [ "$cmd" = search ] && [ "${1-}" = --all ]; then shift; fi
			if [ "$cmd" = search ] && [ "${1-}" = --unlock ]; then shift; unlock=1; fi
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
			collection() { if [ -f "$1/collection" ]; then cat "$1/collection"; else printf default; fi; }
			lockfile() { if [ "$1" = default ]; then printf '%s' "$FAKE_STATE/locked"; else printf '%s' "$FAKE_STATE/locked.$1"; fi; }
			locked() { [ -e "$(lockfile "$1")" ]; }
			case $cmd in
				store)
					if locked default; then : > "$FAKE_STATE/prompted"; rm -f "$(lockfile default)"; fi
					secret=$(cat; printf x); secret=${secret%x}
					item=''
					for d in "$FAKE_STATE"/items/*; do [ -d "$d" ] && [ "$(collection "$d")" = default ] && matches "$d" && item=$d; done
					[ -n "$item" ] || { item=$FAKE_STATE/items/$(date +%s%N); mkdir "$item"; }
					printf '%s' "$ws" > "$item/service"; printf '%s' "$wt" > "$item/target"; printf '%s' "$wu" > "$item/user"
					printf '%s' "$secret" > "$item/secret"; printf '%s' "$label" > "$item/label"
					;;
				lookup)
					for d in "$FAKE_STATE"/items/*; do
						if [ -d "$d" ] && matches "$d"; then
							locked "$(collection "$d")" && { echo 'secret-tool: Cannot get secret of a locked object' >&2; exit 1; }
							cat "$d/secret"; exit 0
						fi
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
						[ $unlock = 0 ] || rm -f "$(lockfile "$(collection "$d")")"
						n=$((n + 1))
						printf '[/%s]\nlabel = %s\n' "$n" "$(cat "$d/label")"
						locked "$(collection "$d")" || printf 'secret = %s\n' "$(cat "$d/secret")"
						printf 'attribute.service = %s\nattribute.target = %s\nattribute.user = %s\n' "$(cat "$d/service")" "$(cat "$d/target")" "$(cat "$d/user")" >&2
					done
					;;
			esac
			""";

		// A fake vault CLI (KV version 2: kv get/put/list/delete, kv metadata get/delete): a secret at <mount>/<path> is the
		// directory $FAKE_STATE/vault/<mount>/<path> with a ".secret" marker (the metadata) and one dot file per field of the
		// latest version; "kv delete" removes the fields and leaves the metadata and a ".deleted" marker, like a soft
		// delete. "kv put <path> -" reads the JSON object that the script writes. "kv list" prints a JSON array with
		// encoding/json's escapes, or the contents of $FAKE_STATE/list.json when that exists. A "fail" file makes every
		// request fail like a refused token.
		const string FakeVault = """
			#!/bin/sh
			set -u
			export LC_ALL=C
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			[ "$1" = kv ] || exit 1; shift
			cmd=$1; shift
			if [ "$cmd" = metadata ]; then cmd=metadata-$1; shift; fi
			mount=''; field=''; format=table
			while [ $# -gt 0 ]; do
				case $1 in
					-mount=*)  mount=${1#-mount=} ;;
					-field=*)  field=${1#-field=} ;;
					-format=*) format=${1#-format=} ;;
					--) shift; break ;;
					*) break ;;
				esac
				shift
			done
			path=$1
			if [ -e "$FAKE_STATE/fail" ]; then
				printf 'Error making API request.\n\nURL: GET http://127.0.0.1:8200/v1/%s\nCode: 403. Errors:\n\n* permission denied\n' "$path" >&2
				exit 2
			fi
			root=$FAKE_STATE/vault/$mount
			item=$root/${path%/}
			case $cmd in
				get)
					[ -f "$item/.secret" ] || { printf 'No value found at %s/data/%s\n' "$mount" "$path" >&2; exit 2; }
					[ ! -f "$item/.deleted" ] || { printf 'No data found at %s/data/%s\n' "$mount" "$path" >&2; exit 2; }
					[ -f "$item/.$field" ] || { printf 'Field "%s" not present in secret\n' "$field" >&2; exit 1; }
					cat "$item/.$field"
					;;
				put)
					[ "${2-}" = - ] || { echo 'the fake reads the data only from stdin' >&2; exit 1; }
					data=$(cat; printf x); data=${data%x}
					rest=${data#'{"username":"'}
					user=${rest%%'","password":"'*}
					secret=${rest#*'","password":"'}; secret=${secret%'"}'}
					unescape() { printf '%s' "$1" | sed 's/\\\(.\)/\1/g'; }
					mkdir -p "$item"
					unescape "$user" > "$item/.username"; unescape "$secret" > "$item/.password"; : > "$item/.secret"; rm -f "$item/.deleted"
					printf '== Secret Path ==\n%s/data/%s\n' "$mount" "$path"
					;;
				list)
					[ "$format" = json ] || { echo 'the fake lists only as JSON' >&2; exit 1; }
					if [ -f "$FAKE_STATE/list.json" ]; then cat "$FAKE_STATE/list.json"; exit 0; fi
					keys=''
					for d in "$item"/*/; do
						[ -d "$d" ] || continue
						n=$(basename "$d")
						[ ! -f "$d/.secret" ] || keys="$keys$n
			"
						[ -z "$(find "$d" -mindepth 2 -name .secret)" ] || keys="$keys$n/
			"
					done
					[ -n "$keys" ] || { echo '{}'; exit 2; }
					printf '%s' "$keys" | sed 's/\\/\\\\/g; s/"/\\"/g; s/</\\u003c/g; s/>/\\u003e/g; s/&/\\u0026/g; s/^/  "/; s/$/",/; $s/,$//; 1s/^/[\
			/; $s/$/\
			]/'
					echo
					;;
				metadata-get)
					[ -f "$item/.secret" ] || { printf 'No value found at %s/metadata/%s\n' "$mount" "$path" >&2; exit 2; }
					printf '== Metadata Path ==\n%s/metadata/%s\n' "$mount" "$path"
					;;
				delete)
					[ ! -f "$item/.secret" ] || { rm -f "$item/.username" "$item/.password"; : > "$item/.deleted"; }
					printf 'Success! Data deleted (if it existed) at: %s/data/%s\n' "$mount" "$path"
					;;
				metadata-delete)
					rm -f "$item/.secret" "$item/.username" "$item/.password" "$item/.deleted"
					while [ "$item" != "$root" ] && rmdir "$item" 2>/dev/null; do item=$(dirname "$item"); done
					printf 'Success! Data deleted (if it existed) at: %s/metadata/%s\n' "$mount" "$path"
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
			WriteExecutable(Path.Combine(_bin, "vault"),       FakeVault);
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
		[TestCase("vault")]
		public void RoundTripWithHostileValues(string store)
		{
			var cli = CreateStore(store, interactive: true);

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
		[TestCase("vault")]
		public void StoreReplacesWithDifferentUser(string store)
		{
			var cli = CreateStore(store, interactive: true);

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
		[TestCase("vault")]
		public void EmptyPasswordRoundTrips(string store)
		{
			var cli = CreateStore(store, interactive: true);

			cli.TryStore("empty", "u", string.Empty, out var error).ShouldBeTrue(error);

			cli.TryRead("linq2db/empty", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe("u");
			password.ShouldBe(string.Empty);

			cli.TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(1);
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		[TestCase("vault")]
		public void ClearRemovesAll(string store)
		{
			var cli = CreateStore(store, interactive: true);

			cli.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			cli.TryStore("b", "u", "p", out error).ShouldBeTrue(error);

			cli.TryClear(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(2);

			cli.TryGetCount(out count, out error).ShouldBeTrue(error);
			count.ShouldBe(0);
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		[TestCase("vault")]
		public void PasswordLookingLikeUserMetadataStaysThePassword(string store)
		{
			// pass keeps the password on line 1 and "user: <name>" on line 2; a password of that shape must not be read as
			// the user name.
			var cli = CreateStore(store, interactive: true);

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
			CreateStore("keyring", interactive: true).TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			File.Delete(Path.Combine(_state, "argv.log"));

			var cli = CreateStore("keyring");

			File.WriteAllText(Path.Combine(_state, "locked"), string.Empty);

			cli.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1: the keyring is locked");

			cli.TryRemove("a", out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the keyring is locked");

			cli.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the keyring is locked");

			cli.TryStore("b", "u", "p", out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("failed with exit code 1");

			// Nobody can answer an unlock prompt: none is raised, not even by storing a new record.
			ArgvLog.ShouldNotContain("--unlock");
			File.Exists(Path.Combine(_state, "prompted")).ShouldBeFalse();
			File.Exists(Path.Combine(_state, "locked")).ShouldBeTrue();
		}

		[Test]
		public void SecretToolNonInteractiveStoreIsRefused()
		{
			// secret-tool store writes to the default collection and prompts when it is locked; without a prompt the script
			// cannot tell. Here the record is in another, unlocked collection while the default one is locked: neither a
			// new record nor an update may be stored without a user to answer.
			CreateStore("keyring", interactive: true).TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var item = Directory.GetDirectories(Path.Combine(_state, "items")).Single();
			File.WriteAllText(Path.Combine(item, "collection"), "other");
			File.WriteAllText(Path.Combine(_state, "locked"), string.Empty);

			var cli = CreateStore("keyring");

			// The record in the unlocked collection stays readable.
			cli.TryRead("linq2db/a", out _, out var password, out error).ShouldBeTrue(error);
			password.ShouldBe("p");

			// A password long enough for the client to remove it from standard error and show the reason.
			cli.TryStore("b", "u", "secret-value", out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("run credentials set from a terminal");

			cli.TryStore("a", "u", "secret-value", out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("run credentials set from a terminal");

			File.Exists(Path.Combine(_state, "prompted")).ShouldBeFalse();
			File.Exists(Path.Combine(_state, "locked")).ShouldBeTrue();

			// From a terminal the prompt is raised and answered.
			CreateStore("keyring", interactive: true).TryStore("b", "u", "secret-value", out error).ShouldBeTrue(error);
			File.Exists(Path.Combine(_state, "prompted")).ShouldBeTrue();
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
		[TestCase("vault")]
		public void UnsupportedVerbAndProtocol(string store)
		{
			var runner = CreateRunner(store);

			Encoding.UTF8.GetString(runner.Run("rename", CredentialsCliTestSupport.Request("protocol=1", "verb=rename")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
			Encoding.UTF8.GetString(runner.Run("get",    CredentialsCliTestSupport.Request("protocol=2", "verb=get")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		[TestCase("vault")]
		public void EveryAnswerStartsWithTheHeader(string store)
		{
			var runner = CreateRunner(store, interactive: true);

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

			// A failing backend: the answer is still framed, as an error. Non-interactive: a keyring would be unlocked by a
			// user answering the prompt.
			File.WriteAllText(Path.Combine(_state, store == "keyring" ? "locked" : "fail"), string.Empty);

			runner = CreateRunner(store);

			var failed = runner.Run("get", CredentialsCliTestSupport.Request("protocol=1", "verb=get", "target=linq2db/b"));

			failed.ExitCode.ShouldBe(1);
			Encoding.UTF8.GetString(failed.Output).ShouldBe("protocol=1\nstatus=error\n");

			failed = runner.Run("list", CredentialsCliTestSupport.Request("protocol=1", "verb=list"));

			failed.ExitCode.ShouldBe(1);
			Encoding.UTF8.GetString(failed.Output).ShouldBe("protocol=1\nstatus=error\n");
		}

		[Test]
		public void VaultListsNestedRecordsOfTheDefaultMount()
		{
			var cli = CreateStore("vault");

			cli.TryStore("a/b/c", "u1", "p1", out var error).ShouldBeTrue(error);
			cli.TryStore("a/d",   "u2", "p2", out error).ShouldBeTrue(error);
			cli.TryStore("e",     "u3", "p3", out error).ShouldBeTrue(error);

			File.Exists(Path.Combine(_state, "vault", "secret", "linq2db", "a", "b", "c", ".secret")).ShouldBeTrue();

			cli.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a/b/c", "u1"), new CredentialProfile("a/d", "u2"), new CredentialProfile("e", "u3")], ignoreOrder: true);

			cli.TryRemove("a/b/c", out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeTrue();

			cli.TryList(out profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a/d", "u2"), new CredentialProfile("e", "u3")], ignoreOrder: true);
		}

		[Test]
		public void VaultSecretWithoutUserNameOrPassword()
		{
			// A secret written by other tooling: the user name is optional, the password is not.
			var item = Directory.CreateDirectory(Path.Combine(_state, "vault", "secret", "linq2db", "nouser")).FullName;
			File.WriteAllText(Path.Combine(item, ".secret"),   string.Empty);
			File.WriteAllText(Path.Combine(item, ".password"), "pw");

			var cli = CreateStore("vault");

			cli.TryRead("linq2db/nouser", out var user, out var password, out var error).ShouldBeTrue(error);
			user.    ShouldBe(string.Empty);
			password.ShouldBe("pw");

			File.Delete(Path.Combine(item, ".password"));

			cli.TryRead("linq2db/nouser", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("has no password field");
		}

		[Test]
		public void VaultListKeepsKeysAsWritten()
		{
			// vault's table trims keys and splits them at some characters; its JSON keeps them, escapes included.
			var cli   = CreateStore("vault");
			var names = new[] { "a b", " lead", "x<y>&z\"q\\r♨" };

			foreach (var name in names)
				cli.TryStore(name, "u", "p", out var storeError).ShouldBeTrue(storeError);

			cli.TryList(out var profiles, out _, out var error).ShouldBeTrue(error);
			profiles.Select(static profile => profile.Name).ShouldBe(names, ignoreOrder: true);

			cli.TryClear(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(3);
			Directory.Exists(Path.Combine(_state, "vault", "secret", "linq2db")).ShouldBeFalse();
		}

		[Test]
		public void VaultSoftDeletedRecordIsListedAndErased()
		{
			// "vault kv delete" of the latest KV version 2 version leaves the metadata and the older, recoverable versions.
			var cli = CreateStore("vault");

			cli.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var item = Path.Combine(_state, "vault", "secret", "linq2db", "a");
			File.Delete(Path.Combine(item, ".username"));
			File.Delete(Path.Combine(item, ".password"));
			File.WriteAllText(Path.Combine(item, ".deleted"), string.Empty);

			cli.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credentials CLI");

			cli.TryList(out var profiles, out _, out error).ShouldBeTrue(error);
			profiles.ShouldBe([new CredentialProfile("a", string.Empty)]);

			cli.TryRemove("a", out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeTrue();
			Directory.Exists(item).ShouldBeFalse();
		}

		[Test]
		public void VaultUserNameWithLineBreaksCannotForgeRecords()
		{
			// Written by someone else with write access to the path: a list must not turn it into a record to erase.
			var cli = CreateStore("vault");

			cli.TryStore("evil", "u", "secret-value", out var error).ShouldBeTrue(error);
			File.WriteAllText(Path.Combine(_state, "vault", "secret", "linq2db", "evil", ".username"), "x\n\ntarget=linq2db/../outside\nusername=y");

			cli.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the user name of linq2db/evil holds a control character");

			cli.TryClear(out _, out error).ShouldBeFalse();
			ArgvLog.ShouldNotContain("outside");

			cli.TryRead("linq2db/evil", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("holds a control character");
		}

		[TestCase("[\n  \"a\\nb\"\n]",     TestName = "VaultKeyWithEscapedLineBreakIsRefused")]
		[TestCase("[\n  \"a\\u0007b\"\n]", TestName = "VaultKeyWithEscapedControlCharacterIsRefused")]
		[TestCase("[\n  \"a\u0007b\"\n]",   TestName = "VaultKeyWithRawControlCharacterIsRefused")]
		[TestCase("[\n  \"a",                 TestName = "VaultListThatIsNotJsonIsRefused")]
		public void VaultKeyThatCannotBeOneLineIsRefused(string json)
		{
			File.WriteAllText(Path.Combine(_state, "list.json"), json);

			CreateStore("vault").TryList(out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("with a control character, or output that is not a JSON array of strings");
		}

		[Test]
		public void VaultErrorIsShownOnOneLine()
		{
			File.WriteAllText(Path.Combine(_state, "fail"), string.Empty);

			CreateStore("vault").TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("vault kv get failed: Error making API request. Code: 403. Errors: * permission denied");
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		[TestCase("vault")]
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
