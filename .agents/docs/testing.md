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

**Targeted repro in a worktree — run the MTP exe directly.** `dotnet test --project <path>` can fall back to the legacy VSTest MSBuild target (error: `MSB1001: Unknown switch … --project … --target:VSTest`) when the new-`dotnet test` opt-in isn't resolved for that invocation — e.g. a relative `--project` pointing into a `git worktree` from another checkout's cwd. For a focused run, skip `dotnet test` and invoke the built test executable directly with MTP options:

```bash
.build/bin/Tests/Testing/net10.0/linq2db.Tests.exe --provider Firebird.5 --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~UpdateFromSubqueryRowFlattened"
```

The `--provider <Name>` flag (repeatable, comma- or space-separated) **replaces** the providers configured in `UserDataProviders.json` for that run — no file edit needed, as long as the provider's connection string is defined in the file. The harness locates `UserDataProviders.json` by walking up the directory tree from the assembly path, so a copy at the worktree root is picked up. Build the project first (`dotnet build <proj> -c Testing -f net10.0`) since you're bypassing `dotnet test`'s implicit build.

**Verifying an `[ActiveIssue]` gate — filter by fixture, not test name.** A `--filter "Name~<Test>"` (or any filter that names the specific test) makes NUnit *explicitly select* it, which **forces `RunState.Explicit` / `[ActiveIssue]` tests to run** — so a freshly-gated test will appear to "still fail." To confirm a per-provider `[ActiveIssue(Configuration=…)]` gate actually skips, filter by the **fixture** (`FullyQualifiedName~<Fixture>`); the fixture filter respects the ActiveIssue category exclusion and the gated case drops out of the run count.

**`[ActiveIssue]` is `AllowMultiple=false`.** You cannot stack two `[ActiveIssue]` attributes on one test. To add a provider to a test that already carries one (e.g. an existing Sybase gate), **extend the existing attribute's `Configurations`** and fold both providers' reasons into `Details` — don't add a second attribute. Single provider → `Configuration = TestProvName.AllX`; multiple → `Configurations = new[] { ProviderName.Sybase, TestProvName.AllYdb }`. (The `Configuration`/`Configurations` setters split on commas, so either form accepts grouped names.)

**Instrumenting engine code to trace a divergence.** To find where the SQL build diverges (e.g. a node dropped only on the remote path), add temporary `System.Console.Error.WriteLine("YDBINST:…")` traces at suspect visitor / builder methods, run the targeted test, `grep` the captured output for the marker, then `git restore <source-files>` to revert (confirm `git status -- Source/ Tests/` is clean before committing). **Do not filter SQL-AST nodes by `ToString()` content** (`expr.ToString()?.Contains("GetLength")`) — it does not reliably contain the function/identifier name and silently matches nothing; filter on **structural properties** instead (node type, `IsMandatory`, `ToType`, `cast.Expression is SqlFunction { Name: … }`).

**Capture caveat — `Console`/`TestContext.Progress` output is only captured from a test's *body*.** Traces emitted from `[OneTimeSetUp]`/`[OneTimeTearDown]`, a custom `IWorkItemDispatcher`, the LinqService server, or any other non-test thread are **not** reliably surfaced by the console logger — they can come back empty and mislead you into a wrong "this code never ran" conclusion. For those, write the diagnostic to a **file** (e.g. under `AppContext.BaseDirectory`) and `Read` it back. (For live *run progress* — current test, completed/total, pass/fail — rather than engine tracing, use the `LINQ2DB_TEST_PROGRESS` heartbeat instead; see *Monitoring a long run* below.)

## BUGCHECK-gated tests

Test fixtures wrapped in `#if BUGCHECK` (internal-invariant unit tests that drive `#if BUGCHECK`-gated test hooks on the core library — e.g. `QueryCacheEvictionTests` driving `QueryCache.RunSweepNow` / `BucketCount`) only compile in **Debug / Testing / Azure** configurations. `BUGCHECK` is defined in `Source/LinqToDB/LinqToDB.csproj` and mirrored into `Tests/Directory.Build.props` so the test project sees the same symbol. Consequences:

- Run them with `-c Testing` (net10.0-only, fast) or `-c Debug`. A **Release** run compiles the fixture to nothing — `--filter` matches zero tests, not a pass.
- The core library must also be built in a BUGCHECK config for the gated test hooks to exist; `-c Testing` covers both.

## Monitoring a long run

A full suite run takes 1–2 hours. To watch progress without scraping console output, the test assembly writes a small JSON heartbeat (updated ~once/sec, immediately on each failure) that you can `Read` at any time. It's **opt-in** via the `--test-progress` command-line option; the reporter is a no-op when the option is absent, so default runs are unaffected.

**Always pass `--test-progress` when launching a run** — via `test-runner`, `/test`, or ad-hoc — so progress is observable. It's a normal CLI option (no env var, no session-wide toggle):

```bash
dotnet test --project Tests/Linq/Tests.csproj -f net10.0 --settings .runsettings --filter "..." --provider SQLite.MS --test-progress
```

A bare `--test-progress` writes to `.build/.agents/test-progress.<tfm>.<pid>.json` (one per TFM/process); pass `--test-progress <dir|*.json>` to redirect it. Fields: `done`, `total`, `completed`, `passed`, `failed`, `skipped`, `currentTest`, `elapsedSec`, `testsPerSec`, `etaSec`, and `recentFailures[]`.

Read the heartbeat directly, or with the summary helper (the `/test-progress` skill reports the latest run):

```bash
pwsh -NoProfile -File .agents/scripts/test-status.ps1          # newest run, one-line summary
pwsh -NoProfile -File .agents/scripts/test-status.ps1 -Raw     # dump the raw JSON
```

**Caveat:** under a `--filter`, `total` reflects the *discovered* (pre-filter) test count, so it over-counts and `etaSec` is unreliable; `total`/`etaSec` are exact only for full, unfiltered runs (the case this is built for). `completed` and `currentTest` are always accurate.

## Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json` (or run `/test-providers reset`, which does the same thing under explicit confirmation and writes a backup of any existing file).
2. This gives you SQLite-based testing out of the box.
3. Add connection strings for other databases as needed.

After the file exists, `/test-providers` is the supported way to enable / disable providers per TFM bucket and to start the docker containers behind them. `/test` reads the resulting state but never edits it — see [`.agents/skills/test-providers/SKILL.md`](../skills/test-providers/SKILL.md).

**For a single run, prefer `--provider` over editing the `Providers` array** — it runs exactly the named providers (any with a connection string defined) without touching the file; see *Scoping a run to specific providers* below. Editing `UserDataProviders.json` is for changing the **default** set (used when no `--provider` is passed), connection strings, and `BaselinesPath`.

**Ask before editing `UserDataProviders.json`.** Before the **first** edit in a session, ask the user whether to stash/back up the current file. The file is gitignored and holds the user's connection strings + enabled-provider flags — an incorrect edit has no git history to recover from. Subsequent edits in the same session don't need to re-prompt.

### `Providers` is keyed by TFM

`UserDataProviders.json` has a top-level section per target framework (`NETFX`, `NETBASE`, `NET80`, `NET90`, `NET100`), each with its own `Providers` array. To enable a provider for a test run, uncomment it in the section matching your `-f` flag — `NETFX` only affects `net462` runs, `NET100` only affects `net10.0` runs, etc. Editing the wrong section silently does nothing.

### Container-backed providers

Most non-file providers (SAP HANA, most Oracle versions, PostgreSQL, MySQL, DB2, Informix, ClickHouse, SQL Server 2017+) connect to local docker containers. Start the needed container(s) before running tests — e.g. `docker start hana2 oracle11`. SAP HANA in particular takes several minutes after container start before its internal HDB database finishes warm-up; tests that run immediately after `docker start hana2` may hit "connection refused".

File-based providers (SQLite, SqlCe, Access) don't need a container.

**Not netfx-only.** SqlCe and Access — despite being "legacy" providers — run on *every* test TFM (`net462` + `net8.0`/`net9.0`/`net10.0`), enabled in the matching `Providers` bucket. Run their suites on `net10.0` (fast single-TFM), not `net462`. (A bare `pwsh 7` host can't load SqlCe's native engine for ad-hoc probes — see [`windows-gotchas.md`](windows-gotchas.md) — but the test process loads it fine on any TFM.)

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

**Multiple providers: comma- (or space-) separate them** — `--provider Firebird.5,PostgreSQL.18` (or `--provider Firebird.5 PostgreSQL.18`) runs both.

**To exercise the remote (LinqService) path, pass the BASE provider name, not the `.LinqService` suffix.** `DataSourcesBaseAttribute` auto-appends a `<provider>.LinqService` case for every base provider (unless `.runsettings` sets `DisableRemoteContext` / `NoLinqService`), so `--provider Firebird.5` runs **both** the direct `Firebird.5` and the remote `Firebird.5.LinqService` cases. Passing `--provider Firebird.5.LinqService` double-suffixes it to `Firebird.5.LinqService.LinqService`, which has no connection string → those cases silently don't run (you'll see only `CreateDatabase` execute). This is also why `test-runner`, given a `.LinqService` provider name, comes back green-but-empty.

### Running EF Core tests locally

The `/test` skill targets the Playground / Linq projects only — the EF Core integration tests are separate and run by hand.

- **Per-EF-version projects.** `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF10.csproj` (plus `EF9` / `EF8` for EF Core 9 / 8, and `EF3` for EF Core 3.1 — netfx-only). Build the one you need: `dotnet build Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF10.csproj -c Debug`.
- **MTP exe, run directly.** These are `OutputType Exe` (Microsoft.Testing.Platform + NUnit runner), so run the built executable, not `dotnet test`: `.build/bin/Tests.EntityFrameworkCore.EF10/Debug/net10.0/linq2db.EntityFrameworkCore.Tests.exe --filter "FullyQualifiedName~MyTest"`. Filter with `--filter` (VSTest-style `FullyQualifiedName~` / `Name~`); the NUnit MTP runner rejects `--treenode-filter`.
- **SQLite needs no container.** `--provider SQLite.MS` runs the SQLite-capable EF tests against a file / `:memory:` DB created via EF `EnsureCreated` — no docker. Other EF providers need their DBs like the main suite. (EF projects intersect their curated `EFProviders` set, so `--provider` only narrows.)
- **EF SQL Server needs the `.MS` variant.** `EFProviders` (`TestConfiguration.cs`) is a curated set — `SQLiteMS`, `AllSqlServer2016PlusMS` (Microsoft.Data.SqlClient only), `AllPostgreSQL13Plus`, `AllMySqlConnector`. `[EFDataSources]` **intersects** `--provider` with it, so a non-`.MS` SQL Server id (`SqlServer.2022`, System.Data.SqlClient) is dropped and the run reports **0 cases as a pass** — a false green, not an error. Use `SqlServer.2022.MS`. A pool-leak repro especially needs a *pooled server* provider (SQLite has no hard pool cap), so this trap can hide the very bug under test.
- **Suppress baseline capture for loop / stress tests.** `using var _ = new DisableBaseline("reason");` (`Tests/Base/ScopedSettings.cs`) turns off SQL-baseline recording for its scope. Use it for a test that runs the same statement many times (e.g. a 300× connection-leak loop) — otherwise every iteration is captured, bloating the baseline by thousands of identical lines per provider/EF combo.
- **In a worktree, use absolute paths.** The Bash tool's cwd is always the *primary* clone, and `cd <worktree> && dotnet …` is hook-rejected (no `&&`). Pass the absolute worktree csproj / exe path to `dotnet build` / the test exe instead.

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

Each process writes its own `.build/.agents/test-progress.<tfm>.<pid>.json`; poll them with `/test-progress` or `test-status.ps1`. The runner's native filter flag is `--filter` (the same `FullyQualifiedName~` syntax), confirmed via `--help`.

**Firebird temp-table teardown must scope its pool clear — never `FirebirdTools.ClearAllPools()`.** Firebird needs the pooled connection evicted after a temp-table drop (its lingering metadata reference otherwise fails the next DDL with `object TABLE … is in use` / `lock conflict on no wait transaction`). But `ClearAllPools()` is **process-wide** across every connection string, so under parallel execution it tears down the connections other concurrently-running Firebird versions (2.5/3/4/5, each a distinct connection string) are actively using. Use the scoped `FirebirdTools.ClearPool(connection)` (direct path) or `ClearPool(connectionString)` (remote/LinqService path, where the context isn't a `DataConnection` — resolve via `GetConnectionString(config.StripRemote())`); `TestUtils.ClearFirebirdPool` is the shared helper. (PR #5689.)

## Database initialization

`Tests.Linq.Create.CreateData.CreateDatabase` is attributed `[Test, Order(0)]` — NUnit runs it first in the session, and every downstream test that calls `GetDataContext(context)` relies on it having created + populated the provider's schema. After a container restart, a fresh clone, or an aborted previous run, the schema may not be current.

**Always include `CreateDatabase` in your `--filter`.** Prepend `FullyQualifiedName~CreateData.CreateDatabase|` to any filter you construct — the test is idempotent, re-running it is cheap, and it removes the "empty database" failure mode entirely.

**Derive the `FullyQualifiedName~` filter from the test's `namespace`, not its folder path — and sanity-check the run's test count.** linq2db test namespaces don't mirror the directory tree: `Tests/Linq/Linq/QueryGenerationTests.cs` is `namespace Tests.Linq` (not `Tests.Linq.Linq`), `Tests/Linq/Update/MergeTests.*.cs` is `Tests.xUpdate`. A filter built from the folder (`FullyQualifiedName~Tests.Linq.Linq.QueryGenerationTests`) matches **zero** real tests, so the run is **vacuously green** — only the prepended `CreateDatabase` cases execute (e.g. `total: 3`, "Passed!"). Always `Grep` the file's `^namespace` line to build the filter, and after the run confirm `total` is in the expected ballpark — a suspiciously small `total` on a "passed" run is the tell. (Surfaced verifying #5599/#5169 ungatings: a `Tests.Linq.Linq.*` filter ran 3 tests and reported green before the count exposed it.)

```bash
dotnet test --project Tests/Linq/Tests.csproj -f net10.0 --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~FirebirdTests.DropTableTest" --settings .runsettings
```

If your test modifies data, revert changes to avoid side effects in downstream tests.

**A filtered run that skips `CreateDatabase` can show failures that masquerade as pre-existing / unrelated.** A prior data-mutating test can leave `TestData*.sqlite` with corrupted rows; a later filtered run then reads bad data and fails on tests that have nothing to do with your change. **Stash-and-rerun does not disprove this** — both runs share the same corrupted file, so they fail identically and the comparison looks like "pre-existing". Before concluding "these failures pre-date my change / are unrelated", re-run with `FullyQualifiedName~CreateData.CreateDatabase|` prepended to rebuild the schema. This applies especially when working in a `git worktree`: `/test` targets the main checkout, not the worktree branch, so you run `dotnet test` directly in the worktree and must prepend `CreateDatabase` yourself.

## A test that fails only on NETFX (net462) jobs

When CI shows a test failing on the `Tests (NETFX x64)` jobs while the same test passes on the .NET 8/9/10 tasks of the same provider, suspect a **runtime driver divergence**, not a TFM-agnostic code bug. ADO.NET providers can behave differently across runtimes — e.g. `SqlDataReader.GetName` returns duplicate empty names for unnamed result columns (`select 1, 1`) on .NET Framework but distinct names on modern .NET (#5659).

The `Testing` config is net10.0-only and a net10.0 run **won't reproduce** such a failure. Reproduce on net462 directly:

```bash
dotnet build Tests/Linq/Tests.csproj -c Debug -f net462
```
```bash
.build/bin/Tests/Debug/net462/linq2db.Tests.exe --provider SqlServer.2022 --filter "FullyQualifiedName~CreateData.CreateDatabase|FullyQualifiedName~<Fixture>.<Test>" --settings .runsettings
```

`--provider` supplies the connection string from `DataProviders.json` without editing the enabled-providers list; the net462 exe runs x86. This is the runtime sibling of the compile-time `agent-rules.md` → *TFM API availability* rule.

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
- **Feature-gated test attributes use *exclude* filters, not include whitelists.** An attribute that scopes a run to providers supporting some feature (the `FeatureSources/` attributes, e.g. `SupportsAnalyticFunctionsContext`) should subclass `DataSourcesAttribute` with an `Unsupported` exclude list — like `InsertOrUpdateDataSourcesAttribute` — **not** an `IncludeDataSourcesAttribute` whitelist. Exclude-based means a newly-added provider context is covered automatically (only the genuinely-unsupported providers are listed); a whitelist silently omits every new context until someone remembers to add it. Reuse / convert an existing feature attribute rather than adding a parallel one, and keep per-feature gaps (a provider that supports the feature family but not one function) as `[ThrowsForProvider]` on the individual test, not in the attribute.
- **EF Core tests only run the `.MS` (Microsoft.Data.SqlClient) SQL Server variants.** `EFIncludeDataSources` / `EFDataSources` intersect the requested providers with `TestConfiguration.EFProviders`, whose SQL Server entry is `AllSqlServer2016PlusMS` — Microsoft.Data.SqlClient only. Passing a `System.Data.SqlClient` name (`SqlServer.2022`) to an EF test — including `test-runner`'s `--provider` — resolves to **zero** variants and reports `none_matched` (not an error), so it silently looks like the test "passed" nothing. Use the `.MS` name (`SqlServer.2022.MS`). The full EF provider scope is `SQLiteMS` + `AllPostgreSQL13Plus` + `AllSqlServer2016PlusMS` (+ `AllMySqlConnector` off net10) — narrower than the plain-linq2db set. (`TestConfiguration.cs` → `EFProviders`.)

## F# tests

F# regression tests live in `Tests/FSharp/*.fs` (compiled into `Tests.FSharp.fsproj`) and are driven from C# fixtures in `Tests/Linq/Linq/FSharpTests.cs`.

- **Call `nonNull` on nullable-returning members before instance methods.** The F# project has nullness enabled, so a member typed `string | null` (e.g. `DataConnection.LastQuery`) trips `FS3261` ("nullness warning … do not have compatible nullability") — a **warning-as-error** — the moment you call an instance method on it (`.Substring`, `.IndexOf`, …). Bind through the F# 9 `nonNull` helper first: `let sql = nonNull db.LastQuery`. Passing the raw value to an API that accepts `obj`/`object` (e.g. an NUnit `Assert.That(x, constraint)`) does **not** trip it — only instance-member access does. (`nonNull` is the established idiom — `FSharpQueryExpressionInterceptor.fs` uses it.) This is F#-compiler-specific, so the fast `Testing` (net10.0) build **does** catch it — unlike the `net462`/`netstandard2.0` BCL-availability traps.
- **`task { … }` returns `Task<unit>`; upcast to `Task`** with `:> System.Threading.Tasks.Task` when a C# fixture awaits a non-generic `Task`.

## Tests that pass but catch nothing

A test that compiles and goes green is not yet a regression guard — it has to be able to go **red** on the bug it targets. The recurring ways an AI-drafted test silently guards nothing:

- **Happy-path only.** Asserts the success case and nothing else; the edge that actually broke (null, empty, overflow, negative, boundary, provider-specific fold) goes untested. A regression test must exercise the *failing* input, not a neighbouring working one.
- **Brittle string assertions.** `ShouldBe("…literal SQL…")` / `ShouldContain("…text…")` locked onto incidental wording — churn mass-fails them while the real logic regresses undetected. Assert behavior (row content, shape, count), not a string. The SQL-shape variant is covered in detail under *When a substring assertion is vacuous* below.
- **Mocking the logic away.** Stubbing the component under test so the assertion verifies the mock, not linq2db. Here that shows up as asserting against a hand-built expected object instead of round-tripping through `AssertQuery` / a real `CreateLocalTable<T>`.
- **Mirroring the implementation.** Deriving the expected value by re-running the same code path under test — or reading the current emitted SQL and pasting it back as the baseline — bakes the current behavior, bugs included, in as "correct".
- **Trivially-true assertions.** `result.ShouldNotBeNull()`, `count.ShouldBeGreaterThanOrEqualTo(0)`, asserting only that the query *ran* — green regardless of whether the targeted logic is correct. Line coverage built from assertions that can't fail is no coverage at all. Assert the specific value / shape / count the fix changes, not that *something* came back.
- **Generation-only, never executed.** A test that only asserts on `query.ToSqlQuery().Sql` (or `GetSelectQuery()`) and never calls `.ToList()` / `AssertQuery` never runs the SQL against the DB — it verifies the builder emitted a string, not that the string is valid or returns the right rows. A test must **execute against the database**; generation-only assertion is an *exception* reserved for two cases: (a) the SQL-generation API itself is the subject under test (e.g. `ToSqlQuery` behavior), or (b) the configured provider cannot execute the generated SQL (bespoke tables absent from the test schema, a dialect construct the test DB can't run) — and case (b) is acceptable **only with explicit user acceptance**, noted at the test. Never reach for generation-only assertion just to skip DB/table setup. (When the tables are only test-local, `CreateLocalTable<T>` makes execution self-contained — see the ClickHouse note under *Cross-provider gotchas*.)

Before reporting a test written, state in one line **which input makes it go red** and confirm it would have failed against the pre-fix code. If you can't name that input, it isn't a guard yet. Mutation-score tooling is out of scope; the red→green demonstration required by `agent-rules.md` → *Before coding a fix or feature* is the bar.

**For a fix that changes a *shared* template / code path serving many providers, run the pre-fix RED on *each* targeted provider — don't extrapolate red→green from a sibling that shares the path.** A regression test enabled for N providers only guards them if each one actually *routes through* the changed code; a provider may hit a different `Configuration`-specific override (or a default with no builder) so the same source change lands as different — or no — SQL. Reasoning "Oracle shares DuckDB's default template, so DuckDB-green implies Oracle-green" skips exactly the question the RED run answers: does this provider emit the broken form pre-fix? Demonstrate it. (Surfaced on #5644: the `Lead(expr, nulls)` IGNORE-NULLS fix touched the shared default template + added a PG19 variant; RED was confirmed independently on DuckDB, PostgreSQL 19, *and* Oracle before trusting the fix, rather than inferring Oracle from DuckDB's shared path.)

## Proposed: property-based / invariant testing for the SQL pipeline (not yet wired in)

The current suite is example-based: a human (or AI) names specific inputs and asserts specific outputs. **Property-based testing** inverts that — you state an *invariant* that must hold for *all* inputs, and a generator throws hundreds of randomized cases at it to find the one that breaks it. It's the automated form of "name the input that makes it go red" above: instead of guessing the edge case, you let the generator find it. This is **a proposal, not built** — recorded here so the idea isn't lost and so a future effort starts from a candidate list rather than a blank page. linq2db's SQL-generating core is unusually well-suited to it because several pipeline invariants are exact, deterministic, and provider-independent:

- **AST clone round-trip** — for any SQL AST node, `clone(node)` must be `Equals` to `node` *and* hash equal. Directly exercises the equality/hash/clone fan-out `code-reviewer` rule 1 polices by hand; a generator over node shapes would surface a dropped field automatically.
- **Visitor identity** — running an identity `QueryElementVisitor` over a tree returns a structurally-equal tree (no field silently neutralized to `None` / `null` / `0` — the same drop class rule 1's "new field propagation" sub-bullet watches for).
- **LinqService serialization round-trip** — `deserialize(serialize(tree))` must equal `tree` for any tree. The remote path's correctness is exactly this property; today it's only spot-checked per construct.
- **Optimizer idempotence** — optimizing an already-optimized tree is a fixed point (`optimize(optimize(x)) == optimize(x)`). This is the very invariant whose violation produces the infinite-recursion hangs in *Diagnosing hung test runs* below — a property test would catch a non-idempotent rewrite before it ships.
- **Type-mapping symmetry** — `DataType` ↔ provider `DbType` / CLR-type mapping conversions round-trip where the mapping is defined as bijective.

Framework note: there is **no property-testing dependency in the repo today**. Options if pursued: a generator library (CsCheck — minimal-dependency, .NET-native shrinking; or FsCheck), or hand-rolled randomized generators feeding ordinary NUnit `[Test]` methods with a logged seed for reproduction. Start with **one** invariant (AST clone round-trip is the highest-leverage and needs no DB) as a proof-of-concept before committing to a framework — the package choice and the shrinking/seed-reproduction story are the real decisions, and they belong to whoever scopes the effort, not to a doc.

## Cross-provider gotchas for new tests

### YDB requires a primary key on every table

YDB rejects `CREATE TABLE` without a primary key, so `db.CreateLocalTable<T>()` of a keyless type fails at setup with `Primary key is required for ydb tables` (wrapped in a `Pre type annotation` error). YDB isn't on CI yet, so this surfaces only when running YDB locally. Fix the test's table type by adding `[PrimaryKey]` to a suitable existing column (a non-nullable key such as `Id`, or the single natural-key column); if none fits, add a dedicated PK column. It's a test-data fix, not a provider change.

### ClickHouse: `CreateLocalTable` needs no engine; ASOF runs on Memory tables

ClickHouse `CREATE TABLE` requires an `ENGINE`, but you don't specify one for `db.CreateLocalTable<T>()` — the builder auto-emits `ENGINE = MergeTree() ORDER BY <pk>` when the mapping has a `[PrimaryKey]`, else `ENGINE = Memory()` (`ClickHouseSqlBuilder.BuildEndCreateTableStatement`; there's a `// TODO` for an engine-config API, so custom engines still need raw SQL). So a keyless test type lands on `Memory()`, which is fine for query tests. `LEFT ASOF JOIN` and `GLOBAL LEFT ASOF JOIN` **execute** against Memory-engine local tables on a single-node container, and `CreateLocalTable` works over the LinqService remote context — so ASOF hint tests can (and should) execute rather than assert on `ToSqlQuery().Sql`. Note bespoke test tables (e.g. `AsofTrade`/`AsofQuote`) are **not** in the ClickHouse test schema (unlike the T4-scaffolded `ReplacingMergeTreeTable`), which is why they need `CreateLocalTable` rather than `GetTable<T>`.

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

### Verifying server-side function translations

When a test asserts a method translates to a server-side function, wrap the call in `Sql.AsSql(...)` (e.g. `select Sql.AsSql(Sql.NewGuid7())`). Without it, a provider that *lacks* the translation can silently client-evaluate the method and the test still passes — a false green that hides a missing translation.

A non-`ServerSideOnly` `Sql.*` method with a CLR body (e.g. `Sql.NewGuid` / `Sql.NewGuid7`) falls back to client-side evaluation wherever no server translation is registered. So one `[DataSources]` test covers the whole matrix: providers with the translation exercise the SQL function, the rest the client fallback. Assert the observable property (e.g. the GUID version nibble), and wrap in `DisableBaseline` when the generated value is non-deterministic (the value, not the SQL shape, varies per run). Don't infer "can't client-evaluate" from another test's provider-exclusion list — those exclusions are often roundtrip-specific, not translation-capability statements.

### Test-proofing a gated provider capability

To empirically determine whether a database actually supports a feature gated off by a capability flag (`Is…Supported` on a translator, a `SqlProviderFlags` bool, etc.) — rather than trusting docs:

1. **Flip the gate** to `true` in the provider's translator/builder.
2. **Enable only that provider** in `UserDataProviders.json` and **disable providers whose containers are down** — otherwise their connection-timeouts dominate the run and bury the signal (start the container the probe needs; a stopped container makes the run crawl).
3. **Run the gated tests** for that feature. A test whose `[ThrowsForProvider]` now reports *"Expected a `LinqToDBException` … but found `''`"* means the DB **executed** the SQL → **supported**. A native DB exception (e.g. `HanaException`, `DB2Exception`, `… does not exist`) means **unsupported**. (The throw-expectation "failing" is the signal — read the failure mode, not the pass/fail.)
4. **Revert the experimental flip** (`git checkout HEAD -- <translator>`); proofing edits are throwaway.

To then *enable* the capability for real, mirror the per-test `[ThrowsForProvider]` gating of a **sibling provider with the same capability profile** (e.g. YDB was the precedent for SAP HANA ROWS-frame support — both ROWS-only), and expect baseline regeneration for the now-executing tests. Watch for coarse flags that can't express asymmetric support — see the `window-fn-coarse-flags` auto-memory for the window-functions case. This is the technique used to find the PR #5468 SAP HANA / Informix statistical-window gaps.

### Testing query cache HIT / MISS behaviour

Three idioms, each proves something different. Full mechanics in [`query-cache-mechanics.md`](query-cache-mechanics.md).

- **Same query, re-execute → HIT.** Build `query` once, call `.ToArray()` twice, assert the second call doesn't bump `GetCacheMissCount()`. Catches the most common bug: `MarkAsNonParameter`'s stored value type disagreeing with the accessor expression's runtime type (the `value1.Equals(value2)` compare returns false for `"abc".Equals(['a','b','c'])` etc.).

- **Local function with captured-value parameter → HIT across invocations.** Build the query inside a local helper that takes the cache-keyed value as a parameter; call the helper twice with equivalent values; assert miss count unchanged. This is the only clean way to verify cross-invocation cache hits because different C# local variables become different closure-display-class fields with different `MemberInfo` — they don't share cache shape even when content is identical. The local-function param shape collapses both calls onto the same display class with the same field, so structural compare matches and the value-compare layer runs.

- **Mutate captured array → MISS on content change.** Hold a reference to a captured array, run `query.ToArray()`, mutate the array in place, run `query.ToArray()` again. With a content-aware cache key the second invocation registers as a miss (counter goes up). Catches stale-SQL bugs where mutation isn't reflected in the cache.

`GetCacheMissCount()` reads `Query<T>.CacheMissCount` — a static per-entity-type counter shared across the AppDomain. Within a single test method, sequential invocations are deterministic; the assertion is always "delta from a captured baseline", never "absolute count".

## Reading test output

Always read the **full** test run log — not just the tail. NUnit and `dotnet test` interleave relevant information across the log: `_CreateData` failures appear near the top, setup exceptions and warnings can come well before the failing assertion, and stack traces may be truncated if you jump to the end. Do not use `tail`, `head`, `head_limit: 1`, or similar tricks to skim the output; read the entire log and scroll back for context when a failure is surprising. The only exception is when you have already read the log once and are fetching a specific slice you've already identified.

## Running a long full suite

For a full-suite run (tens of minutes — e.g. all of one provider across the direct + `LinqService` configs), build the test project first, then run `dotnet test … --no-build` as a **main-agent `run_in_background` Bash task**, and read that task's own output file for the result. Don't delegate the long run to a `test-runner` subagent: the subagent tends to background the `dotnet test` itself and return *before* it finishes, leaving no result to retrieve — and a follow-up agent reading a shared log (e.g. a redirected `ydb-test-full.log`) can pick up a **stale** prior run and report a false pass/fail (this produced a phantom `CalledWithCorrectNames` failure that had actually passed). The `--no-build` flag also avoids the `MSB4166` mid-suite truncation under disk pressure (see [`windows-dev-gotchas.md`](windows-dev-gotchas.md) → *Iterative-build gotchas*). For a small filtered run the `/test` skill / `test-runner` is still the right tool; this is specifically about the long, unattended full-suite case.

## Diagnosing hung test runs

A `dotnet test` that has been running **>30 s with zero test-output lines** AND a live MTP **test-app process** (named after the test assembly — `linq2db.Tests*` — or `dotnet` when it hosts the test dll) at **>1 GB resident memory** is almost certainly in an infinite-recursion loop — typically a visitor that hands its own output back to itself (`Visit(Optimize(converted))` re-entering its own `VisitXxx` with a structurally-equivalent element). Confirm via `Get-Process linq2db.Tests*,dotnet` (under VSTest this was `testhost.exe`); a normal test run keeps it well under 500 MB.

Recovery and triage:

1. `Get-Process linq2db.Tests*,dotnet -ErrorAction SilentlyContinue | Where-Object { $_.WorkingSet64 -gt 1GB } | Stop-Process -Force` — kill the runaway (filter by memory so unrelated `dotnet` processes aren't touched). The background `dotnet test` wrapper will exit shortly after.
2. Re-read the captured output. The recursion's terminal exception is usually `System.InsufficientExecutionStackException: Too many stack hops (> N). Recursion cannot safely continue.`, thrown from `LinqToDB.Internal.Common.StackGuard.RunOnEmptyStack` — that's linq2db's internal stack-overflow guard re-throwing after the runtime ran out of fresh-thread hops. The top of the truncated stack names the offending visitor method.
3. The fix is virtually always idempotence: the visitor's transformation must produce a fixed point (a re-entry on the transformed element returns it unchanged) — check whether the rewrite wraps an operand in a shape the next visit pass will fail to recognize as already-wrapped, and add a structural guard.

## LinqService "address already in use" (port 22654)

Remote (`*.LinqService`) test configs spin up an in-process HTTP host on a **fixed port** (`22654`). When many `.LinqService` configs fail at *startup* with `System.IO.IOException : Failed to bind to address https://127.0.0.1:22654: address already in use` while the **non-remote configs of the same tests pass**, it is **not** a code regression — a leaked test-app process from an earlier run is still holding the port. The failure is at host bind, before any query executes, so an entity-construction / SQL change cannot cause it.

Find and stop the orphaned listener:

```
Get-NetTCPConnection -LocalPort 22654 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

Iterating with many back-to-back local `dotnet test` runs is what leaks these hosts; clear the port (or stop stray `testhost` per the section above) before reading `.LinqService` failures as real.

## Debugging linq2db translators

When debugging query translation or provider issues,
use `Console.WriteLine` to output intermediate values or SQL fragments.
Do not introduce logging dependencies.

### Capturing a passing test's exception / stack

The MTP runner surfaces per-test stdout only for **failed / skipped** tests — a passing test's `TestContext.Out.WriteLine` / `Console.WriteLine` is generated but never routed to the run output (also why `/test` reports succeeded tests by count only). So when a test passes via `Assert.Throws<T>` and you need the *caught* exception's full stack — e.g. to find where deep in the pipeline it's thrown — printing it from inside the test shows nothing.

To get the stack, make the test **fail** so MTP prints it:

- Replace `Assert.Throws<T>(() => x())` with a bare `x();` — the exception propagates and the failure carries the complete trace (the deepest `LinqToDB.*` frame is the throw site); or
- When you've added a probe `throw` that an enclosing `Assert.Throws<T>` would catch, throw a type `T` won't match (e.g. `NotImplementedException` against an expected `InvalidOperationException`) so it escapes and fails the test.

Do this in a throwaway worktree or local checkout only — never commit the assertion change.

### Inspecting generated SQL offline (reproducing "could not be converted to SQL")

Use `query.ToSqlQuery().Sql` to see a query's SQL without running it. `IQueryable.ToString()` returns the query *type* name (`ExpressionQueryImpl`1[…]`), **not** SQL. Translation failures ("The LINQ expression '…' could not be converted to SQL") are raised at SQL-build time, so `ToSqlQuery()` — or a `CreateLocalTable<T>()` + query in the playground — reproduces them with no populated database.

- **Plain linq2db** emits SQL fully offline when the provider version is explicit: `new DataConnection(new DataOptions().UseSqlServer("Server=.;Database=x", SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient))`. A dummy connection string works — nothing is opened. Run this from `Tests.Playground` (link the file in), **not** a hand-rolled console placed under the repo root — `Directory.Build.props` / central package management cascade into any project beneath the root and break a stray project's restore (`Unable to find package <name>`, plus unwanted multi-targeting).
- **EF Core `.ToLinqToDB()`** on SQL Server opens a real connection to auto-detect the server version (`ProviderDetectorBase.DetectServerVersion` → `SqlConnection.Open()`), so it needs a live server even just to emit SQL. SQLite EF Core emits offline.

## Baselines

Many tests compare emitted SQL against a stored baseline file. Baselines live **outside the main repo** under the path configured by `BaselinesPath` in `UserDataProviders.json` → `MyConnectionStrings`. With `BaselinesPath` set, a mismatched test overwrites the baseline with the new SQL; subsequent runs compare against the updated file. With `BaselinesPath` unset, baselines are neither written nor compared.

Baseline capture is **not** gated on `AssertQuery` / `GetSql`. Emitted SQL is captured at the harness level (`Tests/Base/BaselinesWriter.cs`) for any query a test executes through `GetDataContext`, so a plain `GetDataContext` + `ToList()` + `ShouldBe` test with no `AssertQuery` call still writes `.sql` baselines. Never infer "no baselines" from the assertion style — check the baselines output.

### When a substring assertion is vacuous — prefer baselines for shape-changing fixes

A `ToSqlQuery().Sql.ShouldContain` / `ShouldNotContain("COALESCE")`-style assertion only guards a fix if the substring actually differs pre/post on the providers the test runs on. Two normalizations silently defeat it:

- **No-op stripping.** `Coalesce(x, NULL)` is reduced to bare `x` by `ConvertCoalesce` on every standard provider, so a fix that *stops emitting* such a no-op produces byte-identical SQL — the assertion passes before and after, catching nothing.
- **Function aliasing.** Providers that fold COALESCE to a native construct (Informix `Nvl`, Access `IIF`) never emit the literal `COALESCE`, so a `COALESCE` substring check is vacuous there regardless of the bug.

When a fix changes provider-specific SQL *shape* (especially provider-specific folds), the regression artifact is the per-provider **baseline** `.sql` file, not a substring assertion. (Surfaced on #5531: the bug was only observable as Informix `Nvl(x, NULL)`; a SQLite `ShouldNotContain("COALESCE")` test could not go red.)

### Enabling baselines locally

Open `UserDataProviders.json`, find the `MyConnectionStrings` section, and add:

```json
"BaselinesPath": "../linq2db.baselines"
```

Path is arbitrary but the sibling-clone convention — `../linq2db.baselines` — matches the upstream baselines repo's layout and keeps the diff against CI clean.

### Getting a "before" snapshot for diffing

Baselines regenerate in place during the test run. To see "before vs after" you need a snapshot of the prior state. Two options:

1. **Regenerate locally**: check out `master` (or the PR base), run the relevant tests to populate baselines at `BaselinesPath`, then switch back and re-run — diff is between the two generations.
2. **Use the remote baselines repo**: clone `https://github.com/linq2db/linq2db.baselines.git` to `../linq2db.baselines` (or fetch into the existing sibling) and compare `BaselinesPath/<Provider>/…` against `../linq2db.baselines/<Provider>/…` directly. No pre-change test run needed; this is the authoritative "before" for branches based on `master`.

### Unexpected "changed" baselines on a broad run — confirm with a deterministic re-run

A large-suite run against a stale local `BaselinesPath` can report many `changed` baselines — including tests outside your filter and unrelated to your change. That's usually the local `.bls` catching up to current branch code (the cache lagged behind commits made since it was last populated), **not** your change's effect. Before attributing the churn to your work: re-snapshot and re-run the *same* set — a deterministic second run that diffs to ~0 `changed` confirms the first run was cache catch-up. Small focused runs hide this because their few baselines already matched the cache, so the drift only surfaces the first time you run a big suite locally. Corollary: a change that only alters a runtime *value accessor* (e.g. the source a parameter's value is read from) is SQL-text-neutral and cannot move a baseline; if it appears to, suspect stale-cache drift first.

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
