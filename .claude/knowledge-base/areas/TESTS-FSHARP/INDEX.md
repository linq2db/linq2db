---
area: TESTS-FSHARP
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 0/0
coverage_tier_2: 18/18
---

# TESTS-FSHARP

Two F# test projects that validate linq2db's F# language integration and the EFCore F# bridge. Neither project defines NUnit `[<TestFixture>]` classes directly; instead each `.fs` module exposes plain functions that are called from a C# test fixture in the consumer project (TESTS-EFCORE calls into `FSharpTestMethods`; the core F# tests are presumably wired from a C# fixture in `Tests.Base` or a companion project not in this area).

## Subsystems

### Tests/FSharp — core linq2db F# tests (`linq2db.Tests.FSharp`, F# LangVersion 9)

References `Tests.Base.csproj`, `LinqToDB.FSharp.fsproj`, `FSharp.Core`, and `Npgsql.NodaTime`. No TFM constraint in the fsproj itself (inherits from `Directory.Build.props`). F# compilation order (order-sensitive):

`Issue4851.fs` → `Issue2678.fs` → `Models.fs` → `Issue3357.fs` → `WhereTest.fs` → `SelectTest.fs` → `InsertTest.fs` → `MappingSchema.fs` → `Issue3743.fs` → `Issue4132.fs` → `Issue1813.fs` → `Issue5428.fs`

`Models.fs` must precede all test modules because the entity types are `open`ed by them.

### Tests/EntityFrameworkCore.FSharp — EFCore F# bridge (`Tests.EntityFrameworkCore.FSharp`, F# LangVersion 9)

Fixed to `net8.0`. References `LinqToDB.EntityFrameworkCore.EF8.csproj`, `LinqToDB.FSharp.fsproj`, `EntityFrameworkCore.FSharp` (community package), and `FSharp.Core`. Compilation order: `FSharpExtensions.fs` → `FSharpContext.fs` → `FSharpTestMethods.fs`.

## Key types

**`Tests/FSharp/Models.fs`** — entity record types used across all core F# tests:
- `Person` / `Patient` — mutually recursive F# records (no `[<CLIMutable>]`); use `[<PrimaryKey>]`, `[<Column>]`, `[<Association>]`, `[<SequenceName>]` from `LinqToDB.Mapping`. `Person.Gender` is a discriminated union with `[<MapValue>]` attributes.
- `PersonCLIMutable` / `PatientCLIMutable` — same schema as `Person`/`Patient` but `[<CLIMutable>]`; validates the two materialization paths (`FSharpEntityBindingInterceptor` vs. standard property-setter path).
- `ComplexPerson` / `ComplexPersonRecord` / `DeeplyComplexPerson` — nested-record mapping tests: `FullName`, `NestedFullName`, `LastName` show flat-column-to-nested-field mapping via table-level `[<Column("col", "path.field")>]`.
- `PersonWithOptions` — uses `string option` fields; requires `MappingSchema.Initialize()` to register option converters before use.
- `PersonConflictingNamesRecord` / `NameConflictingNamesRecord` — deliberately conflicting case-only field names (`id`, `Id`, `iD`) to test F# record constructor parameter matching.
- `Child` / `Parent` — simple two-field records reused across join tests.

**`Tests/FSharp/MappingSchema.fs`** — `Initialize()` function that adds `string option` scalar type and a `SetConvertExpression` for `Option<_>`. Must be called before any `PersonWithOptions` test.

**`Tests/EntityFrameworkCore.FSharp/FSharpContext.fs`** — `AppDbContext : DbContext` with `WithIdentity : DbSet<WithIdentity>` and `Issue4646Table` (F# option columns `Value : int option`, `ValueN : Nullable<int> option` using EFCore `OptionConverter`). Issue 4646 tests cross-validate that EFCore and linq2db both handle F# option columns correctly.

**`Tests/EntityFrameworkCore.FSharp/FSharpExtensions.fs`** — `WithFSharp(builder)` calls `builder.UseFSharpTypes()` (from `EntityFrameworkCore.FSharp` community package).

**`Tests/EntityFrameworkCore.FSharp/FSharpTestMethods.fs`** — `TestLeftJoin`, `Issue4646TestEF`, `Issue4646TestLinqToDB` — entry points called by TESTS-EFCORE's `FSharpTests` fixture.

## Issue regressions covered

| Module | Issue | Subject |
|---|---|---|
| `Issue1813.fs` | #1813 | Complex `groupJoin`/`leftOuterJoin` combinations (7 test cases, multi-key tuples) |
| `Issue2678.fs` | #2678 | Insert+select round-trip comparing F# class vs. `[<CLIMutable>]` record |
| `Issue3357.fs` | #3357 | `Union`/`Concat` of tuples, named records, and anonymous records in F# query expressions |
| `Issue3743.fs` | #3743 | Chained `.Join().LeftJoin()` via method syntax using tuples and anonymous records |
| `Issue4132.fs` | #4132 | `table.Insert(fun () -> ...)` and `table.Update(predicate, setter)` lambda syntax |
| `Issue4851.fs` | #4851 | `(new DataOptions()).UseFSharp()` compiles without error (smoke) |
| `Issue5428.fs` | #5428 | `UseFSharp()` + NodaTime `LocalDate` + PostgreSQL `DateInterval`, CTE + window `Lead` |

## Files (Tier 1 / Tier 2)

No files are currently designated Tier-1. All 18 files treated as Tier-2 and read in full.

| Tier | File |
|---|---|
| 2 | `Tests/FSharp/Tests.FSharp.fsproj` |
| 2 | `Tests/FSharp/Models.fs` |
| 2 | `Tests/FSharp/MappingSchema.fs` |
| 2 | `Tests/FSharp/SelectTest.fs` |
| 2 | `Tests/FSharp/WhereTest.fs` |
| 2 | `Tests/FSharp/InsertTest.fs` |
| 2 | `Tests/FSharp/Issue1813.fs` |
| 2 | `Tests/FSharp/Issue2678.fs` |
| 2 | `Tests/FSharp/Issue3357.fs` |
| 2 | `Tests/FSharp/Issue3743.fs` |
| 2 | `Tests/FSharp/Issue4132.fs` |
| 2 | `Tests/FSharp/Issue4851.fs` |
| 2 | `Tests/FSharp/Issue5428.fs` |
| 2 | `Tests/EntityFrameworkCore.FSharp/Tests.EntityFrameworkCore.FSharp.fsproj` |
| 2 | `Tests/EntityFrameworkCore.FSharp/FSharpContext.fs` |
| 2 | `Tests/EntityFrameworkCore.FSharp/FSharpExtensions.fs` |
| 2 | `Tests/EntityFrameworkCore.FSharp/FSharpTestMethods.fs` |

`Tests.FSharp.fsproj` does not include `Issue4851.fs` last despite its content being a standalone smoke test — it is first in the fsproj `<Compile>` list (before `Models.fs`), which is valid because it has no dependency on `Models.fs`.

## Inbound / outbound dependencies

**Inbound:**
- TESTS-EFCORE: its `FSharpTests` C# fixture (`#if EF8`-gated) calls `FSharpTestMethods.TestLeftJoin`, `Issue4646TestEF`, `Issue4646TestLinqToDB`, and `WithFSharp`.
- Any C# fixture in `Tests.Base` / the main test project that wires the `Tests.FSharp.*` module functions as NUnit tests (not in this area — the wrapper fixture is in the C# test project).

**Outbound:**
- **FSHARP** production area: `LinqToDB.FSharp.fsproj` — `DataOptions.UseFSharp()`, `FSharpEntityBindingInterceptor` (exercised by non-`[<CLIMutable>]` record reads in `WhereTest.LoadSingle`, `SelectTest.SelectField`, etc.).
- **EFCORE** production area: `LinqToDB.EntityFrameworkCore.EF8.csproj` — `.ToLinqToDB()`, `CreateLinqToDBConnection()`.
- `Tests.Base.csproj` — `Tests.Tools` module (used in `InsertTest`, `Issue1813`, `Issue4132`); `Tests` namespace open in issue modules.
- External: NUnit, `Npgsql.NodaTime` (Issue5428), `EntityFrameworkCore.FSharp` community package (EFCore project).

## Known issues / debt

- `Tests.FSharp.fsproj` carries no TFM override; it inherits from `Directory.Build.props`. The EFCore project is pinned to `net8.0` which limits it to EF8 and prevents testing EF9/EF10 F# scenarios.
- `Issue5428.fs` takes a raw `connectionString : string` parameter (no `IDataContext`), meaning it cannot be run by the standard NUnit DataContext fixture mechanism without a custom wrapper — it is PostgreSQL-only and likely guarded by a provider skip in the C# caller.
- The `MappingSchema.Initialize()` function must be called before any `PersonWithOptions`-using test; there is no test-level `[<SetUp>]` enforcement visible in this area.
- No F# tests exist for async LINQ, `IAsyncEnumerable`, or `CancellationToken` paths.

## See also

- [FSHARP area](../FSHARP/INDEX.md) — production `linq2db.FSharp` package (`DataOptions.UseFSharp`, `FSharpEntityBindingInterceptor`)
- [TESTS-EFCORE area](../TESTS-EFCORE/INDEX.md) — hosts the `FSharpTests` fixture that calls into this area
- [TESTS-INFRA area](../TESTS-INFRA/INDEX.md) — `Tests.Base` / `Tests.Tools` depended on by `InsertTest`, `Issue*` modules

<details><summary>Coverage</summary>

Read (this run):
- `Tests/FSharp/Tests.FSharp.fsproj` — project setup, TFMs, compile order, references
- `Tests/FSharp/Models.fs` — all entity record types
- `Tests/FSharp/MappingSchema.fs` — option-type mapping schema helper
- `Tests/FSharp/SelectTest.fs` — select, left-join, group-join, record projection tests
- `Tests/FSharp/WhereTest.fs` — where, LoadWith, ComplexPerson, option-type tests
- `Tests/FSharp/InsertTest.fs` — insert Child and ComplexPerson tests
- `Tests/FSharp/Issue1813.fs` — 7 groupJoin/leftOuterJoin regression tests
- `Tests/FSharp/Issue2678.fs` — class vs CLIMutable record insert/select
- `Tests/FSharp/Issue3357.fs` — Union/Concat of tuples, records, anon records
- `Tests/FSharp/Issue3743.fs` — chained Join+LeftJoin method syntax
- `Tests/FSharp/Issue4132.fs` — Insert/Update lambda syntax
- `Tests/FSharp/Issue4851.fs` — DataOptions.UseFSharp() smoke test
- `Tests/FSharp/Issue5428.fs` — UseFSharp + NodaTime + PostgreSQL DateInterval + CTE
- `Tests/EntityFrameworkCore.FSharp/Tests.EntityFrameworkCore.FSharp.fsproj` — project setup, net8.0, references
- `Tests/EntityFrameworkCore.FSharp/FSharpContext.fs` — AppDbContext, WithIdentity, Issue4646Table
- `Tests/EntityFrameworkCore.FSharp/FSharpExtensions.fs` — WithFSharp helper
- `Tests/EntityFrameworkCore.FSharp/FSharpTestMethods.fs` — TestLeftJoin, Issue4646 entry points

Tier-2 visited: 17/17 (.fs files) + 2/2 (.fsproj files) = 18/18 total. No skips.
</details>
