---
area: CLI
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 14/14
coverage_tier_2: 11/11
---

# CLI

`dotnet-linq2db` -- the scaffolding dotnet tool. Invoked as `dotnet linq2db <command>` or `dotnet-linq2db <command>`. Ships as the `linq2db.cli` NuGet package (package type: `DotnetTool`). Targets `net8.0`, `net9.0`, `net10.0`.

Assembly name is `dotnet-linq2db` (set by `<AssemblyName>` in `LinqToDB.CLI.csproj:6`). The project uses a custom `.nuspec` file rather than `<IsTool>true` because it must pack multi-arch Windows executables (`win-x86`, `win-x64`, `win-arm64`) alongside the cross-platform managed DLL. A `MultiArchPublish` MSBuild target (`.csproj:38`) republishes the project for each RID after the main build.

## Subsystems

### Entry point and dispatch (`CommandLine/`)

`Program.cs:10` -- `Main` instantiates `LinqToDBCliController` and calls `Execute(args)`.

`CliController` (abstract) -- command registry and parser. `Execute(string[] args)` dispatches: arg[0] is the command name; if not found, falls to `_defaultCommand`. Option parsing supports `--long-name value` and `-x value` forms; `ImportCliOption` (JSON file) values are merged first, then CLI values override.

`LinqToDBCliController` (sealed) -- registers three commands: `HelpCommand`, `ScaffoldCommand`, `TemplateCommand`.

### Command abstractions (`CommandLine/Commands/`)

`CliCommand` (abstract) -- owns option registries; abstract `Execute(...) -> ValueTask<int>`.
`HelpCommand` (singleton) -- default command and `help` command. Width-aware line wrapping; fallback 80 when `Console.BufferWidth` unavailable (issue #3612).
`TemplateCommand` (singleton) -- extracts `Template.tt` from embedded resource.

### ScaffoldCommand (5-file partial class)

- `ScaffoldCommand.cs` -- registers all ~80 options into four `OptionCategory` groups.
- `ScaffoldCommand.Options.cs` -- option static fields. `General.Provider` is a required `StringEnumCliOption` over `DatabaseType` (15 values: Access, DB2, Firebird, Informix, SQLServer, MySQL, Oracle, PostgreSQL, SqlCe, SQLite, Sybase, SapHana, ClickHouseMySql, ClickHouseHttp, ClickHouseTcp, **DuckDB**). `DatabaseType` is a private enum at `ScaffoldCommand.Options.cs:1933`; `DuckDB` added at line 1951.
- `ScaffoldCommand.Configuration.cs` -- `ProcessScaffoldOptions(options)` builds a `ScaffoldOptions` object.
- `ScaffoldCommand.Execute.cs` -- `Execute` override: processes settings, handles `--architecture` restart, opens a `DataConnection`, then calls `Scaffold(...)`. Provider-specific connection setup: `ProviderName.DuckDB` requires no special setup (falls through the `break` case alongside ClickHouse and SQL Server at `ScaffoldCommand.Execute.cs:197-201`). The `DatabaseType.DuckDB` -> `ProviderName.DuckDB` mapping is at line 78. Architecture restart spawns a child process from the same assembly directory.
- `ScaffoldCommand.Interceptors.cs` -- interceptor loading. T4 path: `PreprocessTemplate` uses `Mono.TextTemplating.TemplateGenerator`, then Roslyn `CSharpCompilation` compiles in-memory. Assembly path: `Assembly.LoadFrom` + `DependencyContext`-based resolver.

### Option type system (`CommandLine/Options/`)

`CliOption` (abstract record) -- base for all options. Concrete option types: `BooleanCliOption`, `StringCliOption`, `StringEnumCliOption`, `NamingCliOption`, `ObjectNameFilterCliOption`, `StringDictionaryCliOption`, `ImportCliOption`.

`OptionType` enum -- `String`, `StringEnum`, `StringDictionary`, `Boolean`, `DatabaseObjectFilter`, `Naming`, `JSONImport`.

`NameFilter` -- compiled filter over database object names. Supports exact names, compiled regex patterns, 1-second `RegexOptions.Compiled` timeout.

### T4 host (`T4Host/`)

`LinqToDBHost` (public abstract) -- base class that user T4 templates inherit from. The class is public because mono.t4's codegen references it by name.

`Template.tt` -- the starter T4 template shipped as an embedded resource. The CLI extracts it verbatim when the user runs `dotnet linq2db template`.

## Key types

| Type | File | Role |
|---|---|---|
| `LinqToDBCliController` | `CommandLine/LinqToDBCliController.cs` | Concrete controller; registers 3 commands |
| `CliController` | `CommandLine/CliController.cs` | Abstract base; arg dispatch + option parsing |
| `ScaffoldCommand` | `CommandLine/Commands/ScaffoldCommand*.cs` | Core scaffold logic (5 partials) |
| `HelpCommand` | `CommandLine/Commands/HelpCommand.cs` | help command + default handler |
| `TemplateCommand` | `CommandLine/Commands/TemplateCommand.cs` | template extraction command |
| `CliCommand` | `CommandLine/Commands/CliCommand.cs` | Abstract command base |
| `CliOption` | `CommandLine/Options/CliOption.cs` | Abstract option base |
| `ImportCliOption` | `CommandLine/Options/ImportCliOption.cs` | JSON response-file option |
| `NamingCliOption` | `CommandLine/Options/NamingCliOption.cs` | JSON-only naming config option |
| `NameFilter` | `CommandLine/Options/NameFilter.cs` | DB object inclusion/exclusion filter |
| `LinqToDBHost` | `T4Host/LinqToDBHost.cs` | Public base for user T4 templates |
| `StatusCodes` | `CommandLine/StatusCodes.cs` | Exit code constants |

## Files (Tier 1 / Tier 2)

**Tier 1** (read in full):
- `Program.cs`
- `CommandLine/CliController.cs`
- `CommandLine/LinqToDBCliController.cs`
- `CommandLine/StatusCodes.cs`
- `CommandLine/Commands/CliCommand.cs`
- `CommandLine/Commands/ScaffoldCommand.cs`
- `CommandLine/Commands/ScaffoldCommand.Execute.cs`
- `CommandLine/Commands/ScaffoldCommand.Interceptors.cs`
- `CommandLine/Commands/ScaffoldCommand.Configuration.cs`
- `CommandLine/Commands/ScaffoldCommand.Options.cs`
- `CommandLine/Options/CliOption.cs`
- `T4Host/LinqToDBHost.cs`
- `Template.tt`
- `LinqToDB.CLI.csproj`

**Tier 2** (read in full): `HelpCommand.cs`, `TemplateCommand.cs`, `CommandExample.cs`, all `CommandLine/Options/*.cs`, `DotnetToolSettings.xml`, `linq2db.cli.nuspec`, `readme.md`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`.

## Inbound / outbound dependencies

**Outbound:**
- **SCAFFOLD** -- `Scaffolder`, `ScaffoldOptions`, `ScaffoldInterceptors`, `LegacySchemaProvider`.
- **Core `LinqToDB`** -- `DataConnection`, `DataOptions`, `ProviderName`, provider-specific types.
- **Mono.TextTemplating** -- T4 parsing.
- **Microsoft.CodeAnalysis.CSharp** -- Roslyn in-memory compilation.
- All provider ADO.NET packages bundled in the tool (SQLite, SqlClient, Firebird, MySqlConnector, Oracle.ManagedDataAccess.Core, Npgsql, AdoNetCore.AseClient, ODBC, OleDb, ClickHouse.Driver, Octonica.ClickHouseClient, **DuckDB.NET.Data.Full**) -- except IBM DB2/Informix and SAP HANA (too large).

**Inbound:** standalone tool; no other source project references `LinqToDB.CLI`.

## Known issues / debt

- `ScaffoldCommand.Interceptors.cs:56,138,139` -- unconditional debug log lines marked `// TODO: Verbose logging`.
- `HelpCommand.cs:179,312` -- workaround for `Console.BufferWidth` exception (issue #3612).
- `ScaffoldCommand.Execute.cs:159` -- file name collision/deduplication deferred.
- Architecture restart only works on Windows; `--architecture` silently ignored on Linux/macOS.
- IBM DB2 and Informix providers intentionally excluded from the tool package due to size.

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md) -- the scaffolding library this CLI wraps.
- [INTERCEPTORS area](../INTERCEPTORS/INDEX.md) -- the `ScaffoldInterceptors` base class.
- [T4-TEMPLATES area](../T4-TEMPLATES/INDEX.md) -- the T4 template includes.

<details><summary>Coverage</summary>

**Tier 1 -- 14/14 read.**
**Tier 2 -- 11/11 read.**

**Delta read (this run -- PR #5451 DuckDB additions):**
- `CommandLine/Commands/ScaffoldCommand.Options.cs` -- `DuckDB` added to `DatabaseType` enum (line 1951) and to `General.Provider` value list (line 108); provider count 14 -> 15.
- `CommandLine/Commands/ScaffoldCommand.Execute.cs` -- `DatabaseType.DuckDB` -> `ProviderName.DuckDB` mapping at line 78; `ProviderName.DuckDB` added to no-special-setup `case` group at line 201.
- `LinqToDB.CLI.csproj` -- `<PackageReference Include="DuckDB.NET.Data.Full" />` added at line 89.

</details>
