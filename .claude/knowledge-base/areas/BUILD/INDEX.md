---
area: BUILD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 3/4
coverage_tier_2: 71/71
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

**Tier 2 (all read):** `Build/Azure/pipelines/*.yml` (3 top-level + 8 templates), `Build/Azure/scripts/*.ps1` (3 existing + 1 added: `verify-nuget-sizes.ps1`) + `*.sh` (43), `.github/ISSUE_TEMPLATE/*.yml` (13), `.github/copilot-instructions.md` (added this run), `Data/Create Scripts/DuckDB.sql` (new, PR #5451).

## Subsystems

### TFM matrix and feature flags (`Directory.Build.props`)

`<TargetFrameworks>net462;netstandard2.0;net8.0;net9.0;net10.0</TargetFrameworks>` -- applies to all projects unless overridden. The `Testing` configuration pins to `net10.0` only for fast iteration.

Feature flags are `DefineConstants` conditioned on `IsTargetFrameworkCompatible(..., 'net8.0')` (or `net472` for `SUPPORTS_READONLY`):

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

`<RunAnalyzersDuringBuild>` is `false` by default and enabled only when `$(Configuration) == 'Release'`. `<EnforceCodeStyleInBuild>` is likewise `false` by default and enabled only when `'$(Configuration)' == 'Release'` -- as of PR #5523, IDE-style analyzers are now Release-only in both properties. `<TreatWarningsAsErrors>true` is unconditional. `<WarningLevel>9999</WarningLevel>` activates all Roslyn warnings. `<AnalysisLevel>preview-All</AnalysisLevel>` pulls in preview analyzers.

Analyzers added globally via `<PackageReference>` in `Directory.Build.props`: `AsyncFixer`, `Lindhart.Analyser.MissingAwaitWarning`, `Meziantou.Analyzer`, `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `Microsoft.SourceLink.GitHub`.

`RunApiAnalyzersDuringBuild` is opt-in per-project.

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

`sdk.version: "10.0.200"`, `rollForward: "minor"`, `allowPrerelease: false`. (Bumped from `10.0.0` in this delta.)

### Solution shape (`linq2db.slnx`)

Four build configurations: `Azure`, `Debug`, `Release`, `Testing`. Key project folders:

| Folder | Contents |
|---|---|
| `/Source/` | `LinqToDB.csproj`, `LinqToDB.CLI`, `LinqToDB.EntityFrameworkCore.*` (EF3/8/9/10), `LinqToDB.Extensions`, `LinqToDB.FSharp`, `LinqToDB.LINQPad`, `LinqToDB.Remote.*`, `LinqToDB.Scaffold`, `LinqToDB.Tools`, `CodeGenerators.csproj` |
| `/Tests/` | `Tests.csproj`, `Tests.Base`, `Tests.EntityFrameworkCore.*`, `Tests.FSharp`, `Tests.VisualBasic`, `Tests.Benchmarks`, `Tests.Playground`, `Tests.T4`, `Tests.Model` |
| `/Packaging/` | NuGet wrapper projects per provider |
| `/Build/` | `Directory.Build.props`, `Directory.Packages.props`, `linq2db.snk`, Azure pipeline YMLs/JSONs/scripts |

### Azure Pipelines: top-level pipelines

| File | Trigger | Purpose |
|---|---|---|
| `Build/Azure/pipelines/build.yml` | All PRs | Compile-only check |
| `Build/Azure/pipelines/default.yml` | Push to `master`/`release`; PRs to `release` | Full pipeline |
| `Build/Azure/pipelines/testing.yml` | Manual (`/azp run test-<db>` bot commands) | Test-only: builds for tests, then runs per-`db_filter` subset. Supports `test-duckdb` -> `[duckdb.all]` filter (added PR #5451). |

### Azure Pipelines: test matrix (`test-matrix.yml`, `test-jobs.yml`)

The `test-matrix.yml` template defines ~36 test matrix entries spanning: SQLite (Win/Linux/macOS, all TFMs); Access (MDB, ACE x86, ACE x64 disabled, Windows); SQL CE (Windows); MySQL/MariaDB; PostgreSQL 13--18; SQL Server 2005--2025; Sybase ASE 16; Oracle 11g--23c; Firebird 2.5--5.0; DB2 LUW 11.5; Informix 14.10; SAP HANA 2; ClickHouse (Driver/MySql); **DuckDB (Win/Linux/macOS; net8.0/9.0/10.0 only; no setup script required)**.

DuckDB uses file-based storage (`Data/TestData.duckdb`), so no container startup script is needed. Config files `Build/Azure/net{80,90,100}/duckdb.json` each activate provider `DuckDB`. Filter token: `[duckdb.all]`; pipeline definition: `test-duckdb`.

### DuckDB test schema (`Data/Create Scripts/DuckDB.sql`)

DDL for the DuckDB test database. Uses DuckDB-native type names (`HUGEINT`, `UHUGEINT`, `UINTEGER`, `UBIGINT`, `UTINYINT`, `USMALLINT`, `BITSTRING`, `TIMESTAMPTZ`, `TIMESTAMP_S`, `TIMESTAMP_MS`, `TIMESTAMP_NS`, `TIMETZ`, `TIME_NS`, `BIGNUM`, `INTERVAL`) in the `AllScaffoldTypes` table.

### NuGet package size guard (`Build/Azure/scripts/verify-nuget-sizes.ps1`, `Build/Azure/pipelines/templates/nuget-job.yml`)

Added after the 6.3.0 release-publish job hit HTTP 413 on `linq2db.cli.6.3.0.nupkg` (~416 MB) against nuget.org's 250 MB upload limit. The push had no pre-flight size check: pack succeeded, then push failed atomically after other packages had already been pushed.

`verify-nuget-sizes.ps1` parameters: `-PackagesDir` (required), `-WarnMB 180` (default), `-FailMB 200` (default), `-AzdoLogs $true` (default; emits `##vso[task.logissue]` markers). Exit codes: `0` = clean or warnings-only; `1` = any package over `$FailMB` (release-blocking); `2` = invalid args / no nupkgs found.

`nuget-job.yml` inserts a `PowerShell@2` task ("Verify nupkg sizes (fail-fast before publish)") before both the Azure Artifacts and nuget.org `NuGetCommand@2` push tasks. The task runs with `-WarnMB 180 -FailMB 200` against `.build/nugets/`. The nuget.org push task is conditioned on `$(Build.SourceBranchName) == $(release_branch)`, so the size check gates all publishes.

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
- Tier 2 (visited / total): 71 / 71 (100%)
  - Read (prior run, delta): Build/Azure/net{80,90,100}/duckdb.json; test-matrix.yml (DuckDB entry); testing.yml (test-duckdb filter); Directory.Build.props (EnforceCodeStyleInBuild Release-only, DuckDB DatabasePackageTags); Directory.Packages.props (DuckDB.NET.Data.Full); DataProviders.json (DuckDB entry); Tests/linq2db.Providers.props (DuckDB ref); Data/Create Scripts/DuckDB.sql (new)
  - Read (this run -- delta):
    - `Build/Azure/scripts/verify-nuget-sizes.ps1` (ADDED) -- new CI guard; warn 180 MB, fail (exit 1) 200 MB; motivated by `linq2db.cli` 6.3.0 hitting nuget.org 250 MB limit; emits AzDO `##vso[task.logissue]` by default.
    - `Build/Azure/pipelines/templates/nuget-job.yml` -- new `PowerShell@2` "Verify nupkg sizes" step before both NuGet push tasks; runs `verify-nuget-sizes.ps1 -WarnMB 180 -FailMB 200`; gates all publishes.
    - `Directory.Build.props` -- version 6.3.0 -> 6.4.0; EF3 3.32->3.33, EF8 8.6->8.7, EF9 9.5->9.6, EF10 10.4->10.5; Copyright (c) 2026; no structural changes to TFM matrix / feature flags / analyzer gating.
    - `global.json` -- SDK pin `10.0.0` -> `10.0.200`; rollForward minor, allowPrerelease false unchanged.
    - `Directory.Packages.props` -- bumps: `DuckDB.NET.Data.Full` 1.5.2, `Npgsql`/`Npgsql.NodaTime` 10.0.2 (net8+), `Net.IBM.Data.Db2*` 10.0.0.100 (net10.0), `SourceGear.sqlite3` 3.50.4.5, `BenchmarkDotNet` 0.15.8, `FSharp.Core` 10.1.300, `NUnit3TestAdapter` 6.2.0, `Meziantou.Analyzer` 3.0.85, `Microsoft.SourceLink.GitHub` 10.0.300; structure unchanged.
    - `linq2db.slnx` -- structure confirmed unchanged; no new projects in Tier-1 portion read.
    - `Source/BannedSymbols.txt` -- 287 lines; content matches documented categories; no new ban categories.
    - `.github/copilot-instructions.md` -- Copilot PR review formatting and testing rules; CI-tooling-adjacent.
- Tier 3 (skipped, logged): 0
  - Build/Azure/{net80,net90,net100,netfx}/*.json (150+ files) -- Tier-3 test data
  - Build/Azure/scripts/*.cmd -- Tier 3
</details>
