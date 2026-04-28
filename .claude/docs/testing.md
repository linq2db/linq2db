# Running Tests

Tests use **NUnit3** with **Shouldly** assertions (not NUnit Assert). Test targets: `net462`, `net8.0`, `net9.0`, `net10.0`. **Always use Debug configuration and prefer `net10.0`** — Release enables Roslyn analyzers and is much slower to build. The full test suite takes ~1 hour or more; avoid running it unless necessary.

**Don't prepend a `dotnet build`.** `dotnet test` builds the project automatically if binaries are stale; a standalone `dotnet build` right before `dotnet test` only adds waiting time. If a test run fails with compile errors, fix them and re-run `dotnet test` directly.

```bash
# Run a single test class or method via dotnet test
dotnet test Tests/Linq/Tests.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net10.0

# Run tests with the lightweight playground solution (faster load)
dotnet test linq2db.playground.slnf
```

**Quick iteration with `Tests.Playground`**: the full test suite build is expensive. For fast feedback, link the test file you need into `Tests/Tests.Playground/Tests.Playground.csproj` via `<Compile Include="..." Link="..." />`, then run just that project:

```bash
dotnet test Tests/Tests.Playground/Tests.Playground.csproj --filter "FullyQualifiedName~ClassName.MethodName" -f net10.0
```

## Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json`
2. This gives you SQLite-based testing out of the box
3. Add connection strings for other databases as needed

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
