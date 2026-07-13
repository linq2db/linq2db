# linq2db CLI Agent Skill

Use `dotnet linq2db` to run linq2db command-line tools.

## Query Command

Use `dotnet linq2db query` to execute a read-oriented SQL query against a database supported by linq2db.
`query` is a result-set-oriented database inspection command. It is intended for `SELECT` and other read-oriented SQL that returns rows for analysis. It is not a general DDL/DML command runner.
The core value proposition is that an agent can analyze your code together with data from your database.
For agent hosts that support MCP, prefer `dotnet linq2db mcp` for an integrated tool surface. Use `query` for lighter direct tool invocation when MCP is unavailable, not allowed by policy, or not needed for a specific environment.

When database structure is unknown, inspect metadata before generating SQL:

1. Use `linq2db_info` to discover profiles, providers, dialects, defaults, and safety rules.
2. Use `linq2db_schema` or `dotnet linq2db schema` to inspect database objects.
3. Use `linq2db_query` or `dotnet linq2db query` to execute one concrete SQL statement.

Use `linq2db_execute` or `dotnet linq2db execute` only after explicit user approval for a write-capable operation.

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
- Literal `user` values also support `%NAME%` and `${NAME}` environment variable expansion. This can be used for local machine-qualified Windows users such as `%COMPUTERNAME%\linq2db_cli_svc`.
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
- `--user <user>` and configuration `user` support `%NAME%` and `${NAME}` environment variable expansion. Password literals do not use environment variable expansion; use `--password-env` or `passwordEnv` for secrets.
- `--connection-string-env <name>`, `--user-env <name>`, and `--password-env <name>` read those values from environment variables.
- Configuration profiles can use `connectionStringEnv`, `userEnv`, and `passwordEnv` for the same purpose.
- Value precedence is: command-line literal, command-line environment variable option, selected profile literal, selected profile environment variable option, inherited default profile literal or environment variable option.
- If an environment variable option is specified, the variable must exist.
- The final connection string is always produced with `string.Format(connectionString, user, password)`.
- Use `{0}` in the connection string for the user value and `{1}` for the password value.
- Escape literal braces in connection strings as `{{` and `}}`.
- `{0}` and `{1}` placeholder values are inserted raw. Use trusted startup/config sources for `user` and `password`; when values contain connection-string special characters such as `;`, `=`, `{`, or `}`, apply quoting supported by the selected ADO.NET provider.
- Connection timeout is intentionally not exposed as a separate query command option. It is provider-specific and must be configured in the connection string using the selected provider's supported keywords.
- `--impersonate` enables Windows-only database access impersonation using the same resolved `user` and `password` values.
- `--impersonate-mode` selects the Windows `LogonUser` mode: `network-cleartext` (default, system code `8`), `interactive` (`2`), `network` (`3`), or `new-credentials` (`9`).
- `--impersonate` runs the whole database loop under one impersonation token: connection creation/opening, provider-specific session setup, command execution, reader metadata, row reads, output formatting, and reader/connection disposal.
- Configuration files, SQL files, provider assembly files, output files, stdout, and stderr writers are opened by the original process account before the impersonated database loop starts.
- `--impersonate` requires resolved `user` and `password` values. Use `--user-env` and `--password-env` or configuration `userEnv` and `passwordEnv` when credentials must not be written as literals.
- Windows impersonation uses network credentials intended for database access. It is not supported on Linux or macOS.

## Supported Database Providers

`dotnet linq2db query` and `dotnet linq2db mcp` execute SQL through linq2db database providers.

The CLI can use provider/runtime dependencies bundled with the CLI package and selected externally loaded providers. Use linq2db provider names, not test data source aliases from linq2db tests.

`linq2db_info` returns a compact `supportedProviders` list with provider names, dialects, whether the provider runtime is bundled, and external-provider notes.

### Supported provider families

- SQL Server
- SQLite
- PostgreSQL
- MySQL / MariaDB
- Oracle Managed
- Firebird
- Sybase ASE
- ODBC
- OLE DB
- ClickHouse
- DuckDB
- YDB
- IBM DB2, external provider assembly required
- IBM Informix, external provider assembly required

### Bundled provider families

The CLI package includes provider/runtime dependencies for these provider families:

- SQL Server
- SQLite
- PostgreSQL
- MySQL / MariaDB
- Oracle Managed
- Firebird
- Sybase ASE
- ODBC
- OLE DB
- ClickHouse
- DuckDB
- YDB

Provider-specific behavior still depends on installed native/runtime dependencies where applicable. Some providers, such as ODBC and OLE DB, are bridge providers and their SQL syntax and runtime requirements depend on the selected driver.

### External provider loading

Some providers are not bundled because of package size, licensing, or deployment constraints.

Use `--provider-location` or `providerLocation` to load an external ADO.NET provider assembly.

Known external-provider scenario:

- DB2 / Informix through `IBM.Data.Db2.dll`

Example:

```bash
dotnet linq2db query \
  --provider DB2 \
  --provider-location "C:\providers\IBM.Data.Db2.dll" \
  --connection-string "Server=localhost:50000;Database=testdb;UID=user;PWD=password" \
  --sql "select * from SYSIBM.SYSDUMMY1"
```

For MCP:

```json
{
  "mcpServers": {
    "linq2db": {
      "command": "dotnet",
      "args": [
        "linq2db",
        "mcp",
        "--config",
        "query.json",
        "--profile",
        "db2-dev"
      ]
    }
  }
}
```

Config profile:

```json
{
  "db2-dev": {
    "provider": "DB2",
    "providerLocation": "C:\\providers\\IBM.Data.Db2.dll",
    "connectionStringEnv": "LINQ2DB_DB2_CONNECTION",
    "enableExecute": false
  }
}
```

External provider dependencies must be available next to the specified provider assembly or through normal application probing. The CLI does not scan the user's NuGet package cache.

### Provider Names

Use linq2db provider names.

Do not use test data source aliases from linq2db tests.

Examples:

| Use | Do not use |
| --- | --- |
| `Oracle.Managed` | `Oracle.19.Managed` |
| `SqlServer` | test-specific SQL Server source aliases |
| `PostgreSQL` | test-specific PostgreSQL source aliases |

If provider resolution fails and the name looks like a test data source alias, use the corresponding linq2db provider name instead.

## Schema Command

Use `dotnet linq2db schema` to return database object metadata through linq2db schema providers.
The command is intended for safe SQL generation and database inspection by agents.

`schema` returns tables, views when available, columns, primary keys, and foreign keys when requested and returned by the provider.
It does not accept SQL text, does not read table data, does not modify the database, and does not return procedures or functions.

Connection and profile settings use the same trusted startup/config boundary as `query`: `--config`, `--profile`, `--provider`, `--provider-location`, `--connection-string`, `--connection-string-env`, `--user`, `--user-env`, `--password`, `--password-env`, `--impersonate`, `--impersonate-mode`, and `--command-timeout`.

Schema options:

- `--prefer-provider-specific-types true|false`
- `--get-tables true|false`
- `--get-foreign-keys true|false`
- `--generate-char1-as-string true|false`
- `--ignore-system-history-tables true|false`
- `--default-schema <schema>`
- `--filter-schema <schema>`; repeat or comma-separate for multiple values
- `--filter-catalog <catalog>`; repeat or comma-separate for multiple values
- `--filter-table <table>`; repeat or comma-separate for multiple values; matches table name, `schema.table`, or `catalog.schema.table`
- `--filter-table regex:<pattern>` or `--filter-table rx:<pattern>`; regular expression table filter

Filters are positive-only. Omitted filters mean no restriction for that dimension.
Multiple values can be provided either as comma-separated CLI values or by repeating the same CLI option.
MCP `linq2db_schema` uses arrays: `filterSchemas`, `filterCatalogs`, and `filterTables`.
Values inside one filter dimension are combined with OR; different filter dimensions narrow the result together.
Table filters are exact, case-insensitive matches unless the value starts with `regex:` or `rx:`.
Regex table filters use a bounded match timeout and return an expected error if a pattern times out.

Output:

- `schema` outputs JSON only.
- `--output json` is accepted for explicitness.
- `csv` and `json-table` are not supported because schema metadata is graph-shaped, not tabular query output.
- `--output-file <file>` writes schema JSON to a file.
- Existing output files are not replaced by default. Use `--overwrite` only when the user explicitly wants to replace the file.

Effective schema options are returned in the JSON result so agents can understand which metadata was requested and which metadata was intentionally omitted.
Foreign keys are returned inside each table object as `foreignKeys`; there is no separate top-level foreign-key list.

Procedures and functions are intentionally unsupported:

- Do not request procedure metadata from `schema`.
- Do not infer that a database has no procedures or functions from `schema` output.
- Use a future dedicated routine-inspection workflow if routine metadata is needed.

Examples:

```bash
dotnet linq2db schema --provider SQLite --connection-string "Data Source=data.db"
dotnet linq2db schema --config query.json --profile dev --filter-schema dbo
dotnet linq2db schema --config query.json --profile dev --filter-table dbo.Customer,dbo.Order
dotnet linq2db schema --config query.json --profile dev --filter-table dbo.Customer --filter-table dbo.Order
dotnet linq2db schema --config query.json --profile dev --filter-table "Person,rx:^Child"
dotnet linq2db schema --config query.json --profile dev --get-foreign-keys false --output-file schema.json
```

## SQL Dialects

`linq2db_query` and `dotnet linq2db query` execute raw SQL. The SQL syntax must match the selected database provider.

Use `linq2db_info` to discover the selected profile's provider and dialect before generating SQL.

Typical dialect families:

| Provider family | Dialect |
| --- | --- |
| SQL Server | SQL Server T-SQL |
| SQLite | SQLite |
| PostgreSQL | PostgreSQL |
| MySQL / MariaDB | MySQL / MariaDB |
| Oracle Managed | Oracle SQL |
| Firebird | Firebird SQL |
| DB2 | IBM DB2 SQL |
| Informix | Informix SQL |
| ClickHouse | ClickHouse SQL |
| DuckDB | DuckDB SQL |
| Sybase ASE | Sybase ASE T-SQL |
| ODBC | Provider-specific SQL |
| OLE DB | Provider-specific SQL |
| YDB | YDB SQL |

Do not assume one dialect's row limiting, identifier quoting, date/time functions, or metadata syntax works for another provider.

Configuration profiles:

- Use `--config <file>` to load query settings from a JSON configuration file.
- Use `--profile <name>` to select a named profile from that file.
- If `--profile` is omitted, the `default` profile is used.
- Named profiles inherit missing values from the `default` profile.
- Command-line values override profile values.
- `enableExecute` can be set only in configuration profiles and defaults to `false`.

## Config Init Command

Use `dotnet linq2db config-init` to create or update a query/MCP configuration file.
It is an onboarding helper, not a general JSON editor.
When it writes a file, it emits normalized pretty JSON and does not preserve existing formatting or comments.

Defaults:

- Default config path is `.agents/linq2db-query.json`.
- Default profile is `default`.
- `config-init` creates the `.agents` directory when the default path is used.
- Initialized profiles include editable default values for `maxRows`, `output`, and `enableExecute`.
- The default initialized output is `json-table`, which is suitable for MCP and duplicate-safe query output.
- `config-init` writes common editable settings (`maxRows`, `output`, and `enableExecute`) into every created profile intentionally. This makes generated profiles self-explanatory and easier to edit manually.
- Named profiles still inherit missing values from `default` when those values are removed manually.

Required input:

- `--provider <provider>`.
- Exactly one of `--connection-string <connection-string>` or `--connection-string-env <name>`.

Supported initialization options:

- `--config <file>`.
- `--profile <name>`.
- `--description <text>`.
- `--provider <provider>`.
- `--provider-location <path>`.
- `--connection-string <connection-string>`.
- `--connection-string-env <name>`.
- `--max-rows <count>`.
- `--output json|json-table|csv`.
- `--if-exists error|replace|skip`.

Advanced profile fields such as `user`, `password`, `impersonate`, `commandTimeout`, `lockTimeout`, and `outputFile` are intentionally not exposed by `config-init`; edit the JSON manually when those fields are needed.

Configuration profiles are shared by `query` and `mcp`.
The `query` command supports `json`, `json-table`, and `csv`.
The MCP `linq2db_query` tool supports only `json` and `json-table`.
If the selected profile has `output: "csv"`, MCP calls must pass `output: "json-table"` or `output: "json"` explicitly, or the profile should be adjusted for MCP usage.

Examples:

```bash
dotnet linq2db config-init --provider SQLite --connection-string "Data Source=data.db"
dotnet linq2db config-init --config query.json --profile dev --description "Development database" --provider SqlServer --connection-string-env LINQ2DB_DEV_CONNECTION
dotnet linq2db config-init --config query.json --profile db2-dev --provider DB2 --provider-location "C:\providers\IBM.Data.Db2.dll" --connection-string-env LINQ2DB_DB2_CONNECTION
```

Parameter surface:

| Parameter | CLI option | `query` CLI | `execute` CLI | Config profile | `config-init` CLI | `mcp` startup CLI | MCP tool API | Values |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `description` | `--description` | no | no | yes | yes | no | no | non-secret profile description returned by `linq2db_info` |
| `config` | `--config` | yes | yes | no | yes | yes | no | path; supports `%NAME%` and `${NAME}` for query/MCP execution; `config-init` writes `.agents/linq2db-query.json` by default |
| `profile` | `--profile` | yes | yes | no | yes | yes | yes | profile name from config |
| `provider` | `--provider` | yes | yes | yes | yes | yes | no | linq2db provider name |
| `providerLocation` | `--provider-location` | yes | yes | yes | yes | yes | no | path; supports `%NAME%` and `${NAME}` for query/MCP execution |
| `connectionString` | `--connection-string` | yes | yes | yes | yes | yes | no | connection string; `{0}` user and `{1}` password placeholders are supported |
| `connectionStringEnv` | `--connection-string-env` | yes | yes | yes | yes | yes | no | environment variable name |
| `user` | `--user` | yes | yes | yes | no | yes | no | string; supports `%NAME%` and `${NAME}` expansion |
| `userEnv` | `--user-env` | yes | yes | yes | no | yes | no | environment variable name |
| `password` | `--password` | yes | yes | yes | no | yes | no | string |
| `passwordEnv` | `--password-env` | yes | yes | yes | no | yes | no | environment variable name |
| `impersonate` | `--impersonate` | yes | yes | yes | no | yes | no | boolean; JSON `true` or `false` in config |
| `impersonateMode` | `--impersonate-mode` | yes | yes | yes | no | yes | no | `network-cleartext`, `interactive`, `network`, `new-credentials`, or system codes `8`, `2`, `3`, `9` |
| `commandTimeout` | `--command-timeout` | yes | yes | yes | no | yes | no | non-negative integer seconds; `0` disables the option |
| `lockTimeout` | `--lock-timeout` | yes | yes | yes | no | yes | no | non-negative integer seconds; `0` disables the option |
| `maxRows` | `--max-rows` | yes | yes | yes | yes | yes | yes | non-negative integer row count; `0` disables the limit |
| `output` | `--output` | yes | yes | yes | yes | yes | yes | `query`, `execute`, and `config-init`: `json`, `json-table`, or `csv`; `mcp`: `json` or `json-table`; `query` default is `json`, MCP/config-init/execute default is `json-table` |
| `outputFile` | `--output-file` | yes | yes | yes | no | no | no | path; supports `%NAME%` and `${NAME}` |
| `overwrite` | `--overwrite` | yes | yes | no | no | no | no | boolean CLI flag |
| `enableExecute` | n/a | no | required | yes | default only | no | no | boolean; default is `false`; required for `execute` and `linq2db_execute` |
| `enableExecuteTool` | `--enable-execute-tool` | no | no | no | no | yes | no | boolean MCP startup flag; registers `linq2db_execute`; profile `enableExecute` is still required |
| `ifExists` | `--if-exists` | no | no | no | yes | no | no | `error`, `replace`, or `skip`; default is `error` |
| `sql` | `--sql` | yes | yes | no | no | no | yes | single SQL statement text |
| `sqlFile` | `--sql-file` | yes | yes | no | no | no | no | path; supports `%NAME%` and `${NAME}` |

Example configuration:

```json
{
  "mcp": {
    "title": "linq2db Development Databases",
    "description": "Databases used for linq2db development and provider testing.",
    "instructions": "Use this server only for linq2db development, diagnostics, and provider compatibility testing."
  },
  "default": {
    "description"     : "Use SQL Server T-SQL syntax. Prefer dbo schema qualification.",
    "provider"        : "SqlServer",
    "connectionString": "Server=.;Database=Test;User Id={0};Password={1};TrustServerCertificate=True",
    "commandTimeout"   : 30,
    "lockTimeout"      : 5,
    "maxRows"          : 1000,
    "enableExecute"    : false,
    "output"          : "json",
    "impersonate"     : false,
    "impersonateMode" : "network-cleartext",
    "passwordEnv"     : "LINQ2DB_QUERY_PASSWORD"
  },
  "uat": {
    "user": "readonly_user"
  },
  "dev": {
    "enableExecute": true
  }
}
```

The optional top-level `mcp` section describes this configured MCP server instance during initialization:

- `title` is its human-readable display name.
- `description` identifies the databases or application domain served by this instance.
- `instructions` supplies additional instance-specific guidance that MCP hosts can add to the model context. It is appended to the built-in server instructions rather than replacing them.
- `mcp` is a reserved section name, is not a profile, and is not returned in the `linq2db_info` profile list.
- `config-init` preserves an existing `mcp` section but does not create or modify it.

Without an `mcp` section, the server identifies itself as `linq2db Database Tools` and describes its provider-aware schema and SQL capabilities. Built-in instructions direct agents through `linq2db_info`, `linq2db_schema`, and read-only `linq2db_query`; recommend `linq2db_skill` for the full usage guide; and restrict `linq2db_execute` to explicitly approved operations when that tool is available.

Keep MCP host registration and linq2db server configuration separate:

- The MCP host registration name, `command`, `args`, and `env` control how a server process is started.
- The file passed through `--config` contains the server's `mcp` identity and its database profiles.
- Use a separate configuration file and server registration for each project or database group that needs distinct model context.
- Do not combine unrelated projects into one server merely because they use the same linq2db CLI executable.

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
- `--output csv` writes database values without spreadsheet-specific escaping and is intended for machine processing.
- Do not open CSV containing untrusted values directly in spreadsheet applications, which can interpret values beginning with characters such as `=`, `+`, `-`, or `@` as formulas. Use `json` or `json-table` instead for untrusted data or interactive inspection.
- Configuration profiles can store `json`, `json-table`, or `csv` because profiles are shared by `query` and `mcp`.
- MCP tool calls support only `json` and `json-table`. Use `linq2db_info` to check `defaultOutputSupportedByMcp` before relying on a profile's default output in MCP mode.
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

- `query` and `linq2db_query` are read-only by contract.
- `execute` and `linq2db_execute` are write-capable by contract.
- User-provided SQL must contain exactly one statement. This restriction applies to SQL text passed through `--sql`, `--sql-file`, or MCP tool input.
- The CLI may execute provider-specific setup commands internally, such as lock timeout configuration, before executing the user-provided SQL. Those internal commands are not part of the user SQL contract.
- A single-statement contract keeps result formatting predictable and avoids ambiguous multi-result or partially destructive batches.
- Single-statement user SQL is a hard command contract for all providers. SQL Server is checked with ScriptDom AST; other providers currently use a generic best-effort validator until provider-specific parsers are added.
- If an agent needs to execute several independent operations, it should run several separate command or tool calls.
- Multi-statement workflows that rely on session state, such as temporary tables, are not supported yet.
- `query` is intended for read-oriented SQL. DML, DDL, `EXEC`, procedure calls, transaction control, permission changes, and administrative commands are rejected before execution.
- If the SQL guard cannot confidently classify SQL as read-only, `query` rejects it.
- `execute` requires the selected configuration profile to set `enableExecute` to `true`.
- `linq2db_execute` is not registered by default. Start MCP with `--enable-execute-tool` to publish it, and still use a profile with `enableExecute: true`.
- When write-capable SQL is executed, the command writes a diagnostic notice to stderr without including SQL text or credentials.
- The read-only SQL guard is a guardrail, not a security boundary. Use restricted database accounts as the primary protection.
- The strongest protection against agent mistakes is to execute SQL using a database account with limited permissions appropriate for the task. For read-only agent queries, prefer a read-only account. For development workflows that need DDL/DML, prefer a dedicated disposable database or a restricted development account.

Agent responsibility:

- The agent is responsible for keeping user-provided SQL to one statement and choosing explicit aliases when column names could be duplicated.
- The agent is responsible for choosing an output format appropriate to the task. Use `json-table` when duplicate column names or column metadata matter.
- The agent is responsible for setting provider-specific connection timeout keywords in the connection string when connection establishment must be bounded.
- The agent must treat guardrails as mistake-prevention aids, not as a security boundary.

Agent confirmation rule:

- An agent MUST ask the user for explicit confirmation before using `execute` or `linq2db_execute`.
- Write-capable operations include DML, DDL, `EXEC`, procedure calls, transaction control, permission changes, administrative commands, or any SQL intended to modify schema, data, permissions, configuration, or server state.
- Multiple statements still cannot be executed; split them into separate command or tool calls.
- Do not create or modify configuration to set `enableExecute: true` unless the user explicitly asks for that configuration change.

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

## Execute Command

Use `dotnet linq2db execute` for write-capable SQL after explicit user approval.

`execute` uses the same provider loading, connection resolution, impersonation, command timeout, lock timeout, row limiting, and output formatting path as `query`.

Differences from `query`:

- `execute` requires the selected configuration profile to set `enableExecute` to `true`.
- There is no per-call execute confirmation option. The command name plus trusted profile setting are the capability boundary.
- Direct provider/connection CLI options can still override profile connection settings, but they do not enable execution by themselves.
- Multiple SQL statements are still rejected.
- `recordsAffected` is reported only in `json-table` output, and only when the provider returns it; `json` and `csv` output never include it. `json-table` is the recommended output for `execute` for this reason.

Example:

```bash
dotnet linq2db execute --config query.json --profile dev --sql "update Person set Name = 'x' where Id = 1"
```

## MCP STDIO Command

Use `dotnet linq2db mcp` to run a STDIO Model Context Protocol server exposing shared schema and query tools.
This is the preferred integration mode for MCP-capable agent hosts because it keeps server configuration at startup and exposes query execution through a typed tool call.

The MCP server exposes these tools:

- `linq2db_info` returns non-secret runtime configuration, profiles, providers, dialects, and safety rules.
- `linq2db_schema` returns provider-independent database object metadata for the selected profile.
- `linq2db_query` executes one read-only SQL statement using the selected profile/provider.
- `linq2db_execute` executes one write-capable SQL statement when the server was started with `--enable-execute-tool` and the selected profile has `enableExecute: true`.
- `linq2db_skill` returns this full skill document.

Recommended model workflow:

1. Call `linq2db_info` when available profiles, active profile, provider, or SQL dialect are unknown.
2. Call `linq2db_schema` when table names, column names, keys, relationships, schemas, or catalogs are unknown.
3. Generate provider-appropriate SQL for the selected dialect.
4. Call `linq2db_query` with one SQL statement.
5. Use `json-table` when joins, duplicate column names, expressions, or column metadata are involved.
6. Use `linq2db_execute` only after explicit user approval for a write-capable operation.
7. Call `linq2db_skill` when detailed usage guidance is needed.

Do not pass provider names, connection strings, passwords, provider assembly paths, or impersonation credentials to `linq2db_schema`, `linq2db_query`, or `linq2db_execute`. Those values are configured at MCP server startup or in trusted configuration profiles.

`linq2db_schema` reads database metadata only. It does not accept SQL text, does not read table data, does not modify schema, and does not return procedures or functions.

### MCP runtime discovery

The MCP server exposes `linq2db_info` so models can discover available profiles and SQL dialects before generating SQL.

Use `linq2db_info` before `linq2db_query` when the active provider or dialect is unknown.

If `linq2db_info` returns `defaultProfileUsable: false`, the selected startup/default profile cannot execute queries directly. In that case, choose one of the returned `profiles` and pass its `name` as the `profile` argument to `linq2db_query`.

Use `linq2db_skill` for the full linq2db CLI/MCP usage guide, including supported providers and external provider loading instructions.

`linq2db_info` returns only non-secret configuration metadata. It never returns connection strings, passwords, provider assembly paths, impersonation credentials, or environment variable values.

Configuration profile `description` values are returned by `linq2db_info`. Use them for non-secret profile guidance, such as provider-specific SQL hints or schema conventions.

Each returned profile includes `defaultOutput` and `defaultOutputSupportedByMcp`.
If `defaultOutputSupportedByMcp` is `false`, call `linq2db_query` with explicit `output: "json-table"` or `output: "json"`.
Top-level `supportedOutputFormats` lists MCP tool-call output formats.
Top-level `queryCommandOutputFormats` lists direct `query` command output formats.

MCP transport rules:

- The server reads newline-delimited JSON-RPC messages from stdin.
- The server writes only valid MCP JSON-RPC messages to stdout.
- Logging and diagnostics go to stderr.
- Do not write banners, progress, query diagnostics, or raw query output directly to stdout while the MCP server is running.

The `mcp` command uses the same connection/configuration option model as `query`, but SQL input is supplied through the MCP tool call instead of startup CLI arguments.
When the configuration contains a top-level `mcp` section, its `title`, `description`, and `instructions` identify the purpose of this specific server instance in the MCP initialization response. This allows multiple registered linq2db MCP servers to describe different application or database domains.

Startup/config boundary:

- `--config <file>` and `--profile <name>` select the configuration profile used by default.
- `--provider`, `--provider-location`, `--connection-string`, `--connection-string-env`, `--user`, `--user-env`, `--password`, `--password-env`, `--impersonate`, `--impersonate-mode`, `--command-timeout`, and `--lock-timeout` are startup/config settings.
- `--max-rows` and `--output` can set startup defaults for tool calls.
- `--enable-execute-tool` registers the write-capable `linq2db_execute` tool. It is off by default.
- `--sql`, `--sql-file`, `--output-file`, and `--overwrite` are not startup options for `mcp`.
- `outputFile` from a configuration profile is ignored by MCP tool calls so results are returned through the MCP response content instead of written to files.

Tool-call boundary:

- `sql` is required and contains the single SQL statement to execute.
- `profile` optionally selects a different profile from the startup `--config` file.
- `maxRows` optionally overrides the startup/config row limit.
- `output` optionally overrides the startup/config output format. MCP supports only `json` and `json-table`.
- Provider, connection string, credentials, impersonation, provider assembly location, and timeout setup are not accepted through MCP tool input.

The MCP default output format is `json-table`, which preserves duplicate column names and carries `rowCount`, `truncated`, and `recordsAffected` in-band when available. The existing `query` command keeps `json` as its default.

MCP host configuration schemas vary. The following example uses the `servers`/`type` form; hosts that use `mcpServers` express the same `command`, `args`, and `env` values under that key.

Example MCP host registration:

```json
{
  "servers": {
    "ERP Databases": {
      "type": "stdio",
      "command": "dotnet-linq2db",
      "args": [
        "mcp",
        "--config",
        "C:\\mcp\\erp.json"
      ],
      "env": {
        "ERP_CONNECTION_STRING": "..."
      }
    },
    "Analytics Databases": {
      "type": "stdio",
      "command": "dotnet-linq2db",
      "args": [
        "mcp",
        "--config",
        "C:\\mcp\\analytics.json"
      ],
      "env": {
        "ANALYTICS_CONNECTION_STRING": "..."
      }
    }
  }
}
```

`C:\mcp\erp.json` can identify the ERP project and contain its related profiles:

```json
{
  "mcp": {
    "title": "ERP Databases",
    "description": "Operational databases for the ERP project.",
    "instructions": "Use this server only for ERP development and diagnostics. Inspect the selected database schema before generating SQL."
  },
  "default": {
    "provider": "SqlServer",
    "connectionStringEnv": "ERP_CONNECTION_STRING"
  }
}
```

The analytics server should use a separate `analytics.json` with its own `mcp` metadata, profiles, and environment-variable names. Both registrations can launch the same executable; their config files give the resulting server instances different scope and guidance.

To publish write-capable execution, add `--enable-execute-tool` to MCP startup args and use a trusted profile with `enableExecute: true`.

Tools are model-controlled; the host/client or agent workflow must obtain human confirmation before any write-capable operation is requested.

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

Keep this document synchronized with command behavior. When a command option, configuration rule, execute policy, or agent workflow changes, update `SKILL.md` and the `skill` command tests in the same change.
