# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is linq2db

LINQ to DB is a lightweight, type-safe ORM and LINQ provider for .NET. It translates LINQ expressions into SQL — no change tracking, no heavy abstraction. Think "one step above Dapper" with compiler-checked queries.

Repository: https://github.com/linq2db/linq2db — use this when resolving issue/PR numbers via `gh` (e.g. `gh pr view <n> --repo linq2db/linq2db`).

## Build Commands

```bash
# Build the full solution
dotnet build linq2db.slnx

# Build just the core library
dotnet build Source/LinqToDB/LinqToDB.csproj

# Build in Release (enables Roslyn analyzers and banned API checks)
dotnet build linq2db.slnx -c Release

# Quick single-TFM build for development (fastest iteration)
dotnet build linq2db.slnx -c Testing
# Testing config targets only net10.0 with DEBUG defines
```

## Running Tests

Tests use **NUnit3** with **Shouldly** assertions (not NUnit Assert). Test targets: `net462`, `net8.0`, `net9.0`, `net10.0`. **Always use Debug configuration and prefer `net10.0`** — Release enables Roslyn analyzers and is much slower to build. The full test suite takes ~1 hour or more; avoid running it unless necessary.

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

### Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json`
2. This gives you SQLite-based testing out of the box
3. Add connection strings for other databases as needed

**Important**: `_CreateData.*` tests must run first — they create and populate test databases. If running a single test, ensure `_CreateData` has run. If your test modifies data, revert changes to avoid side effects.

### Test Patterns

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

### Debugging linq2db translators

When debugging query translation or provider issues,
use `Console.WriteLine` to output intermediate values or SQL fragments.
Do not introduce logging dependencies.

## Solution Structure

| Solution | Purpose |
|---|---|
| `linq2db.slnx` | Full solution |
| `linq2db.playground.slnf` | Lightweight — for working on specific tests without full load |
| `linq2db.Benchmarks.slnf` | Benchmarks only |

## Architecture

### Core Query Pipeline

The main flow for translating LINQ to SQL:

1. **LINQ Expressions** (`Source/LinqToDB/Linq/`) — Entry point. LINQ method calls are captured as expression trees. `Translation/` contains the member translator interfaces (`IMemberTranslator`, `ISqlExpressionTranslator`) that convert .NET method calls to SQL expressions.

2. **SQL AST** (`Source/LinqToDB/SqlQuery/`, `Source/LinqToDB/Internal/SqlQuery`) — Internal SQL representation. Contains `SqlDataType`, `SqlObjectName`, window/frame definitions, and the precedence system for SQL operator ordering.

3. **SQL Generation** (`Source/LinqToDB/Sql/`) — `Sql.ExpressionAttribute` and `Sql.FunctionAttribute` map C# methods to SQL expressions. Provider-specific overloads allow different SQL per database.

4. **Data Providers** (`Source/LinqToDB/DataProvider/`, `Source/LinqToDB/Internal/DataProvider`) — One subfolder per database (Access, ClickHouse, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SapHana, SqlCe, SqlServer, Sybase, Ydb). Each implements `IDataProvider` with dialect-specific SQL generation, type mapping, and bulk copy.

5. **Data Access** (`Source/LinqToDB/Data/`) — `DataConnection` (holds connection open until dispose) and `DataContext` (opens/closes per query). `BulkCopy*` classes, `CommandInfo`, retry policies.

### Other Key Directories

- `Source/LinqToDB/Mapping/` — Attribute-based and fluent mapping configuration (`[Table]`, `[Column]`, `FluentMappingBuilder`)
- `Source/LinqToDB/Metadata/` — Metadata readers for mapping discovery
- `Source/LinqToDB/Interceptors/` — Extension points for connection/command lifecycle
- `Source/LinqToDB/Remote/` — Remote context for client/server scenarios
- `Source/LinqToDB/SchemaProvider/` — Database schema introspection
- `Source/LinqToDB/Concurrency/` — Optimistic concurrency support
- `Source/LinqToDB/LinqExtensions/` — Extension methods for LINQ operations (joins, merge, CTE, etc.)
- `Source/Shared/` — Polyfills for missing runtime APIs across TFMs (auto-included by all projects)
- `Source/CodeGenerators/` — Internal Roslyn source generators

### Companion Projects

- `Source/LinqToDB.EntityFrameworkCore/` — EF Core integration
- `Source/LinqToDB.Extensions/` — DI and logging extensions
- `Source/LinqToDB.FSharp/` — F# support
- `Source/LinqToDB.CLI/` and `Source/LinqToDB.Scaffold/` — Database scaffolding CLI tool
- `Source/LinqToDB.Remote.*` — Remote context implementations (gRPC, SignalR, HttpClient, WCF)

## Code Conventions

- **Indentation**: Tabs (not spaces) for C#/VB. Spaces for F#, YAML, shell scripts, markdown.
- **C# version**: 14 (`LangVersion` in Directory.Build.props)
- **Nullable**: Enabled globally
- **Warnings as errors**: `TreatWarningsAsErrors` is true. No compilation warnings allowed.
- **Analyzers**: Run during Release builds only (`RunAnalyzersDuringBuild`). Banned API list in `Build/BannedSymbols.txt`.
- **XML documentation**: Required on new public classes, properties, and methods.
- **Target frameworks**: `net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. Use conditional compilation (`#if`) for TFM-specific code. Feature flags like `SUPPORTS_COMPOSITE_FORMAT`, `SUPPORTS_SPAN`, `ADO_ASYNC` are defined in `Directory.Build.props`.
- **Code style**: Match existing code. The project uses column-aligned formatting intentionally — do not "fix" alignment spacing.
- **.NET SDK**: 10.0 (see `global.json`)

## Versioning

All versions live in `Directory.Build.props`:

- `<Version>` — main product version, applied to every project in the solution
- `<EF3Version>`, `<EF8Version>`, `<EF9Version>`, `<EF10Version>` — per-EF-major versions used by the `LinqToDB.EntityFrameworkCore.EFx` packages

User-triggered version bumps are handled by the `/version-bump` skill (`.claude/skills/version-bump/SKILL.md`).

## Branch Conventions

- `master` — main development branch
- `release` — latest released version
- Bugfix branches: `issue/<issue_id>`
- Feature branches: `feature/<issue_id_or_feature_name>`

### Creating a new branch

- **Name.** If the user didn't specify a branch name, derive one from the task using the schema above (`issue/<id>` for a referenced issue, `feature/<slug>` otherwise). If the task doesn't give enough context to infer a name, ask the user.
- **Base.** Always branch from `origin/master`. Run `git fetch origin master` first so the base isn't stale. Branch from something else only if the user explicitly says so.
- **Dirty working tree.** If there are staged or unstaged changes before branching, stop and ask the user whether to stash or discard them. Never silently discard or carry them across.

## Git commit rules

- **Never run `git commit` without an explicit user request.** "Explicit" means the user told you to commit in the current turn (e.g. "commit", "commit this", "commit changes"). Finishing edits, passing tests, or a clean working tree are not requests. When in doubt, stop and ask.
- This applies even when the preceding turn ended with a commit — each new change needs its own explicit go-ahead.
- Same rule for `git push`, `git tag`, `gh pr create`, and any other publishing action.

## Push to remote rules

- **Never `git push` without an explicit user request.** Same rule as commits — each push needs its own go-ahead.
- **After every successful push**, check for a PR on that branch (`gh pr list --head <branch> --json number,title,body,url`):
  - If **no PR exists**, propose creating one (see Pull request rules) and wait for confirmation.
  - If **a PR exists**, diff the newly pushed commits against the current PR body. If the body no longer accurately describes the work (new summary bullets, new linked issues, etc.), propose a concrete edit and wait for confirmation before calling `gh pr edit`. **Show the proposed change as a diff between the current body and the new one** (e.g. a unified diff or `- old line` / `+ new line` markers) — do not just paste the new body in full. If the body is still accurate, say so and move on — don't edit gratuitously.

## Pull request rules

When creating a PR on `linq2db/linq2db`:

- **Always open as draft** (`gh pr create --draft`). Never publish a ready-for-review PR unless the user explicitly asks.
- **Confirm title and body with the user before running `gh pr create`.** Propose both, wait for approval, then create.
- **Link referenced issues/tasks as closed on merge.** If the work targets a known issue or task, include `Fixes #<n>` / `Closes #<n>` in the PR body so GitHub auto-closes it when the PR merges. One keyword per issue.
- **Assignee.** Assign the PR to the current GitHub user (`gh pr create --assignee @me`) unless the user specifies someone else.
- **Milestone.**
  - If the linked issue/task has a milestone, reuse it.
  - Otherwise ask the user to pick one. Fetch open milestones via `gh api repos/linq2db/linq2db/milestones?state=open` and present a **numbered list** (so the user can reply with just a number) in this order:
    1. The **next-version milestone** (matching `<Version>` in `Directory.Build.props`, or the closest upcoming version) — always first.
    2. Remaining **versioned** milestones (titles starting with a digit, e.g. `6.x`, `7.0.0`), sorted by version.
    3. **Non-versioned** milestones (e.g. `Backlog`, `In-progress`), sorted alphabetically by title.

## Bash command rules

The user may have a PreToolUse hook that rejects compound Bash calls, because the permission system evaluates them as a single opaque command, which forces a prompt instead of matching an allowlisted rule. Regardless of whether the hook is enforcing it in the current session, follow the rule: each Bash tool call must be a single command.

- No `&&` or `||` chaining
- No `;` chaining
- No shell control flow (`for`, `while`, `until`, `case`, `if`, `function`)
- No nested chains inside `$(...)` command substitution (plain `$(cmd)` is fine)
- Pipes (`|`) and heredocs are allowed

Split chained work into separate tool calls — run them in parallel when independent, sequentially when one depends on the previous.

## Agent Guardrails

- **Preserve public API, architecture, and behavior.** This is a library — types, method signatures, and observable SQL output in `Source/LinqToDB/` are contracts. Don't modify them without a clear, explicit reason.
- **Don't touch cross-cutting internals for a local fix.** The SQL AST (`SqlQuery/`), `IDataProvider`, and translator interfaces (`Linq/Translation/`) are shared by every provider. A fix scoped to one provider or test shouldn't reshape them — raise the question first.
- **Don't reformat, rename, or clean up unrelated code** while doing something else. Column-aligned formatting in this repo is intentional (see Code Conventions).
- **Surface trade-offs on non-local choices.** If a decision affects public API, generated SQL, or provider behavior, describe the options rather than picking silently.
- **Document arbitrary values explicitly.** If a change requires picking an arbitrary constant (timeout, threshold, version cutoff) or making an assumption, leave a short comment or `// TODO` at the call site so a reviewer can verify it.

## Review Guidelines

- Ignore minor/intentional formatting differences (column alignment, qualified type spacing)
- Only flag formatting when clearly broken (3+ blank lines, mixed tabs/spaces causing misalignment, broken indentation)
- Use Shouldly for test assertions, not NUnit Assert
