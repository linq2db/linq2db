# Running Tests

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

## Test Database Configuration

Tests run against multiple database providers. Configuration comes from `UserDataProviders.json` (gitignored, user-specific). To get started:
1. Copy `UserDataProviders.json.template` to `UserDataProviders.json`
2. This gives you SQLite-based testing out of the box
3. Add connection strings for other databases as needed

**Important**: `_CreateData.*` tests must run first — they create and populate test databases. If running a single test, ensure `_CreateData` has run. If your test modifies data, revert changes to avoid side effects.

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
