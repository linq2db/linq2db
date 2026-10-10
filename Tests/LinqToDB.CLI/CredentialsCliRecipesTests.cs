using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The <c>get</c>-only programs of CREDENTIALS-CLI.md, "Connecting popular stores": each is composed from the
	/// document's skeleton and the store's <c>fetch</c> function exactly as written there, and run by the real client
	/// against a fake of the store's CLI that answers as its documentation describes (POSIX only).
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliRecipesTests
	{
		const string Marker = "# >>> the fetch function of your store <<<";

		// Leading/trailing spaces, quotes, $, backticks, a command substitution, '=', non-ASCII.
		const string HostileUser     = " DOMAIN\\o'neil \"x\" ";
		const string HostilePassword = "  p'a\"s$HOME`id`$(id)=é  ";

		// Fakes: every one keeps its records in files under $FAKE_STATE/<tool>, fails like a refused sign-in when a
		// $FAKE_STATE/fail file exists, and records its argument vector.
		const string FakeAz = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			[ "$1 $2 $3" = 'keyvault secret show' ] || exit 2; shift 3
			vault=''; name=''; query=''; output=''
			while [ $# -ge 2 ]; do
				case $1 in
					--vault-name) vault=$2 ;;
					--name)       name=$2 ;;
					--query)      query=$2 ;;
					--output)     output=$2 ;;
				esac
				shift 2
			done
			[ "$output" = tsv ] || exit 2
			[ ! -e "$FAKE_STATE/fail" ] || { echo 'ERROR: Please run '"'"'az login'"'"' to setup account.' >&2; exit 1; }
			item=$FAKE_STATE/az/$vault/$name
			[ -d "$item" ] || { printf '(SecretNotFound) A secret with (name/id) %s was not found in this key vault. If you recently deleted this secret you may be able to recover it using the correct recovery command.\nCode: SecretNotFound\n' "$name" >&2; exit 3; }
			case $query in
				value)         cat "$item/value"; echo ;;
				tags.username) [ ! -f "$item/username" ] || cat "$item/username"; echo ;;
				*) exit 2 ;;
			esac
			""";

		const string FakeOp = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			[ "$1 $2" = 'read --no-newline' ] || exit 2
			ref=${3#op://}
			[ ! -e "$FAKE_STATE/fail" ] || { echo '[ERROR] 2026/10/10 12:00:00 You are not currently signed in. Please run `op signin --help` for instructions' >&2; exit 1; }
			field=${ref##*/}; item=${ref%/*}
			[ -d "$FAKE_STATE/op/$item" ] || { printf '[ERROR] 2026/10/10 12:00:00 could not read secret '"'"'%s'"'"': error initializing client: "%s" isn'"'"'t an item in the "%s" vault. Specify the item with its UUID, name, or domain.\n' "$3" "${item#*/}" "${item%%/*}" >&2; exit 1; }
			[ -f "$FAKE_STATE/op/$item/$field" ] || { printf '[ERROR] 2026/10/10 12:00:00 could not read secret '"'"'%s'"'"': "%s" isn'"'"'t a field in the "%s" item\n' "$3" "$field" "${item#*/}" >&2; exit 1; }
			cat "$FAKE_STATE/op/$item/$field"
			""";

		const string FakeAws = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			[ "$1 $2 $3" = 'secretsmanager get-secret-value --secret-id' ] || exit 2
			id=$4
			[ "$5 $6 $7 $8" = '--query SecretString --output text' ] || exit 2
			[ ! -e "$FAKE_STATE/fail" ] || { printf '\nAn error occurred (ExpiredTokenException) when calling the GetSecretValue operation: The security token included in the request is expired\n' >&2; exit 254; }
			[ -f "$FAKE_STATE/aws/$id" ] || { printf '\nAn error occurred (ResourceNotFoundException) when calling the GetSecretValue operation: Secrets Manager can'"'"'t find the specified secret.\n' >&2; exit 254; }
			cat "$FAKE_STATE/aws/$id"; echo
			""";

		const string FakeBw = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			[ "$1 $2" = '--nointeraction get' ] || exit 2
			[ ! -e "$FAKE_STATE/fail" ] || { echo 'Vault is locked.' >&2; exit 1; }
			[ -d "$FAKE_STATE/bw/$4" ] || { echo 'Not found.' >&2; exit 1; }
			cat "$FAKE_STATE/bw/$4/$3"
			""";

		const string FakeKeePassXC = """
			#!/bin/sh
			set -u
			for a in "$@"; do printf '%s\n' "$a" >> "$FAKE_STATE/argv.log"; done
			[ "$1 $2 $3 $4" = "show -q --no-password --key-file" ] || exit 2
			[ "$5" = "$LINQ2DB_KEEPASS_KEYFILE" ] || exit 2
			[ "$6 $7 $8 $9" = '-a UserName -a Password' ] || exit 2
			[ "${10}" = "$LINQ2DB_KEEPASS_DB" ] || exit 2
			[ ! -e "$FAKE_STATE/fail" ] || { echo 'Error while reading the database: Invalid credentials were provided, please try again.' >&2; exit 1; }
			entry=$FAKE_STATE/kp${11}
			[ -d "$entry" ] || { printf 'Could not find entry with path %s.\n' "${11}" >&2; exit 1; }
			cat "$entry/UserName"; echo
			cat "$entry/Password"; echo
			""";

		string _directory = null!;
		string _state     = null!;
		string _bin       = null!;

		[SetUp]
		public void SetUp()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("The programs are POSIX sh scripts.");

			_directory = CredentialsCliTestSupport.CreateTempDirectory();
			_state     = Directory.CreateDirectory(Path.Combine(_directory, "state")).FullName;
			_bin       = Directory.CreateDirectory(Path.Combine(_directory, "bin")).FullName;

			WriteExecutable(Path.Combine(_bin, "az"),            FakeAz);
			WriteExecutable(Path.Combine(_bin, "op"),            FakeOp);
			WriteExecutable(Path.Combine(_bin, "aws"),           FakeAws);
			WriteExecutable(Path.Combine(_bin, "bw"),            FakeBw);
			WriteExecutable(Path.Combine(_bin, "keepassxc-cli"), FakeKeePassXC);
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

		/// <summary>The skeleton and the <c>fetch</c> functions (by store), as CREDENTIALS-CLI.md shows them.</summary>
		static (string Skeleton, Dictionary<string, string> Fetch) ReadDocument()
		{
			var document = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "CredentialsCli", "CREDENTIALS-CLI.md"))
				.Replace("\r\n", "\n", StringComparison.Ordinal);
			var blocks   = Regex.Matches(document, "```sh\n(.*?)```", RegexOptions.Singleline, TimeSpan.FromSeconds(10)).Select(static match => match.Groups[1].Value).ToArray();
			var fetch    = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (var block in blocks.Where(static block => block.StartsWith("# fetch: ", StringComparison.Ordinal)))
			{
				var store = block.Substring("# fetch: ".Length, block.IndexOf(" (", StringComparison.Ordinal) - "# fetch: ".Length);
				fetch.Add(store, block);
			}

			return (blocks.Single(static block => block.Contains(Marker, StringComparison.Ordinal)), fetch);
		}

		string WriteProgram(string store)
		{
			var (skeleton, fetch) = ReadDocument();
			var path              = Path.Combine(_directory, $"linq2db-{store.Replace(' ', '-')}.sh");

			WriteExecutable(path, skeleton.Replace(Marker, fetch[store].TrimEnd('\n'), StringComparison.Ordinal));
			return path;
		}

		CredentialsCliStore CreateStore(string store)
		{
			var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
			{
				["PATH"]                    = _bin + ":" + Environment.GetEnvironmentVariable("PATH"),
				["FAKE_STATE"]              = _state,
				["LINQ2DB_AZURE_VAULT"]     = "myvault",
				["LINQ2DB_KEEPASS_DB"]      = Path.Combine(_directory, "team.kdbx"),
				["LINQ2DB_KEEPASS_KEYFILE"] = Path.Combine(_directory, "team.keyx"),
			};

			return new CredentialsCliStore(CredentialsCliTestSupport.CreateRunner(WriteProgram(store), environment: environment));
		}

		/// <summary>Puts a record where the store's <c>fetch</c> looks for <c>linq2db/&lt;name&gt;</c>.</summary>
		void Seed(string store, string name, string? user, string password)
		{
			switch (store)
			{
				case "Azure Key Vault":
				{
					var item = Directory.CreateDirectory(Path.Combine(_state, "az", "myvault", name.Replace("/", "--", StringComparison.Ordinal))).FullName;
					File.WriteAllText(Path.Combine(item, "value"), password);
					if (user != null)
						File.WriteAllText(Path.Combine(item, "username"), user);
					break;
				}

				case "1Password":
				{
					var item = Directory.CreateDirectory(Path.Combine(_state, "op", name)).FullName;
					File.WriteAllText(Path.Combine(item, "password"), password);
					if (user != null)
						File.WriteAllText(Path.Combine(item, "username"), user);
					break;
				}

				case "AWS Secrets Manager":
				{
					var file = Path.Combine(_state, "aws", "linq2db", name);
					Directory.CreateDirectory(Path.GetDirectoryName(file)!);
					File.WriteAllText(file, user == null
						? JsonSerializer.Serialize(new { password, engine = "postgres" })
						: JsonSerializer.Serialize(new { username = user, password, engine = "postgres" }));
					break;
				}

				case "Bitwarden":
				{
					var item = Directory.CreateDirectory(Path.Combine(_state, "bw", "linq2db", name)).FullName;
					File.WriteAllText(Path.Combine(item, "password"), password);
					File.WriteAllText(Path.Combine(item, "username"), user ?? string.Empty);
					break;
				}

				case "KeePassXC":
				{
					var item = Directory.CreateDirectory(Path.Combine(_state, "kp", name)).FullName;
					File.WriteAllText(Path.Combine(item, "Password"), password);
					File.WriteAllText(Path.Combine(item, "UserName"), user ?? string.Empty);
					break;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(store), store, null);
			}
		}

		static string RecordName(string store)
		{
			// 1Password names the vault and the item.
			return string.Equals(store, "1Password", StringComparison.Ordinal) ? "private/prod-db" : "project-a/prod";
		}

		static void RequireTools(string store)
		{
			if (string.Equals(store, "AWS Secrets Manager", StringComparison.Ordinal) && FindOnPath("jq") == null)
				Assert.Ignore("jq is not installed.");
		}

		[Test]
		public void DocumentHasAFetchForEveryStore()
		{
			ReadDocument().Fetch.Keys.ShouldBe(["Azure Key Vault", "1Password", "AWS Secrets Manager", "Bitwarden", "KeePassXC"], ignoreOrder: true);
		}

		[TestCase("Azure Key Vault")]
		[TestCase("1Password")]
		[TestCase("AWS Secrets Manager")]
		[TestCase("Bitwarden")]
		[TestCase("KeePassXC")]
		public void GetReadsTheRecord(string store)
		{
			RequireTools(store);

			var name = RecordName(store);
			Seed(store, name, HostileUser, HostilePassword);

			CreateStore(store).TryRead("linq2db/" + name, out var user, out var password, out var error).ShouldBeTrue(error);
			user.    ShouldBe(HostileUser);
			password.ShouldBe(HostilePassword);
		}

		[TestCase("Azure Key Vault")]
		[TestCase("1Password")]
		[TestCase("AWS Secrets Manager")]
		[TestCase("Bitwarden")]
		[TestCase("KeePassXC")]
		public void RecordWithoutUserNameHasAnEmptyOne(string store)
		{
			RequireTools(store);

			var name = RecordName(store);
			Seed(store, name, null, "pw");

			CreateStore(store).TryRead("linq2db/" + name, out var user, out var password, out var error).ShouldBeTrue(error);
			user.    ShouldBe(string.Empty);
			password.ShouldBe("pw");
		}

		[TestCase("Azure Key Vault")]
		[TestCase("1Password")]
		[TestCase("AWS Secrets Manager")]
		[TestCase("Bitwarden")]
		[TestCase("KeePassXC")]
		public void MissingRecordIsNotFound(string store)
		{
			RequireTools(store);

			CreateStore(store).TryRead("linq2db/" + RecordName(store), out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found by credentials CLI");
		}

		[TestCase("Azure Key Vault",     "az keyvault secret show failed: ERROR: Please run 'az login'")]
		[TestCase("1Password",           "op read failed: [ERROR] 2026/10/10 12:00:00 You are not currently signed in.")]
		[TestCase("AWS Secrets Manager", "aws secretsmanager get-secret-value failed: An error occurred (ExpiredTokenException)")]
		[TestCase("Bitwarden",           "bw get password failed: Vault is locked.")]
		[TestCase("KeePassXC",           "keepassxc-cli show failed: Error while reading the database: Invalid credentials")]
		public void StoreFailureIsAnErrorWithTheReason(string store, string message)
		{
			RequireTools(store);

			Seed(store, RecordName(store), "u", "a-long-password");
			File.WriteAllText(Path.Combine(_state, "fail"), string.Empty);

			CreateStore(store).TryRead("linq2db/" + RecordName(store), out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);
		}

		[TestCase("Azure Key Vault")]
		[TestCase("1Password")]
		[TestCase("AWS Secrets Manager")]
		[TestCase("Bitwarden")]
		[TestCase("KeePassXC")]
		public void OnlyGetIsSupported(string store)
		{
			var runner = CredentialsCliTestSupport.CreateRunner(WriteProgram(store));

			foreach (var verb in new[] { "store", "erase", "list" })
				Encoding.UTF8.GetString(runner.Run(verb, CredentialsCliTestSupport.Request("protocol=1", $"verb={verb}", "target=linq2db/a", "username=u", "password=p")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");

			Encoding.UTF8.GetString(runner.Run("get", CredentialsCliTestSupport.Request("protocol=2", "verb=get", "target=linq2db/a")).Output).ShouldBe("protocol=1\nstatus=unsupported\n");
		}

		[TestCase("Azure Key Vault")]
		[TestCase("1Password")]
		[TestCase("AWS Secrets Manager")]
		[TestCase("Bitwarden")]
		[TestCase("KeePassXC")]
		public void ShellCheckIsClean(string store)
		{
			var shellcheck = FindOnPath("shellcheck");

			if (shellcheck == null)
			{
				if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null || Environment.GetEnvironmentVariable("TF_BUILD") != null)
					Assert.Fail("shellcheck is required on CI to check the documented credentials CLI programs.");

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
			startInfo.ArgumentList.Add(WriteProgram(store));

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
