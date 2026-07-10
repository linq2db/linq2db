# Authoring user-facing analyzers (`linq2db.Analyzers`)

How to add a **shipped, user-facing** Roslyn analyzer + code fix to the `linq2db.Analyzers` NuGet package. This is distinct from [`analyzer-rules.md`](analyzer-rules.md) (which covers the *internal* `.editorconfig` analyzer config enforced on the linq2db source) and from `Source/CodeGenerators` (internal build-only generator + `LINQ2DB0001`, never packaged).

Established on the `feature/analyzers-package-window-rule` work (rule `LINQ2DB1001`, the old→new `Sql.Ext`→`Sql.Window` migration). Read this before writing a new rule; `/create-analyzer` scaffolds from it.

## Project layout — two assemblies, one package

`EnforceExtendedAnalyzerRules` turns on **RS1038**, which (as an error under `TreatWarningsAsErrors`) forbids a `DiagnosticAnalyzer` assembly from referencing `Workspaces`. That is also the performance-correct split (the analyzer loads on every keystroke; only the code fix needs `Workspaces`). So:

- **`Source/LinqToDB.Analyzers`** — analyzers only. `netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, **`IsPackable=false`**. References `Microsoft.CodeAnalysis.Analyzers` + `Microsoft.CodeAnalysis.CSharp` (`VersionOverride` to the low pin) — **no Workspaces**. Holds each `DiagnosticAnalyzer`, its `DiagnosticId`/descriptor, and `AnalyzerReleases.*.md`.
- **`Source/LinqToDB.Analyzers.CodeFixes`** — code fixes + the packable project. `PackageId=linq2db.Analyzers`, `IsPackable=true`, `IncludeBuildOutput=false`, `SuppressDependenciesWhenPacking=true`, `DevelopmentDependency=true`, `NoWarn=NU5128`, `EnablePackageValidation=false`. References `Microsoft.CodeAnalysis.CSharp.Workspaces` + `Microsoft.CodeAnalysis.Analyzers` + a `ProjectReference` to the analyzer project (`PrivateAssets=all`). Packs **both** DLLs into `analyzers/dotnet/cs/` + `readme.md`. It has no `DiagnosticAnalyzer`, so RS1038 doesn't apply.

Each project has its own `Directory.Build.props` that **imports the *root* `../../Directory.Build.props`** (shared Nuget metadata / version / signing) — not `Source/Directory.Build.props` (its banned-symbols `AdditionalFiles`, PublicAPI analyzers, and 5-TFM list must not apply to a Roslyn component). Each csproj then overrides:

- `TargetFramework=netstandard2.0` and clears `TargetFrameworks`.
- A **minimal Meziantou polyfill list** (`T:System.HashCode` + `T:System.Runtime.CompilerServices.IsExternalInit`).
- **Excludes `Source/Shared/*.cs`** (`<Compile Remove="$(MSBuildThisFileDirectory)..\..\Source\Shared\*.cs" />`) — those are runtime-library compat shims (`IAsyncDisposable`, `[module: SkipLocalsInit]`) a Roslyn component doesn't need and can't satisfy with the minimal polyfill set.

Verify the nupkg (`dotnet pack …CodeFixes.csproj -c Release`, unzip): both DLLs under `analyzers/dotnet/cs`, `developmentDependency=true`, readme + icon, **no `lib/`, no dependency group**. CI packs the whole solution, so the package auto-ships once both projects are registered in `linq2db.slnx`.

## Roslyn version rule

Pin the shipped analyzer's Roslyn (`Microsoft.CodeAnalysis.CSharp.Workspaces`, and the analyzer project's `Microsoft.CodeAnalysis.CSharp` `VersionOverride`) to **the Roslyn that ships with the lowest supported .NET SDK** — today .NET 8 → **4.8.0**. This maximizes IDE reach (mirrors NUnit.Analyzers' broad-reach approach) and is decoupled from the repo's `5.3.0` runtime pin. Only raise it when that SDK is dropped; if users report their IDE/SDK is too old, downgrade in a later release. This rule is also recorded in [`release/nuget-package-notes.md`](release/nuget-package-notes.md) so a routine CodeAnalysis bump doesn't silently raise the consumer floor.

## Diagnostic IDs

Continue the `LINQ2DB` space but reserve **`LINQ2DB1xxx` for user-facing** rules (`0001`–`0999` stay internal, e.g. `CodeGenerators`' `LINQ2DB0001`). First user-facing rule = `LINQ2DB1001`. Record every rule in `AnalyzerReleases.Unshipped.md` (moved to `Shipped.md` at release) — the release-tracking analyzer (RS2000/RS2001) enforces this. Default severity for "prefer the new API" rules is **Info** while the old API is still supported (not `[Obsolete]`).

## Roslyn 4.8 gotchas

The bundled `System.Collections.Immutable` predates `CollectionBuilder`, so `ImmutableArray<T>` **collection expressions (`[x]`) don't compile** — use `ImmutableArray.Create(...)`. `System.Index`/`Range` aren't polyfilled (minimal list) — avoid `[^1]`, use an explicit index. Meziantou runs in Release (inherited via root props), so analyzer/code-fix code must be MA-clean (`string.Equals(…, Ordinal)` not `==`/`!=`, `.ToString(CultureInfo.InvariantCulture)` for int→string, no chained `.Where`).

## Performance checklist (runs on every keystroke, on huge solutions)

- `EnableConcurrentExecution()` + `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`.
- Resolve target symbols **once** in `RegisterCompilationStartAction` (cache the `INamedTypeSymbol`s); bail immediately if the linq2db assembly isn't referenced.
- Register the narrowest callback (`RegisterOperationAction(…, OperationKind.Invocation)`); cheap string-name gate **before** any `SymbolEqualityComparer`/containing-type resolution; no full-tree walks, no LINQ/regex allocations on the hot path.
- Validate with `/profile-analyzers` before shipping.

## Code-fix correctness checklist

Two failure modes specific to a LINQ/ORM library, since the analyzed code often lives inside **expression trees**, not executed C#:

- **Don't emit code invalid in an `Expression<>`** — use only expression lambdas, reuse the user's original (already-compiling) argument subtrees, avoid statement bodies / `out` / `dynamic`. Add a test that the fixed snippet compiles **inside a real `Expression<Func<>>`/`IQueryable` query**, not just as free-standing C#.
- **Don't apply executed-code semantics to translated code** — scope the rule by symbol identity so it can't mis-fire as "use async instead of sync" / "materialize the enumerable" on an expression that becomes SQL.
- **Return-type reconciliation** — a source-level fix can't inject the CLR `Convert` nodes the runtime translator uses. When the replacement API's return type differs (e.g. `double?` vs the original slot), guard: offer the fix only when the new type fits the target (`ClassifyConversion(...).IsImplicit`) **or** the context is type-inferred (anonymous member / `var`, which re-infers). See `LegacyWindowChainRewriter.NullableDoubleFitsTarget`.

## Trivia preservation (the headline; the recurring failure of new analyzers)

Build the new-shape scaffold with **placeholder** identifiers for every reused argument, `NormalizeWhitespace()` the scaffold, then `ReplaceNodes` the placeholders with the **original** argument subtrees (restoring their trivia verbatim). Preserve the whole expression's leading trivia (`GetFirstToken().LeadingTrivia`) and salvage any comments that lived on the old chain's scaffolding (between calls, excluding those inside reused args) onto the result's trailing trivia so **no comment is dropped**. Never round-trip through `ParseExpression(string)`. See `LegacyWindowChainRewriter`.

## Tests + CI

- Test project `Tests/Tests.Analyzers` — `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` + `.CodeFix.Testing` with `DefaultVerifier`, NUnit + MTP. **Target `net8.0`** so the `linq2db` build the harness loads matches the `ReferenceAssemblies.Net.Net80` reference pack used to compile snippets (a higher ref pack trips `CS1705`). Add the linq2db assembly via `TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location))`. Also add a direct `Microsoft.CodeAnalysis.CSharp.Workspaces` reference so the testing SDK's ancient transitive Roslyn floor (1.0.1, netfx-only → NU1701) unifies up.
- Diagnostics use `{|LINQ2DBnnnn:…|}` markup; code-fix tests assert byte-exact `FixedCode` (which is also compiled + re-analyzed → guards expression-tree validity and proves the fix clears the diagnostic). Include a **trivia battery** (leading comment, argument comment, mid-chain comment) and an **expression-tree** before/after.
- CI: `build-job.yml` runs `dotnet test Tests/Tests.Analyzers` via a `with_analyzer_tests` parameter (default true), wired through `build.yml`/`default.yml`. DB-free, runs on the build agent — not the provider matrix.

## Registration

Both projects under `/Source/` and the test project under `/Tests/` in `linq2db.slnx` (via `/update-slnx`). New central package versions in `Directory.Packages.props`. Post-merge follow-ups: add the package to `/release-postpublish`'s expected-nuget list, and add a docs/wiki page for the rule (the descriptor's `helpLinkUri`).
