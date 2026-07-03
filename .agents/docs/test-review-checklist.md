## Test review checklist

Specific test-quality traps that the abstract "is there a test? does it cover the edge cases?" framing in [`code-reviewer.md`](../agents/code-reviewer.md) rule 6 misses in practice. Apply every item below to every test added or modified by a PR — most apply only when the test was added specifically as a regression test for the PR's fix.

### Substring SQL assertion must reject the bug, not just accept the fix

When a new/modified test calls `LastQuery.ShouldContain("X")` / `Assert.That(LastQuery, Does.Contain("X"))` / similar, mentally compute the **buggy SQL** (what was emitted before this PR's fix) and check whether the assertion *also* passes against that buggy form. If yes, the assertion is too weak:

- `Contains.Substring("FINAL")` passes against the buggy `mtFINAL` (no space) — should be `" FINAL"` or `"FINAL "`.
- `Does.Contain("now()")` passes against the buggy `timezone('UTC', now())` — should be `"timezone('UTC'"` or `Does.Not.Contain("timezone(")`.

Tighten the substring, anchor it (`StartsWith` / `EndsWith`), or invert (`Should.NotContain(<buggy-substring>)`). Flag as MAJ when the test was added specifically to prevent the regression this PR fixes — the test isn't doing its job.

### `LastQuery` capture point

`DataConnection.LastQuery` updates on every `BeforeExecute`, including auxiliary `CreateLocalTable` / `AssertQuery` calls that materialize sources to memory or roundtrip data. If the test executes the target query *after* `AssertQuery` (or any helper that runs its own query), `LastQuery` will hold the last auxiliary query, not the target. The fix is to capture before the auxiliary call or to use a code path that runs only the target query (e.g. `.ToArray()` directly without `AssertQuery`).

### `context.IsAnyOf(ProviderName.X)` against versioned context strings

`ProviderName.MySql` etc. are bare names; the actual test context strings are versioned (`MySql.5.7`, `PostgreSQL.16`, `SQLite.Classic`, `ClickHouse.Octonica`). `IsAnyOf` is exact-match, so a check like `context.IsAnyOf(ProviderName.MySql)` will never fire — the test always falls through to the else branch silently. Use the `TestProvName.All<Provider>` aggregates instead (e.g. `TestProvName.AllMySql`), or list the specific versioned context names. Flag at the comparison site.

### Coverage matches the issue's reported provider

Cross-check the linked issue's reported provider against the test's `[DataSources(...)]` / `[IncludeDataSources(...)]` / `[ExcludeDataSources(...)]` filter. If the issue is reported for provider P and P is excluded (or not included), flag as MAJ regardless of whether the test passes in CI — the regression test isn't covering the regression's actual platform. Same applies when the issue's repro pattern (e.g. `OrderBy + GroupBy` or `Sum + nullable`) is missing from the test method body.

### Test member matches its name

When the test is named `TrimEndN` / `OrderByDesc` / `XyzAsync`, the LINQ projection inside should actually call `TrimEnd` / `OrderByDescending` / the `Async` variant. Mismatches make the test pass while not exercising the new path. Read the test body and confirm the method-under-test name appears at least once.

### `[DataSources]` parameter on regression tests

A regression test for a cross-provider bug needs a `[DataSources]` (or `[IncludeDataSources(...)]`) parameter, otherwise it runs against the default provider only and misses the providers where the bug actually manifests. New tests using `GetDataConnection()` / `GetDataContext()` without a `context` parameter are a red flag — read the surrounding fixture for the convention.

### Time-based assertions / DB-server-vs-runner timezone

`Sql.GetDate()` / server-side `NOW()` / `CURRENT_TIMESTAMP` returns the DB server's local time, which may differ from the runner's `DateTime.Now` by hours when the DB runs in Docker or on a remote host. Assertions on:

- the wall-clock difference between server now and `DateTime.Now` (`Math.Abs((sqlNow - DateTime.Now).TotalSeconds) < N`)
- `result.Offset == DateTime.Now.Offset` for a `DateTimeOffset` round-tripped through the server
- equality of a stripped-TZ value to the original UTC value

are flaky in those setups. Flag and propose either a server-side-only comparison (`server local vs server UTC in one query`) or restriction to providers / contexts where the timezone is guaranteed to match.

### Identifier-length limits for `CreateLocalTable("name-with-guid")`

Firebird v3 caps identifiers at 31 characters; Oracle at 30 / 128 depending on version; SQL Server at 128. A test-name + GUID combination longer than the smallest provider's limit will fail at table creation on that provider. Either use `TestUtils.GetNext()` for short unique suffixes or let `CreateLocalTable` generate the name (no explicit name argument).

### `query.ToSqlQuery()` vs the SQL of an *aggregate* call

`IQueryable.ToSqlQuery()` returns the SQL of the non-terminal sequence — it doesn't include the terminal aggregate's wrapping. To assert SQL emitted by `query.Sum()` / `query.Count()` / `query.Min()`, capture from `db.LastQuery` *after* the terminal aggregate call, not via `query.ToSqlQuery()`. The latter will assert against the wrong SQL and pass for the wrong reason.

### `[TestFixture]` doubled on a partial class

A `[TestFixture]` attribute on the same partial class declared in two files runs the fixture's tests twice (or NUnit complains, depending on version). When the diff adds a new partial of an existing fixture, the new file should not re-declare `[TestFixture]`. Flag at the new attribute.

### Async test pattern

A test of an `Async` method that doesn't `await` the call (or doesn't return `Task` / `ValueTask`) exercises the synchronous fallback, not the async path. When the PR fixes an async-specific issue, confirm the test actually awaits.

### Inline per-provider gating where a FeatureSource attribute fits

When tests gate provider support *inline* — repeated `[DataSources(false, ProviderName.X, …)]` / `[ExcludeDataSources(...)]` skip-lists, or `context.IsAnyOf(...)` branches — scattered across many tests for the **same** feature, check whether `Tests/Base/Attributes/FeatureSources/` already has (or warrants) a feature-based context-source attribute for it: `SupportsAnalyticFunctionsContextAttribute` (window / analytic functions), `MergeDataContextSourceAttribute`, `CteContextSourceAttribute`, `RecursiveCteContextSourceAttribute`, `AllJoinsSourceAttribute`, and peers. Repeating the provider skip-list per test is the trap — it drifts as providers gain support, and it buries which providers genuinely lack the feature behind boilerplate. Flag `MIN` and point at the matching feature-source attribute; when none fits, suggest introducing one alongside the existing family rather than inlining the gate. (Surfaced on PR #5468 — window-function tests inline-gated providers instead of using a feature-source attribute, despite `SupportsAnalyticFunctionsContextAttribute` already existing.)

### Numeric assertions on sample statistical aggregates (STDDEV / VARIANCE) must relax the single-row window

When a test asserts the *value* of a sample stddev/variance window aggregate (`Sql.Window.StdDev`/`Variance`/`StdDevSamp`/`VarSamp`) across providers, the **single-row-window** result is engine-defined and not part of the sample-vs-population contract: it comes back NULL on PostgreSQL/DuckDB, `0` on Oracle/MySQL/SAP HANA/Informix, and `NaN` on ClickHouse. Asserting a fixed value (e.g. NULL) for the single-row case produces **false failures that fail fast on the first row and mask the real multi-row divergence** the test is meant to catch. Relax the n=1 case (accept null / ~0 / NaN) and assert strictly only for windows of **≥2 rows**, where sample (÷ n−1) and population (÷ n) genuinely differ — that is what discriminates a provider silently returning the population statistic for the documented-sample API. (Surfaced on PR #5468: an n=1-strict assertion flagged Oracle/SAP HANA as wrong when only their single-row convention differed; relaxing n=1 cleared them and pinned the real population bug to MySQL/MariaDB/DB2/Informix.)

### Execution-only test (`ToList()` / `_ =`) can't catch a wrong result

A test that only materializes the query (`_ = query.ToList()`, `.ToArray()` with no assertion) proves the SQL *executes*, not that the result is *correct*. When the feature under test has observable runtime semantics — row ordering, NULLS FIRST/LAST placement, rank/dense-rank values, aggregate values, filtered/partitioned counts — the test passes even when a provider's emulation produces the wrong rows. Flag `MAJ` when the test was added for a feature whose correctness is the point (a wrong emulation goes undetected), `MIN` otherwise; propose asserting the expected values against the materialized result, and confirm the seed data actually exercises the edge (e.g. a NULL in the ordering key for a NULLS-placement test). (Surfaced on PR #5468: the `*WithNulls` window tests set a `Sql.NullsPosition` and had a NULL-key seed row but only called `ToList()`, so a wrong per-provider NULLS emulation would not fail — result assertions were added and confirmed correct across ClickHouse/DuckDB/YDB/SQLite.)

### NUnit `Assert.*` in a new/modified test → prefer Shouldly

The repo standardizes on Shouldly for assertions (`AGENTS.md` → Tests; "Use **Shouldly**, not NUnit `Assert`"). Flag any new or modified test that uses `Assert.That` / `Assert.AreEqual` / `Assert.IsTrue` / `Assert.IsFalse` / etc. as `MIN`, and propose the Shouldly equivalent (`ShouldBe` / `ShouldBeFalse` / `ShouldBeTrue` / `ShouldContain` / …) plus `using Shouldly;` if the file doesn't already import it. Mixing styles in new coverage is the trap — it reads inconsistently and loses Shouldly's clearer failure messages. (Surfaced on PR #5468: `WindowFunctionsTests.RowNumber`/`Equality` shipped `Assert.That` and only an external bot flagged it; this checklist had no rule.)

---

Apply this checklist whenever rule 6 of `code-reviewer.md` fires — i.e. on any test file added or modified by the PR. Findings from this checklist go in the regular `findings[]` stream with severity per the per-item guidance (most are MAJ when the test fails to exercise its named purpose, MIN otherwise).
