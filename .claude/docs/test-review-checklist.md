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

---

Apply this checklist whenever rule 6 of `code-reviewer.md` fires — i.e. on any test file added or modified by the PR. Findings from this checklist go in the regular `findings[]` stream with severity per the per-item guidance (most are MAJ when the test fails to exercise its named purpose, MIN otherwise).
