---
area: BUILD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 3/4
coverage_tier_2: 83/83
---

# BUILD

CI and build infrastructure for linq2db. Controls: TFM matrix, feature flags, versioning, analyzer gating, SDK pinning, solution shape, Azure Pipelines test matrix (30+ database/version combinations), and GitHub issue triage forms. No GitHub Actions workflows exist -- all CI runs on Azure Pipelines.

## Key types

This area has no C# types. Its artifacts are MSBuild/YAML/shell configuration files.

## Files (Tier 1 / Tier 2)

**Tier 1:**

| File | Role |
|---|---|
| `Directory.Build.props` | Global MSBuild anchor: TFMs, version numbers, feature flags, analyzer gating, polyfills, NuGet metadata, assembly signing. Imported by every project in the solution. |
| `global.json` | SDK pin: .NET 10.0, `rollForward: minor`, `allowPrerelease: false`. |
| `linq2db.slnx` | Solution file (VS 2022 XML format). Lists all projects across 7 folders. |
| `Build/BannedSymbols.txt` | **MISSING** -- the actual banned-API list is at `Source/BannedSymbols.txt`. |

**Tier 2 (all read):** `Build/Azure/pipelines/*.yml` (3 top-level + 8 templates), `Build/Azure/scripts/*.ps1` (3 existing + 1 added: `verify-nuget-sizes.ps1`) + `*.sh` (43), `.github/ISSUE_TEMPLATE/*.yml` (13), `.github/copilot-instructions.md` (added prior run), `Data/Create Scripts/DuckDB.sql` (new, PR #5451), `.editorconfig` (root), `Build/Azure/scripts/db2.provider.sh`, `Build/Azure/scripts/mac.db2.provider.sh`.

## Subsystems

### TFM matrix and feature flags (`Directory.Build.props`)

`<TargetFrameworks>net462;netstandard2.0;net8.0;net9.0;net10.0</TargetFrameworks>` -- applies to all projects unless overridden. The `Testing` configuration pins to `net10.0` only for fast iteration.

Feature flags are `DefineConstants` conditioned on `IsTargetFrameworkCompatible(..., net8.0)` (or `net472` for `SUPPORTS_READONLY`):

| Flag | Meaning |
|---|---|
| `SUPPORTS_COMPOSITE_FORMAT` | `CompositeFormat` type available |
| `SUPPORTS_DATEONLY` | `DateOnly` + `TimeOnly` types available |
| `SUPPORTS_ENSURE_CAPACITY` | `List.EnsureCapacity`, `Enumerable.TryGetNonEnumeratedCount` |
| `ADO_ASYNC` | Async transaction/connection APIs (`DbConnection.CloseAsync`, etc.) |
| `ADO_IS_TRANSIENT` | `DbException.IsTransient` |
| `SUPPORTS_SPAN` | `Span<T>` operations |
| `SUPPORTS_READONLY` | `IsReadOnlyAttribute` (net8+ or net472) |
| `SUPPORTS_REGEX_GENERATORS` | Source-generated regex |
| `SUPPORTS_INT128` | `(u)int128` types |

All flags gated on `net8.0` compatibility; `netstandard2.0` and `net462` receive none of them.

### Version variables (`Directory.Build.props`)

| Property | Value | Notes |
|---|---|---|
| `<Version>` | `6.4.0` | Main product version (bumped from 6.3.0) |
| `<EF3Version>` | `3.33.0` | EF Core 3.x package |
| `<EF8Version>` | `8.7.0` | EF Core 8.x package |
| `<EF9Version>` | `9.6.0` | EF Core 9.x package |
| `<EF10Version>` | `10.5.0` | EF Core 10.x package |
| `<BaselineVersion>` | `6.0.0` | API compatibility baseline |

`<VersionSuffix>` defaults to `-local.1` and `<ApplyVersionSuffix>` defaults to `true`.

### Roslyn analyzer gating (`Directory.Build.props`, `Source/Directory.Build.props`)

`<RunAnalyzersDuringBuild>` is `false` by default and enabled only when `$(Configuration) == Release`. `<EnforceCodeStyleInBuild>` is likewise `false` by default and enabled only when `$(Configuration) == Release` -- as of PR #5523, IDE-style analyzers are now Release-only in both properties. `<TreatWarningsAsErrors>true` is unconditional. `<WarningLevel>9999</WarningLevel>` activates all Roslyn warnings. `<AnalysisLevel>preview-All</AnalysisLevel>` pulls in preview analyzers.

Analyzers added globally via `<PackageReference>` in `Directory.Build.props`: `AsyncFixer`, `Lindhart.Analyser.MissingAwaitWarning`, `Meziantou.Analyzer`, `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `Microsoft.SourceLink.GitHub`.

`RunApiAnalyzersDuringBuild` is opt-in per-project.

### EditorConfig analyzer rules (`.editorconfig`)

The root `.editorconfig` governs the full analyzer diagnostic severity catalog for all `*.{cs,vb}` files. Key structure:

- Global `dotnet_analyzer_diagnostic.severity = error` enables all analyzers by default for source files.
- **Public API analyzers** (RS0016--RS0061): most set to `error`; RS0026/RS0027/RS0041/RS0051/RS0056 set to `none`.
- **Active diagnostics** (explicitly configured): CS8618, CS4014, CS1998; CA1018/CA1050/CA1200/CA1305/CA1507/CA1510--CA1513/CA1725/CA1805/CA1823/CA1825--CA1830/CA1834/CA1836/CA2007/CA2012/CA2016/CA2101/CA2200/CA2201/CA2208/CA2215; IDE0001--IDE0003/IDE0009/IDE0036/IDE0047/IDE0048/IDE0051/IDE0052/IDE0060/IDE0070/IDE0330; `LindhartAnalyserMissingAwaitWarningVariable`; AsyncFixer02--06.
- **Meziantou.Analyzer active rules**: MA0044/MA0047/MA0048/MA0056/MA0069/MA0075/MA0076/MA0079/MA0080/MA0106/MA0107/MA0129/MA0151, plus the full MA0008--MA0200 catalog introduced for `Meziantou.Analyzer` 3.0.85 (each entry individually configured with rationale comments for `none` cases -- e.g. MA0032 disabled for no-token public overloads, MA0104 disabled for `DataType` enum clash, MA0137/MA0138 disabled for `IAsyncEnumerable` naming conflicts, MA0191 disabled for 438 deliberate `!` uses in NRT escape hatches).
- **Inactive diagnostics** (not reviewed yet): large catalog including CA1001/CA1501--CA1509/CA1816/CA2000/CA2231/CA3076; IDE0004/IDE0005/IDE0055/IDE0370/SYSLIB1054; MA0002/MA0009/MA0016/MA0018/MA0036/MA0038--MA0042/MA0045--MA0046/MA0051/MA0071/MA0099/MA0101/MA0110/MA0113/MA0127/MA0136/MA0159/MA0165/MA0182/MA0185/MA0190/MA0193/MA0197.
- **Test overrides** (`Tests/**.{cs,vb}`): relaxes ~60 rules including IDE0001/IDE0039/IDE0004/IDE0051/IDE0078/IDE0083; CA1027/CA1044/CA1050/CA1304/CA1305/CA1307/CA1309/CA1310/CA1311/CA1515/CA1802/CA1812/CA1827/CA1829/CA1849/CA1847/CA1851/CA1852/CA1858/CA1860/CA1861/CA1862/CA1866/CA2007/CA2237; MA0001/MA0002/MA0004/MA0005/MA0006/MA0007/MA0009/MA0011/MA0020/MA0021/MA0023/MA0028--MA0031/MA0044/MA0047/MA0048/MA0053/MA0062/MA0063/MA0073--MA0080/MA0089/MA0095/MA0097/MA0098/MA0107/MA0111/MA0112/MA0132/MA0133/MA0150/MA0169/MA0172/MA0175/MA0176/MA0186; SYSLIB1045; NUnit2045; AsyncFixer01/02/04/06.
- ReSharper properties: `resharper_csharp_allow_far_alignment = true`, `resharper_int_align_switch_sections = true` (preserve column-aligned formatting in VS/Rider).

### Banned-API enforcement (`Source/BannedSymbols.txt`)

287-line list consumed by `Microsoft.CodeAnalysis.BannedApiAnalyzers`. Key ban categories:

- **Flawed concurrent collections**: `ConcurrentBag<T>` banned (PR #2066).
- **ADO.NET interfaces**: All `IDataReader`, `IDbCommand`, `IDbConnection`, etc. banned; use `DbDataReader`, `DbCommand`, `DbConnection`.
- **Attribute reflection without cache**: All `GetCustomAttribute`/`GetCustomAttributes`/`IsDefined` direct calls banned; use `AttributesExtensions.GetAttribute<T>()`.
- **Culture-dependent formatting**: Parameterless overloads of `DateTime.ToString`, `Decimal.Parse`, `String.Format`, `StringBuilder.Append`, `Convert.ToString` banned.
- **Expression.Compile direct call**: `LambdaExpression.Compile()` banned; use `CompileExpression` extension.
- **Reflection invocation**: `MethodBase.Invoke`, `Activator.CreateInstance`, `Delegate.DynamicInvoke` banned.
- **DbCommand.Dispose direct**: `DbCommand.DisposeAsync`/`Component.Dispose` banned; use `IDataProvider.DisposeCommandAsync`/`DisposeCommand`.
- **CurrentCulture string.IndexOf**: `string.IndexOf(string)` without `StringComparison` banned (issue #5188).
- **Type.GetInterfaceMap**: Banned; use `GetInterfaceMapEx` extension.

### Polyfills (`Directory.Build.props`)

`Meziantou.Polyfill` is configured via `<MeziantouPolyfill_IncludedPolyfills>`. Polyfills span: `System.Diagnostics.CodeAnalysis`, `HashCode`, `Index`/`Range`, `System.Threading.Lock`, `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty/WhiteSpace`, `ArgumentOutOfRangeException.ThrowIf*`, `ObjectDisposedException.ThrowIf`, `Enum.GetNames<T>`, `AsyncEnumerable.FirstAsync/SingleAsync/ToArrayAsync/ToListAsync`.

### `global.json` -- SDK pin

`sdk.version: "10.0.200"`, `rollForward: "minor"`, `allowPrerelease: false`. (Bumped from `10.0.0` in prior delta.)

### Solution shape (`linq2db.slnx`)

Four build configurations: `Azure`, `Debug`, `Release`, `Testing`. Key project folders:

| Folder | Contents |
|---|---|
| `/Source/` | `LinqToDB.csproj`, `LinqToDB.CLI`, `LinqToDB.EntityFrameworkCore.*` (EF3/8/9/10), `LinqToDB.Extensions`, `LinqToDB.FSharp`, `LinqToDB.LINQPad`, `LinqToDB.Remote.*`, `LinqToDB.Scaffold`, `LinqToDB.Tools`, `CodeGenerators.csproj` |
| `/Tests/` | `Tests.csproj`, `Tests.Base`, `Tests.EntityFrameworkCore.*`, `Tests.FSharp`, `Tests.VisualBasic`, `Tests.Benchmarks`, `Tests.Playground`, `Tests.T4`, `Tests.Model`, `Tests.SingleFile` (new) |
| `/Packaging/` | NuGet wrapper projects per provider |
| `/Build/` | `Directory.Build.props`, `Directory.Packages.props`, `linq2db.snk`, Azure pipeline YMLs/JSONs/scripts |

`Tests.SingleFile` is excluded from `Azure`, `Release`, and `Testing` solution configurations -- it is smoke-test only.

### Azure Pipelines: top-level pipelines

| File | Trigger | Purpose |
|---|---|---|
| `Build/Azure/pipelines/build.yml` | All PRs | Compile-only check |
| `Build/Azure/pipelines/default.yml` | Push to `master`/`release`; PRs to `release` | Full pipeline |
| `Build/Azure/pipelines/testing.yml` | Manual (`/azp run test-<db>` bot commands) | Test-only: builds for tests, then runs per-`db_filter` subset. Supports `test-duckdb` -> `[duckdb.all]` filter (added PR #5451). |

### Azure Pipelines: build job (`build-job.yml`)

Build pool image: `windows-2025` (updated from `windows-2022`). `timeoutInMinutes: 120`. Key steps:

- Installs .NET SDK 9.x then 10.x via `UseDotNet@2`.
- `PublishSingleFile` smoke test: publishes `Tests.SingleFile.csproj` as win-x64 self-contained single file and executes it; guards against `Assembly.Location` / `File.Exists` regressions in provider detectors (PR #5488).
- Publishes test binaries for NETFX, net8.0, net9.0, net10.0 (x86 and x64); includes EF Core 3/8/9/10 tests.
- Builds and packs for NuGet on Release configuration; publishes nugets and LINQPad LPX artifacts.
- Creates GitHub Release draft (via `gh`) when building the release branch.

### Azure Pipelines: test matrix (`test-matrix.yml`, `test-jobs.yml`)

The `test-matrix.yml` template defines ~38 test matrix entries spanning: SQLite (Win/Linux/macOS, all TFMs); Access (MDB, ACE x86, ACE x64 disabled, Windows); SQL CE (Windows); MySQL/MariaDB; PostgreSQL 13--18; SQL Server 2005--2025; Sybase ASE 16; Oracle 11g--23c; Firebird 2.5--5.0; DB2 LUW 11.5; Informix 14.10; SAP HANA 2; ClickHouse (Driver/MySql); **DuckDB (Win/Linux/macOS; net8.0/9.0/10.0 only; no setup script required)**.

New entries since prior build run:
- **PostgreSQL 18** (`pgsql18`): Linux + macOS; net8/9/10 only; uses `pgsql18.sh` / `mac.pgsql18.sh` scripts.
- **SQL Server 2025** (`SqlServer2025`): Win/Linux/macOS; all TFMs; uses `sqlserver.2025.cmd` / `sqlserver.2025.sh` scripts.
- **SqlServer2019Extras** (`SqlServer2019Extras`): Win/Linux/macOS; all TFMs; config `sqlserver.extras`; enabled by `[sqlserver.all]` or `[sqlserver.2019]` filters -- separate job because SQL Server 2019 run is too large for one job.

DuckDB uses file-based storage (`Data/TestData.duckdb`), so no container startup script is needed. Config files `Build/Azure/net{80,90,100}/duckdb.json` each activate provider `DuckDB`. Filter token: `[duckdb.all]`; pipeline definition: `test-duckdb`.

### DuckDB test schema (`Data/Create Scripts/DuckDB.sql`)

DDL for the DuckDB test database. Uses DuckDB-native type names (`HUGEINT`, `UHUGEINT`, `UINTEGER`, `UBIGINT`, `UTINYINT`, `USMALLINT`, `BITSTRING`, `TIMESTAMPTZ`, `TIMESTAMP_S`, `TIMESTAMP_MS`, `TIMESTAMP_NS`, `TIMETZ`, `TIME_NS`, `BIGNUM`, `INTERVAL`) in the `AllScaffoldTypes` table.

### NuGet package size guard (`Build/Azure/scripts/verify-nuget-sizes.ps1`, `Build/Azure/pipelines/templates/nuget-job.yml`)

Added after the 6.3.0 release-publish job hit HTTP 413 on `linq2db.cli.6.3.0.nupkg` (~416 MB) against nuget.org's 250 MB upload limit. The push had no pre-flight size check: pack succeeded, then push failed atomically after other packages had already been pushed.

`verify-nuget-sizes.ps1` parameters: `-PackagesDir` (required), `-WarnMB 180` (default), `-FailMB 200` (default), `-AzdoLogs $true` (default; emits `##vso[task.logissue]` markers). Exit codes: `0` = clean or warnings-only; `1` = any package over `$FailMB` (release-blocking); `2` = invalid args / no nupkgs found.

`nuget-job.yml` inserts a `PowerShell@2` task (Verify nupkg sizes fail-fast before publish) before both the Azure Artifacts and nuget.org `NuGetCommand@2` push tasks. The task runs with `-WarnMB 180 -FailMB 200` against `.build/nugets/`. The nuget.org push task is conditioned on `$(Build.SourceBranchName) == $(release_branch)`, so the size check gates all publishes.

### DB2 provider setup scripts (`Build/Azure/scripts/db2.provider.sh`, `Build/Azure/scripts/mac.db2.provider.sh`)

Both scripts are now TFM-aware. They detect the active TFM from the directory path and select the correct `Net.IBM.Data.Db2-lnx` / `Net.IBM.Data.Db2-osx` package version:

- net10.0: `DB2_PKG_VERSION=10.0.0.100`
- net8.0 / net9.0: `DB2_PKG_VERSION=9.0.0.400`

This version must be kept in lockstep with the `Net.IBM.Data.Db2*` entries in `Directory.Packages.props`. The scripts download the package from nuget.org (wget + unzip, replacing the deprecated `nuget install`), swap in the linux/osx DLL, and set `PATH`/`LD_LIBRARY_PATH` for DB2 CLI driver.

### .github/ISSUE_TEMPLATE: issue triage forms

13 YAML forms in two series: Bug reports (01--09) and Feature requests (11--19).

### .github/copilot-instructions.md: Copilot PR review rules

Instructs Copilot to ignore intentional formatting differences (column-aligned code, minor spacing) and comment only on clearly problematic formatting (3+ consecutive blank lines, trailing whitespace on multiple lines, visibly broken indentation, mixed tabs/spaces). Testing guideline: prefer Shouldly for assertions over NUnit Assert.

## Inbound / outbound dependencies

**Inbound (everything depends on this area):**
- Every C# project in `Source/` and `Tests/` imports `Directory.Build.props`.
- `Source/Directory.Build.props` adds `Source/BannedSymbols.txt` as an `<AdditionalFiles>` input.

**Outbound:**
- The CI test matrix touches every PROV-* provider area.
- `build-job.yml` publishes test binaries for every TESTS-* area.
- `nuget-job.yml` publishes packages produced by all packaging projects; now also runs `verify-nuget-sizes.ps1` as a pre-flight gate before any push.

## Known issues / debt

- **`Build/BannedSymbols.txt` pin is stale.** Listed as a Tier-1 pin but does not exist. Actual file is `Source/BannedSymbols.txt`.
- **Roslyn analyzer disabled in `build.yml`.** PR build pipeline has `with_analyzers: false` due to Roslyn issue #80621.
- **macOS tests disabled by default.** `mac_enabled: false` is the default in `test-matrix.yml`.
- **Access ACE x64 disabled.** Due to dotnet/runtime#46187.
- **ClickHouse Octonica always disabled.**
- **DuckDB no netfx support.** Does not support .NET Framework.

## See also

- [architecture overview](../../architecture/overview.md)
- [CLAUDE.md](../../../../CLAUDE.md) -- build commands reference
- `.claude/docs/testing.md`
- `.claude/docs/ci-tests.md`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 3 / 4
  - Directory.Build.props -- read in full
  - global.json -- read in full
  - linq2db.slnx -- read in full
  - Build/BannedSymbols.txt -- MISSING on disk
  - Source/BannedSymbols.txt -- read in full (actual location)
- Tier 2 (visited / total): 83 / 83 (100%)
  - Read (prior run, delta): Build/Azure/net{80,90,100}/duckdb.json; test-matrix.yml (DuckDB entry); testing.yml (test-duckdb filter); Directory.Build.props (EnforceCodeStyleInBuild Release-only, DuckDB DatabasePackageTags); Directory.Packages.props (DuckDB.NET.Data.Full); DataProviders.json (DuckDB entry); Tests/linq2db.Providers.props (DuckDB ref); Data/Create Scripts/DuckDB.sql (new)
  - Read (prior run -- delta 2): Build/Azure/scripts/verify-nuget-sizes.ps1 (ADDED); Build/Azure/pipelines/templates/nuget-job.yml; Directory.Build.props (version 6.3.0 -> 6.4.0); global.json (SDK pin 10.0.0 -> 10.0.200); Directory.Packages.props (DuckDB.NET.Data.Full 1.5.2, Npgsql 10.0.2); linq2db.slnx; Source/BannedSymbols.txt; .github/copilot-instructions.md
  - Read (this run -- delta):
    - `.editorconfig` -- full Meziantou.Analyzer 3.0.85 diagnostic catalog; MA0008--MA0200 all individually configured with rationale comments; test-file overrides for ~60 rules; ReSharper far-alignment properties preserved.
    - `Build/Azure/pipelines/build.yml` -- no structural changes; `with_analyzers: false` still present.
    - `Build/Azure/pipelines/default.yml` -- no structural changes confirmed.
    - `Build/Azure/pipelines/templates/build-job.yml` -- build pool updated windows-2022 -> windows-2025; PublishSingleFile smoke test step added (Tests.SingleFile, net10.0 win-x64); EF10 publish steps added for both x86 and x64.
    - `Build/Azure/pipelines/templates/test-matrix.yml` -- PostgreSQL 18 entry added (pgsql18); SQL Server 2025 entry added; SqlServer2019Extras job added (separate from SqlServer2019 due to run size); DuckDB entry confirmed unchanged.
    - `Build/Azure/pipelines/templates/test-workflow-linux.yml` -- structure unchanged; --blame-hang-timeout 5m confirmed on all dotnet test invocations.
    - `Build/Azure/pipelines/templates/test-workflow-macos.yml` -- structure unchanged; docker installed via brew install colima docker.
    - `Build/Azure/pipelines/templates/test-workflow-windows.yml` -- NET 10 x86 test steps added; EF10 x86 steps added; all TFMs now have x86/x64 coverage.
    - `Build/Azure/scripts/db2.provider.sh` -- TFM-aware version split: net10.0 -> 10.0.0.100, others -> 9.0.0.400; uses wget+unzip (nuget install deprecated); sets PATH and LD_LIBRARY_PATH for DB2 CLI.
    - `Build/Azure/scripts/mac.db2.provider.sh` -- same TFM-aware version split as linux; osx variant lacks PATH export (handled differently on macOS).
    - `Directory.Packages.props` -- Net.IBM.Data.Db2* split to per-TFM versions (9.0.0.400 pre-net10 / 10.0.0.100 net10.0); Npgsql/Npgsql.NodaTime 10.0.2 (net8+); DuckDB.NET.Data.Full 1.5.2; SourceGear.sqlite3 3.50.4.5; BenchmarkDotNet 0.15.8; FSharp.Core 10.1.300; NUnit3TestAdapter 6.2.0; Meziantou.Analyzer 3.0.85; Microsoft.SourceLink.GitHub 10.0.300.
    - `linq2db.slnx` -- Tests.SingleFile project added (excluded from Azure/Release/Testing configs); .claude/agents/ folder entries expanded; no new production source projects.
- Tier 3 (skipped, logged): 0
  - Build/Azure/{net80,net90,net100,netfx}/*.json (150+ files) -- Tier-3 test data
  - Build/Azure/scripts/*.cmd -- Tier 3
</details>
