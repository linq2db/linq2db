# linq2db-cli credential stores and credentials CLIs

`dotnet linq2db` reads database user names and passwords from a credential store when a profile sets
`"credentials": "linq2db/<name>"` or a command passes `--credentials linq2db/<name>`. This document describes the stores,
how one is selected, and the **linq2db credentials CLI protocol, version 1**: the contract for any program that keeps
credentials for linq2db-cli, written in any language (a wrapper around HashiCorp Vault, the 1Password CLI, a corporate
secret tool, `pass`, `secret-tool`, or anything else). linq2db-cli starts such a program, writes a request to its
standard input, reads the answer from its standard output, and waits for it to exit.

## Contents

1. [Stores](#1-stores)
2. [Which store is used](#2-which-store-is-used)
3. [The credentials directory](#3-the-credentials-directory)
4. [The built-in local store](#4-the-built-in-local-store)
5. [Configuring a credentials CLI](#5-configuring-a-credentials-cli)
6. [Invocation](#6-invocation)
7. [Framing and versioning](#7-framing-and-versioning)
8. [Targets](#8-targets)
9. [Verbs and answers](#9-verbs-and-answers)
10. [Limits](#10-limits)
11. [Environment](#11-environment)
12. [Promises](#12-promises)
13. [Security](#13-security)
14. [The credentials command](#14-the-credentials-command)
15. [Generated scripts: keyring and gpg](#15-generated-scripts-keyring-and-gpg)
16. [Example credentials CLI](#16-example-credentials-cli)

## 1. Stores

| store | value | operating systems | what it is |
| --- | --- | --- | --- |
| local | `@local` | all | linq2db's own built-in encrypted store (section 4); the default on Linux and macOS |
| Windows Credential Manager | `@credential-manager` | Windows | the Windows store; the default on Windows |
| keyring | `@keyring` | Linux | your desktop keyring (GNOME Keyring or KWallet) through `secret-tool`, by a generated script (section 15) |
| gpg | `@gpg` | Linux, macOS, WSL | `pass`: one GPG-encrypted file per password in `~/.password-store`, by a generated script (section 15) |
| credentials CLI | `"<program> [arguments]"` | all | any program speaking the protocol of sections 6 to 12 |

## 2. Which store is used

Every command that needs credentials (`query`, `execute`, `schema`, `mcp`, `credentials`) uses:

1. `--credentials-cli <value>` on the command line;
2. else, with `--config`, the selected profile's `credentialsCli` (inherited from `default` like every key);
3. else the default for the operating system:

| Operating system | Nothing names a store | A store is named |
| --- | --- | --- |
| Windows | Windows Credential Manager | that store |
| Linux, macOS | the built-in local store | that store |

The generated `keyring` and `gpg` scripts are never picked automatically: they are used only when named (`@keyring`,
`@gpg`). There is no fallback from one store to another.

`dotnet linq2db credentials` commands print the store and where the choice came from on standard error (never on
standard output, which carries the result), for example:

```text
Using the local store (/home/me/.config/linq2db, default for this OS).
Using credentials CLI 'vault-cli --mount db' (profile 'prod' of .agents/linq2db-query.json).
```

## 3. The credentials directory

One directory holds the local store's files and the generated scripts:

- Linux, macOS: `$LINQ2DB_CREDENTIALS_DIR`, else `$XDG_CONFIG_HOME/linq2db`, else `~/.config/linq2db`;
- Windows: `%LINQ2DB_CREDENTIALS_DIR%`, else `%LOCALAPPDATA%\linq2db`.

A relative value of these variables is ignored. `LINQ2DB_CREDENTIALS_DIR` is meant for tests and containers; it must not
be under a directory that every user can write to (see below), and it must be a directory you created. When no
directory can be determined (no home directory), set `LINQ2DB_CREDENTIALS_DIR` to an absolute path.

On Linux and macOS the directory is created owner-only (`0700`); a directory that is a symbolic link or that other users
can write to (group or other write permission) is refused, because they could replace the key, the data or a generated
script. So is a directory whose real path (symbolic links resolved) has an ancestor that every user can write to
without the sticky bit (`/tmp` has the sticky bit): another user could rename the whole directory and put their own in
its place. Group write on an ancestor is accepted, since with private user groups (umask `002`) `~/.config` is often
`0775`. The error names that ancestor. linq2db-cli warns when the directory is inside a git working tree.

Ownership is not checked: the directory, its files and the generated scripts must be created by you. A predictable path
in a shared location (for example under `/tmp`) can be created first by another user, who then owns what you use; keep
the credentials directory in your home directory, or point `LINQ2DB_CREDENTIALS_DIR` at a directory you created.

On Windows the local store refuses a directory outside your user profile: a folder elsewhere may give other users write
access, and while they could neither read nor forge entries, they could delete the data or put back an older copy. The
check follows junctions and symbolic links and understands 8.3 short names, so a link inside the profile that leads
elsewhere is outside.

## 4. The built-in local store

linq2db's own store, built in: an encrypted file in the credentials directory, readable only by your user account,
with its key in a separate file next to it (on Windows protected with DPAPI). Like Windows Credential Manager it keeps
passwords from other users and from anyone opening the file; any program running as you can read them. On Linux/macOS a
copy of the disk, home directory or a backup with both files opens it (use disk encryption); on Windows an
administrator's password reset loses the entries, as for Credential Manager. Keep both files together; losing the key
loses the passwords; do not commit them. Default on Linux and macOS.

- Files: `credentials.key` (32 random bytes; on Windows wrapped with DPAPI for the current user), `credentials.dat`
  (AES-256-GCM, a fresh nonce on every write) and `credentials.lock`.
- Every write goes to a new file that is flushed to disk and then renamed over `credentials.dat` (or into place as
  `credentials.key`). The directory itself is not flushed after the rename, so after a power loss the previous data may
  come back; a torn or partly written file never does.
- Every operation takes an operating-system lock on `credentials.lock`; it is released when the process ends, so it is
  never stale. A command and an MCP server can use the store at the same time.
- Reading never creates anything: before the first `credentials set` (or `credentials cli init --store local`) there is
  no directory, and a read finds nothing.
- `credentials.dat` without `credentials.key` is an error for every operation, and no new key is created then; delete
  `credentials.dat` and enter the passwords again.
- On Linux and macOS a key or data file that is a symbolic link or accessible by other users is refused
  (`chmod 600 <file>`); files created by another user (for example with `sudo`) show as access denied.
- The first `credentials set` that creates the store while it is the Linux/macOS default says so on standard error.

## 5. Configuring a credentials CLI

`credentialsCli` is a string: a reserved value of section 1, or a command line, the program and its arguments.

```json
{
  "default": {
    "credentialsCli": "/opt/vault-cli/vault-cli --mount db"
  },
  "production": {
    "provider": "PostgreSQL",
    "connectionString": "Host=db;Database=app;Username={0};Password={1}",
    "credentials": "linq2db/project-a/production"
  }
}
```

On the command line, `--credentials-cli "<value>"` overrides the profile's value for `query`, `execute`, `schema`, `mcp`
and `credentials`. The value is never expanded: `~`, `$HOME`, `${NAME}` and `%NAME%` are passed as written. A program
whose name starts with `@` is written with a path (`./@name`).

## 6. Invocation

The first word of the command line is the program: up to the first space or tab, or, when the value starts with `"`,
up to the next `"` (a path with spaces is quoted; in JSON the quotes are escaped). The rest is passed to the program
unchanged as its argument string, followed by the verb as one more argument:

    <program> [<arguments>] <verb>

- `<verb>` is the last argument: `get`, `store`, `erase` or `list`. It is also the `verb` line of the request.
- The program is a file name looked up in the `PATH` directories (the current directory is never searched, and neither
  are empty or relative `PATH` entries), or a path; a relative path is resolved against the configuration file's
  directory (the current directory for `--credentials-cli`). Relative paths in the *arguments* are the program's
  business: it runs in your home directory.
- A program path with spaces is quoted; in JSON the quotes are escaped:
  `"credentialsCli": "\"C:\\Program Files\\Vault\\vault-cli.exe\" --mount db"` runs
  `C:\Program Files\Vault\vault-cli.exe` with the arguments `--mount db`.
- No shell is involved and nothing is expanded. The argument string is split into arguments by the Windows rules on
  every operating system: whitespace separates, `"..."` groups, `\"` is a quote; single quotes are ordinary characters.
  `"a b" c\"d e` arrives as `a b`, `c"d`, `e`.
- On Linux and macOS a script needs the execute bit and a `#!` line (`.sh` files are not run through `/bin/sh`).
- On Windows a program is an `.exe`, `.com`, `.cmd` or `.bat` file; a name without an extension is tried with each
  `PATHEXT` extension in order (other `PATHEXT` entries are ignored). A `.cmd` or `.bat` file is run as
  `cmd.exe /d /v:off /s /c ""<file>" <arguments> <verb>"`: a path with spaces and parentheses
  (`C:\Program Files (x86)\...`) stays intact, AutoRun commands are skipped, and `cmd.exe` parses the arguments:
  `%NAME%` in them is expanded, and `&`, `|`, `^`, `<` and `>` outside quotes are `cmd.exe` syntax, not text. `cmd.exe`
  has no backslash escape for `"`; keep arguments of a batch file simple, or run the program the batch file runs.
- Run a script through its interpreter:
  - PowerShell: `"credentialsCli": "pwsh -NoProfile -File \"C:\\Tools\\vault.ps1\" -Mount db"`. The execution policy and
    the console encoding are the script's business: set UTF-8 on both streams, as the example below does.
  - C# file-based app: `"credentialsCli": "dotnet run --file /home/me/cli/vault.cs --"`.
  - Python: `"credentialsCli": "python3 /home/me/cli/vault.py"`.

```powershell
# vault.ps1: the start of a PowerShell credentials CLI
param([string] $Mount, [string] $Verb)
$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding  = $utf8
[Console]::OutputEncoding = $utf8
```

- Never put a secret in the command line: arguments are visible to other processes. Secrets travel only on standard
  input and output.
- `get` is required; `store`, `erase` and `list` are optional. The working directory is your home directory. Several
  clients (a CLI command and an MCP server) may run the program at the same time; it must tolerate concurrent runs.

## 7. Framing and versioning

Request (standard input), closed by the client after the last line:

```text
protocol=1
verb=store
target=linq2db/prod
username=app_reader
password=s3cret value
```

Answer (standard output): line 1 `protocol=<N>`, line 2 `status=<ok|not-found|unsupported|error>`, then `key=value` lines.

- Text is UTF-8. Lines end with LF; CRLF is accepted, and a byte order mark before line 1 is ignored.
- Every line is `key=value`, split at the **first** `=`; the value is the rest of the line, including leading and
  trailing spaces. Keys are exact lower case. No line contains NUL. Unknown keys are ignored by both sides.
- Values never contain a line break or another control character: the client refuses such values before it runs the
  program (this also keeps a secret from forging a line of the answer), and an answer with one is invalid.
- The client accepts an answer only when the exit code is 0, line 1 is `protocol=1` and line 2 is a known status.
  `status=ok` with a non-zero exit code is a failure; `status=error` is a failure whatever the exit code.
- `protocol` and `status` appear exactly once. After `status=not-found`, `status=unsupported` or `status=error` only
  empty lines may follow.
- The client never shows standard output (it can hold secrets). A bad header is reported by its kind: standard output
  is empty, the first line is not `protocol=1` (with its length), or the first line looks like build output (shown up to
  the diagnostic code, for example `` `vault.cs(3,1): warning CS8321` ``).
- A program that does not speak protocol 1 answers `protocol=<its version>` and `status=unsupported`; the client
  reports "the credentials CLI speaks protocol N, linq2db-cli speaks 1". An incompatible change is version 2 and is
  announced in the release notes.

## 8. Targets

- `credentials set|remove` manage the record `linq2db/<name>`; `--credentials <target>` and the `credentials`
  configuration property read the target `<target>`.
- Targets that start with `linq2db/` are linq2db's own. They are case-insensitive: the client converts them to lower
  case with invariant-culture rules before sending them, so a program compares them byte for byte. After the prefix they
  have no leading or trailing `/`, no empty segment, and no `.` or `..` segment.
- Any other target belongs to the program's store and is sent exactly as written (a Vault path, a 1Password item, a URL).
- No target is empty, contains a control character, or starts with `-`. Programs should still pass targets to other
  programs after `--`.
- `credentials list`, the count shown by `credentials clear`, and `credentials clear` itself use only the `list`
  records whose target starts with `linq2db/`; the client never erases any other target.

## 9. Verbs and answers

| verb | request keys | `status=ok` answer | other valid statuses |
| --- | --- | --- | --- |
| `get` | `target` | exactly one `username` and one `password` (either may be empty) | `not-found` |
| `store` | `target`, `username`, `password` | nothing more; an existing record with this target is replaced | |
| `erase` | `target` | nothing more: the record was removed | `not-found`: there was no such record |
| `list` | | one record per credential, each preceded by an empty line: one `target`, at most one `username`, never a password | |

Every verb may also answer `status=unsupported` (the program does not implement the verb; `credentials set|remove|list|clear`
then say so) or `status=error` (with the reason on standard error, and preferably a non-zero exit code).

`get`, found:

```text
protocol=1
status=ok
username=app_reader
password=s3cret value
```

`get`, not found, and `erase` of a missing record:

```text
protocol=1
status=not-found
```

`get` or `store` failed (exit code 1, the reason on standard error):

```text
protocol=1
status=error
```

`store` and `erase`, done:

```text
protocol=1
status=ok
```

`list`, two records, and empty:

```text
protocol=1                      protocol=1
status=ok                       status=ok

target=linq2db/project-a/prod
username=app_reader

target=linq2db/project-b/read
username=reader
```

An unknown verb (new verbs may be added to version 1), and a request with `protocol=2`:

```text
protocol=1                      protocol=1
status=unsupported              status=unsupported
```

On a failure the client shows the first non-empty line of standard error (at most 200 characters; a password the client
sent in the request is replaced by `***`).

## 10. Limits

- The client reads at most 1 MB of standard output; a program that writes more is stopped.
- When a user can answer a prompt (`LINQ2DB_CREDENTIAL_INTERACTIVE=1`), the client waits at most 60 seconds for any
  verb, so a keyring unlock dialog or a gpg passphrase prompt can be answered. Otherwise it waits at most 10 seconds.
  After that it stops the program and everything it started, and reports that it did not answer. A program that builds
  itself on first use (`dotnet run`) should be built once before it is used non-interactively.
- After a non-interactive run has timed out, the same linq2db-cli process does not start that program (with the same
  arguments) again for 60 seconds and fails fast with a message asking to unlock the credential store; this matters
  mostly for the long-running MCP server. (A keyring prompter or `pinentry` is not the program's child; it survives the
  stop, and starting the program again would stack prompts.)
- After the program exits, the client waits at most 5 seconds for its output pipes to close and for its request to be
  read; a program must not leave a background process holding its standard input, output or error. Such a process is
  no longer part of the program's process tree, so the client cannot stop it: it reports the failure and, as after a
  timeout, does not start the program again for 60 seconds in the same process.
- Standard error is read to the end, but only its first 4 KB are kept for the error message; a password the client
  sent is removed from that text, including a part of it cut off at the 4 KB boundary.

## 11. Environment

- The program inherits the client's environment (a keyring, `gpg-agent` or a CLI session needs it), plus
  `DOTNET_NOLOGO=1`, so a program started through `dotnet` does not print the first-run welcome text on standard output.
- The client sets `LINQ2DB_CREDENTIAL_INTERACTIVE` to `1` when a user can answer a prompt (a command run in a terminal)
  and to `0` otherwise (the MCP server, redirected input), on every operating system. A program should not prompt when
  it is `0`, and should fail fast instead.
- Secrets are never placed in arguments or environment variables.

## 12. Promises

The client:

- passes secrets only on the program's standard input and reads them only from its standard output;
- never logs or prints standard output, and never includes it in an error message;
- validates targets and values before running the program (sections 7 and 8);
- never erases a target outside `linq2db/` (section 8).

The program:

- reads the request only from standard input, and never prompts on it (standard input is the protocol);
- writes secrets only to standard output, never to standard error, a log, or a file it does not own;
- **never reports "not found" or success when its store failed or is locked**: it answers `status=error`, exits with a
  non-zero code, and says why on standard error;
- prints the two header lines on every path, including failures;
- exits; it does not stay in the background holding the client's pipes.

## 13. Security

The configured credentials CLI is a trusted executable: a configuration that sets `credentialsCli` runs that program
as you, like git's `credential.helper`. Treat write access to a configuration file as the ability to run programs as
you; securing the program (where it lives and who can change it) is your responsibility.

## 14. The credentials command

```text
credentials set    [--config <file> [--profile <p>] | --credentials linq2db/<name>] --user <user> [--credentials-cli "<value>"]
credentials remove [--config <file> [--profile <p>] | --credentials linq2db/<name>]               [--credentials-cli "<value>"]
credentials list   [--config <file> [--profile <p>]]                                             [--credentials-cli "<value>"]
credentials clear  [--config <file> [--profile <p>]] [--force]                                   [--credentials-cli "<value>"]
credentials cli init --store <keyring|gpg|local> [--output <file>] [--config <file>] [--force]
```

- `--profile` selects a profile of `--config`, as for `query` (default `default`); it requires `--config`. The store is
  that profile's (section 2). For `set` and `remove` the record is the profile's `credentials`, or `--credentials`; only
  `linq2db/` records are managed.
- `list` and `clear` work on one store; `clear` asks for confirmation unless `--force` is given.
- Before version 6.6 `--profile` named the record (`--profile prod` meant `linq2db/prod`); use
  `--credentials linq2db/<name>` for that now.

```sh
dotnet linq2db credentials set --credentials linq2db/project-a/production --user app_reader
dotnet linq2db credentials set --config .agents/linq2db-query.json --profile production --user app_reader
dotnet linq2db credentials list --credentials-cli @gpg
```

## 15. Generated scripts: keyring and gpg

On Linux and macOS, `credentials cli init` writes a credentials CLI script over a secret store's command-line tool. The
script contains no secrets; it is yours to review and edit.

```sh
dotnet linq2db credentials cli init --store keyring --config .agents/linq2db-query.json
dotnet linq2db credentials cli init --store gpg
dotnet linq2db credentials cli init --store local --config .agents/linq2db-query.json
```

- `--store keyring`: your Linux desktop keyring (GNOME Keyring or KWallet) through `secret-tool` (package
  `libsecret-tools` on Debian/Ubuntu); Linux with a desktop session. Items carry the attributes `service=linq2db-cli`,
  `target`, `user`.
- `--store gpg`: [pass](https://www.passwordstore.org/), one GPG-encrypted file per password in `~/.password-store`;
  servers, SSH, WSL, macOS; needs a GPG key and an initialized store (`pass init <gpg-id>`). Entries are
  `linq2db-cli/<target>` with the password on the first line and `user: <name>` on the second.
- `--store local`: the built-in store of section 4; `init` creates the directory and the key at once, so problems show
  up now. It never replaces an existing key.
- The script is written to `credentials-<store>.sh` in the credentials directory unless `--output`/`-o` names another
  path, owner-only (`0700`). An existing script is replaced only with `--force`.
- With `--config <file>` the store is recorded as `credentialsCli` in that file's `default` profile (the file is created
  when missing): `@keyring`/`@gpg`/`@local` for the default location, otherwise the script's path (quoted when it has
  spaces). A different existing value is replaced only with `--force`. The script and the configuration change
  together: if the configuration cannot be written, the script is left as it was; if the script cannot be put in place,
  the configuration is restored. Without `--config`, `init` prints how to name the store.
- On Windows only `--store local` is available; Windows Credential Manager is the default there.

## 16. Example credentials CLI

[`CredentialsCli/secrethelper.cs`](CredentialsCli/secrethelper.cs) is a complete credentials CLI for protocol 1 in
under 200 lines of C#: a .NET file-based app that keeps credentials in a JSON file. It is an illustration of the
protocol, not a secure store; the file is plain text protected only by file permissions. The linq2db-cli tests run it
exactly as shown below, with `dotnet run --file`, on Linux and Windows, so it cannot drift from this specification.

```json
{
  "default": {
    "credentialsCli": "dotnet run --file /home/me/cli/secrethelper.cs --"
  }
}
```

```sh
dotnet linq2db credentials set --config .agents/linq2db-query.json --credentials linq2db/dev --user reader
dotnet linq2db query --config .agents/linq2db-query.json --credentials linq2db/dev --provider SQLite --connection-string "Data Source={0}" --sql "select 1"
```

The first `dotnet run` builds it, which can take longer than the 10-second non-interactive limit: run a command once at
a terminal first, or build it once and configure the executable (`secrethelper`, or `secrethelper.exe` on Windows),
which also starts faster:

```sh
dotnet build secrethelper.cs -o ~/.local/lib/linq2db-secrethelper
dotnet linq2db credentials list --credentials-cli /home/me/.local/lib/linq2db-secrethelper/secrethelper
```

The file is `$SECRETHELPER_FILE`, or `~/.linq2db-secrethelper.json` by default. The example builds without warnings
(`WarningLevel=0`) because `dotnet run` prints build warnings on standard output.
