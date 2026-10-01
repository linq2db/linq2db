# linq2db-cli credential helpers

A credential helper is any program that stores database user names and passwords for `dotnet linq2db`. It can be
written in any language: a wrapper around HashiCorp Vault, the 1Password CLI, a corporate secret tool, `pass`,
`secret-tool`, or anything else. linq2db-cli starts it, writes a request to its standard input, reads the answer from
its standard output, and waits for it to exit.

This document is the specification of the **linq2db credential helper protocol, version 1**, and the description of
how linq2db-cli finds and runs a helper. Version 1 changes only by addition (section 4).

## Contents

1. [Which credential store is used](#1-which-credential-store-is-used)
2. [Configuring a helper](#2-configuring-a-helper)
3. [Invocation](#3-invocation)
4. [Framing and versioning](#4-framing-and-versioning)
5. [Targets](#5-targets)
6. [Verbs and answers](#6-verbs-and-answers)
7. [Limits](#7-limits)
8. [Environment](#8-environment)
9. [Promises](#9-promises)
10. [Security](#10-security)
11. [Example helper](#11-example-helper)
12. [Starter scripts for secret-tool and pass](#12-starter-scripts-for-secret-tool-and-pass)
13. [Docker credential helpers](#13-docker-credential-helpers)

## 1. Which credential store is used

| Operating system | No helper configured | Helper configured |
| --- | --- | --- |
| Windows | Windows Credential Manager (built in) | the helper |
| Linux, macOS | error: configure a helper or use `userEnv`/`passwordEnv` | the helper |

There is no silent fallback: when a helper is configured, Windows Credential Manager is not consulted.

## 2. Configuring a helper

In a configuration profile (inherited from the `default` profile like every other key):

```json
{
  "default": {
    "credentialHelper": "/home/me/.config/linq2db/helpers/secret-tool.sh"
  },
  "production": {
    "provider": "PostgreSQL",
    "connectionString": "Host=db;Database=app;Username={0};Password={1}",
    "credentials": "linq2db/project-a/production"
  }
}
```

The value is either a string (a command, protocol `linq2db`) or an object:

```json
"credentialHelper": { "command": "<command>", "args": ["<argument>", "..."], "protocol": "linq2db", "timeout": 120 }
"credentialHelper": { "command": "dotnet", "args": ["run", "${HOME}/helpers/secrethelper.cs", "--"], "timeout": 120 }
"credentialHelper": { "command": "docker-credential-secretservice", "protocol": "docker" }
```

| key | required | meaning |
| --- | --- | --- |
| `command` | yes | the program to start (see below) |
| `args` | no | fixed arguments passed before the verb, each as one argument; no shell is involved, so quotes, `$`, spaces and `;` are passed literally. Use them to start a helper through another program: `dotnet run <file>.cs --`, `pwsh -NoProfile -File <script>.ps1`, `python3 <script>.py` |
| `protocol` | no | `linq2db` (default, this specification) or `docker` (section 13) |
| `timeout` | no | seconds a run may take, replacing the defaults of section 7 — for example for a file-based app that `dotnet run` builds on first use |

On the command line, `--credential-helper <command>` (protocol `linq2db`) overrides the profile's value for `query`,
`execute`, `schema` and `mcp`. The Docker adapter (section 13) is configured in the file only.

`<command>`:

- a file name without a directory (for example `my-credential-helper`) is looked up in the `PATH` directories; the
  current directory is never searched, and neither are empty or relative `PATH` entries;
- a path with a directory is used as written; a relative path is resolved against the configuration file's directory
  (or the current directory for `--credential-helper`);
- `%NAME%` and `${NAME}` are replaced with environment variables in `command` and in every `args` item, so
  `${HOME}/.config/linq2db/helpers/pass.sh` works; `~` is not expanded. File arguments should be absolute paths: the
  helper runs in the user's home directory.

On Windows a helper `command` must be an `.exe`, `.com`, `.cmd` or `.bat` file. A name without one of these
extensions is tried with each `PATHEXT` extension in order (other `PATHEXT` entries are ignored). Run a PowerShell or
other script through its interpreter with `args` (`{ "command": "pwsh", "args": ["-NoProfile", "-File",
"C:\\Tools\\helper.ps1"] }`), or through a `.cmd` wrapper next to it:

```bat
@pwsh -NoProfile -File "%~dp0helper.ps1" %1
```

`args` are not supported for `.cmd` and `.bat` commands, because `cmd.exe` would interpret them; put them in the batch
file instead.

`--credential-helper` takes a command without arguments; a helper that needs `args` or a `timeout` is configured in the
file. `dotnet linq2db credentials set|list|remove|clear` use the helper named by `--credential-helper`, or by
`credentialHelper` in the `default` profile of the file given with `--config`:

```sh
dotnet linq2db credentials set --config .agents/linq2db-query.json --profile project-a/production --user app_reader
dotnet linq2db credentials list --credential-helper "$HOME/.config/linq2db/helpers/pass.sh"
```

Only the configuration file and the command line name the helper: there is no dedicated environment variable for it,
and MCP tool calls cannot set it (`%NAME%`/`${NAME}` references inside `command` and `args` are expanded as above).

## 3. Invocation

    <command> [<args>...] <verb>

- `<verb>` is the last argument: `get`, `store`, `erase` or `list`. Apart from the configured `args`, nothing else is
  ever passed in arguments, and no shell command line is built.
- On Linux and macOS a file whose name ends in `.sh` is run as `/bin/sh <file> [<args>...] <verb>`, so a script needs
  neither an execute permission nor a `#!` line. Any other file is executed directly.
- On Windows a `.cmd` or `.bat` file is run as `cmd.exe /d /v:off /s /c ""<file>" <verb>"`, which keeps a path with
  spaces and parentheses intact. cmd.exe expands `%NAME%` even inside quotes, so a batch file whose path contains `%`
  is refused, and `args` are not supported for batch files. Any other file is executed directly.
- `get` is required; `store`, `erase` and `list` are optional.
- A helper that does not implement a verb answers with the single line `unsupported=verb` and exit code 0.
  `dotnet linq2db credentials set|remove|list|clear` then report that the helper does not support the operation. New
  verbs may be added to version 1; a helper must answer an unknown verb this way.
- "Unsupported" is signalled in the output, never by an exit code: an exit code cannot be told apart from an ordinary
  failure of the tools a helper calls.
- The working directory is the user's home directory. Several clients (a CLI command and an MCP server) may run the
  helper at the same time; a helper must tolerate concurrent runs.

## 4. Framing and versioning

- Text is UTF-8. Lines end with LF; a helper may end its lines with CRLF, the client ignores the CR.
- Every line is `key=value`, split at the **first** `=`; the value is the rest of the line, including leading and
  trailing spaces. Key names are exact lower case. No line contains NUL.
- The request ends at end of input: the client closes standard input after writing it.
- Unknown keys are ignored by both sides.
- Values never contain a line break or another control character: the client refuses such values before it calls a
  helper (this also keeps a secret from forging a line of the answer).
- An answer that is not valid UTF-8, or has a non-empty line without `=`, is a failure.
- The first request line is `protocol=1`. A helper that does not understand the version answers with the single line
  `unsupported=protocol` and exit code 0.
- Version 1 changes only by addition (new optional keys, new optional verbs). An incompatible change is version 2 and
  is announced in the release notes.

## 5. Targets

- `dotnet linq2db credentials set --profile <p>` stores target `linq2db/<p>`; `--credentials <t>` and the `credentials`
  configuration property read target `<t>`.
- Targets that start with `linq2db/` are linq2db's own. They are case-insensitive: the client converts them to lower
  case with invariant-culture rules before sending them, so a helper compares them byte for byte. After the prefix they
  have no leading or trailing `/`, no empty segment, and no `.` or `..` segment.
- Any other target belongs to the helper's store and is sent exactly as written; the helper matches it by its own
  rules (a Vault path, a 1Password item, a URL).
- No target is empty, contains a control character, or starts with `-`. Helpers should still pass targets to other
  programs after `--`.
- `credentials list`, the count shown by `credentials clear`, and `credentials clear` itself use only the `list`
  records whose target starts with `linq2db/`; the client never erases any other target.

## 6. Verbs and answers

| verb | request keys | answer on success (exit code 0) |
| --- | --- | --- |
| `get` | `protocol`, `target` | found: `username=…` and `password=…`; not found: no output |
| `store` | `protocol`, `target`, `username`, `password` | nothing; an existing credential with this target is replaced |
| `erase` | `protocol`, `target` | optionally `removed=true` or `removed=false`; erasing a missing target is success |
| `list` | `protocol` | one record per credential: `target=…`, `username=…`, then an empty line; never a password |

`get`:

- Output that is empty, or holds only empty lines, means **not found**.
- Otherwise the answer must contain exactly one `username` and exactly one `password` line (either value may be
  empty); a missing or repeated key is a failure. Unknown keys are ignored.

`erase` without a `removed` line means the helper does not know whether the credential existed; the client reports it
as removed.

Exit codes:

- 0: the answer above (including "not found" and `unsupported=…`).
- Any other exit code: failure. The client shows the first non-empty line of standard error (at most 200 characters;
  a password the client sent in the request is replaced by `***`). It never shows standard output.

Example exchange for `get`:

```text
request (stdin)              answer (stdout)
protocol=1                   username=app_reader
target=linq2db/project-a     password=s3cret value
```

## 7. Limits

- The client reads at most 1 MB of standard output; a helper that writes more is stopped.
- When a user can answer a prompt (`LINQ2DB_CREDENTIAL_INTERACTIVE=1`), the client waits at most 60 seconds for any
  verb, so a keyring unlock dialog or a gpg passphrase prompt can be answered. Otherwise it waits at most 10 seconds.
  A configured `timeout` replaces both. After that it stops the helper process and everything it started, and reports
  that the helper did not answer.
- After a non-interactive run of a helper (same program and `args`) has timed out, the same linq2db-cli process does
  not start that helper again for 60 seconds and fails fast with a message asking to unlock the credential store; this
  matters mostly for the long-running MCP server. (A keyring prompter or `pinentry` is not the helper's child;
  it survives the stop, and starting the helper again would stack prompts.)
- After the helper exits, the client waits at most 5 seconds for its output pipes to close and for its request to be
  read; a helper must not leave a background process holding its standard input, output or error. Such a process is no
  longer part of the helper's process tree, so the client cannot stop it: it reports the failure and, as after a
  timeout, does not start that helper again for 60 seconds in the same process.
- Standard error is read to the end, but only its first 4 KB are kept for the error message; a password the client sent
  is removed from that text, including a part of it cut off at the 4 KB boundary.

## 8. Environment

- The helper inherits the client's environment (a keyring, `gpg-agent` or a CLI session needs it).
- The client sets `LINQ2DB_CREDENTIAL_INTERACTIVE` to `1` when a user can answer a prompt (a command run in a terminal)
  and to `0` otherwise (the MCP server, redirected input), on every operating system. A helper should not prompt when it
  is `0`, and should fail fast instead.
- Secrets are never placed in arguments or environment variables.

## 9. Promises

The client:

- passes secrets only on the helper's standard input and reads them only from its standard output;
- never logs or prints standard output, and never includes it in an error message (the Docker adapter is the one
  exception, section 13);
- validates targets and values before calling (sections 4 and 5);
- never erases a target outside `linq2db/` (section 5).

The helper:

- reads the request only from standard input, and never prompts on it (standard input is the protocol);
- writes secrets only to standard output, never to standard error, a log, or a file it does not own;
- **never reports "not found" or success when its store failed or is locked**: it exits with a non-zero code and says
  why on standard error;
- exits; it does not stay in the background holding the client's pipes.

## 10. Security

linq2db-cli runs the configured helper as given, like git's `credential.helper`; securing the helper (where it lives and
who can change it) is the user's responsibility.

## 11. Example helper

[`CredentialHelpers/secrethelper.cs`](CredentialHelpers/secrethelper.cs) is a complete helper for protocol 1 in under
200 lines of C#: a .NET file-based app that keeps credentials in a JSON file. It is an illustration of the protocol, not
a secure store — the file is plain text protected only by file permissions. The linq2db-cli tests run it exactly as
shown below, with `dotnet run`, on Linux and Windows, so it cannot drift from this specification.

Run it with `dotnet run` (the first run builds it, hence the `timeout`):

```json
{
  "default": {
    "credentialHelper": { "command": "dotnet", "args": ["run", "${HOME}/helpers/secrethelper.cs", "--"], "timeout": 120 }
  }
}
```

```sh
dotnet linq2db credentials set --config .agents/linq2db-query.json --profile dev --user reader
dotnet linq2db query --config .agents/linq2db-query.json --credentials linq2db/dev --provider SQLite --connection-string "Data Source={0}" --sql "select 1"
```

Or build it once and configure the executable (`secrethelper`, or `secrethelper.exe` on Windows), which starts faster:

```sh
dotnet build secrethelper.cs -o ~/.local/lib/linq2db-secrethelper
dotnet linq2db credentials list --credential-helper ~/.local/lib/linq2db-secrethelper/secrethelper
```

The file is `$SECRETHELPER_FILE`, or `~/.linq2db-secrethelper.json` by default. `dotnet` must not print anything else
on standard output: if it shows its first-run welcome text, run any `dotnet` command once, or set `DOTNET_NOLOGO=1`.

## 12. Starter scripts for secret-tool and pass

On Linux and macOS, `dotnet linq2db credentials helper init` writes a starter helper: a POSIX `sh` script over a secret
store's command-line tool. The script contains no secrets; it is yours to review and edit.

```sh
dotnet linq2db credentials helper init --backend secret-tool --config .agents/linq2db-query.json
dotnet linq2db credentials helper init --backend pass -o "$HOME/bin/linq2db-pass.sh"
```

- `--backend secret-tool`: the Secret Service (GNOME Keyring, KWallet) through `secret-tool` (package
  `libsecret-tools` on Debian/Ubuntu). Items carry the attributes `service=linq2db-cli`, `target`, `user`.
- `--backend pass`: [pass](https://www.passwordstore.org/) (an initialized store: `pass init <gpg-id>`). Entries are
  `linq2db-cli/<target>` with the password on the first line and `user: <name>` on the second.
- The script is written to `$XDG_CONFIG_HOME/linq2db/helpers/<backend>.sh` (`~/.config/linq2db/helpers/<backend>.sh`)
  unless `--output`/`-o` names another path, with permissions `0700` (a new directory also gets `0700`). An existing
  file is replaced only with `--force`.
- With `--config <file>` the script is recorded as `credentialHelper` in that file's `default` profile (the file is
  created when missing). An existing different `credentialHelper` is replaced only with `--force`.
- No script is generated on Windows, where Windows Credential Manager is the default; any `.exe` or `.cmd` program that
  speaks this protocol can be configured there.

## 13. Docker credential helpers

Existing `docker-credential-*` programs (`secretservice`, `pass`, `osxkeychain`, `wincred`) speak Docker's own protocol,
not this one. linq2db-cli can use them through a built-in adapter, declared explicitly in the configuration file:

```json
"credentialHelper": { "command": "docker-credential-secretservice", "protocol": "docker" }
```

The adapter follows Docker's contract, not this specification. It was verified with docker-credential-helpers v0.9.9
on Linux (`secretservice` and `pass`); other versions, `docker-credential-osxkeychain` (macOS) and
`docker-credential-wincred` (Windows) are expected to work but are not verified.

- Targets follow section 5 and are sent as Docker's `ServerURL` unchanged.
- "Not found" is Docker's exit code 1 with `credentials not found in native keychain` on standard output. A locked
  keyring gives the same answer, so the adapter reports **"not found (or the keyring is locked)"**.
- Docker helpers print their error messages on standard output; on a failed run (which carries no credential) the
  adapter shows the first line of standard error, or of standard output when standard error is empty.
- `list` keeps only `linq2db/` entries (Docker's own registry logins share the store). `secretservice` lists item labels
  of the form `Registry credentials for <url>`; the adapter strips that prefix. An entry that mentions `linq2db/` but
  does not start with it after stripping is a format the adapter does not know: it **fails** rather than silently
  dropping one of your credentials.
- `erase` of a missing item succeeds with some helpers and fails with others, so the adapter lists before erasing.
