---
area: TESTS-T4
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 0/0
coverage_tier_2: 51/2318
---

# TESTS-T4

Scaffolder output validation. Two projects exercise different axes:

- `Tests/Tests.T4/` -- compile-time plus T4-regeneration tests. `.tt` templates run the CLI scaffolder (or T4 engine) against live databases and write committed baseline `.cs` files; the test suite validates that re-running produces identical output.
- `Tests/Tests.T4.Nugets/` -- NuGet bundle smoke test. 16 per-provider `.tt` templates include from the `linq2db.t4models` NuGet, run against live databases, emit `.generated.cs` files, and 17 per-provider `.csproj` files compile those outputs against the provider NuGet packages.

Neither project contains NUnit fixtures. The baseline-comparison loop is **not a test runner assertion** -- it is a T4-script-driven regeneration that overwrites the committed files in-place; CI detects regressions via `git diff`.

## Two test projects

### Tests/Tests.T4 (`Tests.T4.csproj`)

Multi-TFM project (WPF items are `net462`-only, all other items compile on every TFM). References `LinqToDB.Extensions`, `Tests.Base`, and NUnit packages, but does not expose NUnit test fixtures of its own.

Key top-level T4 driver files (`Tests/Tests.T4/Cli/`):
- `All.tt` / `Default.tt` / `Fluent.tt` / `NoMetadata.tt` / `T4.tt` / `NewCliFeatures.tt` -- each invokes `CLI.ttinclude:RunCliTool()` once per provider, passing `targetDir`, mode (`"default"` or `"t4"`), and a set of `extraOptions` flags. `RunCliTool` shells out to `dotnet linq2db scaffold ...` with a 60-second timeout.
- `CLI.ttinclude` -- contains the `RunCliTool` helper. Reads connection strings from `ConnectionStrings.ttinclude`. **PR #5451 added `duckdbCN` connection string** alongside SQLite, SQLCE, Access.

`Tests/Tests.T4/Default/` holds per-provider `.tt`/`.generated.cs` pairs using the legacy T4-template code path. `Tests/Tests.T4/Databases/` holds per-provider T4 templates with specific non-default options. `Tests/Tests.T4/Models/` has hand-coded partial classes plus `.tt` files that test `T4Model.ttinclude` features.

`Tests/Tests.T4/Compat/Stubs.cs` is a `#if !NET8_0_OR_GREATER` stub providing `System.Net.IPNetwork`, `System.DateOnly`, and `System.TimeOnly` as empty `readonly struct` declarations. **As of PR #5451, this file was consolidated**: the previously separate `IPNetwork.cs` stub was removed and folded into `Stubs.cs`.

### Tests/Tests.T4.Nugets

Standalone solution under `Tests/Tests.T4.Nugets/`. Targets `net10.0` only. Uses central package management via `Directory.Packages.props`. **As of this delta, the pinned version is `6.3.0-local.2`** (previously `6.2.0-local.1`). No runtime test framework dependency -- purely a compile check.

- `Templates/<Provider>.tt` -- 16 templates. Each includes from `$(LinqToDBT4<Provider>TemplatesPath)`.
- `Templates/<Provider>.generated.cs` -- 16 committed output baselines.
- `Projects/<Provider>.csproj` -- 16 single-template projects.
- `Projects/t4models.csproj` -- aggregate project referencing all 16 outputs.

**`Templates/Informix.tt` now sets `GenerateSchemaAsType = true`** in addition to `LoadInformixMetadata(...)` and `GenerateModel()`. The analogous `Default/Informix.tt` and `Databases/Informix.tt` inside `Tests.T4/` do not set this flag, so the Nugets-project baseline includes schema-typed output the main T4 project's Informix baselines do not.

## CLI mode taxonomy

| Mode subdir | `--metadata` flag | Notable extra options | Provider count |
|---|---|---|---|
| `All/` | attributes (default) | Every optional column; equatable; all find-methods | ~25 dirs |
| `Default/` | (default) | None (out-of-the-box) | ~22 dirs |
| `Fluent/` | `fluent` | `--add-association-extensions true` | ~22 dirs |
| `NoMetadata/` | `none` | `--add-association-extensions true`, `--add-init-context false` | ~22 dirs |
| `T4/` | n/a (mode = `"t4"`) | None -- runs CLI's T4-template code path | ~22 dirs |
| `NewCliFeatures/` | fluent + attr variants | `--context-modifier internal`, `--customize scaffold.tt` | SQLite only |

`All/` covers the largest provider set including Azure/AzureMI, ClickHouse x3, and **DuckDB** (24 dirs). **`Default/`, `Fluent/`, `NoMetadata/`, and `T4/` now cover 22 dirs each (DuckDB added; Azure/AzureMI are `All/`-only).**

Each provider directory contains ~10--45 `.cs` files. **DuckDB baseline dirs contain 20 files each** (All, Default, Fluent, NoMetadata modes): 18 entity classes + `TestDataDB.cs` + one more. **`T4/DuckDB/` contains only `TestDataDB.cs`** -- the T4 code path emits a single context file rather than per-entity files for DuckDB. DuckDB type mappings (Default mode): `BIGINT`->`long?`, `DECIMAL`->`decimal?`, `TIMESTAMP`->`DateTime?`, `TIMESTAMP WITH TIME ZONE`->`DateTimeOffset?`, `DATE`->`DateOnly?`, `TIME`->`TimeOnly?`, `INTERVAL`->`TimeSpan?`, `UUID`->`Guid?`, `BLOB`->`byte[]?`, `JSON`->`string?`.

**All/ mode DuckDB output** additionally carries `IEquatable<AllType>` and full `DataType`/`DbType`/`Precision`/`Scale` column annotations consistent with `All.tt`'s `--equatable-entities true`, `--include-datatype true`, etc.

`CLI.ttinclude` defines `duckdbCN` as `$"Data Source={databasesPath}TestData.duckdb"`. The `All.tt` template invokes DuckDB without the `extraOptionsWithoutTypes` carve-out (unlike MariaDB/MySql which strip `--prefer-provider-types`).

## Files (Tier 1 / Tier 2)

No Tier-1 files designated. The ~2318 total files break down approximately as: `Cli/<mode>/<provider>/*.cs` generated baselines (~2150), `Cli/*.tt`+`.generated.cs`+`CLI.ttinclude`+`scaffold.tt` (~15), `Default/*` (~34), `Databases/*` (~40), `Models/*` (~11), `WPF/*` (~3), project/compat/ttinclude (~5), `Tests.T4.Nugets/Templates/*` (~32), `Tests.T4.Nugets/Projects/*.csproj` (~17).

## Inbound / outbound dependencies

**Outbound:**
- `Source/LinqToDB.Templates/` (T4-TEMPLATES) -- `Default/`, `Databases/`, `Models/`, `WPF/`, `Tests.T4.Nugets/Templates/` all include from `$(LinqToDBT4TemplatesPath)`.
- `Source/LinqToDB.CLI/` (CLI) -- `Cli/<mode>/<provider>/` baselines are the expected output of `dotnet linq2db scaffold`.
- `Source/LinqToDB.Scaffold/Scaffold/` (SCAFFOLD) -- `ScaffoldInterceptors`, `ScaffoldOptions`, `Scaffolder`.
- Every PROV-* indirectly. **DuckDB provider addition (PR #5451) triggered all five CLI-mode baseline sets.**

**Inbound:** No other test project imports from this area.

## Known issues / debt

- No NUnit test fixtures inside the area -- regressions detected only through CI `git diff`.
- `NewCliFeatures/` covers only SQLite.
- `Tests.T4.Nugets/` is a separate isolated solution pinned to a locally built NuGet version (`6.3.0-local.2`); must be rebuilt against locally packed NuGet artifacts each time the version advances.
- `Cli/CLI.ttinclude:RunCliTool` has a hard-coded 60-second timeout and silently ignores non-zero exit codes (error-reporting lines commented out at `CLI.ttinclude:69,83`).
- **DuckDB CLI runs in `All.tt` do not use the `extraOptionsWithoutTypes` carve-out** (no type-name conflict for DuckDB).
- **`T4/DuckDB/` contains only `TestDataDB.cs`** -- whether intentional (T4 code path doesn't scaffold per-entity files for DuckDB) or a partial regen needs investigation.
- `Tests.T4.Nugets/Templates/Informix.tt` sets `GenerateSchemaAsType = true`; the main `Tests.T4/Default/Informix.tt` and `Databases/Informix.tt` do not -- an intentional divergence or an oversight.

## See also

- `areas/CLI/INDEX.md`
- `areas/T4-TEMPLATES/INDEX.md`
- `areas/SCAFFOLD/INDEX.md`
- `architecture/overview.md`

<details><summary>Coverage</summary>

- Tier 1: 0/0 (no Tier-1 files designated)
- Tier 2: 51/2318 (2.2%) -- ~2267 deferred files are near-identical generated baselines
- Tier 3: 0

**Read (prior run -- SHA 4a478ff14):**
- `Tests/Tests.T4/Cli/CLI.ttinclude` -- `duckdbCN` addition
- `Tests/Tests.T4/Cli/All.tt` -- `RunCliTool("DuckDB", ...)` at line 37
- `Tests/Tests.T4/Cli/Default/DuckDB/AllType.cs` -- DuckDB type mapping baseline
- `Tests/Tests.T4/Cli/Default/DuckDB/TestDataDB.cs` -- 19-table context + find-helpers
- `Tests/Tests.T4/Compat/Stubs.cs` -- confirmed consolidation; `IPNetwork.cs` removed

**Read (this run -- delta, SHA 2e67bafc9):**
- `Tests/Tests.T4.Nugets/Directory.Packages.props` -- version bumped `6.2.0-local.1` -> `6.3.0-local.2`; provider package refs unchanged
- `Tests/Tests.T4.Nugets/Templates/Informix.tt` -- added `GenerateSchemaAsType = true`; otherwise identical structure
- `Tests/Tests.T4/Databases/Informix.tt` -- cross-checked: does NOT set `GenerateSchemaAsType`; `InformixDataContext` namespace
- `Tests/Tests.T4/Default/Informix.tt` -- cross-checked: does NOT set `GenerateSchemaAsType`; `Default.Informix` namespace, `DataContextName = "TestDataDB"`
- `Tests/Tests.T4/Cli/All.tt` -- DuckDB line 37 unchanged; full `extraOptions` (not `extraOptionsWithoutTypes`) passed for DuckDB
- `Tests/Tests.T4/Cli/{All,Default,Fluent,NoMetadata}/DuckDB/` -- 20 files each confirmed; `All/DuckDB/AllType.cs` sampled (`IEquatable<AllType>`, full type annotations)
- `Tests/Tests.T4/Cli/T4/DuckDB/` -- 1 file only (`TestDataDB.cs`); T4 mode emits context-only baseline for DuckDB

</details>
