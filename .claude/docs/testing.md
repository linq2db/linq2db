# Running Tests

Tests use **NUnit3** with **Shouldly** assertions (not NUnit Assert). Test targets: `net462`, `net8.0`, `net9.0`, `net10.0`. **Always use Debug configuration and prefer `net10.0`** — Release enables Roslyn analyzers and is much slower to build. The full test suite takes ~1 hour or more; avoid running it unless necessary.

**Don't prepend a `dotnet build`.** `dotnet test` builds the project automatically if binaries are stale; a standalone `dotnet build` right before `dotnet test` only adds waiting time. If a test run fails with compile errors, fix them and re-run `dotnet test` directly.

```bash
# Run a single test class or method via dotnet test
dotnet test Tests/Linq/Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net10.0

# Run tests with the lightweight playground solution (faster load)
dotnet test linq2db.playground.slnf
```

**Default to `Tests.Playground` for any iterative test run** — fresh tests, fix-verification on existing tests, ad-hoc repro. The full `Tests/Linq/Tests.csproj` build is ~3+ minutes; the playground project is ~30s. Two distinct shapes:

1. **Scratch verification** — one-off tests that won't be committed. Edit `Tests/Tests.Playground/TestTemplate.cs` directly with a self-contained fixture (define converters / tables / data inline). No `<Compile Include>` needed; the SDK-style csproj implicitly compiles the project's own `*.cs` files. Revert when done — the template-edit is scratch (per `agent-rules.md` → *Never commit playground scratch*).
2. **Iterating on a real test in `Tests/Linq/`** — add `<Compile Include="..\Linq\<sub>\<File>.cs" Link="<File>.cs" />` to `Tests/Tests.Playground/Tests.Playground.csproj`. The link is local scratch and must **not** be committed (same rule). Use this shape when iterating on a test that already lives in `Tests/Linq/` (e.g. a regression test you just wrote alongside a fix) without paying the cost of the full `Tests/Linq/Tests.csproj` multi-TFM build.

```bash
dotnet test Tests/Tests.Playground/Tests.Playground.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net10.0
```

Reach for the full `Tests/Linq/Tests.csproj` only when the test target spans many files that would require a wide playground link, or when running a broad filter (e.g. an entire test class).

## Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json` (or run `/test-providers reset`, which does the same thing under explicit confirmation and writes a backup of any existing file).
2. This gives you SQLite-based testing out of the box.
3. Add connection strings for other databases as needed.

After the file exists, `/test-providers` is the supported way to enable / disable providers per TFM bucket and to start the docker containers behind them. `/test` reads the resulting state but never edits it — see [`.claude/skills/test-providers/SKILL.md`](../skills/test-providers/SKILL.md).

**Ask before editing `UserDataProviders.json`.** Before the **first** edit in a session, ask the user whether to stash/back up the current file. The file is gitignored and holds the user's connection strings + enabled-provider flags — an incorrect edit has no git history to recover from. Subsequent edits in the same session don't need to re-prompt.

### `Providers` is keyed by TFM

`UserDataProviders.json` has a top-level section per target framework (`NETFX`, `NETBASE`, `NET80`, `NET90`, `NET100`), each with its own `Providers` array. To enable a provider for a test run, uncomment it in the section matching your `-f` flag — `NETFX` only affects `net462` runs, `NET100` only affects `net10.0` runs, etc. Editing the wrong section silently does nothing.

### Container-backed providers

Most non-file providers (SAP HANA, most Oracle versions, PostgreSQL, MySQL, DB2, Informix, ClickHouse, SQL Server 2017+) connect to local docker containers. Start the needed container(s) before running tests — e.g. `docker start hana2 oracle11`. SAP HANA in particular takes several minutes after container start before its internal HDB database finishes warm-up; tests that run immediately after `docker start hana2` may hit "connection refused".

File-based providers (SQLite, SqlCe, Access) don't need a container.

### Provider variant defaults

When the user asks to run tests against a provider family without naming the exact variant:

- **Oracle** → `Oracle.<ver>.Managed`. Run `Oracle.*.Native` only on explicit request — Native pulls in bitness-specific drivers the user prefers to avoid by default.
- **Access** → `Access.*.Ace.OleDb` + `Access.*.Ace.Odbc`. Never enable `Access.*.Jet.*` without an explicit request.
- **SAP HANA** → both `SapHana.Native` and `SapHana.Odbc`.

**Access Jet requires x86.** If the user does request `Access.*.Jet.*`, the test process must run under x86 (32-bit). When running x86, **do not** enable any other provider alongside Jet — other providers may be x64-only (native drivers) or architecture-agnostic but untested on x86, so a mixed x86 run risks failures unrelated to the Jet work.

## Database initialization

`Tests.Linq.Create.CreateData.CreateDatabase` is attributed `[Test, Order(0)]` — NUnit runs it first in the session, and every downstream test that calls `GetDataContext(context)` relies on it having created + populated the provider's schema. After a container restart, a fresh clone, or an aborted previous run, the schema may not be current.

**Always include `CreateDatabase` in your `--filter`.** Prepend `FullyQualifiedName~CreateData.CreateDatabase|` to any filter you construct — the test is idempotent, re-running it is cheap, and it removes the "empty database" failure mode entirely.

```bash
dotnet test Tests/Linq/Tests.csproj -f net10.0 --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~FirebirdTests.DropTableTest"
```

If your test modifies data, revert changes to avoid side effects in downstream tests.

**A filtered run that skips `CreateDatabase` can show failures that masquerade as pre-existing / unrelated.** A prior data-mutating test can leave `TestData*.sqlite` with corrupted rows; a later filtered run then reads bad data and fails on tests that have nothing to do with your change. **Stash-and-rerun does not disprove this** — both runs share the same corrupted file, so they fail identically and the comparison looks like "pre-existing". Before concluding "these failures pre-date my change / are unrelated", re-run with `FullyQualifiedName~CreateData.CreateDatabase|` prepended to rebuild the schema. This applies especially when working in a `git worktree`: `/test` targets the main checkout, not the worktree branch, so you run `dotnet test` directly in the worktree and must prepend `CreateDatabase` yourself.

## Test Patterns

```csharp
[TestFixture]
public class MyTest : TestBase
{
    [Test]
    public void TestSomething([DataSources] string context)
    {
        using var db = GetDataContext(context);
        // AssertQuery runs query against both DB and in-memory collections
        // and verifies matching results
        AssertQuery(db.Person.Where(_ => _.Name == "John"));
    }
}
```

- `[DataSources]` — runs test for each enabled database provider
- `TestBase` — base class providing `GetDataContext()`, `AssertQuery()`, etc.
- Tests live in `Tests/Linq/` (main tests), `Tests/Base/` (test framework), `Tests/Model/` (model classes)
- **Provider-specific tests go in that provider's own fixture.** A test exercising one provider's behavior (a YDB CTE-naming quirk, an Oracle row-predicate case) belongs in the provider fixture (`Tests/Linq/DataProvider/YdbTests.cs`, `Issue<N>Tests.cs`) — not prepended to a shared cross-provider fixture (`CteTests`, `WhereTests`). Shared fixtures are for behavior asserted across many providers.
- **No `#region` blocks or banner comments in test methods.** Don't wrap added tests in a new `#region`, and don't introduce `//----` banner comment blocks. Keep explanatory comments as plain `//` lines **inside** the method body next to the code they describe — and don't relocate existing in-body comments out to the method/attribute level when making an unrelated edit.
- **Mapping attributes live in the `LinqToDB` root namespace.** `ExpressionMethodAttribute` (and peers) are `LinqToDB.*`, not `LinqToDB.Mapping.*` — a test/playground file needs `using LinqToDB;` in addition to `using LinqToDB.Mapping;`, otherwise `[ExpressionMethod(...)]` fails with `CS0246`. (`ColumnAttribute`, `TableAttribute`, `InheritanceMappingAttribute` are in `LinqToDB.Mapping`.)

## Tests that pass but catch nothing

A test that compiles and goes green is not yet a regression guard — it has to be able to go **red** on the bug it targets. The recurring ways an AI-drafted test silently guards nothing:

- **Happy-path only.** Asserts the success case and nothing else; the edge that actually broke (null, empty, overflow, negative, boundary, provider-specific fold) goes untested. A regression test must exercise the *failing* input, not a neighbouring working one.
- **Brittle string assertions.** `ShouldBe("…literal SQL…")` / `ShouldContain("…text…")` locked onto incidental wording — churn mass-fails them while the real logic regresses undetected. Assert behavior (row content, shape, count), not a string. The SQL-shape variant is covered in detail under *When a substring assertion is vacuous* below.
- **Mocking the logic away.** Stubbing the component under test so the assertion verifies the mock, not linq2db. Here that shows up as asserting against a hand-built expected object instead of round-tripping through `AssertQuery` / a real `CreateLocalTable<T>`.
- **Mirroring the implementation.** Deriving the expected value by re-running the same code path under test — or reading the current emitted SQL and pasting it back as the baseline — bakes the current behavior, bugs included, in as "correct".

Before reporting a test written, state in one line **which input makes it go red** and confirm it would have failed against the pre-fix code. If you can't name that input, it isn't a guard yet. Mutation-score tooling is out of scope; the red→green demonstration required by `agent-rules.md` → *Before coding a fix or feature* is the bar.

## Cross-provider gotchas for new tests

### YDB requires a primary key on every table

YDB rejects `CREATE TABLE` without a primary key, so `db.CreateLocalTable<T>()` of a keyless type fails at setup with `Primary key is required for ydb tables` (wrapped in a `Pre type annotation` error). YDB isn't on CI yet, so this surfaces only when running YDB locally. Fix the test's table type by adding `[PrimaryKey]` to a suitable existing column (a non-nullable key such as `Id`, or the single natural-key column); if none fits, add a dedicated PK column. It's a test-data fix, not a provider change.

### Affected-rows assertions need `SupportsRowcount`

`.Update()` / `.Insert()` / `.Delete()` return an affected-row count, but **ClickHouse Driver** and **YDB** return `0` for every UPDATE regardless of how many rows actually changed. A bare `affected.ShouldBe(1)` in a `[DataSources]` test breaks on those providers.

Guard the assertion with `context.SupportsRowcount()` (defined in `Tests/Base/ProviderNameHelpers.cs`):

```csharp
var affected = db.GetTable<Foo>()
    .Where(x => x.Id == 1)
    .Set(x => x.Test1, x => !x.Test2)
    .Update();

if (context.SupportsRowcount())
    affected.ShouldBe(1);

// The substantive assertions (row content after update) run unconditionally.
```

Pattern used 10+ times in `Tests/Linq/Update/InsertTests.cs`.

### `[Sql.Expression]` with `bool` return type needs `IsPredicate = true`

When the template emits a SQL predicate (`({0} > 0)`, `{0} IS NULL`, `{0} = {1}`, …) and the CLR method returns `bool`, set `IsPredicate = true`. Otherwise the translator wraps the predicate in a bool→bit coercion (`IIF((pred) = 1, …)`) that emits invalid SQL on **SQL Server**, **Oracle**, **Sybase**, **SAP HANA**, **Access**, **ClickHouse**, and **Firebird 2.5**. PostgreSQL and MySQL/MariaDB tolerate the wrap because they have native `boolean`.

```csharp
[Sql.Expression("({0} > 0)", ServerSideOnly = true, IsPredicate = true)]
static bool IsPositive(int x) => throw new InvalidOperationException();
```

Other examples in code: `Tests/Linq/Linq/BooleanTests.cs:725`, `Tests/Linq/Linq/OperatorsTests.cs:128-129`, `Tests/Linq/DataProvider/PostgreSQLTests.cs:2799`.

### Column nullability is not inferred from C# NRT

linq2db does **not** derive column nullability from C# nullable-reference annotations: an un-annotated `[Column] string Foo` (non-nullable CLR type) still maps to a **nullable** column, so `CanBeNullable(NullabilityContext)` returns `true` for it at SQL-build time. A test that needs a provably non-null column — e.g. asserting a provider omits a `… IS NULL` guard — must say so explicitly with `[Column(CanBeNull = false)]` or FluentMapping `.IsNullable(false)`; the CLR type alone won't make the column non-null.

```csharp
sealed class Entity
{
    [PrimaryKey]                public int     Id  { get; set; }
    [Column(CanBeNull = false)] public string  Req { get; set; } = ""; // NOT NULL column
    [Column(CanBeNull = true)]  public string? Opt { get; set; }        // nullable column
}
```

### Testing query cache HIT / MISS behaviour

Three idioms, each proves something different. Full mechanics in [`query-cache-mechanics.md`](query-cache-mechanics.md).

- **Same query, re-execute → HIT.** Build `query` once, call `.ToArray()` twice, assert the second call doesn't bump `GetCacheMissCount()`. Catches the most common bug: `MarkAsNonParameter`'s stored value type disagreeing with the accessor expression's runtime type (the `value1.Equals(value2)` compare returns false for `"abc".Equals(['a','b','c'])` etc.).

- **Local function with captured-value parameter → HIT across invocations.** Build the query inside a local helper that takes the cache-keyed value as a parameter; call the helper twice with equivalent values; assert miss count unchanged. This is the only clean way to verify cross-invocation cache hits because different C# local variables become different closure-display-class fields with different `MemberInfo` — they don't share cache shape even when content is identical. The local-function param shape collapses both calls onto the same display class with the same field, so structural compare matches and the value-compare layer runs.

- **Mutate captured array → MISS on content change.** Hold a reference to a captured array, run `query.ToArray()`, mutate the array in place, run `query.ToArray()` again. With a content-aware cache key the second invocation registers as a miss (counter goes up). Catches stale-SQL bugs where mutation isn't reflected in the cache.

`GetCacheMissCount()` reads `Query<T>.CacheMissCount` — a static per-entity-type counter shared across the AppDomain. Within a single test method, sequential invocations are deterministic; the assertion is always "delta from a captured baseline", never "absolute count".

## Reading test output

Always read the **full** test run log — not just the tail. NUnit and `dotnet test` interleave relevant information across the log: `_CreateData` failures appear near the top, setup exceptions and warnings can come well before the failing assertion, and stack traces may be truncated if you jump to the end. Do not use `tail`, `head`, `head_limit: 1`, or similar tricks to skim the output; read the entire log and scroll back for context when a failure is surprising. The only exception is when you have already read the log once and are fetching a specific slice you've already identified.

## Diagnosing hung test runs

A `dotnet test` that has been running **>30 s with zero test-output lines** AND a live `testhost.exe` process at **>1 GB resident memory** is almost certainly in an infinite-recursion loop — typically a visitor that hands its own output back to itself (`Visit(Optimize(converted))` re-entering its own `VisitXxx` with a structurally-equivalent element). Confirm via `tasklist /FI "IMAGENAME eq testhost.exe"` (or `Get-Process testhost`); a normal test run keeps testhost well under 500 MB.

Recovery and triage:

1. `Get-Process testhost,dotnet -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq 'testhost' } | Stop-Process -Force` — kill the runaway. The background `dotnet test` wrapper will exit shortly after.
2. Re-read the captured output. The recursion's terminal exception is usually `System.InsufficientExecutionStackException: Too many stack hops (> N). Recursion cannot safely continue.`, thrown from `LinqToDB.Internal.Common.StackGuard.RunOnEmptyStack` — that's linq2db's internal stack-overflow guard re-throwing after the runtime ran out of fresh-thread hops. The top of the truncated stack names the offending visitor method.
3. The fix is virtually always idempotence: the visitor's transformation must produce a fixed point (a re-entry on the transformed element returns it unchanged) — check whether the rewrite wraps an operand in a shape the next visit pass will fail to recognize as already-wrapped, and add a structural guard.

## LinqService "address already in use" (port 22654)

Remote (`*.LinqService`) test configs spin up an in-process HTTP host on a **fixed port** (`22654`). When many `.LinqService` configs fail at *startup* with `System.IO.IOException : Failed to bind to address https://127.0.0.1:22654: address already in use` while the **non-remote configs of the same tests pass**, it is **not** a code regression — a leaked `testhost` from an earlier run is still holding the port. The failure is at host bind, before any query executes, so an entity-construction / SQL change cannot cause it.

Find and stop the orphaned listener:

```
Get-NetTCPConnection -LocalPort 22654 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

Iterating with many back-to-back local `dotnet test` runs is what leaks these hosts; clear the port (or stop stray `testhost` per the section above) before reading `.LinqService` failures as real.

## Debugging linq2db translators

When debugging query translation or provider issues,
use `Console.WriteLine` to output intermediate values or SQL fragments.
Do not introduce logging dependencies.

### Inspecting generated SQL offline (reproducing "could not be converted to SQL")

Use `query.ToSqlQuery().Sql` to see a query's SQL without running it. `IQueryable.ToString()` returns the query *type* name (`ExpressionQueryImpl`1[…]`), **not** SQL. Translation failures ("The LINQ expression '…' could not be converted to SQL") are raised at SQL-build time, so `ToSqlQuery()` — or a `CreateLocalTable<T>()` + query in the playground — reproduces them with no populated database.

- **Plain linq2db** emits SQL fully offline when the provider version is explicit: `new DataConnection(new DataOptions().UseSqlServer("Server=.;Database=x", SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient))`. A dummy connection string works — nothing is opened. Run this from `Tests.Playground` (link the file in), **not** a hand-rolled console placed under the repo root — `Directory.Build.props` / central package management cascade into any project beneath the root and break a stray project's restore (`Unable to find package <name>`, plus unwanted multi-targeting).
- **EF Core `.ToLinqToDB()`** on SQL Server opens a real connection to auto-detect the server version (`ProviderDetectorBase.DetectServerVersion` → `SqlConnection.Open()`), so it needs a live server even just to emit SQL. SQLite EF Core emits offline.

## Baselines

Many tests compare emitted SQL against a stored baseline file. Baselines live **outside the main repo** under the path configured by `BaselinesPath` in `UserDataProviders.json` → `MyConnectionStrings`. With `BaselinesPath` set, a mismatched test overwrites the baseline with the new SQL; subsequent runs compare against the updated file. With `BaselinesPath` unset, baselines are neither written nor compared.

### When a substring assertion is vacuous — prefer baselines for shape-changing fixes

A `ToSqlQuery().Sql.ShouldContain` / `ShouldNotContain("COALESCE")`-style assertion only guards a fix if the substring actually differs pre/post on the providers the test runs on. Two normalizations silently defeat it:

- **No-op stripping.** `Coalesce(x, NULL)` is reduced to bare `x` by `ConvertCoalesce` on every standard provider, so a fix that *stops emitting* such a no-op produces byte-identical SQL — the assertion passes before and after, catching nothing.
- **Function aliasing.** Providers that fold COALESCE to a native construct (Informix `Nvl`, Access `IIF`) never emit the literal `COALESCE`, so a `COALESCE` substring check is vacuous there regardless of the bug.

When a fix changes provider-specific SQL *shape* (especially provider-specific folds), the regression artifact is the per-provider **baseline** `.sql` file, not a substring assertion. (Surfaced on #5531: the bug was only observable as Informix `Nvl(x, NULL)`; a SQLite `ShouldNotContain("COALESCE")` test could not go red.)

### Enabling baselines locally

Open `UserDataProviders.json`, find the `MyConnectionStrings` section, and add:

```json
"BaselinesPath": "c:\\GitHub\\linq2db.bls"
```

Path is arbitrary but the sibling-clone convention — `../linq2db.baselines` — matches the upstream baselines repo's layout and keeps the diff against CI clean.

### Getting a "before" snapshot for diffing

Baselines regenerate in place during the test run. To see "before vs after" you need a snapshot of the prior state. Two options:

1. **Regenerate locally**: check out `master` (or the PR base), run the relevant tests to populate baselines at `BaselinesPath`, then switch back and re-run — diff is between the two generations.
2. **Use the remote baselines repo**: clone `https://github.com/linq2db/linq2db.baselines.git` to `../linq2db.baselines` (or fetch into the existing sibling) and compare `BaselinesPath/<Provider>/…` against `../linq2db.baselines/<Provider>/…` directly. No pre-change test run needed; this is the authoritative "before" for branches based on `master`.

## Known flaky baselines

Tests whose SQL baselines are known to reorder / churn without a real code change. Surfacing reorders here in a PR's baselines diff is **not** a finding — the PR didn't cause it, and flagging it wastes reviewer attention. Mention in passing at most when the diff otherwise runs clean.

- **`Tests.SchemaProvider.SchemaProviderTests.NorthwindTest` on `Northwind.SQLite` / `Northwind.SQLite.MS`** — enumerates views in non-deterministic order (e.g. `[Products by Category]`, `[Alphabetical list of products]`, and `[Summary of Sales by Quarter]` swap positions between runs). The SQL text per view is unchanged; only ordering moves. Confirmed on PR #5443 as pre-existing behavior. The real fix is sorting views by name in the test; out of scope for unrelated PRs.

## `.sql.other` files — direct-vs-remote SQL trace mismatch

When a test runs in both direct (`DataConnection`) and remote (`LinqService` → `DataConnection`) modes for the same provider config, `Tests/Base/BaselinesWriter.cs:62-79` compares the captured SQL across the two runs. If they differ, the second run's body is written to `<test>.sql.other` and the test fails with `"Baselines for remote context doesn't match direct access baselines"`.

`.sql.other` is therefore a **failure indicator**, not an alternate-acceptable form. New PR-introduced `.sql.other` entries warrant root-cause analysis. **Pre-existing entries — always on Oracle — are usually flaky CI artifacts, not a reproducible bug** (#5513):

- Oracle and Access test jobs run with `retry: true` (`retryCountOnTaskFailure: 2`, set because those providers crash / have resource-management issues — see `Build/Azure/pipelines/templates/test-matrix.yml`). A flaky attempt that writes a `.sql.other` (or a crash leaving a partial `.sql`) fails the first `dotnet test`; the retry re-runs, the flake doesn't recur, the job goes **green** — but the failed attempt's files persist in the baselines working tree and the `Commit test baselines` step (condition `succeeded()`) commits them. Nothing prunes `.sql.other`, so they accumulate (e.g. the long-standing `AnalyticTests.TestMin(Oracle.21.Managed).sql.other`).
- The original "#5513 = remote-mode trace drops scalar `IQueryable<T>` aggregate queries" framing was **disproven**: those analytic aggregates trace fine in remote mode on both SQLite and Oracle.21.Managed in isolation. PR #5572 resets the baselines working tree before each retried attempt to stop the leak.
- So: a pre-existing `.sql.other` on a `retry: true` provider that doesn't reproduce locally is flake. Confirm by **blob-comparing** `<test>.sql` vs `<test>.sql.other` (`git ls-tree -r <ref>`): identical blob = stale never-pruned leftover; differing = a real divergence that was captured.

Investigation tip — when the CI logs for an old committed `.sql.other` have expired, look for the same or similar artifacts in the unmerged `linq2db.baselines/baselines/pr_*` branches; those carry fresh CI logs (and the per-job commit message — e.g. `[Linux / Oracle 21c] baselines` — names the exact job that produced it).
