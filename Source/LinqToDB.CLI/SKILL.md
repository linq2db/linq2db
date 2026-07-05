# linq2db CLI Agent Skill

Use `dotnet linq2db` to run linq2db command-line tools.

## Query Command

Use `dotnet linq2db query` to execute a read-only SQL query against a database supported by linq2db.

Required command-line input:

- `--sql <sql>` provides SQL text directly.
- `--sql-file <file>` reads SQL text from a file.
- Exactly one of `--sql` or `--sql-file` must be specified.
- SQL text is command-line only and cannot be provided by configuration profiles.

Connection settings:

- `--provider <provider>` is the linq2db provider name.
- `--connection-string <connection-string>` is the database connection string.
- `--user <user>` and `--password <password>` are optional values for connection string formatting.
- The final connection string is always produced with `string.Format(connectionString, user, password)`.
- Use `{0}` in the connection string for the user value and `{1}` for the password value.

Configuration profiles:

- Use `--config <file>` to load query settings from a JSON configuration file.
- Use `--profile <name>` to select a named profile from that file.
- If `--profile` is omitted, the `default` profile is used.
- Named profiles inherit missing values from the `default` profile.
- Command-line values override profile values.
- `unsafeSql` can be set only in configuration profiles.

Example configuration:

```json
{
  "default": {
    "provider"        : "SqlServer",
    "connectionString": "Server=.;Database=Test;User Id={0};Password={1};TrustServerCertificate=True",
    "unsafeSql"       : "deny",
    "output"          : "json"
  },
  "uat": {
    "unsafeSql": "confirm"
    "user"     : "readonly_user",
    "password" : "secret",
  },
  "dev": {
    "unsafeSql": "allow"
  }
}
```

Output:

- `--output json` writes JSON output. This is the default and preferred format for agents.
- `--output csv` writes CSV output.
- `--output-file <file>` writes command output to a file.
- When `--output-file` is not specified, output is written to stdout.

Safety:

- Default mode is safe mode: unsafe SQL is rejected unless configuration explicitly allows it.
- Safe mode can be relaxed only by creating a configuration profile with `unsafeSql: "confirm"` or `unsafeSql: "allow"`.
- The query command always accepts only one SQL statement per invocation. Multiple statements are rejected even when unsafe SQL is allowed.
- Single-statement execution is a hard command contract for all providers. SQL Server is checked with ScriptDom AST; other providers currently use a generic best-effort validator until provider-specific parsers are added.
- If an agent needs to execute several independent operations, it should run several separate `query` commands.
- Multi-statement workflows that rely on session state, such as temporary tables, are not supported yet.
- The query command is intended for read-only SQL.
- DML, DDL, and `EXEC` are rejected before execution unless unsafe SQL is explicitly allowed.
- SQL Server queries are validated using the SQL Server ScriptDom parser.
- Other providers use a generic read-only validator.
- `unsafeSql: "deny"` rejects unsafe SQL. This is the default.
- `unsafeSql: "confirm"` allows unsafe SQL only when `--allow-unsafe-sql` is specified.
- `unsafeSql: "allow"` allows unsafe SQL without confirmation.
- Use `confirm` or `allow` only in trusted development profiles.
- The SQL safety validator is a guardrail, not a security boundary. If an agent can edit the configuration file, it can change configuration-based safety policy.
- The strongest protection against agent mistakes is to execute SQL using a database account with limited permissions appropriate for the task. For read-only agent queries, prefer a read-only account. For development workflows that need DDL/DML, prefer a dedicated disposable database or a restricted development account.

Agent confirmation rule:

- An agent MUST ask the user for explicit confirmation before executing any unsafe SQL operation.
- Unsafe operations include DML, DDL, `EXEC`, procedure calls, multiple statements, or any SQL intended to modify schema, data, permissions, configuration, or server state.
- Multiple statements still cannot be executed by this command; split them into separate command invocations.
- This requirement applies even when the selected configuration profile has `unsafeSql: "allow"`.
- Do not create or modify configuration to enable unsafe SQL unless the user explicitly asks for that configuration change.
- When confirmation is granted for a single unsafe execution and the profile uses `unsafeSql: "confirm"`, pass `--allow-unsafe-sql`.

Common examples:

```bash
dotnet linq2db query --provider SQLite --connection-string "Data Source=data.db" --sql "select * from Person"
dotnet linq2db query --provider SQLite --connection-string "Data Source=data.db" --sql-file query.sql
dotnet linq2db query --config query.json --profile uat --sql-file query.sql
dotnet linq2db query --config query.json --profile uat --user readonly_user --password secret --sql "select * from Person"
dotnet linq2db query --config query.json --profile dev --allow-unsafe-sql --sql "create table Test(Id int)"
```

## Scaffold Command

To be updated.

## Template Command

To be updated.

## Help

Use `dotnet linq2db help` for general help and `dotnet linq2db help <command>` for command-specific help.

## Skill Command

Use `dotnet linq2db skill` or `dotnet linq2db skills` to print this agent-oriented Markdown document to stdout.

The command has no options and doesn't accept arguments.

Use this command when an agent needs current instructions for using linq2db CLI from the installed tool instead of relying on repository files or external documentation.

To add the current tool-provided skill to this solution, run one of the following commands from the repository root.

PowerShell:

```powershell
New-Item -ItemType Directory -Force ".\.agents\skills\linq2db-cli" | Out-Null; dotnet linq2db skill > ".\.agents\skills\linq2db-cli\SKILL.md"
```

cmd.exe:

```cmd
mkdir ".\.agents\skills\linq2db-cli" 2>nul & dotnet linq2db skill > ".\.agents\skills\linq2db-cli\SKILL.md"
```

Keep this document synchronized with command behavior. When a command option, configuration rule, safety policy, or agent workflow changes, update `SKILL.md` and the `skill` command tests in the same change.
