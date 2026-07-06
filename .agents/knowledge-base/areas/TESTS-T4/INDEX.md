---
area: TESTS-T4
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 0/0
coverage_tier_2: 59/2375
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
- **This delta** adds a `RunCliTool("Ydb", "YDB", ...)` call to all five of `All.tt` / `Default.tt` / `Fluent.tt` / `NoMetadata.tt` / `T4.tt` (not `NewCliFeatures.tt`, which stays SQLite-only). Unlike DuckDB's dedicated `duckdbCN` variable, YDB's second positional arg passes the connection-string key `"YDB"` directly, resolved from `ConnectionStrings.ttinclude` (not touched by this delta). `All.tt` passes YDB the full `extraOptions` -- no `extraOptionsWithoutTypes` carve-out needed, same as DuckDB (no type-name conflict).

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
| `All/` | attributes (default) | Every optional column; equatable; all find-methods | 25 dirs |
| `Default/` | (default) | None (out-of-the-box) | 23 dirs |
| `Fluent/` | `fluent` | `--add-association-extensions true` | 23 dirs |
| `NoMetadata/` | `none` | `--add-association-extensions true`, `--add-init-context false` | 23 dirs |
| `T4/` | n/a (mode = `"t4"`) | None -- runs CLI's T4-template code path | 23 dirs |
| `NewCliFeatures/` | fluent + attr variants | `--context-modifier internal`, `--customize scaffold.tt` | SQLite only |

`All/` covers the largest provider set including Azure/AzureMI, ClickHouse x3, DuckDB, and (this delta) **YDB** (25 dirs). **`Default/`, `Fluent/`, `NoMetadata/`, and `T4/` now cover 23 dirs each (YDB added this delta, joining DuckDB; Azure/AzureMI are `All/`-only).**

Each provider directory contains ~10--45 `.cs` files. **DuckDB baseline dirs contain 20 files each** (All, Default, Fluent, NoMetadata modes): 18 entity classes + `TestDataDB.cs` + one more. **`T4/DuckDB/` contains only `TestDataDB.cs`** (1 file) -- the T4 code path emits a single all-in-one file per provider rather than per-entity files; confirmed (via YDB's identical `T4/Ydb/TestDataDB.cs`, this delta) that the single file holds the context class, every entity `partial class`, and a shared `ExtensionMethods` class together -- not context-only, resolving the earlier open question. DuckDB type mappings (Default mode): `BIGINT`->`long?`, `DECIMAL`->`decimal?`, `TIMESTAMP`->`DateTime?`, `TIMESTAMP WITH TIME ZONE`->`DateTimeOffset?`, `DATE`->`DateOnly?`, `TIME`->`TimeOnly?`, `INTERVAL`->`TimeSpan?`, `UUID`->`Guid?`, `BLOB`->`byte[]?`, `JSON`->`string?`.

**YDB baseline dirs contain 14 files each** (All, Default, Fluent, NoMetadata modes): 13 entity classes (`AllType`, `Child`, `CollatedTable`, `Doctor`, `GrandChild`, `InheritanceChild`, `InheritanceParent`, `LinqDataType`, `Parent`, `Patient`, `Person`, `TestMerge1`, `TestMerge2`) + `TestDataDB.cs` context. YDB type mappings observed (`Default/Ydb/AllType.cs`, `T4/Ydb/TestDataDB.cs`): `Int32`->`int`/`int?`, `Int16`->`short?`, `Float`->`float?`, `Double`->`double?`, `Text`->`string?`, `Bool`->`bool?`, `Decimal(p,s)`->`decimal?`, `Timestamp`->`DateTime?`, `Date`->`DateTime?` (no `DateOnly?`, unlike DuckDB), `Uuid`->`Guid?`, `Bytes`->`byte[]?`, `Int64`->`long?`. Notably `LinqDataType.FieldTime` scaffolds as `long?` commented `// Int64`, not `TimeSpan?` -- no dedicated interval/time .NET mapping surfaced in this sample.

**All/ mode DuckDB output** additionally carries `IEquatable<AllType>` and full `DataType`/`DbType`/`Precision`/`Scale` column annotations consistent with `All.tt`'s `--equatable-entities true`, `--include-datatype true`, etc.

**All/ mode YDB output** likewise carries `IEquatable<AllType>` plus `DataType`/`DbType` column annotations (e.g. `DataType = DataType.NVarChar, DbType = "Text"` for YDB `Text` columns) -- same shape as DuckDB's `All/` output, minus `Precision`/`Scale` on the sampled `AllType` (no decimal/numeric columns in that table).

`CLI.ttinclude` defines `duckdbCN` as `$"Data Source={databasesPath}TestData.duckdb"`. The `All.tt` template invokes DuckDB without the `extraOptionsWithoutTypes` carve-out (unlike MariaDB/MySql which strip `--prefer-provider-types`).

## Files (Tier 1 / Tier 2)

No Tier-1 files designated. The ~2375 total files (was ~2318; **+57 from this delta's YDB baseline set**) break down approximately as: `Cli/<mode>/<provider>/*.cs` generated baselines (~2207), `Cli/*.tt`+`.generated.cs`+`CLI.ttinclude`+`scaffold.tt` (~15), `Default/*` (~34), `Databases/*` (~40), `Models/*` (~11), `WPF/*` (~3), project/compat/ttinclude (~5), `Tests.T4.Nugets/Templates/*` (~32), `Tests.T4.Nugets/Projects/*.csproj` (~17).

## Inbound / outbound dependencies

**Outbound:**
- `Source/LinqToDB.Templates/` (T4-TEMPLATES) -- `Default/`, `Databases/`, `Models/`, `WPF/`, `Tests.T4.Nugets/Templates/` all include from `$(LinqToDBT4TemplatesPath)`.
- `Source/LinqToDB.CLI/` (CLI) -- `Cli/<mode>/<provider>/` baselines are the expected output of `dotnet linq2db scaffold`.
- `Source/LinqToDB.Scaffold/Scaffold/` (SCAFFOLD) -- `ScaffoldInterceptors`, `ScaffoldOptions`, `Scaffolder`.
- Every PROV-* indirectly. **DuckDB provider addition (PR #5451) triggered all five CLI-mode baseline sets; this delta's YDB provider addition triggered the same five (`All/`, `Default/`, `Fluent/`, `NoMetadata/`, `T4/`).**

**Inbound:** No other test project imports from this area.

## Known issues / debt

- No NUnit test fixtures inside the area -- regressions detected only through CI `git diff`.
- `NewCliFeatures/` covers only SQLite.
- `Tests.T4.Nugets/` is a separate isolated solution pinned to a locally built NuGet version (`6.3.0-local.2`); must be rebuilt against locally packed NuGet artifacts each time the version advances.
- `Cli/CLI.ttinclude:RunCliTool` has a hard-coded 60-second timeout and silently ignores non-zero exit codes (error-reporting lines commented out at `CLI.ttinclude:69,83`).
- **DuckDB and (this delta) YDB CLI runs in `All.tt` do not use the `extraOptionsWithoutTypes` carve-out** (no type-name conflict for either provider).
- **Resolved (this delta):** `T4/DuckDB/` and `T4/Ydb/` both contain only a single `TestDataDB.cs`. Confirmed via a direct read of `T4/Ydb/TestDataDB.cs` that the file holds the context class, every entity `partial class`, and a shared `ExtensionMethods` class together -- the T4 code path's normal all-in-one-file output (legacy pre-CLI T4-template layout), not a partial regen. The earlier "needs investigation" framing is closed.
- **Open question (this delta):** `PROV-YDB`'s `INDEX.md` records `YdbDataProvider.GetSchemaProvider()` as not implemented, yet this delta's new `Cli/*/Ydb/` baselines imply `dotnet linq2db scaffold --provider Ydb` successfully retrieved live schema across all 5 CLI modes. Not resolved here -- would need a `PROV-YDB` source read, out of this area's scope. See companion `AUDIT-NOTE`.
- `Tests.T4.Nugets/Templates/Informix.tt` sets `GenerateSchemaAsType = true`; the main `Tests.T4/Default/Informix.tt` and `Databases/Informix.tt` do not -- an intentional divergence or an oversight.

## See also

- `areas/CLI/INDEX.md`
- `areas/T4-TEMPLATES/INDEX.md`
- `areas/SCAFFOLD/INDEX.md`
- `areas/PROV-YDB/INDEX.md`
- `architecture/overview.md`

<details><summary>Coverage</summary>

- Tier 1: 0/0 (no Tier-1 files designated)
- Tier 2: 59/2375 (2.5%) -- ~2316 deferred files are near-identical generated baselines
- Tier 3: 0

**Read (prior run -- SHA 4a478ff14):**
- `Tests/Tests.T4/Cli/CLI.ttinclude` -- `duckdbCN` addition
- `Tests/Tests.T4/Cli/All.tt` -- `RunCliTool("DuckDB", ...)` at line 37
- `Tests/Tests.T4/Cli/Default/DuckDB/AllType.cs` -- DuckDB type mapping baseline
- `Tests/Tests.T4/Cli/Default/DuckDB/TestDataDB.cs` -- 19-table context + find-helpers
- `Tests/Tests.T4/Compat/Stubs.cs` -- confirmed consolidation; `IPNetwork.cs` removed

**Read (prior run -- delta, SHA 2e67bafc9):**
- `Tests/Tests.T4.Nugets/Directory.Packages.props` -- version bumped `6.2.0-local.1` -> `6.3.0-local.2`; provider package refs unchanged
- `Tests/Tests.T4.Nugets/Templates/Informix.tt` -- added `GenerateSchemaAsType = true`; otherwise identical structure
- `Tests/Tests.T4/Databases/Informix.tt` -- cross-checked: does NOT set `GenerateSchemaAsType`; `InformixDataContext` namespace
- `Tests/Tests.T4/Default/Informix.tt` -- cross-checked: does NOT set `GenerateSchemaAsType`; `Default.Informix` namespace, `DataContextName = "TestDataDB"`
- `Tests/Tests.T4/Cli/All.tt` -- DuckDB line 37 unchanged; full `extraOptions` (not `extraOptionsWithoutTypes`) passed for DuckDB
- `Tests/Tests.T4/Cli/{All,Default,Fluent,NoMetadata}/DuckDB/` -- 20 files each confirmed; `All/DuckDB/AllType.cs` sampled (`IEquatable<AllType>`, full type annotations)
- `Tests/Tests.T4/Cli/T4/DuckDB/` -- 1 file only (`TestDataDB.cs`); T4 mode emits context-only baseline for DuckDB

**Read (this run -- delta, SHA 36ee4f82f):**
- `Tests/Tests.T4/Cli/All.tt` -- confirmed new `RunCliTool("Ydb", "YDB", ...)` line (25th, last provider before Azure/AzureMI); full `extraOptions`, no type-conflict carve-out
- `Tests/Tests.T4/Cli/Default.tt` -- new `RunCliTool("Ydb", "YDB", ...)` line, no extraOptions (out-of-the-box mode)
- `Tests/Tests.T4/Cli/Fluent.tt` -- new `RunCliTool("Ydb", "YDB", ...)` line, `--metadata fluent` extraOptions
- `Tests/Tests.T4/Cli/NoMetadata.tt` -- new `RunCliTool("Ydb", "YDB", ...)` line, `--metadata none --add-init-context false` extraOptions
- `Tests/Tests.T4/Cli/T4.tt` -- new `RunCliTool("Ydb", "YDB", ...)` line, mode = `"t4"`
- `Tests/Tests.T4/Cli/{All,Default,Fluent,NoMetadata}/Ydb/*.cs` (56 files, enumerated via Glob, not individually read) -- new baseline set, 14 files/dir (13 entities + `TestDataDB.cs`)
- `Tests/Tests.T4/Cli/All/Ydb/AllType.cs` -- sampled: `IEquatable<AllType>`, `DataType`/`DbType` column annotations
- `Tests/Tests.T4/Cli/Default/Ydb/AllType.cs` -- sampled: plain `[Column(...)]`, no type annotations
- `Tests/Tests.T4/Cli/All/Ydb/TestDataDB.cs` -- sampled: per-entity `Find`/`FindAsync`/`FindQuery` extension methods, 13-table context, same shape as other `All/` providers
- `Tests/Tests.T4/Cli/T4/Ydb/TestDataDB.cs` -- sampled: single-file T4-legacy layout (context + all 13 entities + shared `ExtensionMethods`); resolves the DuckDB "context-only vs partial regen" open question

</details>
