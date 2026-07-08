# linq2db CLI Agent Skill

Use `dotnet linq2db` to run linq2db command-line tools.

## Query Command

Use `dotnet linq2db query` to execute a read-oriented SQL query against a database supported by linq2db.
`query` is a result-set-oriented database inspection command. It is intended for `SELECT` and other read-oriented SQL that returns rows for analysis. It is not a general DDL/DML command runner.
The core value proposition is that an agent can analyze your code together with data from your database.
For agent hosts that support MCP, prefer `dotnet linq2db mcp` for an integrated tool surface. Use `query` for lighter direct tool invocation when MCP is unavailable, not allowed by policy, or not needed for a specific environment.

Primary scenarios:

- Analyze application code together with live database data to investigate performance bottlenecks, data distribution issues, slow workflows, or suspicious production-like behavior.
- Let business analysts and domain experts ask code-aware questions that also depend on real database state, reference data, or business data samples.
- Prepare regression tests for bugs found in real data by querying the database first and then generating focused test cases, fixtures, or seed data.
- Support synchronous development of code and a development database by inspecting recently changed schema/data and then updating mappings, DTOs, projections, or query code.
- Inspect schema and data shape before changing code, such as checking nullable values, enum-like columns, outliers, orphaned records, duplicates, and migration readiness.
- Compare expected business rules in code with actual persisted data to find data quality issues or rule drift.
- Generate documentation or diagnostics from database-backed facts, such as row counts, reference-data coverage, lookup values, and examples of real records.
- Provide a portable direct database access path for environments where an MCP database server is unavailable, not allowed, or not needed, such as enterprise or restricted corporate environments.

Required command-line input:

- `--sql <sql>` provides SQL text directly.
- `--sql-file <file>` reads SQL text from a file.
- Exactly one of `--sql` or `--sql-file` must be specified.
- SQL text is command-line only and cannot be provided by configuration profiles.
- Path options support `%NAME%` and `${NAME}` environment variable expansion. This applies to `--config`, `--sql-file`, `--output-file`, and `--provider-location` or `providerLocation` from configuration.
- Referenced environment variables in path options must exist.

Connection settings:

- `--provider <provider>` is the linq2db provider name.
- Provider names must be names registered by linq2db itself, not test data source aliases from linq2db test configuration. For example, use `Oracle.Managed` in CLI configuration instead of a test alias like `Oracle.19.Managed`.
- `--provider-location <path>` loads an external ADO.NET provider assembly before the query provider is resolved. Use it when the provider is not bundled with `linq2db.cli` or when a specific provider assembly version must be supplied by the user.
- Loading an external assembly only makes it available to the process; compatibility with the selected linq2db provider and any provider dependencies remains the user's responsibility.
- External provider dependencies must be available next to the specified provider assembly or through normal application probing. The command does not scan the user's NuGet package cache to find missing dependencies.
- During external provider loading, the provider assembly directory can be used as the current directory so providers that resolve native files relative to the process directory can initialize.
- DB2 provider assemblies are not bundled because of package size. For DB2, install the matching IBM provider package separately and pass the path to `IBM.Data.Db2.dll` with `--provider-location` or `providerLocation` in the configuration profile.
- `--connection-string <connection-string>` is the database connection string.
- `--user <user>` and `--password <password>` are optional values for connection string formatting.
- `--connection-string-env <name>`, `--user-env <name>`, and `--password-env <name>` read those values from environment variables.
- Configuration profiles can use `connectionStringEnv`, `userEnv`, and `passwordEnv` for the same purpose.
- Value precedence is: command-line literal, command-line environment variable option, selected profile literal, selected profile environment variable option, inherited default profile literal or environment variable option.
- If an environment variable option is specified, the variable must exist.
- The final connection string is always produced with `string.Format(connectionString, user, password)`.
- Use `{0}` in the connection string for the user value and `{1}` for the password value.
- Escape literal braces in connection strings as `{{` and `}}`.
- Connection timeout is intentionally not exposed as a separate query command option. It is provider-specific and must be configured in the connection string using the selected provider's supported keywords.
- `--impersonate` enables Windows-only database access impersonation using the same resolved `user` and `password` values.
- `--impersonate-mode` selects the Windows `LogonUser` mode: `network-cleartext` (default, system code `8`), `interactive` (`2`), `network` (`3`), or `new-credentials` (`9`).
- `--impersonate` runs the whole database loop under one impersonation token: connection creation/opening, provider-specific session setup, command execution, reader metadata, row reads, output formatting, and reader/connection disposal.
- Configuration files, SQL files, provider assembly files, output files, stdout, and stderr writers are opened by the original process account before the impersonated database loop starts.
- `--impersonate` requires resolved `user` and `password` values. Use `--user-env` and `--password-env` or configuration `userEnv` and `passwordEnv` when credentials must not be written as literals.
- Windows impersonation uses network credentials intended for database access. It is not supported on Linux or macOS.

Configuration profiles:

- Use `--config <file>` to load query settings from a JSON configuration file.
- Use `--profile <name>` to select a named profile from that file.
- If `--profile` is omitted, the `default` profile is used.
- Named profiles inherit missing values from the `default` profile.
- Command-line values override profile values.
- `unsafeSql` can be set only in configuration profiles.

Parameter surface:

| Parameter | CLI option | `query` CLI | Config profile | `mcp` startup CLI | MCP tool API | Values |
|---|---|---:|---:|---:|---:|---|
| `description` | n/a | no | yes | no | no | non-secret profile description returned by `linq2db_info` |
| `config` | `--config` | yes | no | yes | no | path; supports `%NAME%` and `${NAME}` |
| `profile` | `--profile` | yes | no | yes | yes | profile name from config |
| `provider` | `--provider` | yes | yes | yes | no | linq2db provider name |
| `providerLocation` | `--provider-location` | yes | yes | yes | no | path; supports `%NAME%` and `${NAME}` |
| `connectionString` | `--connection-string` | yes | yes | yes | no | connection string; `{0}` user and `{1}` password placeholders are supported |
| `connectionStringEnv` | `--connection-string-env` | yes | yes | yes | no | environment variable name |
| `user` | `--user` | yes | yes | yes | no | string |
| `userEnv` | `--user-env` | yes | yes | yes | no | environment variable name |
| `password` | `--password` | yes | yes | yes | no | string |
| `passwordEnv` | `--password-env` | yes | yes | yes | no | environment variable name |
| `impersonate` | `--impersonate` | yes | yes | yes | no | boolean; JSON `true` or `false` in config |
| `impersonateMode` | `--impersonate-mode` | yes | yes | yes | no | `network-cleartext`, `interactive`, `network`, `new-credentials`, or system codes `8`, `2`, `3`, `9` |
| `commandTimeout` | `--command-timeout` | yes | yes | yes | no | non-negative integer seconds; `0` disables the option |
| `lockTimeout` | `--lock-timeout` | yes | yes | yes | no | non-negative integer seconds; `0` disables the option |
| `maxRows` | `--max-rows` | yes | yes | yes | yes | non-negative integer row count; `0` disables the limit |
| `output` | `--output` | yes | yes | yes | yes | `query`: `json`, `json-table`, or `csv`; `mcp`: `json` or `json-table`; `query` default is `json`, `mcp` default is `json-table` |
| `outputFile` | `--output-file` | yes | yes | no | no | path; supports `%NAME%` and `${NAME}` |
| `overwrite` | `--overwrite` | yes | no | no | no | boolean CLI flag |
| `unsafeSql` | n/a | no | config-only | no | no | `deny`, `confirm`, or `allow`; default is `deny` |
| `allowUnsafeSql` | `--allow-unsafe-sql` | yes | no | no | yes | boolean confirmation for `unsafeSql: "confirm"` |
| `sql` | `--sql` | yes | no | no | yes | single SQL statement text |
| `sqlFile` | `--sql-file` | yes | no | no | no | path; supports `%NAME%` and `${NAME}` |

Example configuration:

```json
{
  "default": {
    "description"     : "Use SQL Server T-SQL syntax. Prefer dbo schema qualification.",
    "provider"        : "SqlServer",
    "connectionString": "Server=.;Database=Test;User Id={0};Password={1};TrustServerCertificate=True",
    "commandTimeout"   : 30,
    "lockTimeout"      : 5,
    "maxRows"          : 1000,
    "unsafeSql"       : "deny",
    "output"          : "json",
    "impersonate"     : false,
    "impersonateMode" : "network-cleartext",
    "passwordEnv"     : "LINQ2DB_QUERY_PASSWORD"
  },
  "uat": {
    "unsafeSql": "confirm",
    "user"     : "readonly_user"
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
- Timeout value `0` disables the corresponding timeout option.
- `lockTimeout` is best-effort and currently has provider-specific behavior; unsupported providers ignore it.
- Connection timeout is intentionally left to the provider connection string. The query command exposes command timeout and provider-specific lock timeout only.

Supported `lockTimeout` providers:

- SQL Server: `SET LOCK_TIMEOUT <milliseconds>`.
- PostgreSQL: `SET lock_timeout = '<seconds>s'`.
- MySQL and MariaDB: `SET SESSION innodb_lock_wait_timeout = <seconds>`.
- SQLite: `PRAGMA busy_timeout = <milliseconds>`.

Output:

- `--output json` writes JSON output. This is the default and preferred format for agents.
- `--output json-table` writes a duplicate-safe JSON object with column metadata and rows as arrays.
- `--output csv` writes CSV output.
- `--output-file <file>` writes command output to a file.
- Existing output files are not replaced by default. Use `--overwrite` only when the user explicitly wants to replace the file.
- When `--output-file` is not specified, output is written to stdout.
- Output is streamed while rows are read. If the command is cancelled or fails after the output file is opened, the file can contain partial output.
- Query output reads database values using .NET `DbDataReader.GetProviderSpecificValue` and serializes them as strings using invariant culture and provider-specific safe formatting. Binary values are emitted using SQL-style hexadecimal notation like `0x010203`. `NULL` values are emitted as JSON `null`.
- Provider-specific reader behavior has been validated for SQL Server, Oracle, DB2, Informix, PostgreSQL, SAP HANA, Sybase, YDB, DuckDB, SQLite, Access ODBC, Firebird, MySQL, MariaDB, and ClickHouse provider families in the main test suite.
- Special provider-specific output formatting currently covers SQL Server, Oracle, DB2, Firebird, PostgreSQL, DuckDB, and MySQL/MariaDB wide decimal values. Other validated providers use `DbDataReader.GetProviderSpecificValue` with invariant fallback formatting unless a type-specific rule is added.
- For `json` output, projected column names must be unique because rows are emitted as JSON objects. The agent is responsible for adding explicit SQL aliases when a query could produce duplicate names.
- Duplicate column names are rejected for `json` output. Use explicit aliases or switch to duplicate-safe `json-table` output when column metadata and duplicate names must be preserved.
- `json-table` output contains `rowCount`, `truncated`, `columns`, and `rows`. Each column has `ordinal`, `name`, `fieldType`, `providerSpecificFieldType`, and `dataTypeName`; rows are arrays of string or null values.

Result limits:

- `--max-rows <count>` limits the number of result rows read by the query command.
- `maxRows` can be set in configuration profiles.
- The default limit is 1000 rows.
- `maxRows` value `0` disables the row limit.
- When `json` or `csv` output is truncated, the command writes a truncation diagnostic to stderr.
- When `json-table` output is truncated, the command sets `truncated` to `true`.

Guardrails:

- Default policy is guarded mode: unsafe SQL is rejected unless configuration explicitly allows it.
- Guarded mode can be relaxed only by creating a configuration profile with `unsafeSql: "confirm"` or `unsafeSql: "allow"`.
- The query command accepts exactly one user-provided SQL statement per invocation. This restriction applies to SQL text passed through `--sql` or `--sql-file`.
- The CLI may execute provider-specific setup commands internally, such as lock timeout configuration, before executing the user-provided SQL. Those internal commands are not part of the user SQL contract.
- User-provided SQL must contain exactly one statement. Multiple user-provided statements are rejected even when unsafe SQL is allowed.
- Single-statement user SQL is a hard command contract for all providers. SQL Server is checked with ScriptDom AST; other providers currently use a generic best-effort validator until provider-specific parsers are added.
- If an agent needs to execute several independent operations, it should run several separate `query` commands.
- Multi-statement workflows that rely on session state, such as temporary tables, are not supported yet.
- The query command is intended for read-oriented SQL that returns a result set.
- DML, DDL, and `EXEC` are rejected before execution unless unsafe SQL is explicitly allowed.
- `unsafeSql` only controls the read-only guardrail. It does not change the `query` command contract into a general DDL/DML execution workflow.
- General DDL/DML execution belongs to a future dedicated `execute` workflow.
- SQL Server queries are validated using the SQL Server ScriptDom parser.
- Other providers use a generic read-only validator.
- `unsafeSql: "deny"` rejects unsafe SQL. This is the default.
- `unsafeSql: "confirm"` allows unsafe SQL only when `--allow-unsafe-sql` is specified.
- `unsafeSql: "allow"` allows unsafe SQL without confirmation.
- Use `confirm` or `allow` only in trusted development profiles.
- The read-only SQL guard is a guardrail, not a security boundary. If an agent can edit the configuration file, it can change configuration-based unsafe SQL policy.
- All safety measures in this command are best-effort guardrails intended to help avoid agent mistakes; they are not absolute protection for a database.
- The strongest protection against agent mistakes is to execute SQL using a database account with limited permissions appropriate for the task. For read-only agent queries, prefer a read-only account. For development workflows that need DDL/DML, prefer a dedicated disposable database or a restricted development account.

Agent responsibility:

- The agent is responsible for keeping user-provided SQL to one statement and choosing explicit aliases when column names could be duplicated.
- The agent is responsible for choosing an output format appropriate to the task. Use `json-table` when duplicate column names or column metadata matter.
- The agent is responsible for setting provider-specific connection timeout keywords in the connection string when connection establishment must be bounded.
- The agent must treat guardrails as mistake-prevention aids, not as a security boundary.

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
dotnet linq2db query --provider DB2 --provider-location "C:\path\to\IBM.Data.Db2.dll" --connection-string "Server=localhost:50000;Database=testdb;UID=db2inst1;PWD=Password12!" --sql "select * from SYSIBM.SYSDUMMY1"
dotnet linq2db query --config query.json --profile uat --sql-file query.sql
dotnet linq2db query --config query.json --profile uat --user readonly_user --password secret --sql "select * from Person"
dotnet linq2db query --config query.json --profile uat --output json-table --sql "select p.Id, o.Id from Person p join Orders o on o.PersonId = p.Id"
dotnet linq2db query --config query.json --profile uat --max-rows 100 --sql "select * from Person"
```

## MCP STDIO Command

Use `dotnet linq2db mcp` to run a STDIO Model Context Protocol server exposing the shared query executor as the `linq2db_query` tool.
This is the preferred integration mode for MCP-capable agent hosts because it keeps server configuration at startup and exposes query execution through a typed tool call.

### MCP runtime discovery

The MCP server exposes `linq2db_info` so models can discover available profiles and SQL dialects before generating SQL.

Use `linq2db_info` before `linq2db_query` when the active provider or dialect is unknown.

`linq2db_info` returns only non-secret configuration metadata. It never returns connection strings, passwords, provider assembly paths, impersonation credentials, or environment variable values.

Configuration profile `description` values are returned by `linq2db_info`. Use them for non-secret profile guidance, such as provider-specific SQL hints or schema conventions.

MCP transport rules:

- The server reads newline-delimited JSON-RPC messages from stdin.
- The server writes only valid MCP JSON-RPC messages to stdout.
- Logging and diagnostics go to stderr.
- Do not write banners, progress, query diagnostics, or raw query output directly to stdout while the MCP server is running.

The `mcp` command uses the same connection/configuration option model as `query`, but SQL input is supplied through the MCP tool call instead of startup CLI arguments.

Startup/config boundary:

- `--config <file>` and `--profile <name>` select the configuration profile used by default.
- `--provider`, `--provider-location`, `--connection-string`, `--connection-string-env`, `--user`, `--user-env`, `--password`, `--password-env`, `--impersonate`, `--impersonate-mode`, `--command-timeout`, and `--lock-timeout` are startup/config settings.
- `--max-rows` and `--output` can set startup defaults for tool calls.
- `--sql`, `--sql-file`, `--output-file`, `--overwrite`, and `--allow-unsafe-sql` are not startup options for `mcp`.
- `outputFile` from a configuration profile is ignored by MCP tool calls so results are returned through the MCP response content instead of written to files.

Tool-call boundary:

- `sql` is required and contains the single SQL statement to execute.
- `profile` optionally selects a different profile from the startup `--config` file.
- `maxRows` optionally overrides the startup/config row limit.
- `output` optionally overrides the startup/config output format. MCP supports only `json` and `json-table`.
- `allowUnsafeSql` confirms unsafe SQL only when the selected configuration profile uses `unsafeSql: "confirm"`.
- Provider, connection string, credentials, impersonation, provider assembly location, and timeout setup are not accepted through MCP tool input.

The MCP default output format is `json-table`, which preserves duplicate column names and carries `rowCount` and `truncated` in-band. The existing `query` command keeps `json` as its default.

Example MCP host registration:

```json
{
  "mcpServers": {
    "linq2db": {
      "command": "dotnet",
      "args": [
        "linq2db",
        "mcp",
        "--config",
        "C:\\path\\to\\linq2db-query.json",
        "--profile",
        "dev"
      ],
      "env": {
        "LINQ2DB_QUERY_PASSWORD": "secret"
      }
    }
  }
}
```

Agent confirmation rules for unsafe SQL are the same as for `query`. Tools are model-controlled; the host/client or agent workflow must obtain human confirmation before any unsafe operation is requested.

## Scaffold Command

This skill currently documents query and skill commands. Scaffold documentation will be expanded separately.

## Template Command

This skill currently documents query and skill commands. Template documentation will be expanded separately.

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

Keep this document synchronized with command behavior. When a command option, configuration rule, unsafe SQL policy, or agent workflow changes, update `SKILL.md` and the `skill` command tests in the same change.
