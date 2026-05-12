---
area: TESTS-T4
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 0/0
coverage_tier_2: 45/2318
---

# TESTS-T4

Scaffolder output validation. Two projects exercise different axes:

- `Tests/Tests.T4/` -- compile-time plus T4-regeneration tests. `.tt` templates run the CLI scaffolder (or T4 engine) against live databases and write committed baseline `.cs` files; the test suite validates that re-running produces identical output.
- `Tests/Tests.T4.Nugets/` -- NuGet bundle smoke test. 16 per-provider `.tt` templates include from the `linq2db.t4models` NuGet, run against live databases, emit `.generated.cs` files, and 17 per-provider `.csproj` files compile those outputs against the provider NuGet packages.

Neither project contains NUnit fixtures. The baseline-comparison loop is **not a test runner assertion** -- it is a T4-script-driven regeneration that overwrites the committed files in-place; CI detects regressions via `git diff`.

## Two test projects

### Tests/Tests.T4 (`Tests.T4.csproj`)

Multi-TFM project (WPF items are `net462`-only, all other items compile on every TFM). The project references `LinqToDB.Extensions`, `Tests.Base`, and NUnit packages, but does not expose NUnit test fixtures of its own.

Key top-level T4 driver files (`Tests/Tests.T4/Cli/`):
- `All.tt` / `Default.tt` / `Fluent.tt` / `NoMetadata.tt` / `T4.tt` / `NewCliFeatures.tt` -- each invokes `CLI.ttinclude:RunCliTool()` once per provider, passing `targetDir`, mode (`"default"` or `"t4"`), and a set of `extraOptions` flags. `RunCliTool` shells out to `dotnet linq2db scaffold ...` with a 60-second timeout.
- `CLI.ttinclude` -- contains the `RunCliTool` helper. Reads connection strings from `ConnectionStrings.ttinclude`. **PR #5451 added `duckdbCN` connection string** alongside SQLite, SQLCE, Access.

`Tests/Tests.T4/Default/` holds per-provider `.tt`/`.generated.cs` pairs using the legacy T4-template code path. `Tests/Tests.T4/Databases/` holds per-provider T4 templates with specific non-default options. `Tests/Tests.T4/Models/` has hand-coded partial classes plus `.tt` files that test `T4Model.ttinclude` features.

`Tests/Tests.T4/Compat/Stubs.cs` is a `#if !NET8_0_OR_GREATER` stub providing `System.Net.IPNetwork`, `System.DateOnly`, and `System.TimeOnly` as empty `readonly struct` declarations. **As of PR #5451, this file was consolidated**: the previously separate `IPNetwork.cs` stub was removed and its content folded into `Stubs.cs` alongside `DateOnly`/`TimeOnly`.

### Tests/Tests.T4.Nugets

Standalone solution under `Tests/Tests.T4.Nugets/`. Targets `net10.0` only. Uses central package management with `Version=6.2.0-local.1` pointing to locally built NuGet packages. No runtime test framework dependency -- purely a compile check.

- `Templates/<Provider>.tt` -- 16 templates. Each includes from `$(LinqToDBT4<Provider>TemplatesPath)`.
- `Templates/<Provider>.generated.cs` -- 16 committed output baselines.
- `Projects/<Provider>.csproj` -- 16 single-template projects.
- `Projects/t4models.csproj` -- aggregate project referencing all 16 outputs.

## CLI mode taxonomy

| Mode subdir | `--metadata` flag | Notable extra options | Provider count |
|---|---|---|---|
| `All/` | attributes (default) | Every optional column; equatable; all find-methods | ~25 dirs |
| `Default/` | (default) | None (out-of-the-box) | ~22 dirs |
| `Fluent/` | `fluent` | `--add-association-extensions true` | ~22 dirs |
| `NoMetadata/` | `none` | `--add-association-extensions true`, `--add-init-context false` | ~22 dirs |
| `T4/` | n/a (mode = `"t4"`) | None -- runs CLI's T4-template code path | ~22 dirs |
| `NewCliFeatures/` | fluent + attr variants | `--context-modifier internal`, `--customize scaffold.tt` | SQLite only |

`All/` covers the largest provider set: AccessOdbc, AccessOleDb, AccessBoth, DB2, **DuckDB**, Firebird, Informix, MariaDB, MySql, Oracle, PostgreSQL, SapHana, SqlCe, SQLite, SQLiteNorthwind, SqlServer, SqlServer2025, SqlServerNorthwind, Sybase, ClickHouse.MySql, ClickHouse.Driver, ClickHouse.Octonica, Azure, AzureMI (24 dirs). **`Default/`, `Fluent/`, `NoMetadata/`, and `T4/` now cover 22 dirs each (DuckDB added; Azure/AzureMI are `All/`-only).**

Each provider directory contains ~10--45 `.cs` files. **DuckDB baseline dirs contain 20 files each**: 18 entity classes (`AllScaffoldType`, `AllType`, `Child`, `CollatedTable`, `Doctor`, `GrandChild`, `InheritanceChild`, `InheritanceParent`, `LinqDataType`, `Parent`, `Patient`, `Person`, `SequenceTest1--3`, `TestIdentity`, `TestMerge1--2`, `TestMergeIdentity`) plus `TestDataDB.cs`. DuckDB type mappings (Default mode): `BIGINT` -> `long?`, `DECIMAL` -> `decimal?`, `TIMESTAMP` -> `DateTime?`, `TIMESTAMP WITH TIME ZONE` -> `DateTimeOffset?`, `DATE` -> `DateOnly?`, `TIME` -> `TimeOnly?`, `INTERVAL` -> `TimeSpan?`, `UUID` -> `Guid?`, `BLOB` -> `byte[]?`, `JSON` -> `string?`.

`CLI.ttinclude` defines `duckdbCN` as `$"Data Source={databasesPath}TestData.duckdb"`. The `All.tt` template invokes DuckDB without `extraOptions` (no `--prefer-provider-types` or full options set).

## Tests.T4.Nugets flow

```
Templates/<Provider>.tt
  -> T4 engine
  -> Templates/<Provider>.generated.cs  (committed baseline)
  -> Projects/<Provider>.csproj compiles against linq2db.<Provider> NuGet
  -> build success = template still functional
```

## Provider matrix

| Area | Providers covered |
|---|---|
| `Cli/All/` | 24 dirs (including Azure/AzureMI, ClickHouse x3, **DuckDB**) |
| `Cli/Default/` `Cli/Fluent/` `Cli/NoMetadata/` `Cli/T4/` | 22 dirs each (**DuckDB added**) |
| `Cli/NewCliFeatures/` | 2 dirs (SQLite, SQLite.Fluent) |
| `Default/` | ~17 per-provider `.tt`/`.generated.cs` pairs |
| `Databases/` | ~18 per-provider `.tt`/`.generated.cs` pairs |
| `Tests.T4.Nugets/Templates/` | 16 providers |

## Files (Tier 1 / Tier 2)

No Tier-1 files designated. The ~2318 total files break down as (PR #5451 added ~100 DuckDB baseline files):

| Category | Approximate count |
|---|---|
| `Cli/<mode>/<provider>/*.cs` -- generated baseline outputs | ~2150 |
| `Cli/*.tt` + `Cli/*.generated.cs` + `CLI.ttinclude` + `scaffold.tt` | ~15 |
| `Default/*.tt` + `Default/*.generated.cs` | ~34 |
| `Databases/*.tt` + `Databases/*.generated.cs` | ~40 |
| `Models/*.tt` + `Models/*.cs` + `Models/*.generated.cs` | ~11 |
| `WPF/*.tt` + `WPF/*.cs` + `WPF/*.generated.cs` | ~3 |
| `Tests.T4.csproj`, `Compat/*.cs`, `Shared.ttinclude`, `Unlock.tt` | ~5 |
| `Tests.T4.Nugets/Templates/*.tt` + `*.generated.cs` | ~32 |
| `Tests.T4.Nugets/Projects/*.csproj` | ~17 |

## Inbound / outbound dependencies

**Outbound:**
- `Source/LinqToDB.Templates/` (T4-TEMPLATES area) -- `Default/`, `Databases/`, `Models/`, `WPF/`, `Tests.T4.Nugets/Templates/` all include from `$(LinqToDBT4TemplatesPath)`.
- `Source/LinqToDB.CLI/` (CLI area) -- `Cli/<mode>/<provider>/` baselines are the expected output of `dotnet linq2db scaffold`.
- `Source/LinqToDB.Scaffold/Scaffold/` (SCAFFOLD area) -- `ScaffoldInterceptors`, `ScaffoldOptions`, `Scaffolder`.
- Every PROV-* area indirectly. **DuckDB provider addition (PR #5451) triggered all five CLI-mode baseline sets.**

**Inbound:** No other test project imports from this area.

## Known issues / debt

- No NUnit test fixtures inside the area -- regressions detected only through CI `git diff`.
- `NewCliFeatures/` covers only SQLite.
- `Tests.T4.Nugets/` is a separate isolated solution pinned to `6.2.0-local.1` -- must be rebuilt against locally packed NuGet artifacts.
- `Cli/CLI.ttinclude:RunCliTool` has a hard-coded 60-second timeout and silently ignores non-zero exit codes (error-reporting lines commented out at `CLI.ttinclude:69,83`).
- **DuckDB CLI runs in `All.tt` do not pass the full `extraOptions` set** (no `--prefer-provider-types`, no equatable/find-methods flags). Appears intentional but means DuckDB `All/` output differs structurally from other providers' `All/` baseline.

## See also

- `areas/CLI/INDEX.md`
- `areas/T4-TEMPLATES/INDEX.md`
- `areas/SCAFFOLD/INDEX.md`
- `architecture/overview.md`

<details><summary>Coverage</summary>

- Tier 1: 0/0 (no Tier-1 files designated)
- Tier 2: 45/2318 (1.9%) -- ~2273 deferred files are near-identical generated baselines
- Tier 3: 0

**Read (this delta run, SHA 4a478ff14):**
- `Tests/Tests.T4/Cli/CLI.ttinclude` -- confirmed `duckdbCN` addition
- `Tests/Tests.T4/Cli/All.tt` -- confirmed `RunCliTool("DuckDB", ...)` at line 37
- `Tests/Tests.T4/Cli/Default/DuckDB/AllType.cs` -- DuckDB type mapping baseline (26 columns, 25 DuckDB-native types)
- `Tests/Tests.T4/Cli/Default/DuckDB/TestDataDB.cs` -- 19-table context + `ExtensionMethods` find-helpers
- `Tests/Tests.T4/Compat/Stubs.cs` -- confirmed consolidation: `IPNetwork`, `DateOnly`, `TimeOnly` all in one `#if !NET8_0_OR_GREATER` block; `IPNetwork.cs` removed

**Skipped (cross-cutting, near-identical):**
- ~100 DuckDB .cs files across All/, Fluent/, NoMetadata/, T4/ variants
- All other ~2218 prior-deferred baseline files

</details>
