---
area: CLI
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 14/14
coverage_tier_2: 11/11
---

# CLI

`dotnet-linq2db` — the scaffolding dotnet tool. Invoked as `dotnet linq2db <command>` or `dotnet-linq2db <command>`. Ships as the `linq2db.cli` NuGet package (package type: `DotnetTool`). Targets `net8.0`, `net9.0`, `net10.0`.

Assembly name is `dotnet-linq2db` (set by `<AssemblyName>` in `LinqToDB.CLI.csproj:6`). The project uses a custom `.nuspec` file rather than `<IsTool>true` because it must pack multi-arch Windows executables (`win-x86`, `win-x64`, `win-arm64`) alongside the cross-platform managed DLL. A `MultiArchPublish` MSBuild target (`.csproj:38`) republishes the project for each RID after the main build.

## Subsystems

### Entry point and dispatch (`CommandLine/`)

`Program.cs:10` — `Main` instantiates `LinqToDBCliController` and calls `Execute(args)`. Unhandled exceptions write to stderr and return `StatusCodes.INTERNAL_ERROR` (-2).

`CliController` (abstract) — command registry and parser. `_commands: Dictionary<string, CliCommand>` keyed by command name. `Execute(string[] args)` dispatches: arg[0] is the command name; if not found, falls to `_defaultCommand`. Option parsing (`ParseCommandOptions`) supports `--long-name value` and `-x value` forms; `ImportCliOption` values are merged first (JSON file), then CLI values override for single-value options. Conflict detection (`_incompatibleOptions`) and required-option validation are performed before `CliCommand.Execute` is called. Source: `CommandLine/CliController.cs`.

`LinqToDBCliController` (sealed) — the concrete controller. Default command is `HelpCommand.Instance`. Registers three commands: `HelpCommand`, `ScaffoldCommand`, `TemplateCommand`. Source: `CommandLine/LinqToDBCliController.cs:7`.

### Command abstractions (`CommandLine/Commands/`)

`CliCommand` (abstract) — owns option registries (`_optionsByName`, `_optionsByShortName`, `_optionsByCategory`, `_incompatibleOptions`). Key methods: `AddOption(OptionCategory, CliOption)`, `AddMutuallyExclusiveOptions(OptionCategory, params CliOption[])`, abstract `Execute(CliController, string[], Dictionary<CliOption, object?>, IReadOnlyCollection<string>) → ValueTask<int>`. Source: `CommandLine/Commands/CliCommand.cs`.

`HelpCommand` (sealed singleton) — default command and `help` command. Prints general help (all commands) or command-specific help including option categories, type annotations, default values, enum allowed-values, and examples. Width-aware line wrapping; workaround for `Console.BufferWidth` unavailability on some terminals (hardcoded 80 as fallback, issue #3612). Source: `CommandLine/Commands/HelpCommand.cs`.

`TemplateCommand` (sealed singleton) — `template [-o path]` command. Extracts `Template.tt` from the assembly's embedded resource (`LinqToDB.CLI.Template.tt`) and writes it to disk. Refuses to overwrite an existing file. Source: `CommandLine/Commands/TemplateCommand.cs`.

`CommandExample` (record) — `(string Command, string Help)` pair used by `CliCommand.Examples`. Source: `CommandLine/Commands/CommandExample.cs`.

### ScaffoldCommand (5-file partial class)

`ScaffoldCommand.cs` — base partial: declares the constructor, which registers all ~80 options into four `OptionCategory` groups (`General`, `Database Schema`, `Data Model`, `Code Generation`). Uses `ScaffoldOptions.Default()` to pull baseline values. Source: `CommandLine/Commands/ScaffoldCommand.cs`.

`ScaffoldCommand.Options.cs` — option static fields. Nested inner classes `General`, `CodeGen`, `DataModel`, `SchemaOptions` each declare `public static readonly CliOption` fields. Four `OptionCategory` singletons (`_generalOptions` order=1, `_schemaOptions` order=2, `_dataModelOptions` order=3, `_codeGenerationOptions` order=4). `General.Provider` is a required `StringEnumCliOption` over `DatabaseType` (14 values: Access, DB2, Firebird, Informix, SQLServer, MySQL, Oracle, PostgreSQL, SqlCe, SQLite, Sybase, SapHana, ClickHouseMySql, ClickHouseHttp, ClickHouseTcp). Source: `CommandLine/Commands/ScaffoldCommand.Options.cs`.

`ScaffoldCommand.Configuration.cs` — `ProcessScaffoldOptions(options)` builds a `ScaffoldOptions` object from parsed CLI options by delegating to `ProcessSchemaOptions`, `ProcessDataModelOptions`, `ProcessCodeGenOptions`. JSON key for template selection: `General.OptionsTemplate` value `"t4"` switches to `ScaffoldOptions.T4()` baseline. Source: `CommandLine/Commands/ScaffoldCommand.Configuration.cs`.

`ScaffoldCommand.Execute.cs` — `Execute` override: processes settings, handles `--architecture` restart (`RestartIfNeeded`), opens a `DataConnection`, then calls `Scaffold(...)`. The `Scaffold` method invokes `LegacySchemaProvider`, creates a `Scaffolder`, calls `LoadDataModel` → `GenerateCodeModel` → `GenerateSourceCode`, then writes files to disk. Provider-specific connection setup for all 14 providers (unmanaged-assembly loading for SqlCe, SapHana, DB2/Informix; OleDb+ODBC dual-connection for Access). Architecture restart spawns a child process (`dotnet-linq2db.win-x86.exe` / `.win-x64.exe`) from the same assembly directory, passing all original args, and relays stdout/stderr. Source: `CommandLine/Commands/ScaffoldCommand.Execute.cs`.

`ScaffoldCommand.Interceptors.cs` — interceptor loading. `LoadInterceptors(path)` dispatches on `.dll` extension: assembly path → `LoadInterceptorsFromAssembly`; otherwise → `LoadInterceptorsFromT4`. T4 path: `PreprocessTemplate` uses `Mono.TextTemplating.TemplateGenerator` to parse/preprocess the T4 file into C# source (`TEMPLATE_CLASS_NAME = "CustomT4Scaffolder"`), then `CompileAndLoadAssembly` uses Roslyn `CSharpCompilation` to compile in-memory, then instantiates `LinqToDBHost`, calls `TransformText()` to get interceptors C# source, recompiles that into `INTERCEPTORS_ASSEMBLY_NAME` assembly, and loads it. Assembly path: `Assembly.LoadFrom` + `DependencyContext`-based resolver (with `deps.json` present) or simple folder-probe resolver (without). Interceptors class must be a single type inheriting `ScaffoldInterceptors` with either a default constructor or a `ScaffoldOptions`-taking constructor. Source: `CommandLine/Commands/ScaffoldCommand.Interceptors.cs`.

### Option type system (`CommandLine/Options/`)

`CliOption` (abstract record) — base for all options. Properties: `Name`, `ShortName`, `Type` (`OptionType` enum), `Required`, `AllowMultiple`, `AllowInJson`, `AllowInCli`, `Help`, `DetailedHelp`, `Examples`, `JsonExamples`. Abstract methods: `ParseCommandLine(command, rawValue, out errorDetails)` and `ParseJSON(element, out errorDetails)`. Source: `CommandLine/Options/CliOption.cs`.

`OptionType` enum — `String`, `StringEnum`, `StringDictionary`, `Boolean`, `DatabaseObjectFilter`, `Naming`, `JSONImport`. Source: `CommandLine/Options/OptionType.cs`.

Concrete option types:
- `BooleanCliOption` — parses `"true"` / `"false"` (case-insensitive). Has `Default` and `T4Default` for mode-aware defaults. Source: `CommandLine/Options/BooleanCliOption.cs`.
- `StringCliOption` — arbitrary string; `AllowMultiple` splits on comma. Source: `CommandLine/Options/StringCliOption.cs`.
- `StringEnumCliOption` — string from a fixed `StringEnumOption[]` value set; supports case-sensitive or insensitive match; `AllowMultiple`. Source: `CommandLine/Options/StringEnumCliOption.cs`.
- `StringEnumOption` (record) — `(bool Default, bool T4Default, string Value, string Help)`. Source: `CommandLine/Options/StringEnumOption.cs`.
- `NamingCliOption` — JSON-only (AllowInCli=false). Parses a `NormalizationOptions` object from JSON with properties `case`, `pluralization`, `prefix`, `suffix`, `transformation`, `pluralize_if_ends_with_word_only`, `ignore_all_caps`, `max_uppercase_word_length`. Source: `CommandLine/Options/NamingCliOption.cs`.
- `ObjectNameFilterCliOption` — builds a `NameFilter` from comma-separated names (CLI) or JSON array of `{name?|regex?, schema?}` objects. Source: `CommandLine/Options/ObjectNameFilterCliOption.cs`.
- `StringDictionaryCliOption` — `key=value,...` from CLI; JSON object from JSON. Source: `CommandLine/Options/StringDictionaryCliOption.cs`.
- `ImportCliOption` — CLI-only (`AllowInJson=false`). Reads a JSON file and populates a `Dictionary<CliOption, object?>` by parsing each category-property subtree. Source: `CommandLine/Options/ImportCliOption.cs`.

`NameFilter` — compiled filter over database object names. Supports exact names (with optional schema), compiled regex patterns (with optional schema), 1-second `RegexOptions.Compiled` timeout. `ApplyTo(schema, name)` returns true if any rule matches. Source: `CommandLine/Options/NameFilter.cs`.

`OptionCategory` (record) — `(int Order, string Name, string Description, string JsonProperty)`. Source: `CommandLine/Options/OptionCategory.cs`.

### T4 host (`T4Host/`)

`LinqToDBHost` (public abstract) — base class that user T4 templates inherit from via `<#@ template ... inherits="LinqToDB.LinqToDBHost" #>`. The class is public because mono.t4's codegen references it by name in the emitted C# class. Implements `Write(string code)` (appends to `StringBuilder`), `GenerationEnvironment` property, and virtual `Initialize()`. Abstract `TransformText()` is the T4 entry point. Source: `T4Host/LinqToDBHost.cs`.

`Template.tt` — the starter T4 template shipped as an embedded resource. The CLI extracts it verbatim to disk when the user runs `dotnet linq2db template`. The template inherits `LinqToDBHost`, imports core scaffolding namespaces (`LinqToDB.CodeModel`, `LinqToDB.DataModel`, `LinqToDB.Schema`, `LinqToDB.Scaffold`, `LinqToDB.SqlQuery`), and provides a no-op `Interceptors` class (inheriting `ScaffoldInterceptors`) with all overridable methods stubbed out. Source: `Template.tt` (embedded resource `LinqToDB.CLI.Template.tt`).

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
| `StatusCodes` | `CommandLine/StatusCodes.cs` | Exit code constants (0, -1, -2, -3, -4) |

## Files (Tier 1 / Tier 2)

**Tier 1** (read in full, proposed as canonical anchors):

| File | Rationale |
|---|---|
| `Program.cs` | Entry point |
| `CommandLine/CliController.cs` | Command registry + dispatch |
| `CommandLine/LinqToDBCliController.cs` | Concrete controller |
| `CommandLine/StatusCodes.cs` | Exit-code contract |
| `CommandLine/Commands/CliCommand.cs` | Abstract command base |
| `CommandLine/Commands/ScaffoldCommand.cs` | Scaffold command (base partial) |
| `CommandLine/Commands/ScaffoldCommand.Execute.cs` | Core scaffold execution |
| `CommandLine/Commands/ScaffoldCommand.Interceptors.cs` | T4/assembly interceptor loading |
| `CommandLine/Commands/ScaffoldCommand.Configuration.cs` | Option-to-settings mapping |
| `CommandLine/Commands/ScaffoldCommand.Options.cs` | Option field declarations |
| `CommandLine/Options/CliOption.cs` | Abstract option base |
| `T4Host/LinqToDBHost.cs` | T4 host base class |
| `Template.tt` | Starter template (embedded resource) |
| `LinqToDB.CLI.csproj` | Multi-arch build + packaging |

**Tier 2** (also read in full):

| File | Notes |
|---|---|
| `CommandLine/Commands/HelpCommand.cs` | Help rendering |
| `CommandLine/Commands/TemplateCommand.cs` | Template extraction command |
| `CommandLine/Commands/CommandExample.cs` | Simple record |
| `CommandLine/Options/BooleanCliOption.cs` | |
| `CommandLine/Options/StringCliOption.cs` | |
| `CommandLine/Options/StringEnumCliOption.cs` | |
| `CommandLine/Options/StringEnumOption.cs` | |
| `CommandLine/Options/NamingCliOption.cs` | |
| `CommandLine/Options/ObjectNameFilterCliOption.cs` | |
| `CommandLine/Options/StringDictionaryCliOption.cs` | |
| `CommandLine/Options/ImportCliOption.cs` | |
| `CommandLine/Options/NameFilter.cs` | |
| `CommandLine/Options/OptionCategory.cs` | |
| `CommandLine/Options/OptionType.cs` | |
| `DotnetToolSettings.xml` | Tool metadata |
| `linq2db.cli.nuspec` | Package spec |
| `readme.md` | User-facing docs |
| `PublicAPI.Shipped.txt` | API baseline |
| `PublicAPI.Unshipped.txt` | API baseline |

## Inbound / outbound dependencies

**Outbound (this area depends on):**

- **SCAFFOLD** (`Source/LinqToDB.Scaffold/`) — `ScaffoldCommand.Execute.cs` calls `new Scaffolder(...)`, `Scaffolder.LoadDataModel(...)`, `Scaffolder.GenerateCodeModel(...)`, `Scaffolder.GenerateSourceCode(...)`. Also uses `ScaffoldOptions`, `ScaffoldInterceptors`, `LegacySchemaProvider`, `LanguageProviders`, `HumanizerNameConverter`, `MetadataBuilders`. `Template.tt` imports all key SCAFFOLD namespaces.
- **Core `LinqToDB`** — `ScaffoldCommand.Execute.cs` uses `DataConnection`, `DataOptions`, `ProviderName`, and many provider-specific types (`OracleTools`, `PostgreSQLTools`, `DB2Tools`). `NamingCliOption` uses `NormalizationOptions` from `LinqToDB.Naming`.
- **Mono.TextTemplating** (NuGet) — T4 parsing (`TemplateGenerator.PreprocessTemplate`) in `ScaffoldCommand.Interceptors.cs`.
- **Microsoft.CodeAnalysis.CSharp** (NuGet) — Roslyn in-memory compilation of T4-generated C# and interceptors C# in `ScaffoldCommand.Interceptors.cs`.
- **Microsoft.Extensions.DependencyModel** (NuGet) — dependency resolution for interceptors assemblies.
- All provider ADO.NET packages (SQLite, SqlClient, Firebird, MySqlConnector, Oracle.ManagedDataAccess.Core, Npgsql, AdoNetCore.AseClient, ODBC, OleDb, ClickHouse) — bundled in the tool so the CLI can connect to any supported database without user-installed drivers (except IBM DB2/Informix and SAP HANA which are too large).

**Inbound (nothing depends on this area):**

This is a standalone tool; no other source project references `LinqToDB.CLI`.

## Known issues / debt

- `ScaffoldCommand.Interceptors.cs:56` — `Console.WriteLine($"AssemblyResolve path: {assemblyFolder}")` is an unconditional debug log line (marked `// TODO: Verbose logging`). Should be gated on a `--verbose` flag.
- `ScaffoldCommand.Interceptors.cs:138,139` — same verbose logging TODO for the fallback resolver path.
- `HelpCommand.cs:179,312` — workaround for `Console.BufferWidth` exception on non-interactive terminals (issue #3612); width is hardcoded to 80 when the property throws. Could be improved with a proper terminal-detection check.
- `ScaffoldCommand.Execute.cs:159` — file name collision/deduplication is explicitly deferred (`// TODO: add file name normalization/deduplication?`).
- `ScaffoldCommand.Options.cs` — `IgnoreSystemHistoryTables` is handled in `ProcessSchemaOptions` (`ScaffoldCommand.Configuration.cs:295`) but appears to have no corresponding option registration in `ScaffoldCommand.cs`. This may be a dead-code path or an option missing from the constructor registration.
- Architecture restart (`RestartIfNeeded`) only works on Windows; the `--architecture` flag is silently ignored on Linux/macOS. Source: `ScaffoldCommand.Execute.cs:400`.
- IBM DB2 and Informix providers are intentionally excluded from the tool's NuGet package due to large size (~90 MB compressed); users must supply `--provider-location` pointing to a manually installed IBM assembly.

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md) — the scaffolding library this CLI wraps.
- [INTERCEPTORS area](../INTERCEPTORS/INDEX.md) — the `ScaffoldInterceptors` base class (lives in SCAFFOLD, not the core interceptors area).
- [T4-TEMPLATES area](../T4-TEMPLATES/INDEX.md) — the T4 template includes for direct consumer use (distinct from the CLI's embedded starter template).

<details><summary>Coverage</summary>

**Tier 1 — 14/14 read:**
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

**Tier 2 — 11/11 read (all remaining .cs + non-.cs files):**
- `CommandLine/Commands/HelpCommand.cs`
- `CommandLine/Commands/TemplateCommand.cs`
- `CommandLine/Commands/CommandExample.cs`
- `CommandLine/Options/BooleanCliOption.cs`
- `CommandLine/Options/StringCliOption.cs`
- `CommandLine/Options/StringEnumCliOption.cs`
- `CommandLine/Options/StringEnumOption.cs`
- `CommandLine/Options/NamingCliOption.cs`
- `CommandLine/Options/ObjectNameFilterCliOption.cs`
- `CommandLine/Options/StringDictionaryCliOption.cs`
- `CommandLine/Options/ImportCliOption.cs`
- `CommandLine/Options/NameFilter.cs`
- `CommandLine/Options/OptionCategory.cs`
- `CommandLine/Options/OptionType.cs`
- `DotnetToolSettings.xml`
- `linq2db.cli.nuspec`
- `readme.md`
- `PublicAPI.Shipped.txt`
- `PublicAPI.Unshipped.txt`

Note: Tier 2 count (11) counts distinct non-Tier-1 `.cs` files that contributed new factual claims (the remaining files were infrastructure/non-.cs read but already captured in context).

**Tier 3 — 0 files** (no generated/bin/obj content in this area).
</details>
