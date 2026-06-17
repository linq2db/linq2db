# Running Tests

Tests use **NUnit3** with **Shouldly** assertions (not NUnit Assert), running on **Microsoft.Testing.Platform** (MTP) — selected via `global.json` — not VSTest. Test targets: `net462`, `net8.0`, `net9.0`, `net10.0`. **Always use Debug configuration and prefer `net10.0`** — Release enables Roslyn analyzers and is much slower to build. The full test suite takes ~1 hour or more; avoid running it unless necessary.

**Don't prepend a `dotnet build`.** `dotnet test` builds the project automatically if binaries are stale; a standalone `dotnet build` right before `dotnet test` only adds waiting time. If a test run fails with compile errors, fix them and re-run `dotnet test` directly.

Under MTP, `dotnet test` takes the project via `--project` (a solution/filter via `--solution`) — the bare `dotnet test <project>` form is rejected. Always pass `--settings .runsettings` so NUnit honors `AssemblySelectLimit`; otherwise a broad `--filter` can fall back to running the whole assembly.

```bash
# Run a single test class or method
dotnet test --project Tests/Linq/Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net10.0 --settings .runsettings

# Run tests with the lightweight playground solution (faster load)
dotnet test --solution linq2db.playground.slnf --settings .runsettings
```

**Default to `Tests.Playground` for any iterative test run** — fresh tests, fix-verification on existing tests, ad-hoc repro. The full `Tests/Linq/Tests.csproj` build is ~3+ minutes; the playground project is ~30s. Two distinct shapes:

1. **Scratch verification** — one-off tests that won't be committed. Edit `Tests/Tests.Playground/TestTemplate.cs` directly with a self-contained fixture (define converters / tables / data inline). No `<Compile Include>` needed; the SDK-style csproj implicitly compiles the project's own `*.cs` files. Revert when done — the template-edit is scratch (per `agent-rules.md` → *Never commit playground scratch*).
2. **Iterating on a real test in `Tests/Linq/`** — add `<Compile Include="..\Linq\<sub>\<File>.cs" Link="<File>.cs" />` to `Tests/Tests.Playground/Tests.Playground.csproj`. The link is local scratch and must **not** be committed (same rule). Use this shape when iterating on a test that already lives in `Tests/Linq/` (e.g. a regression test you just wrote alongside a fix) without paying the cost of the full `Tests/Linq/Tests.csproj` multi-TFM build.

```bash
dotnet test --project Tests/Tests.Playground/Tests.Playground.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net10.0 --settings .runsettings
```

Reach for the full `Tests/Linq/Tests.csproj` only when the test target spans many files that would require a wide playground link, or when running a broad filter (e.g. an entire test class).

## Monitoring a long run

A full suite run takes 1–2 hours. To watch progress without scraping console output, the test assembly writes a small JSON heartbeat (updated ~once/sec, immediately on each failure) that you can `Read` at any time. It's **opt-in** via the `--test-progress` command-line option; the reporter is a no-op when the option is absent, so default runs are unaffected.

**Always pass `--test-progress` when launching a run** — via `test-runner`, `/test`, or ad-hoc — so progress is observable. It's a normal CLI option (no env var, no session-wide toggle):

```bash
dotnet test --project Tests/Linq/Tests.csproj -f net10.0 --settings .runsettings --filter "..." --provider SQLite.MS --test-progress
```

A bare `--test-progress` writes to `.build/.claude/test-progress.<tfm>.<pid>.json` (one per TFM/process); pass `--test-progress <dir|*.json>` to redirect it. Fields: `done`, `total`, `completed`, `passed`, `failed`, `skipped`, `currentTest`, `elapsedSec`, `testsPerSec`, `etaSec`, and `recentFailures[]`.

Read the heartbeat directly, or with the summary helper (the `/test-progress` skill reports the latest run):

```bash
pwsh -NoProfile -File .claude/scripts/test-status.ps1          # newest run, one-line summary
pwsh -NoProfile -File .claude/scripts/test-status.ps1 -Raw     # dump the raw JSON
```

**Caveat:** under a `--filter`, `total` reflects the *discovered* (pre-filter) test count, so it over-counts and `etaSec` is unreliable; `total`/`etaSec` are exact only for full, unfiltered runs (the case this is built for). `completed` and `currentTest` are always accurate.

## Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json` (or run `/test-providers reset`, which does the same thing under explicit confirmation and writes a backup of any existing file).
2. This gives you SQLite-based testing out of the box.
3. Add connection strings for other databases as needed.

After the file exists, `/test-providers` is the supported way to enable / disable providers per TFM bucket and to start the docker containers behind them. `/test` reads the resulting state but never edits it — see [`.claude/skills/test-providers/SKILL.md`](../skills/test-providers/SKILL.md).

**For a single run, prefer `--provider` over editing the `Providers` array** — it runs exactly the named providers (any with a connection string defined) without touching the file; see *Scoping a run to specific providers* below. Editing `UserDataProviders.json` is for changing the **default** set (used when no `--provider` is passed), connection strings, and `BaselinesPath`.

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

## Scoping a run to specific providers (`--provider`)

The test executables accept a `--provider` command-line option that runs exactly the named provider(s), **replacing** the providers configured in `UserDataProviders.json`. It is repeatable and accepts comma- or space-separated values:

```bash
dotnet test --project Tests/Linq/Tests.csproj -f net10.0 --settings .runsettings --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~MyTest" --provider PostgreSQL.18 --test-progress
```

`--provider` replaces the active main-test provider set (`TestConfiguration.UserProviders`) with exactly the names given, so **any provider with a connection string defined in `DataProviders.json` / `UserDataProviders.json` runs without editing the enabled-providers list** — it need not be enabled. (EF Core test runs instead intersect their curated `EFProviders` list, so `--provider` can only narrow EF runs to supported providers, never add an unsupported one.) Connection strings still come from those files; a `--provider` name with no connection logs a warning and fails to connect. An absent option leaves behavior unchanged (the providers enabled in `UserDataProviders.json` run).

### Running providers in parallel

Each provider config maps to a distinct database, so runs scoped to **distinct** providers don't collide and can run concurrently. Two cautions, both from the standing single-session rule:

- **One distinct provider (= one database) per parallel run.** The same provider on two concurrent runs — including the *same* provider across two TFMs — hits the same database and corrupts state. The parallel unit is the database, not the process.
- **Build once, and don't let the runs build concurrently.** Two `dotnet test` invocations on the same project race to write and lock the shared `linq2db.Tests.dll` (fails with `MSB3027`). Build once, then launch each run as the **test executable directly** — it never builds, and each process writes its own per-PID heartbeat:

```bash
dotnet build Tests/Linq/Tests.csproj -c Debug -f net10.0
```
```bash
.build/bin/Tests/Debug/net10.0/linq2db.Tests.exe --provider PostgreSQL.18 --test-progress --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~MyTest"
```
```bash
.build/bin/Tests/Debug/net10.0/linq2db.Tests.exe --provider MySql.8.0 --test-progress --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~MyTest"
```

Each process writes its own `.build/.claude/test-progress.<tfm>.<pid>.json`; poll them with `/test-progress` or `test-status.ps1`. The runner's native filter flag is `--filter` (the same `FullyQualifiedName~` syntax), confirmed via `--help`.

## Database initialization

`Tests.Linq.Create.CreateData.CreateDatabase` is attributed `[Test, Order(0)]` — NUnit runs it first in the session, and every downstream test that calls `GetDataContext(context)` relies on it having created + populated the provider's schema. After a container restart, a fresh clone, or an aborted previous run, the schema may not be current.

**Always include `CreateDatabase` in your `--filter`.** Prepend `FullyQualifiedName~CreateData.CreateDatabase|` to any filter you construct — the test is idempotent, re-running it is cheap, and it removes the "empty database" failure mode entirely.

```bash
dotnet test --project Tests/Linq/Tests.csproj -f net10.0 --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~FirebirdTests.DropTableTest" --settings .runsettings
```

If your test modifies data, revert changes to avoid side effects in downstream tests.

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

## Cross-provider gotchas for new tests

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

## Reading test output

Always read the **full** test run log — not just the tail. NUnit and `dotnet test` interleave relevant information across the log: `_CreateData` failures appear near the top, setup exceptions and warnings can come well before the failing assertion, and stack traces may be truncated if you jump to the end. Do not use `tail`, `head`, `head_limit: 1`, or similar tricks to skim the output; read the entire log and scroll back for context when a failure is surprising. The only exception is when you have already read the log once and are fetching a specific slice you've already identified.

## Debugging linq2db translators

When debugging query translation or provider issues,
use `Console.WriteLine` to output intermediate values or SQL fragments.
Do not introduce logging dependencies.

## Baselines

Many tests compare emitted SQL against a stored baseline file. Baselines live **outside the main repo** under the path configured by `BaselinesPath` in `UserDataProviders.json` → `MyConnectionStrings`. With `BaselinesPath` set, a mismatched test overwrites the baseline with the new SQL; subsequent runs compare against the updated file. With `BaselinesPath` unset, baselines are neither written nor compared.

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

`.sql.other` is therefore a **failure indicator**, not an alternate-acceptable form. When you see one in a baselines diff, treat it as a real test failure that needs investigation — but check master first: 4 known pre-existing entries on Oracle stem from a long-standing test-framework limitation tracked as #5513 (remote-mode trace drops scalar `IQueryable<T>` aggregate queries). New PR-introduced `.sql.other` entries warrant root-cause analysis.
