---
area: CLI
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 14/14
coverage_tier_2: 11/11
---

# CLI

`dotnet-linq2db` -- the scaffolding dotnet tool. Invoked as `dotnet linq2db <command>` or `dotnet-linq2db <command>`. Ships as the `linq2db.cli` NuGet package (package type: `DotnetTool`). Targets `net8.0`, `net9.0`, `net10.0`.

Assembly name is `dotnet-linq2db` (set by `<AssemblyName>` in `LinqToDB.CLI.csproj:5`). The project uses `PackAsTool=true` with `ToolPackageRuntimeIdentifiers` in the csproj SDK-pack pipeline (a .NET 10 SDK feature). This replaces the prior approach of a custom `.nuspec` file and a `MultiArchPublish` MSBuild target. `dotnet pack` now produces a thin pointer package plus one sub-package per RID; `dotnet tool install -g linq2db.cli` selects the sub-package matching the SDK architecture. RIDs covered: `win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-arm64`, `osx-x64` (`LinqToDB.CLI.csproj:17,34`). Installation requires .NET 10 SDK; runtime requires .NET 8+.

## Subsystems

### Entry point and dispatch (`CommandLine/`)

`Program.cs:10` -- `Main` instantiates `LinqToDBCliController` and calls `Execute(args)`.

`CliController` (abstract) -- command registry and parser. `Execute(string[] args)` dispatches: arg[0] is the command name; if not found, falls to `_defaultCommand`. Option parsing supports `--long-name value` and `-x value` forms; `ImportCliOption` (JSON file) values are merged first, then CLI values override.

`LinqToDBCliController` (sealed) -- registers three commands: `HelpCommand`, `ScaffoldCommand`, `TemplateCommand`.

### Command abstractions (`CommandLine/Commands/`)

`CliCommand` (abstract) -- owns option registries; abstract `Execute(...) -> ValueTask<int>`.
`HelpCommand` (singleton) -- default command and `help` command. Width-aware line wrapping; fallback 80 when `Console.BufferWidth` unavailable (issue #3612). On Windows, `PrintGeneralHelp` now emits an expanded bitness-guidance section: 32-bit-only providers (Jet OLE DB), bitness-must-match providers (ACE OLE DB, SQL CE, SAP HANA), and instructions for installing the x86 variant or maintaining parallel x86/x64 tool paths (`HelpCommand.cs:404-432`).
`TemplateCommand` (singleton) -- extracts `Template.tt` from embedded resource.

### ScaffoldCommand (5-file partial class)

- `ScaffoldCommand.cs` -- registers all ~80 options into four `OptionCategory` groups.
- `ScaffoldCommand.Options.cs` -- option static fields. `General.Provider` is a required `StringEnumCliOption` over `DatabaseType` (17 values: Access, DB2, Firebird, Informix, SQLServer, MySQL, Oracle, PostgreSQL, SqlCe, SQLite, Sybase, SapHana, ClickHouseMySql, ClickHouseHttp, ClickHouseTcp, DuckDB, Ydb). `DatabaseType` is a private enum at `ScaffoldCommand.Options.cs:1913`; `DuckDB` at line 1931, `Ydb` at line 1932. The `--provider` `StringEnumOption` list (`ScaffoldCommand.Options.cs:93-109`) carries a matching `Ydb` entry with description "YDB".
- `ScaffoldCommand.Configuration.cs` -- `ProcessScaffoldOptions(options)` builds a `ScaffoldOptions` object.
- `ScaffoldCommand.Execute.cs` -- `Execute` override: processes settings, selects the `ILanguageProvider` from `--target-language` (`c#` default / `f#`, #1553), handles `--architecture` restart, opens a `DataConnection`, then calls `Scaffold(...)`. For F# it rejects `-t t4`, forces `ClassPerFile=false` + space indent + single typed-options ctor, and fail-fast errors on any explicitly-set C#-only option. Provider-specific connection setup: `ProviderName.DuckDB` and `ProviderName.Ydb` both require no special setup (fall through the `break` case alongside ClickHouse and SQL Server at `ScaffoldCommand.Execute.cs:186-192`). The `DatabaseType.DuckDB` -> `ProviderName.DuckDB` mapping is at line 66; `DatabaseType.Ydb` -> `ProviderName.Ydb` at line 67. Architecture restart spawns a child process from the same assembly directory.
- `ScaffoldCommand.Interceptors.cs` -- interceptor loading. T4 path: `PreprocessTemplate` uses `Mono.TextTemplating.TemplateGenerator`, then Roslyn `CSharpCompilation` compiles in-memory. Assembly path: `Assembly.LoadFrom` + `DependencyContext`-based resolver.

### Option type system (`CommandLine/Options/`)

`CliOption` (abstract record) -- base for all options. Concrete option types: `BooleanCliOption`, `StringCliOption`, `StringEnumCliOption`, `NamingCliOption`, `ObjectNameFilterCliOption`, `StringDictionaryCliOption`, `ImportCliOption`. Carries a `[Flags] TargetLanguages Languages` facet (`CommandLine/Options/TargetLanguages.cs`, default `All`; #1553) — options that don't apply to F# (`--nrt`, `--partial-entities`, `--add-init-context`, `--add-static-init-context`) declare `CSharp` only; `HelpCommand` renders a `Supported in: C#` line when an option isn't `All`, and `ScaffoldCommand.Execute` fail-fast rejects an unsupported option set for the chosen language.

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

**Tier 1** (read in full): `Program.cs`, `CommandLine/CliController.cs`, `CommandLine/LinqToDBCliController.cs`, `CommandLine/StatusCodes.cs`, `CommandLine/Commands/CliCommand.cs`, `CommandLine/Commands/ScaffoldCommand.cs`, `CommandLine/Commands/ScaffoldCommand.Execute.cs`, `CommandLine/Commands/ScaffoldCommand.Interceptors.cs`, `CommandLine/Commands/ScaffoldCommand.Configuration.cs`, `CommandLine/Commands/ScaffoldCommand.Options.cs`, `CommandLine/Options/CliOption.cs`, `T4Host/LinqToDBHost.cs`, `Template.tt`, `LinqToDB.CLI.csproj`.

**Tier 2** (read in full): `HelpCommand.cs`, `TemplateCommand.cs`, `CommandExample.cs`, all `CommandLine/Options/*.cs`, `readme.md`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`. Note: `DotnetToolSettings.xml` and `linq2db.cli.nuspec` were Tier-2 in the prior run; both files have been deleted from the repository as part of the SDK-pack migration.

## Inbound / outbound dependencies

**Outbound:**
- **SCAFFOLD** -- `Scaffolder`, `ScaffoldOptions`, `ScaffoldInterceptors`, `LegacySchemaProvider`.
- **Core `LinqToDB`** -- `DataConnection`, `DataOptions`, `ProviderName`, provider-specific types.
- **Mono.TextTemplating** -- T4 parsing.
- **Microsoft.CodeAnalysis.CSharp** -- Roslyn in-memory compilation.
- All provider ADO.NET packages bundled in the tool (SQLite, SqlClient, Firebird, MySqlConnector, Oracle.ManagedDataAccess.Core, Npgsql, AdoNetCore.AseClient, ODBC, OleDb, ClickHouse.Driver, Octonica.ClickHouseClient, DuckDB.NET.Data.Full, Ydb.Sdk) -- except IBM DB2/Informix and SAP HANA (too large).

**Inbound:** standalone tool; no other source project references `LinqToDB.CLI`.

## Known issues / debt

- `ScaffoldCommand.Interceptors.cs:56,138,139` -- unconditional debug log lines marked `// TODO: Verbose logging`.
- `HelpCommand.cs:185,312` -- workaround for `Console.BufferWidth` exception (issue #3612).
- `ScaffoldCommand.Execute.cs:148` -- file name collision/deduplication deferred (`// TODO: add file name normalization/deduplication?`).
- Architecture restart only works on Windows; `--architecture` silently ignored on Linux/macOS.
- IBM DB2 and Informix providers intentionally excluded from the tool package due to size.

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md) -- the scaffolding library this CLI wraps.
- [INTERCEPTORS area](../INTERCEPTORS/INDEX.md) -- the `ScaffoldInterceptors` base class.
- [T4-TEMPLATES area](../T4-TEMPLATES/INDEX.md) -- the T4 template includes.

<details><summary>Coverage</summary>

**Tier 1 -- 14/14 read.**
**Tier 2 -- 11/11 read.** (denominator reflects current on-disk set; `DotnetToolSettings.xml` and `linq2db.cli.nuspec` deleted -- counted as visited with skip reason: file removed from repo)

**Delta read (prior run -- PR #5451 DuckDB additions):**
- `ScaffoldCommand.Options.cs` -- `DuckDB` added to `DatabaseType` enum and provider value list; provider count 14 -> 15.
- `ScaffoldCommand.Execute.cs` -- `DatabaseType.DuckDB` -> `ProviderName.DuckDB` mapping; no-special-setup case group.
- `LinqToDB.CLI.csproj` -- `DuckDB.NET.Data.Full` package reference added.

**Read (this run -- delta, sha 2e67bafc9):**
- `CommandLine/Commands/HelpCommand.cs` -- `PrintGeneralHelp` extended with Windows bitness-selection guidance block (lines 404-432); no structural change to option rendering.
- `CommandLine/Commands/ScaffoldCommand.Execute.cs` -- no changes beyond DuckDB already noted; `GetConnection` switch stable.
- `CommandLine/Commands/ScaffoldCommand.Options.cs` -- DuckDB already present; option surface otherwise stable.
- `CommandLine/Commands/ScaffoldCommand.cs` -- no changes to option registration structure.
- `LinqToDB.CLI.csproj` -- packaging completely replaced: `PackAsTool=true` + `ToolPackageRuntimeIdentifiers` replaces custom `.nuspec` + `MultiArchPublish` MSBuild target; cross-platform RIDs added (`linux-x64`, `linux-arm64`, `osx-arm64`, `osx-x64`); `DotnetToolSettings.xml` and `linq2db.cli.nuspec` deleted; install now requires .NET 10 SDK, runtime still .NET 8+.
- `PublicAPI.Shipped.txt` -- `LinqToDBHost` surface only; release-promotion churn.
- `readme.md` -- updated install section: `.NET 10 SDK required`, 32-bit vs 64-bit guidance, per-RID install/update/switch examples.
- `DotnetToolSettings.xml` -- DELETED (SDK-pack migration; was Tier-2).
- `linq2db.cli.nuspec` -- DELETED (SDK-pack migration; was Tier-2).

**Read (this run -- delta, sha 36ee4f82f):**
- `CommandLine/Commands/ScaffoldCommand.Execute.cs` -- added `DatabaseType.Ydb -> ProviderName.Ydb` mapping (line 67, alongside the existing DuckDB mapping); `GetConnection`'s no-special-setup `break` case group (`ScaffoldCommand.Execute.cs:186-192`) extended with `ProviderName.Ydb` alongside `ClickHouseMySql`/`ClickHouseDriver`/`ClickHouseOctonica`/`SqlServer`/`DuckDB` -- YDB needs no auto-detect flag, no assembly probing, no bitness check.
- `CommandLine/Commands/ScaffoldCommand.Options.cs` -- `DatabaseType` enum gained a `Ydb` member (`ScaffoldCommand.Options.cs:1932`, after `DuckDB`); the `--provider` `StringEnumOption` list gained a matching `Ydb` / "YDB" entry (`ScaffoldCommand.Options.cs:109`). Provider count 16 -> 17.
- `LinqToDB.CLI.csproj` -- added a `Ydb.Sdk` `PackageReference` to the bundled-providers item group (`LinqToDB.CLI.csproj:74`), so the CLI tool now ships the YDB ADO.NET driver alongside the other bundled providers.

</details>
