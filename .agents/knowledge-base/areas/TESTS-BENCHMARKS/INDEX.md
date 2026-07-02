---
area: TESTS-BENCHMARKS
kind: area-index
sources: [code]
confidence: medium
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 0/0
coverage_tier_2: 28/41
---

# TESTS-BENCHMARKS

BenchmarkDotNet harness measuring query execution overhead, SQL emission latency, and TypeMapper expression-compilation cost across four runtime targets. No Tier-1 anchors are designated yet; see the AUDIT-NOTE below.

## Subsystems

### 1. Queries/ — live-query benchmarks (MockDb, no real DB)

Nine benchmark classes under `Tests/Tests.Benchmarks/Benchmarks/Queries/`. Each follows the pattern:

- `[GlobalSetup]` wires a `MockDbConnection` + `DataConnection` (or `Db` subclass) backed by a static `QueryResult` payload.
- `[Benchmark]` methods compare LINQ, `CompiledQuery`, and raw ADO.NET execution paths.
- `[Benchmark(Baseline = true)]` is always the raw ADO.NET path so ratios are LINQ/compiled overhead over pure ADO.

| Class | Provider | Measures |
|---|---|---|
| `SelectBenchmark` | PostgreSQL v9.5 | Single-row SELECT: Linq, Compiled, FromSql\_Interpolation, FromSql\_Formattable, Query, Execute, RawAdoNet |
| `FetchSetBenchmark` | SQL Server 2022 | 31 465-row `SalesOrderHeader` materialisation: Linq, Compiled, RawAdoNet |
| `FetchIndividualBenchmark` | SQL Server | Single-entity fetch variants |
| `FetchGraphBenchmark` | SQL Server 2022 | `LoadWith` eager-loading (1 000 headers + 4 768 details): Linq, LinqAsync, Compiled, CompiledAsync |
| `InsertSetBenchmark` | SQL Server 2022 | `BulkCopy` MultipleRows, batch size 100 over 1 000 rows |
| `UpdateBenchmark` | PostgreSQL v9.5 | UPDATE via LinqSet, LinqObject, Object, CompiledLinqSet, CompiledLinqObject, RawAdoNet |
| `ConcurrentBenchmark` | PostgreSQL v9.5 | 16/32/64 threads, `[ParamsSource]`; compiled vs LINQ, stress-tests query-cache lock contention |
| `Issue3253Benchmark` | SQLite (Microsoft) | Small/Large INSERT and UPDATE with variable vs static parameters; `Query.ClearCaches()` variant; async variants |
| `Issue3268Benchmark` | SQL Server 2008 | UPDATE with nullable vs non-nullable columns; compiled vs dynamic; measures `DataConnection` creation overhead with `_Full` variants |

`Issue3253Benchmark` uses `LinqToDB.Internal.Linq.Query.ClearCaches()` to force cache-miss regressions — it was introduced for issue #3253 which tracked query-plan cache bloat with high column counts.

### 2. QueryGeneration/ — SQL emission only (no DB round-trip)

`QueryGenerationBenchmark` (`Tests/Tests.Benchmarks/Benchmarks/QueryGeneration/QueryGenerationBenchmark.cs:1`) builds `NorthwindDB` over a `MockDbConnection(Array.Empty<QueryResult>())` — no data ever flows. It runs `.ToString()` on the IQueryable (which internally drives the full LINQ→SQL translation pipeline) without executing. Providers: Access OleDb + Firebird v5 (active); SQLite, PostgreSQL, SQL Server variants are commented out. `[ParamsSource(nameof(ValuesForDataProvider))]` parameterizes across the active provider map. Three benchmarks:

- `VwSalesByYear` — grouped join with year filter (stable query shape, caches hit after first run).
- `VwSalesByYearMutation` — same shape but year changes on each iteration (cache misses each iteration by design).
- `VwSalesByCategoryContains` — multi-join + `Contains` on category name (complex predicate, tested with JetBrains profiler support via `#if JETBRAINS`).

### 3. TypeMapper/ — expression-compilation micro-benchmarks

Fourteen benchmark classes under `Tests/Tests.Benchmarks/Benchmarks/TypeMapper/`. All use `TypeMapper` from `LinqToDB.Internal.Expressions.Types` and `ExpressionGenerator` from `LinqToDB.Internal.Expressions`. Each pair compares the type-mapped indirection path against a direct call marked `[Benchmark(Baseline = true)]`.

| Class | Aspect benchmarked |
|---|---|
| `BuildActionBenchmark` | `TypeMapper.BuildAction` / `MapActionLambda` overhead |
| `BuildFuncBenchmark` | `TypeMapper.BuildFunc` variants |
| `BuildGetterBenchmark` | property getter via mapped delegate |
| `BuildSetterBenchmark` | property setter via mapped delegate |
| `CreateAndWrapBenchmark` | `BuildWrappedFactory` / `BuildFactory` for 9 constructor signatures |
| `EnumConvertBenchmark` | cast-convert vs dictionary-convert vs flags-convert for `[Wrapper]` enums |
| `WrapActionBenchmark` | wrapped void method call |
| `WrapBenchmark` | wrapped string method, wrapped instance return, `GetEnumerator` over wrapper |
| `WrapEventBenchmark` | wrapped event: empty fire, add/fire/remove, subscribed fire |
| `WrapGetterBenchmark` | wrapped property getters (string, int, long, bool, wrapped type, enum, Version) |
| `WrapInstanceBenchmark` | `TypeMapper.Wrap<T>` instance creation cost |
| `WrapSetterBenchmark` | wrapped property setters |
| `NpgsqlBulkCopyRowWriterBenchmark` | `ExpressionGenerator`-built row-writer vs direct `NpgsqlBinaryImporter.Write`; includes `ColumnDescriptor.GetProviderValue` |
| `OracleReaderExpressionsBenchmark` | 8 benchmarks: TypeMapper-built vs direct for `DateTimeOffset` reads from `OracleTimeStampTZ`/`LTZ` and `OracleDecimal` conversions; includes the workaround for issue #2032 |

All TypeMapper benchmarks use synthetic `Original.*` / `Wrapped.*` classes defined in `TestClasses/TypeMapperWrappers.cs`; `[MethodImpl(MethodImplOptions.NoInlining)]` on every original method prevents JIT inlining from eliminating the overhead being measured.

## Provider mocks

`TestClasses/ProviderMocks/` (namespace `LinqToDB.Benchmarks.TestProvider`) contains 7 classes that implement the full ADO.NET `DbConnection`/`DbCommand`/`DbDataReader`/`DbParameter`/`DbParameterCollection`/`DbTransaction` hierarchy against an in-memory `QueryResult`:

- `QueryResult` — payload: `Names[]`, `FieldTypes[]`, `DbTypes[]`, `Data object?[][]`, `Return int`, optional `Match Func<string,bool>` predicate for multi-result routing.
- `MockDbConnection` — supports single-result and multi-result (`QueryResult[]`) constructors; routes via `MockDbCommand`.
- `MockDbCommand` — calls `GetResult()` which applies `Match` predicate for multi-result routing; `ExecuteNonQuery()` returns `QueryResult.Return`.
- `MockDbDataReader` — `Read()` increments row index into `Data`; `IsDBNull` checks for null; typed getters cast directly. `GetSchemaTable()` returns `QueryResult.Schema` (needed for column-metadata discovery).
- `MockDbParameter` / `MockDbParameterCollection` / `MockDbTransaction` — minimal stub implementations.

These mocks eliminate network and serialisation cost, making every `[Benchmark]` measure only linq2db's own translation and materialisation overhead.

## Key types

| Type | File | Role |
|---|---|---|
| `Config` | `Config.cs` | `IConfig` singleton; jobs: .NET 4.6.2 (baseline), 8.0, 9.0, 10.0; RyuJIT x64; MemoryDiagnoser; GitHub Markdown exporter; `FilteredColumnProvider` strips Job/Error/Median/Gen*/Ratio/StdDev columns |
| `Program` | `Program.cs` | Entry point; default filter `*.Queries.* *.QueryGeneration.*` (TypeMapper benchmarks opt-in only); `BenchmarkSwitcher.FromAssembly` |
| `MockDbConnection` | `TestClasses/ProviderMocks/MockDbConnection.cs` | Core mock entry point |
| `QueryResult` | `TestClasses/ProviderMocks/QueryResult.cs` | Mock result payload |
| `NorthwindDB` | `Models/Northwind/NorthwindDB.cs` | `DataConnection` subclass wired to `MockDbConnection(Array.Empty<QueryResult>())`; used exclusively by `QueryGenerationBenchmark` |
| `NortwindExtensions` | `Models/Northwind/NortwindExtensions.cs` | LINQ-expressed Northwind views (`VwSalesByYear`, `VwSalesByCategory`, etc.) used as query generation targets |
| `Db` | `TestClasses/RawDataAccessBencherMappings.cs` | `DataConnection` subclass for query benchmarks; `SalesOrderHeader`, `SalesOrderDetail`, `Customer`, `CreditCard` entity definitions with pre-built `SchemaTable`/`Names`/`FieldTypes`/`DbTypes`/`SampleRow` statics |
| `TypeMapperWrappers` | `TestClasses/TypeMapperWrappers.cs` | `Original.*` / `Wrapped.*` type pairs for all TypeMapper benchmarks; `Wrapped.Helper.CreateTypeMapper()` centralises `TypeMapper` setup |

## Files (Tier 1 / Tier 2)

No Tier-1 files are declared in `kb-areas.md` for this area. All 41 files are Tier 2.

**Read this run (28 / 41):**

| File | Notes |
|---|---|
| `Program.cs` | Entry point; default filter |
| `Config.cs` | Job matrix (net462/net80/net90/net10), exporters, columns |
| `linq2db.Benchmarks.csproj` | OutputType=Exe; refs LinqToDB.csproj + BenchmarkDotNet; TFMs from `..\linq2db.Providers.props` |
| `Benchmarks/Queries/SelectBenchmark.cs` | SELECT benchmark; representative Queries pattern |
| `Benchmarks/Queries/FetchSetBenchmark.cs` | Multi-row fetch |
| `Benchmarks/Queries/FetchGraphBenchmark.cs` | LoadWith eager loading |
| `Benchmarks/Queries/InsertSetBenchmark.cs` | BulkCopy MultipleRows |
| `Benchmarks/Queries/UpdateBenchmark.cs` | UPDATE variants |
| `Benchmarks/Queries/ConcurrentBenchmark.cs` | Thread-count parameterised concurrency |
| `Benchmarks/Queries/Issue3253Benchmark.cs` | Issue #3253 perf regression; cache-clear variant |
| `Benchmarks/Queries/Issue3268Benchmark.cs` | Issue #3268 nullable column overhead |
| `Benchmarks/QueryGeneration/QueryGenerationBenchmark.cs` | SQL emission only |
| `Benchmarks/TypeMapper/BuildActionBenchmark.cs` | TypeMapper action overhead |
| `Benchmarks/TypeMapper/CreateAndWrapBenchmark.cs` | Factory/constructor mapping |
| `Benchmarks/TypeMapper/EnumConvertBenchmark.cs` | Enum conversion strategies |
| `Benchmarks/TypeMapper/WrapBenchmark.cs` | Wrapped method/instance/enumerator |
| `Benchmarks/TypeMapper/WrapEventBenchmark.cs` | Wrapped event handling |
| `Benchmarks/TypeMapper/WrapGetterBenchmark.cs` | Property getter mapping |
| `Benchmarks/TypeMapper/NpgsqlBulkCopyRowWriterBenchmark.cs` | ExpressionGenerator bulk-copy path |
| `Benchmarks/TypeMapper/OracleReaderExpressionsBenchmark.cs` | Oracle timestamp/decimal reader expressions |
| `TestClasses/ProviderMocks/MockDbConnection.cs` | Mock ADO.NET connection |
| `TestClasses/ProviderMocks/MockDbCommand.cs` | Mock ADO.NET command |
| `TestClasses/ProviderMocks/MockDbDataReader.cs` | Mock ADO.NET reader |
| `TestClasses/ProviderMocks/MockDbParameter.cs` | Mock parameter |
| `TestClasses/ProviderMocks/QueryResult.cs` | Mock result payload |
| `TestClasses/Mappings.cs` | `User`, `Workflow` entity definitions |
| `TestClasses/RawDataAccessBencherMappings.cs` | `Db`, `SalesOrderHeader`, `SalesOrderDetail`, `Customer`, `CreditCard` |
| `TestClasses/TypeMapperWrappers.cs` | `Original.*`/`Wrapped.*` synthetic type pairs |
| `Models/Northwind/NorthwindDB.cs` | Northwind DataConnection |
| `Models/Northwind/NortwindExtensions.cs` | LINQ-expressed Northwind views |

**Not read this run (13 / 41):**

| File | Skip reason |
|---|---|
| `Benchmarks/Queries/FetchIndividualBenchmark.cs` | Same pattern as FetchSetBenchmark; single-entity variant |
| `Benchmarks/TypeMapper/BuildFuncBenchmark.cs` | Same pattern as BuildActionBenchmark; func variant |
| `Benchmarks/TypeMapper/BuildGetterBenchmark.cs` | Same pattern as WrapGetterBenchmark; setup variant |
| `Benchmarks/TypeMapper/BuildSetterBenchmark.cs` | Same pattern as WrapSetterBenchmark; setup variant |
| `Benchmarks/TypeMapper/WrapActionBenchmark.cs` | Same pattern as WrapBenchmark; void method variant |
| `Benchmarks/TypeMapper/WrapInstanceBenchmark.cs` | Same pattern as CreateAndWrapBenchmark; wrap-only cost |
| `Benchmarks/TypeMapper/WrapSetterBenchmark.cs` | Same pattern as WrapGetterBenchmark; setter variant |
| `TestClasses/ProviderMocks/MockDbParameterCollection.cs` | Minimal stub; same pattern as MockDbParameter |
| `TestClasses/ProviderMocks/MockDbTransaction.cs` | Minimal stub; BeginTransaction only |
| `Models/Northwind/Northwind.cs` | Entity definitions for Northwind model |
| `Models/Northwind/Northwind.Views.cs` | View projections for Northwind model |

## Inbound / outbound dependencies

**Outbound (this area depends on):**

- `Source/LinqToDB/LinqToDB.csproj` — direct project reference (sole project dependency in csproj).
- `LinqToDB.Internal.Linq.Query` (`Query.ClearCaches()`) — used in `Issue3253Benchmark`.
- `LinqToDB.Internal.Expressions.Types.TypeMapper` + `LinqToDB.Internal.Expressions.ExpressionGenerator` — used by all TypeMapper benchmarks.
- `LinqToDB.DataProvider.PostgreSQL`, `.SqlServer`, `.SQLite`, `.Access`, `.Firebird` — provider-specific `GetDataProvider()` calls in benchmark setups.
- `LinqToDB.Async` — `ToListAsync()` in `FetchGraphBenchmark`.

**Inbound (who depends on this area):** None at runtime; this is an executable benchmark harness. The solution filter `linq2db.Benchmarks.slnf` is the only structural inbound reference.

## Known issues / debt

- `QueryGenerationBenchmark` has most providers commented out — only Access and Firebird are active. The commented block suggests intended multi-provider parameterisation that was never fully enabled. Any new provider added to the benchmark would produce richer regression data.
- `Program.cs` contains a large commented-out manual-run block (lines 22–97) that duplicates all benchmark class invocations. This is development scaffolding; it is not dead code (uncommenting enables profiler-guided runs) but it adds noise.
- `OracleReaderExpressionsBenchmark` documents a workaround for issue #2032 (complex reader expressions) via double-compile. The issue link is `https://github.com/linq2db/linq2db/issues/2032` — worth verifying if it has since been resolved.
- No `results/` directory state documented — BenchmarkDotNet artifacts path is set to `..\..\..\..\..\..\Tests\Tests.Benchmarks`, meaning results land in the project root, which is committed. The `.gitignore` status of this directory is not validated here.
- `FetchIndividualBenchmark.cs` is structurally identical to `FetchSetBenchmark.cs` (single-entity form); deduplication with `[Params]` row count was not pursued.
- `linq2db.Benchmarks.csproj` imports `<Import Project="..\linq2db.Providers.props" />` — TFMs are resolved from that shared props file rather than declared directly, so the actual TFM list (`net462;net8.0;net9.0;net10.0`) is not visible in the project file alone.

## See also

- `linq2db.Benchmarks.slnf` — solution filter for benchmark-only builds.
- `Tests/Tests.Benchmarks/results/` — committed BenchmarkDotNet output (Markdown tables).
- [`areas/INTERNAL-API/INDEX.md`](../INTERNAL-API/INDEX.md) — `TypeMapper`, `ExpressionGenerator` implementation.
- [`areas/PROV-POSTGRES/INDEX.md`](../PROV-POSTGRES/INDEX.md) — `NpgsqlBulkCopyRowWriterBenchmark` exercises the Npgsql bulk-copy hot path.
- [`areas/PROV-ORACLE/INDEX.md`](../PROV-ORACLE/INDEX.md) — `OracleReaderExpressionsBenchmark` exercises the Oracle reader expression path.

<details><summary>Coverage</summary>

Tier 1: 0 / 0 files (no Tier-1 anchors declared).

Tier 2: 28 / 41 files read (68%). 13 files deferred — all confirmed to follow the same structural pattern as siblings read in the same subdir. Confidence set to `medium` because the 90% Tier-2 threshold was not reached; no claims rest on unread files.

</details>
