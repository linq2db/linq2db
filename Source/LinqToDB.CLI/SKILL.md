# linq2db CLI Agent Skill

Use `dotnet linq2db` to run linq2db command-line tools.

## Query Command

Use `dotnet linq2db query` to execute a SQL query against a database supported by linq2db.
The core value proposition is that an agent can analyze your code together with data from your database.

Primary scenarios:

- Analyze application code together with live database data to investigate performance bottlenecks, data distribution issues, slow workflows, or suspicious production-like behavior.
- Let business analysts and domain experts ask code-aware questions that also depend on real database state, reference data, or business data samples.
- Prepare regression tests for bugs found in real data by querying the database first and then generating focused test cases, fixtures, or seed data.
- Support synchronous development of code and a development database: create or modify a table in a dev database, then immediately update mappings, DTOs, projections, or query code.
- Inspect schema and data shape before changing code, such as checking nullable values, enum-like columns, outliers, orphaned records, duplicates, and migration readiness.
- Compare expected business rules in code with actual persisted data to find data quality issues or rule drift.
- Generate documentation or diagnostics from database-backed facts, such as row counts, reference-data coverage, lookup values, and examples of real records.

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
    "commandTimeout"   : 30,
    "lockTimeout"      : 5,
    "maxRows"          : 1000,
    "unsafeSql"       : "deny",
    "output"          : "json"
  },
  "uat": {
    "unsafeSql": "confirm",
    "user"     : "readonly_user",
    "password" : "secret"
  },
  "dev": {
    "unsafeSql": "allow"
  }
}
```

Timeouts:

- `--command-timeout <seconds>` sets the ADO.NET command timeout through linq2db.
- `--lock-timeout <seconds>` applies a provider-specific lock wait timeout before the query when supported by the selected provider.
- Timeout values are non-negative integer seconds.
- `lockTimeout` is best-effort and currently has provider-specific behavior; unsupported providers ignore it.
- Connection timeout should be specified directly in the provider connection string using provider-supported keywords.

Supported `lockTimeout` providers:

- SQL Server: `SET LOCK_TIMEOUT <milliseconds>`.
- PostgreSQL: `SET lock_timeout = '<seconds>s'`.
- MySQL and MariaDB: `SET SESSION innodb_lock_wait_timeout = <seconds>`.
- SQLite: `PRAGMA busy_timeout = <milliseconds>`.
- Firebird: `SET LOCK TIMEOUT <seconds>`.

Output:

- `--output json` writes JSON output. This is the default and preferred format for agents.
- `--output json-table` writes a duplicate-safe JSON object with column metadata and rows as arrays.
- `--output csv` writes CSV output.
- `--output-file <file>` writes command output to a file.
- Existing output files are not replaced by default. Use `--overwrite` only when the user explicitly wants to replace the file.
- When `--output-file` is not specified, output is written to stdout.
- Query output reads database values using .NET `DbDataReader.GetProviderSpecificValue` and serializes them as strings using invariant culture and provider-specific safe formatting. `byte[]` values are emitted as base64 strings. `NULL` values are emitted as JSON `null`.
- For `json` output, projected column names must be unique because rows are emitted as JSON objects. The agent is responsible for adding explicit SQL aliases when a query could produce duplicate names.
- Duplicate column names are rejected for `json` output. Use explicit aliases or switch to duplicate-safe `json-table` output when column metadata and duplicate names must be preserved.
- `json-table` output contains `rowCount`, `truncated`, `columns`, and `rows`. Each column has `ordinal`, `name`, `fieldType`, `providerSpecificFieldType`, and `dataTypeName`; rows are arrays of string or null values.

Result limits:

- `--max-rows <count>` limits the number of result rows read by the query command.
- `maxRows` can be set in configuration profiles.
- The default limit is 1000 rows.
- When `json` or `csv` output is truncated, the command writes a truncation diagnostic to stderr.
- When `json-table` output is truncated, the command sets `truncated` to `true`.

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
dotnet linq2db query --config query.json --profile uat --command-timeout 30 --sql-file query.sql
dotnet linq2db query --config query.json --profile uat --sql-file query.sql
dotnet linq2db query --config query.json --profile uat --user readonly_user --password secret --sql "select * from Person"
dotnet linq2db query --config query.json --profile uat --output json-table --sql "select Id, Id from Person"
dotnet linq2db query --config query.json --profile uat --max-rows 100 --sql "select * from Person"
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
