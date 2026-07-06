---
area: BUILD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 3/4
coverage_tier_2: 93/93
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
| `global.json` | SDK pin: .NET 10.0, `rollForward: minor`, `allowPrerelease: false`. Also pins the `dotnet test` CLI to the `Microsoft.Testing.Platform` (MTP) native runner via `"test": {"runner": "Microsoft.Testing.Platform"}` (added this run). |
| `linq2db.slnx` | Solution file (VS 2022 XML format). Lists all projects across 7 folders. |
| `Build/BannedSymbols.txt` | **MISSING** -- the actual banned-API list is at `Source/BannedSymbols.txt`. |

**Tier 2 (all read):** `Build/Azure/pipelines/*.yml` (3 top-level + 8 templates), `Build/Azure/scripts/*.ps1` (5: `verify-nuget-sizes.ps1`, `ensure-baselines-branch.ps1` (new this run), 3 pre-existing) + `*.sh` (46: 43 pre-existing + `pgsql19.sh`, `mac.pgsql19.sh`, `ydb.sh` (new this run)), `.github/ISSUE_TEMPLATE/*.yml` (13), `.github/copilot-instructions.md`, `Data/Create Scripts/DuckDB.sql`, `.editorconfig` (root), `Build/Azure/scripts/db2.provider.sh`, `Build/Azure/scripts/mac.db2.provider.sh`, `Build/Azure/net{80,90,100}/{duckdb,pgsql19,ydb}.json` (9: 3 pre-existing duckdb + 6 new this run), `Build/Azure/README.md`.

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

### `global.json` -- SDK pin and test runner

`sdk.version: "10.0.200"`, `rollForward: "minor"`, `allowPrerelease: false` (unchanged this run). Adds `"test": { "runner": "Microsoft.Testing.Platform" }` -- pins the `dotnet test` CLI to the MTP-native test host instead of legacy VSTest. This is the switch that makes the CI-side MTP argument changes (see "CI test invocation" subsystem below) apply to local `dotnet test` invocations too, not just the direct-executable path CI now uses.

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
| `Build/Azure/pipelines/testing.yml` | Manual (`/azp run test-<db>` bot commands) | Test-only: builds for tests, then runs per-`db_filter` subset. Supports `test-duckdb` -> `[duckdb.all]` and `test-ydb` -> `[ydb.all]` filters (`ydb` mapping added this run). |

`Build/Azure/README.md` documents the `/azp run` command catalog and the per-database/TFM test matrix table; this run adds the `/azp run test-ydb` line and a `PostgreSQL 19` + `YDB` row to both the OS/TFM support matrix and the `ProviderName`/`TestProvName` reference table.

### Azure Pipelines: build job (`build-job.yml`)

Build pool image: `windows-2025`. `timeoutInMinutes: 120`. Key steps:

- Installs .NET SDK 9.x then 10.x via `UseDotNet@2`.
- `PublishSingleFile` smoke test: publishes `Tests.SingleFile.csproj` as win-x64 self-contained single file and executes it; guards against `Assembly.Location` / `File.Exists` regressions in provider detectors (PR #5488).
- Publishes test binaries for NETFX, net8.0, net9.0, net10.0 (x86 and x64); includes EF Core 3/8/9/10 tests. The EF3 net462 x64 publish now pins an explicit `-a x64` RID (comment: SQLitePCLRaw's net4x native target only ships the x86 `e_sqlite3`, which an x64 EF test process can't load without the RID pin).
- Copies `.runsettings` alongside each published test app (`main` and `efcore`, all TFM/arch combos) -- comment: "MTP does not auto-discover it, NUnit honors it via `--settings` (`AssemblySelectLimit`)" (added this run, part of the MTP migration).
- The x86 stub MSBuild property passed to `dotnet publish` for `Tests/Linq/Tests.csproj` is renamed from `/p:DB2STUB=True` to `/p:X86STUBS=True` (broader-scoped name; the underlying stub source is `Tests/Base/X86Stubs/DB2Stubs.cs` -- see TESTS area for consumer-side detail).
- Builds and packs for NuGet on Release configuration; publishes nugets and LINQPad LPX artifacts.
- Creates GitHub Release draft (via `gh`) when building the release branch.

### CI test invocation: `dotnet test` -> Microsoft Testing Platform (MTP) native execution

All three OS test-workflow templates (`test-workflow-linux.yml`, `test-workflow-macos.yml`, `test-workflow-windows.yml`) replace `dotnet test <dll> -f <tfm> -l trx $(extra) --blame-hang-timeout 5m` with direct execution of the published test host: `dotnet ./net{8,9,10}.0/{main,efcore}/x64/linq2db*.Tests.dll ...` on Linux/macOS, and the bare `.exe` (no `dotnet` prefix) on Windows, e.g. `net10.0\main\x64\linq2db.Tests.exe`. The new MTP-native argument set is `--filter "TestCategory != SkipCI" --settings <path>\.runsettings --report-trx --report-trx-filename <tfm>-<suite>-<arch>.trx --results-directory TestResults --hangdump --hangdump-timeout 5m`, replacing VSTest's `-f <tfm> -l trx --blame-hang-timeout 5m`.

The `$(extra)` variable (`--arch x86` / `--arch x64`, previously injected per Access matrix entry in `test-matrix.yml`) is removed entirely -- per-architecture selection now comes from the publish output path (`.../x86/` vs `.../x64/`), not a `dotnet test` CLI flag. `test-matrix.yml`'s `extra` parameter documentation comment and all three `extra: --arch x86`/`--arch x64` entries (Access x86/x64 jobs) are deleted.

All three templates also add a `DownloadPipelineArtifact@2` step for `$(artifact_test_scripts)` earlier in the sequence (before the baselines-branch self-heal step, see below), and drop the old later duplicate download of the same artifact.

`build-vars.yml` adds `test_retry_args: '--retry-failed-tests 2 --retry-failed-tests-max-tests 5'` for crash/resource-unstable providers (Access, Oracle). MTP re-runs only the failed tests in-process on a flaky failure, so a single flaky test no longer trips `retryCountOnTaskFailure` into re-running the whole ~50-min test leg; the max-tests cap skips retry on mass failures (real breakage, not flakiness). The retry-path script blocks in each `test-workflow-*.yml` append `$(test_retry_args)` to the MTP invocation.

### Baselines branch creation (`Build/Azure/scripts/ensure-baselines-branch.ps1`, new this run)

Extracted from an inline `PowerShell@2` script previously embedded directly in `test-jobs.yml`'s `create_baselines_branch` job. Same `linq2db.baselines` repo create/rebase logic, now parameterized (`-Branch`, `-PrId`, `-BaselinesMaster`, `-BaseHash`, `-Rebase`, `-EmitOutputs`) and shared by two callers:

- **Central** (`create_baselines_branch`, once per run): `-PrId "$(source_pr_id)" -BaselinesMaster "$(baselines_master)" -Rebase -EmitOutputs`. Creates the branch if missing, rebases it onto `baselines_master` when it already exists but is behind, and exports `baselines_branch` / `baselines_head` / `baselines_new_branch` as task-output variables.
- **Self-heal** (`test_windows_job` / `test_linux_job` / `test_macos_job`, one new step each, before their `baselines` clone): `-Branch "$(baselines_branch)" -BaselinesMaster "$(baselines_master)" -BaseHash "$(baselines_head)"`. Re-creates the branch at the recorded `baselines_head` hash if a prior completed run already deleted it via empty-branch cleanup -- without this, an Azure Pipelines "rerun failed jobs" restart fails because `create_baselines_branch` is not re-run on a partial restart and its branch was already removed by `create_baselines_pr` (referenced incident: build 21555).

Branch creation is race-tolerant: when several test jobs self-heal a missing branch concurrently, only one wins the `git/refs` POST; the losers re-query and proceed once they see the branch created by a sibling job.

`test-jobs.yml` also adds `baselines_head` as a second job-output variable (alongside the pre-existing `baselines_branch`) for all three OS jobs, so the self-heal steps can read it.

### Azure Pipelines: test matrix (`test-matrix.yml`, `test-jobs.yml`)

The `test-matrix.yml` template defines ~40 test matrix entries spanning: SQLite (Win/Linux/macOS, all TFMs); Access (MDB, ACE x86, ACE x64 disabled, Windows); SQL CE (Windows); MySQL/MariaDB; PostgreSQL 13--19; SQL Server 2005--2025; Sybase ASE 16; Oracle 11g--23c; Firebird 2.5--5.0; DB2 LUW 11.5; Informix 14.10; SAP HANA 2; ClickHouse (Driver/MySql); DuckDB (Win/Linux/macOS; net8.0/9.0/10.0 only; no setup script required); **YDB** (Linux/macOS; net8.0/9.0/10.0 only).

New entries since prior build run:
- **PostgreSQL 19** (`pgsql19`): Linux + macOS; net8/9/10 only; uses `pgsql19.sh` / `mac.pgsql19.sh` scripts; enabled by `[all]` or `[postgresql.all]` filters. Docker image tag is `postgres:19beta1` (pre-release build).
- **YDB** (`YDB`): Linux + macOS (docker installed on the macOS runner); net8/9/10 only; both OSes share the same `ydb.sh` setup script (no separate `mac.ydb.sh`); filter token `[ydb.all]`; new `/azp run test-ydb` pipeline definition maps to it in `testing.yml`.
- (from prior run, retained) **PostgreSQL 18** (`pgsql18`), **SQL Server 2025** (`SqlServer2025`), **SqlServer2019Extras** (`SqlServer2019Extras`).

`test-jobs.yml`'s inline baselines-branch `PowerShell@2` script (the large ls-remote/create/rebase block) is replaced by a call to `Build/Azure/scripts/ensure-baselines-branch.ps1` (see subsystem above); the `create_baselines_branch` job now downloads `$(artifact_test_scripts)` first via `DownloadPipelineArtifact@2`, then invokes the script with `-PrId "$(source_pr_id)" -BaselinesMaster "$(baselines_master)" -Rebase -EmitOutputs`. The `extra` matrix-entry property (Access x86/x64 `--arch` flags) is removed -- see the MTP subsystem above.

DuckDB and YDB both use file-based/container-only storage without a Windows job; config files `Build/Azure/net{80,90,100}/{duckdb,ydb}.json` each activate their respective provider (`DuckDB` / `YDB`). PostgreSQL 19 similarly gets `Build/Azure/net{80,90,100}/pgsql19.json`, each `{"NET{80,90,100}.Azure": {"Providers": ["PostgreSQL.19"]}}`.

### PostgreSQL 19 and YDB test container setup (`Build/Azure/scripts/pgsql19.sh`, `mac.pgsql19.sh`, `ydb.sh`, all new this run)

- `pgsql19.sh` (Linux): `docker run -d --name pgsql -e POSTGRES_PASSWORD=... -p 5432:5432 ... postgres:19beta1`, polls up to 100 retries (1s apart) via `psql -U postgres -c '\l'` grepping for the `testdata` database, creating it each retry until present.
- `mac.pgsql19.sh` (macOS): same `postgres:19beta1` image with a named volume (`pgdb:/var/run/postgresql`) and container hostname `-h pgsql`; polls unconditionally until `psql -U postgres -c '\l'` succeeds, then creates `testdata` once (no retry loop on the create step, unlike the Linux variant).
- `ydb.sh` (shared Linux + macOS): `docker run -d --name ydb -p 2136:2136 -e YDB_FEATURE_FLAGS=enable_temp_tables ydbplatform/local-ydb:latest`; polls up to 50 retries (5s apart) via `docker logs ydb` grepping for the startup marker string `"Table profiles were not loaded"`.

Both new provider setups follow the same docker-run + log/psql-poll pattern already used by `db2.provider.sh` and the other `scripts/*.sh` container bootstraps in this area.

### DuckDB test schema (`Data/Create Scripts/DuckDB.sql`)

DDL for the DuckDB test database. Uses DuckDB-native type names (`HUGEINT`, `UHUGEINT`, `UINTEGER`, `UBIGINT`, `UTINYINT`, `USMALLINT`, `BITSTRING`, `TIMESTAMPTZ`, `TIMESTAMP_S`, `TIMESTAMP_MS`, `TIMESTAMP_NS`, `TIMETZ`, `TIME_NS`, `BIGNUM`, `INTERVAL`) in the `AllScaffoldTypes` table.

### NuGet package size guard (`Build/Azure/scripts/verify-nuget-sizes.ps1`, `Build/Azure/pipelines/templates/nuget-job.yml`)

Added after the 6.3.0 release-publish job hit HTTP 413 on `linq2db.cli.6.3.0.nupkg` (~416 MB) against nuget.org's 250 MB upload limit. The push had no pre-flight size check: pack succeeded, then push failed atomically after other packages had already been pushed.

`verify-nuget-sizes.ps1` parameters: `-PackagesDir` (required), `-WarnMB 180` (default), `-FailMB 200` (default), `-AzdoLogs $true` (default; emits `##vso[task.logissue]` markers). Exit codes: `0` = clean or warnings-only; `1` = any package over `$FailMB` (release-blocking); `2` = invalid args / no nupkgs found.

`nuget-job.yml` inserts a `PowerShell@2` task (Verify nupkg sizes fail-fast before publish) before both the Azure Artifacts and nuget.org `NuGetCommand@2` push tasks. The task runs with `-WarnMB 180 -FailMB 200` against `.build/nugets/`. The nuget.org push task is conditioned on `$(Build.SourceBranchName) == $(release_branch)`, so the size check gates all publishes.

### DB2 provider setup scripts (`Build/Azure/scripts/db2.provider.sh`, `Build/Azure/scripts/mac.db2.provider.sh`)

Both scripts are TFM-aware. They detect the active TFM from the directory path and select the correct `Net.IBM.Data.Db2-lnx` / `Net.IBM.Data.Db2-osx` package version:

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
- **PostgreSQL 19 CI job targets a pre-release Docker image.** Both `pgsql19.sh` and `mac.pgsql19.sh` pin `postgres:19beta1`; expect a tag swap once PostgreSQL 19 reaches GA.
- **YDB and PostgreSQL 19 have no netfx/Windows job.** Same pattern as DuckDB -- Linux/macOS + net8/9/10 only, no Windows test-matrix entry.

## See also

- [architecture overview](../../architecture/overview.md)
- [CLAUDE.md](../../../../CLAUDE.md) -- build commands reference
- `.agents/docs/testing.md`
- `.agents/docs/ci-tests.md`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 3 / 4
  - Directory.Build.props -- read in full
  - global.json -- read in full
  - linq2db.slnx -- read in full
  - Build/BannedSymbols.txt -- MISSING on disk
  - Source/BannedSymbols.txt -- read in full (actual location)
- Tier 2 (visited / total): 93 / 93 (100%)
  - Read (prior run, delta): Build/Azure/net{80,90,100}/duckdb.json; test-matrix.yml (DuckDB entry); testing.yml (test-duckdb filter); Directory.Build.props (EnforceCodeStyleInBuild Release-only, DuckDB DatabasePackageTags); Directory.Packages.props (DuckDB.NET.Data.Full); DataProviders.json (DuckDB entry); Tests/linq2db.Providers.props (DuckDB ref); Data/Create Scripts/DuckDB.sql (new)
  - Read (prior run -- delta 2): Build/Azure/scripts/verify-nuget-sizes.ps1 (ADDED); Build/Azure/pipelines/templates/nuget-job.yml; Directory.Build.props (version 6.3.0 -> 6.4.0); global.json (SDK pin 10.0.0 -> 10.0.200); Directory.Packages.props (DuckDB.NET.Data.Full 1.5.2, Npgsql 10.0.2); linq2db.slnx; Source/BannedSymbols.txt; .github/copilot-instructions.md
  - Read (prior run -- delta 3): `.editorconfig` (full Meziantou.Analyzer 3.0.85 catalog); `Build/Azure/pipelines/build.yml` / `default.yml` (no structural change); `build-job.yml` (windows-2025 pool, PublishSingleFile smoke test, EF10 publish steps); `test-matrix.yml` (PostgreSQL 18, SQL Server 2025, SqlServer2019Extras entries); `test-workflow-linux.yml` / `test-workflow-macos.yml` / `test-workflow-windows.yml` (structure baseline, NET10 x86 steps); `db2.provider.sh` / `mac.db2.provider.sh` (TFM-aware version split); `Directory.Packages.props` (per-TFM DB2 versions, Npgsql/DuckDB/BenchmarkDotNet/FSharp.Core/NUnit3TestAdapter/Meziantou.Analyzer/SourceLink bumps); `linq2db.slnx` (Tests.SingleFile project added)
  - Read (this run -- delta):
    - `Build/Azure/README.md` -- adds `/azp run test-ydb` line; adds PostgreSQL 19 row (OS/TFM matrix + `ProviderName.PostgreSQL19` reference row); adds YDB row (OS/TFM matrix + `ProviderName.Ydb` reference row).
    - `Build/Azure/net100/pgsql19.json`, `Build/Azure/net80/pgsql19.json`, `Build/Azure/net90/pgsql19.json` -- new provider-activation configs, each `{"NET{80,90,100}.Azure": {"Providers": ["PostgreSQL.19"]}}`.
    - `Build/Azure/net100/ydb.json`, `Build/Azure/net80/ydb.json`, `Build/Azure/net90/ydb.json` -- new provider-activation configs, each `{"NET{80,90,100}.Azure": {"Providers": ["YDB"]}}`.
    - `Build/Azure/pipelines/templates/build-job.yml` -- copies `.runsettings` next to each published test app (MTP does not auto-discover it); renames x86 stub property `DB2STUB` -> `X86STUBS`; EF3 net462 x64 publish now pins explicit `-a x64` RID (SQLitePCLRaw native-asset comment).
    - `Build/Azure/pipelines/templates/build-vars.yml` -- adds `test_retry_args: '--retry-failed-tests 2 --retry-failed-tests-max-tests 5'` for MTP in-process failed-test retry.
    - `Build/Azure/pipelines/templates/test-jobs.yml` -- inline baselines-branch PowerShell script replaced by a call to `ensure-baselines-branch.ps1`; downloads `$(artifact_test_scripts)` earlier (before the baselines step); drops the `extra` variable passthrough for all three OS jobs; adds `baselines_head` job-output variable alongside `baselines_branch`.
    - `Build/Azure/pipelines/templates/test-matrix.yml` -- adds `PostgreSQL19` matrix entry (Linux+macOS, net8/9/10, `pgsql19.sh`/`mac.pgsql19.sh`) and `YDB` matrix entry (Linux+macOS, net8/9/10, `ydb.sh`); removes the `extra` parameter doc-comment and all `extra: --arch x86`/`--arch x64` Access-entry properties.
    - `Build/Azure/pipelines/templates/test-workflow-linux.yml` -- switches all `dotnet test <dll> -f <tfm> -l trx $(extra) --blame-hang-timeout 5m` invocations to direct MTP host execution (`dotnet ./<tfm>/{main,efcore}/x64/linq2db*.Tests.dll ... --settings .../.runsettings --report-trx ... --hangdump --hangdump-timeout 5m [$(test_retry_args)]`); adds `DownloadPipelineArtifact` for test scripts earlier + a new "Ensure test baselines branch" self-heal step (`ensure-baselines-branch.ps1`) before the baselines clone; removes the old later duplicate scripts-artifact download.
    - `Build/Azure/pipelines/templates/test-workflow-macos.yml` -- same MTP invocation switch + self-heal step + download reordering as the linux template.
    - `Build/Azure/pipelines/templates/test-workflow-windows.yml` -- same MTP invocation switch (bare `.exe`, no `dotnet` prefix, e.g. `net10.0\main\x64\linq2db.Tests.exe`) + self-heal step + download reordering; covers netfx x86/x64 and net8/9/10 x86/x64 legs.
    - `Build/Azure/pipelines/testing.yml` -- adds `test-ydb` -> `[ydb.all]` `db_filter` mapping alongside the existing `test-duckdb`/`test-clickhouse`/etc. mappings.
    - `Build/Azure/scripts/ensure-baselines-branch.ps1` -- NEW; factors the baselines-branch create/rebase logic out of `test-jobs.yml` into a shared, parameterized script with a central create/rebase mode (`-Rebase -EmitOutputs`) and a per-job self-heal mode (`-BaseHash`); addresses a "rerun failed jobs" restart failure referencing build 21555.
    - `Build/Azure/scripts/mac.pgsql19.sh`, `Build/Azure/scripts/pgsql19.sh` -- NEW; docker container setup for PostgreSQL 19 (`postgres:19beta1` image), poll-and-create the `testdata` database.
    - `Build/Azure/scripts/ydb.sh` -- NEW; docker container setup for YDB (`ydbplatform/local-ydb:latest`, `YDB_FEATURE_FLAGS=enable_temp_tables`), polls container logs for the "Table profiles were not loaded" startup marker.
    - `global.json` -- adds `"test": {"runner": "Microsoft.Testing.Platform"}`; SDK version/rollForward unchanged (`10.0.200`, `minor`, `false`).
- Tier 3 (skipped, logged): 0
  - Build/Azure/{net80,net90,net100,netfx}/*.json (150+ files, minus the 9 provider-activation configs now called out by name above) -- Tier-3 test data
  - Build/Azure/scripts/*.cmd -- Tier 3
</details>
