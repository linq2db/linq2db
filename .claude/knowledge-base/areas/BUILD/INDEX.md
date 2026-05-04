---
area: BUILD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 3/4
coverage_tier_2: 70/70
---

# BUILD

CI and build infrastructure for linq2db. Controls: TFM matrix, feature flags, versioning, analyzer gating, SDK pinning, solution shape, Azure Pipelines test matrix (30+ database/version combinations), and GitHub issue triage forms. No GitHub Actions workflows exist — all CI runs on Azure Pipelines.

## Key types

This area has no C# types. Its artifacts are MSBuild/YAML/shell configuration files.

## Files (Tier 1 / Tier 2)

**Tier 1:**

| File | Role |
|---|---|
| `Directory.Build.props` | Global MSBuild anchor: TFMs, version numbers, feature flags, analyzer gating, polyfills, NuGet metadata, assembly signing. Imported by every project in the solution. |
| `global.json` | SDK pin: .NET 10.0, `rollForward: minor`, `allowPrerelease: false`. |
| `linq2db.slnx` | Solution file (VS 2022 XML format). Lists all projects across 7 folders: Source, Tests, Packaging (NuGet), Build/Azure, and `.claude` meta-folders. |
| `Build/BannedSymbols.txt` | **MISSING** — see UNCLASSIFIED-FILE below. The actual banned-API list is at `Source/BannedSymbols.txt`, referenced by `Source/Directory.Build.props`. |

**Tier 2 (all read):** `Build/Azure/pipelines/*.yml` (3 top-level + 8 templates), `Build/Azure/scripts/*.ps1` (3) + `*.sh` (43), `.github/ISSUE_TEMPLATE/*.yml` (13).

## Subsystems

### TFM matrix and feature flags (`Directory.Build.props`)

`<TargetFrameworks>net462;netstandard2.0;net8.0;net9.0;net10.0</TargetFrameworks>` — applies to all projects unless overridden. The `Testing` configuration pins to `net10.0` only for fast iteration.

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
| `<Version>` | `6.3.0` | Main product version; drives `PackageVersion` and `AssemblyVersion` |
| `<EF3Version>` | `3.32.0` | EF Core 3.x package |
| `<EF8Version>` | `8.6.0` | EF Core 8.x package |
| `<EF9Version>` | `9.5.0` | EF Core 9.x package |
| `<EF10Version>` | `10.4.0` | EF Core 10.x package |
| `<BaselineVersion>` | `6.0.0` | API compatibility baseline for `PackageValidationBaselineVersion` |

`<VersionSuffix>` defaults to `-local.1` and `<ApplyVersionSuffix>` defaults to `true` — local builds get a pre-release suffix. CI strips it on the `release` branch (`ApplyVersionSuffix=false`).

### Roslyn analyzer gating (`Directory.Build.props`, `Source/Directory.Build.props`)

`<RunAnalyzersDuringBuild>` is `false` by default and enabled only when `$(Configuration) == 'Release'` (`Directory.Build.props`:108–110). `<TreatWarningsAsErrors>true` is unconditional. `<WarningLevel>9999</WarningLevel>` activates all Roslyn warnings. `<AnalysisLevel>preview-All</AnalysisLevel>` and `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` pull in IDE-level analyzers.

Analyzers added to every project via global `<PackageReference>` in `Directory.Build.props`:
- `AsyncFixer` — await-usage correctness
- `Lindhart.Analyser.MissingAwaitWarning`
- `Meziantou.Analyzer` — broad best-practices
- `Microsoft.CodeAnalysis.BannedApiAnalyzers` — enforces `Source/BannedSymbols.txt`
- `Microsoft.SourceLink.GitHub` — deterministic builds

`RunApiAnalyzersDuringBuild` (API compatibility analyzer via `Microsoft.CodeAnalysis.PublicApiAnalyzers`) is opt-in per-project, off by default in `Source/Directory.Build.props`:5, enabled explicitly in `build-job.yml` for release-branch builds.

### Banned-API enforcement (`Source/BannedSymbols.txt`)

288-line list consumed by `Microsoft.CodeAnalysis.BannedApiAnalyzers`. Key ban categories:

- **Flawed concurrent collections**: `ConcurrentBag<T>` banned (PR #2066).
- **ADO.NET interfaces**: All `IDataReader`, `IDbCommand`, `IDbConnection`, etc. interfaces banned; callers must use `DbDataReader`, `DbCommand`, `DbConnection` (concrete base classes).
- **Attribute reflection without cache**: All `GetCustomAttribute`/`GetCustomAttributes`/`IsDefined` direct calls banned; callers must use `AttributesExtensions.GetAttribute<T>()` helpers.
- **Culture-dependent formatting**: Parameterless overloads of `DateTime.ToString`, `Decimal.Parse`, `String.Format`, `StringBuilder.Append`, `Convert.ToString`, etc. banned; callers must supply `IFormatProvider`.
- **Expression.Compile direct call**: `LambdaExpression.Compile()` banned; callers must use `CompileExpression` extension.
- **Reflection invocation**: `MethodBase.Invoke`, `Activator.CreateInstance`, `Delegate.DynamicInvoke` banned; callers must use `InvokeExt`/`ActivatorExt` extensions that unwrap `TargetInvocationException`.
- **DbCommand.Dispose direct**: `DbCommand.DisposeAsync`/`Component.Dispose` banned; callers must use `IDataProvider.DisposeCommandAsync`/`DisposeCommand`.
- **CurrentCulture string.IndexOf**: `string.IndexOf(string)` and overloads without `StringComparison` banned (issue #5188).
- **Type.GetInterfaceMap**: Banned; use `GetInterfaceMapEx` extension.

### Polyfills (`Directory.Build.props`)

`Meziantou.Polyfill` is configured via `<MeziantouPolyfill_IncludedPolyfills>` (lines 123–180). Polyfills span: `System.Diagnostics.CodeAnalysis`, `HashCode`, `Index`/`Range`, `System.Threading.Lock`, `ArgumentNullException.ThrowIfNull`, `Enum.GetNames<T>`, `AsyncEnumerable.FirstAsync`, etc. This lets the main library code use modern APIs uniformly; polyfills inject shims for older TFMs.

### `global.json` — SDK pin

`sdk.version: "10.0.0"`, `rollForward: "minor"`, `allowPrerelease: false`. Forces .NET 10 SDK for all builds; minor-band roll-forward allows patch updates.

### Solution shape (`linq2db.slnx`)

Four build configurations: `Azure`, `Debug`, `Release`, `Testing`. Key project folders:

| Folder | Contents |
|---|---|
| `/Source/` | `LinqToDB.csproj`, `LinqToDB.CLI`, `LinqToDB.EntityFrameworkCore.*` (EF3/8/9/10), `LinqToDB.Extensions`, `LinqToDB.FSharp`, `LinqToDB.LINQPad`, `LinqToDB.Remote.*`, `LinqToDB.Scaffold`, `LinqToDB.Tools`, `CodeGenerators.csproj` |
| `/Tests/` | `Tests.csproj`, `Tests.Base`, `Tests.EntityFrameworkCore.*` (EF3/8/9/10), `Tests.FSharp`, `Tests.VisualBasic`, `Tests.Benchmarks`, `Tests.Playground`, `Tests.T4`, `Tests.Model` |
| `/Packaging/` | NuGet wrapper projects per provider: Access, ClickHouse, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SapHana, SqlCe, SQLite, SqlServer, Sybase, t4models, plus `NuGet.csproj` |
| `/Build/` | `Directory.Build.props`, `Directory.Packages.props`, `linq2db.snk`, Azure pipeline YMLs/JSONs/scripts |

Most `Testing|*` builds are disabled for packaging and heavy test variants to keep fast-iteration builds lean.

### Azure Pipelines: top-level pipelines

| File | Trigger | Purpose |
|---|---|---|
| `Build/Azure/pipelines/build.yml` | All PRs | Compile-only check: builds solution (`with_nugets: true`, `with_tests: false`, `with_analyzers: false`). Analyzers currently disabled due to Roslyn issue #80621. |
| `Build/Azure/pipelines/default.yml` | Push to `master`/`release`; PRs to `release` | Full pipeline: build + NuGet pack + release draft creation; tests enabled only for PRs targeting `release`; full test matrix (`[all][metrics]`) on release PRs. |
| `Build/Azure/pipelines/testing.yml` | Manual (`/azp run test-<db>` bot commands) | Test-only: builds for tests, then runs per-`db_filter` subset of the test matrix. |

Variables (`build-vars.yml`): `solution = linq2db.slnx`, `netfx_tfm = net462`, `release_configuration = Release`, `test_configuration = Azure`. Artifact names follow the pattern `test_net{80,90,100}_binaries`.

### Azure Pipelines: test matrix (`test-matrix.yml`, `test-jobs.yml`)

The `test-matrix.yml` template defines ~35 test matrix entries spanning:

| Provider group | Versions covered | OS |
|---|---|---|
| SQLite | — | Win/Linux/macOS, all TFMs |
| Access | MDB (Jet/ODBC), ACE x86, ACE x64 (disabled) | Windows only |
| SQL CE | — | Windows only |
| MySQL/MariaDB | MySQL 5.7, MySQL 9, MariaDB 11 | Linux/macOS |
| PostgreSQL | 13–18 | Linux/macOS |
| SQL Server | 2005–2025 (plus Extras, Metrics variant) | Win/Linux/macOS; older versions Windows-only |
| Sybase ASE | 16 | Linux/macOS |
| Oracle | 11g–23c | Linux/macOS; retry=true for all (resource mgmt issues) |
| Firebird | 2.5–5.0 | Linux/macOS |
| DB2 LUW | 11.5 | Linux/macOS; retry=true |
| Informix | 14.10 | Linux/macOS; retry=true |
| SAP HANA | 2 | Linux/macOS |
| ClickHouse | Driver, MySql variants (Octonica always disabled) | Linux/macOS |

Each matrix entry carries: `config_win`/`config_linux`/`config_macos` (JSON filename under `Build/Azure/{netXX}/`), `script_*_global` (DB startup script), `script_*_local` (per-TFM test setup), `enable_fw_*` (TFM selectors), `enable_os_*`, and optional `retry: true`.

`test-jobs.yml` expands entries into three parallel jobs: `test_windows_job` (windows-2025), `test_ubuntu_job` (ubuntu-24.04), `test_macos_job` (macOS-15). It also manages `create_baselines_branch` (creates/rebases a PR-scoped branch in `linq2db/linq2db.baselines`) and `create_baselines_pr` (opens a draft PR in the baselines repo if new baseline files were committed).

Each test workflow template (`test-workflow-{windows,linux,macos}.yml`) iterates TFMs sequentially (netfx → net8.0 → net9.0 → net10.0), downloading binaries, copying the per-db `UserDataProviders.json` config, running `dotnet test ... --filter "TestCategory != SkipCI"`, then committing baselines changes.

### Azure Pipelines: build job (`build-job.yml`)

Pool: `windows-2025`. Steps:
1. Install .NET 9.x and 10.x SDKs explicitly (Azure images lag).
2. Set `versionSuffix` to `-dev.$(BuildId)` (non-release) or strip it (release).
3. Build `Examples/Examples.slnf` in Debug (compile verification).
4. Build full solution in `Azure` config and publish test artifacts for TFMs `net462`, `net8.0`, `net9.0`, `net10.0` (x64 and x86 variants). x86 DB2 stub built with `/p:DB2STUB=True`.
5. Build + pack in `Release` config for NuGet artifacts. API analyzers (`RunApiAnalyzersDuringBuild`, `RunApiAnalyzersDuringBuild`) conditionally enabled.
6. Publish LPX artifacts for LINQPad.
7. Create GitHub release draft (`gh release create v$version ...`) on release branch using `RELEASES_GH_PAT`.

### Azure Pipelines: nuget-job (`nuget-job.yml`)

Runs after `build_job`, depends on `build_job`. Downloads `nugets` artifact, then:
- On `master` branch: pushes to Azure Artifacts internal feed.
- On `release` branch: pushes to NuGet.org using `linq2db nuget feed` service connection.

### Build/Azure/scripts: provider setup scripts

Scripts are small and uniform. Two patterns:

- **`.sh` scripts** (Linux/macOS): `docker run -d ...` to start the provider container, then a polling loop checking container logs for a ready signal. E.g., `pgsql17.sh` waits for `testdata` database, `db2.sh` waits for `Setup has completed`, `oracle23.sh` waits for `DATABASE IS READY TO USE!`. The Oracle scripts also set `TZ=CET` pipeline variable. Firebird scripts use `sleep 15` rather than log polling. SAP HANA has a separate `hana2.tests.sh` (per-TFM local setup).
- **`.ps1` scripts** (Windows): Install native provider components via MSI/EXE download from Microsoft or the `linq2db/linq2db.ci` companion repo (Access ACE runtime, SQL CE runtime).
- `mac.*.sh` scripts mirror the corresponding Linux scripts, often identical except for DB2/Informix which use a macOS-specific provider download.

### Build/Azure/{netXX}/*.json: test configs

Per-provider JSON files under `Build/Azure/net{100,80,90}/` and `Build/Azure/netfx/` contain the `UserDataProviders.json` format consumed by the test harness. Each file activates a specific provider+version combination. Config files are published as the `test_configs` pipeline artifact, then copied onto test agents.

### .github/ISSUE_TEMPLATE: issue triage forms

13 YAML forms in two series:

- **Bug reports** (`01–09`): Core linq2db (01), EF.Core (02), CLI/scaffold (03), T4 templates (04), LINQPad (05), Other (09). Common fields: Description, Reproduction Steps, Exception details, Regression?, version/runtime/database/OS.
- **Feature requests** (`11–19`): Core (11), EF.Core (12), CLI (13), T4 (14), LINQPad (15), Other (19). Fields: Description, New API proposal.

Pre-applied labels: bug reports auto-assign `status: needs-tests`; EF.Core forms add `area: efcore`, CLI forms add `area: scaffold`. `config.yml` redirects new-DB support requests to issue #1014, general questions to Discussions, and provides Discord link.

## Inbound / outbound dependencies

**Inbound (everything depends on this area):**
- Every C# project in `Source/` and `Tests/` imports `Directory.Build.props` implicitly via MSBuild directory inheritance.
- Every source project in `Source/` additionally imports `Source/Directory.Build.props`, which itself imports the root `Directory.Build.props`.
- `Source/Directory.Build.props` adds `Source/BannedSymbols.txt` (the actual banned-API list) as an `<AdditionalFiles>` input to `Microsoft.CodeAnalysis.BannedApiAnalyzers`.

**Outbound (this area consumes):**
- The CI test matrix touches every PROV-* provider area (script and config per database).
- `build-job.yml` publishes test binaries for every TESTS-* area.
- `nuget-job.yml` publishes packages produced by all packaging projects.

## Known issues / debt

- **`Build/BannedSymbols.txt` pin is stale.** The file listed in `kb-areas.md` as a Tier-1 pin (`Build/BannedSymbols.txt`) does not exist. The actual file is `Source/BannedSymbols.txt`, referenced by `Source/Directory.Build.props`. See AUDIT-NOTE below.
- **Roslyn analyzer disabled in `build.yml`.** The PR build pipeline has `with_analyzers: false` with an inline comment `# true disabled till https://github.com/dotnet/roslyn/pull/80621 shipped`. This means analyzer violations can land on `master` without a CI gate — only caught on Release-config local builds.
- **macOS tests disabled by default.** `mac_enabled: false` is the default in `test-matrix.yml`. The macOS job runs only when `test-jobs.yml` sets it explicitly. The commented-out line in `testing.yml` (`# mac_enabled: true`) suggests it was recently disabled to control costs.
- **Access ACE x64 disabled.** `access.ace.x64` entry has `enable_os_windows: false` (commented as `# true`) due to dotnet/runtime#46187 (random AV crash in x64 OLEDB).
- **ClickHouse Octonica always disabled.** The Octonica ClickHouse client entry in `test-matrix.yml` has `enabled: false` in both branches of its `${{ if }}` — it is never tested.
- **`testing.yml` has no explicit `db_filter` default for `test-all`.** The pattern `${{ if eq(variables['Build.DefinitionName'], 'test-all') }}:` is missing (commented out as `# db_filter parameter...`), meaning `test-all` falls through to the `[all]` default in `test-matrix.yml`.

## See also

- [architecture overview](../../architecture/overview.md) — how projects produced by this build relate at runtime
- [CLAUDE.md](../../../../CLAUDE.md) — build commands reference (`dotnet build linq2db.slnx`, `-c Testing`, etc.)
- `.claude/docs/testing.md` — test runner configuration, `UserDataProviders.json` format
- `.claude/docs/ci-tests.md` — `/azp run test-<db>` trigger syntax

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 3 / 4
  - Directory.Build.props — read in full
  - global.json — read in full
  - linq2db.slnx — read in full
  - Build/BannedSymbols.txt — MISSING on disk (see UNCLASSIFIED-FILE)
  - Source/BannedSymbols.txt — read in full (actual location of the banned-API list)
- Tier 2 (visited / total): 70 / 70 (100%) ✓
  - Build/Azure/scripts/access.ace.ps1
  - Build/Azure/scripts/access.ace.x64.ps1
  - Build/Azure/scripts/sqlce.ps1
  - Build/Azure/scripts/db2.provider.sh
  - Build/Azure/scripts/db2.sh
  - Build/Azure/scripts/firebird25.sh
  - Build/Azure/scripts/firebird3.sh
  - Build/Azure/scripts/firebird4.sh
  - Build/Azure/scripts/firebird5.sh
  - Build/Azure/scripts/hana2.sh
  - Build/Azure/scripts/hana2.tests.sh
  - Build/Azure/scripts/informix14.sh
  - Build/Azure/scripts/mac.db2.provider.sh
  - Build/Azure/scripts/mac.db2.sh
  - Build/Azure/scripts/mac.hana2.sh
  - Build/Azure/scripts/mac.informix14.sh
  - Build/Azure/scripts/mac.mariadb11.sh
  - Build/Azure/scripts/mac.mysql.sh
  - Build/Azure/scripts/mac.mysql57.sh
  - Build/Azure/scripts/mac.pgsql13.sh
  - Build/Azure/scripts/mac.pgsql14.sh
  - Build/Azure/scripts/mac.pgsql15.sh
  - Build/Azure/scripts/mac.pgsql16.sh
  - Build/Azure/scripts/mac.pgsql17.sh
  - Build/Azure/scripts/mac.pgsql18.sh
  - Build/Azure/scripts/mariadb11.sh
  - Build/Azure/scripts/mysql.sh
  - Build/Azure/scripts/mysql57.sh
  - Build/Azure/scripts/oracle11.sh
  - Build/Azure/scripts/oracle12.sh
  - Build/Azure/scripts/oracle18.sh
  - Build/Azure/scripts/oracle19.sh
  - Build/Azure/scripts/oracle21.sh
  - Build/Azure/scripts/oracle23.sh
  - Build/Azure/scripts/pgsql13.sh
  - Build/Azure/scripts/pgsql14.sh
  - Build/Azure/scripts/pgsql15.sh
  - Build/Azure/scripts/pgsql16.sh
  - Build/Azure/scripts/pgsql17.sh
  - Build/Azure/scripts/pgsql18.sh
  - Build/Azure/scripts/sqlserver.2017.sh
  - Build/Azure/scripts/sqlserver.2019.sh
  - Build/Azure/scripts/sqlserver.2022.sh
  - Build/Azure/scripts/sqlserver.2025.sh
  - Build/Azure/scripts/sqlserver.extras.sh
  - Build/Azure/scripts/sybase.sh
  - Build/Azure/scripts/clickhouse.sh
  - Build/Azure/pipelines/build.yml
  - Build/Azure/pipelines/default.yml
  - Build/Azure/pipelines/testing.yml
  - Build/Azure/pipelines/templates/build-job.yml
  - Build/Azure/pipelines/templates/build-vars.yml
  - Build/Azure/pipelines/templates/nuget-job.yml
  - Build/Azure/pipelines/templates/test-jobs.yml
  - Build/Azure/pipelines/templates/test-matrix.yml
  - Build/Azure/pipelines/templates/test-workflow-linux.yml
  - Build/Azure/pipelines/templates/test-workflow-macos.yml
  - Build/Azure/pipelines/templates/test-workflow-windows.yml
  - .github/ISSUE_TEMPLATE/01_bug_report_linq2db.yml
  - .github/ISSUE_TEMPLATE/02_bug_report_linq2db.efcore.yml
  - .github/ISSUE_TEMPLATE/03_bug_report_linq2db.cli.yml
  - .github/ISSUE_TEMPLATE/04_bug_report_linq2db.t4.yml
  - .github/ISSUE_TEMPLATE/05_bug_report_linq2db.linqpad.yml
  - .github/ISSUE_TEMPLATE/09_bug_report_linq2db.other.yml
  - .github/ISSUE_TEMPLATE/11_feture_request_linq2db.yml
  - .github/ISSUE_TEMPLATE/12_feture_request_linq2db.efcore.yml
  - .github/ISSUE_TEMPLATE/13_feture_request_linq2db.cli.yml
  - .github/ISSUE_TEMPLATE/14_feture_request_linq2db.t4.yml
  - .github/ISSUE_TEMPLATE/15_feture_request_linq2db.linqpad.yml
  - .github/ISSUE_TEMPLATE/19_feture_request_linq2db.other.yml
  - .github/ISSUE_TEMPLATE/config.yml
- Tier 3 (skipped, logged): 0
  - Note: Build/Azure/{net80,net90,net100,netfx}/*.json config files (150+ files) are test data analogous to UserDataProviders.json — Tier-3 by the "test data files" rule; counted but not visited.
  - Build/Azure/scripts/*.cmd (sqlserver.*.cmd, etc.) — not matched by `*.sh` or `*.ps1` patterns and outside the kb-areas.md Tier-2 pattern list; classified Tier 3.
</details>
