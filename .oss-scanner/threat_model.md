# Threat model

## What this project does and where untrusted input enters

linq2db is a .NET ORM: it translates LINQ expression trees into a SQL AST and then into provider-specific SQL text,
executes it through ADO.NET providers and materializes the results. It runs in-process inside the consuming
application; the database and its credentials are configured by that application.

The trust split:

- **Trusted:** the application's code and configuration — the shape of LINQ queries, mapping (attributes, fluent
  mapping, `MappingSchema`), connection strings, table and column names, and the format strings of `Sql.Expression`,
  `Sql.Function` and `Sql.Extension` attributes.
- **Untrusted:** *values* that flow into a query — constants and captured variables in a LINQ expression, arguments of
  compiled queries, entity property values passed to `Insert` / `Update` / `Merge` / `BulkCopy`, and the
  interpolated arguments of `FromSql($"...")` and the parameter values passed alongside raw SQL
  (`FromSql(RawSqlString, params object[])`, `DataConnection.Execute(sql, DataParameter[])`). An application must be able to pass any
  string, number, date, GUID or binary value from an attacker without that value changing the meaning of the SQL.
- **Untrusted, if the host exposes it:** the request body received by the remote-query servers
  (`LinqToDB.Remote.*`, see below).

## Components that matter most / least

- **Most:** everything that turns a value into SQL text.
  - Parameter generation, and the cases where values are inlined as SQL literals instead: `InlineParameters`,
    `Sql.ToSql`, values the optimizer folds into literals, and the multiple-rows bulk copy path, which renders
    literals unless `BulkCopyOptions.UseParameters` is set (`BulkCopyType.MultipleRows`, and `Default` /
    `ProviderSpecific` on providers without a native bulk copy). String, binary, date/time and GUID literal escaping
    lives in each provider's `SqlBuilder` and `MappingSchema` value-to-SQL converters under
    `Source/LinqToDB/Internal/DataProvider/<Provider>/`.
  - `LIKE` pattern escaping for `Contains` / `StartsWith` / `EndsWith` with untrusted search text.
  - Identifier quoting in the provider `SqlBuilder.Convert(..., ConvertType)` overrides: a name that reaches it must
    come out as one quoted identifier.
- **Remote query servers** — `Source/LinqToDB/Remote/LinqService.cs` and the gRPC, WCF, HttpClient and SignalR hosts.
  The client sends a serialized SQL AST that the server deserializes (`Internal/Remote/LinqServiceSerializer.cs`) and
  executes against a server-side connection. Authentication and authorization are the host application's job;
  linq2db provides `AllowUpdates` (rejects non-`SELECT` statements) and the overridable `ValidateQuery`.
- **Less:** `LinqToDB.Scaffold`, `LinqToDB.CLI`, `LinqToDB.Templates` (T4), `LinqToDB.LINQPad`, the analyzers and code
  generators. They are developer tools that read a database schema chosen by the developer; their output is reviewed
  source code.
- **Out of scope:** `Tests/`, `Build/`, `.github/`, `.claude/`, benchmark and example projects; the ADO.NET providers
  themselves.

## How to exercise it

- The image builds the core library and the main test suite for `net10.0` under `/src`.
- `linq2db-oss-build` rebuilds after an edit (packages are already restored, no network needed).
- `linq2db-oss-test` runs the whole suite against SQLite; `linq2db-oss-test
  "FullyQualifiedName~<TestClassOrMethod>"` runs a subset. Only `SQLite.MS` is available
  offline, so reproducers should target SQLite where the bug is not provider-specific; for a provider-specific SQL
  generation bug, the generated SQL text (`query.ToSqlQuery().Sql`, or `DataConnection.LastQuery`) is sufficient
  evidence.
- Tests live in `Tests/Linq/`; fixtures derive from `TestBase` and take a `[DataSources] string context` parameter
  (template: `Tests/Tests.Playground/TestTemplate.cs`).

## How you rate severity

- **Critical:** an untrusted *value* passed through a documented, parameterized or escaped API changes the meaning of
  the generated SQL (SQL injection) in the default configuration, on any supported provider.
- **High:** the same, but only with a non-default but documented option (`InlineParameters`, `MultipleRows` bulk copy,
  a specific provider dialect or version); or a remote server accepting a statement that `AllowUpdates = false` /
  `ValidateQuery` should have rejected.
- **Medium:** untrusted values cause wrong results that cross a row/tenant filter the application expressed in LINQ
  (for example a predicate silently dropped); unbounded resource use from an untrusted value.
- **Low:** problems reachable only through trusted input (mapping, attribute format strings, configuration), denial of
  service by a crafted LINQ expression shape that only application code can build, and information disclosure through
  exception messages.

## Anything to leave alone

- The SQL *text* of raw SQL APIs (`FromSql(RawSqlString, ...)`, `DataConnection.Execute(string, ...)`,
  `Sql.Expression` format strings) is trusted by design; concatenating untrusted data into it is an application bug,
  not a linq2db one. The `FormattableString` overloads must parameterize their arguments.
- Table / column / schema names from mapping or `TableName()` are trusted.
- Missing authentication or authorization on a remote-query endpoint is the host's configuration, not a linq2db defect.
