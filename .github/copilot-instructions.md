# GitHub Copilot Instructions

This file is **self-contained on purpose.** The full contributor ruleset lives in `.claude/AGENTS.md`, inside the [linq2db/agents](https://github.com/linq2db/agents) submodule — which is *not* checked out for GitHub-side review, so anything a reviewer must honour is stated here rather than linked. A local agent (Copilot in an IDE, with the submodule populated) should also read `.claude/AGENTS.md` for the complete rules.

## Repository invariants worth flagging in review

- **Generated files are never hand-edited:** `Source/**/CompatibilitySuppressions.xml` (ApiCompat baselines) and `linq2db.baselines` test baselines are tool output. A hand-written change to them is a finding.
- **New or changed public API** needs XML doc comments on the new public types/members (`TreatWarningsAsErrors` is on, so a dangling `<see cref="…"/>` is a build error, not doc rot). A missing `PublicAPI.Unshipped.txt` entry is **not** a finding — that drift is reconciled at release time.
- **Never interpolate a value into a SQL string.** linq2db generates SQL: a concatenated value is SQL injection by construction. Values go through a parameter or a `Sql.*` / AST builder.
- **Tabs for C#/VB; spaces for F#, YAML, shell, markdown.** Target frameworks include `net462` and `netstandard2.0`, so a BCL API newer than .NET Standard 2.0 needs a polyfill rather than an unguarded call.
- **Modern runtimes load the `netstandard2.0` build too**: an app gets it through a netstandard2.0 library, or because its runtime's TFM was dropped. A new `#if NETx_0_OR_GREATER` / `SUPPORTS_*` / `ADO_*` gate may withhold an opt-in feature (`DateOnly`, `DistinctBy`, …). It must not withhold recognition of a shape that net462-compilable source produces on a modern compiler or BCL, such as C# 14's span-based `array.Contains` (#6067), an overload re-binding, or a driver returning a modern type for a classic column. The same goes for a provider-discovery path (assembly names, probe lists). Those need name-based or `Type.GetType` detection instead.
- **Don't propose reformatting, renaming, or cleanup of lines the PR doesn't already touch** — the column-aligned formatting in this codebase is deliberate (see below).

## Pull request review — formatting

When performing a code review, **ignore minor or intentional formatting differences**, including:
- column-aligned code (table-like alignment),
- extra spaces inside qualified type names,
- minor spacing inconsistencies that do not affect readability,
- harmless whitespace or padding used for visual alignment.

When performing a code review, **comment on formatting only when it is clearly problematic**, such as:
- 3 or more consecutive blank lines,
- blank lines that contain only spaces or tabs,
- trailing whitespace repeated across multiple lines,
- indentation that is clearly broken (e.g., half-indented blocks or accidental deep indentation),
- mixed tabs and spaces *when it creates visibly misaligned code*.

## Using directives

- `using System;` is always the first using directive in every `.cs` file, even when that file's code doesn't reference any `System` type. This is an intentional, deliberate repo convention, not dead code — do not flag it for removal or suggest removing it as an "unused using" cleanup.

## Indentation

- Respect the repository's `.editorconfig` for indentation rules.

## Testing

- Use Shouldly for value assertions in tests instead of NUnit Assert.
- **Exception assertions use NUnit's constraint form:** `Assert.That(() => …, Throws.TypeOf<T>())` for an exact type, `Throws.InstanceOf<T>()` when subclasses are allowed, and `await Assert.ThatAsync(…)` in async code, with one `.With.Message…` / `.With.Property(…)` chain if needed. Use fully qualified `Shouldly.Should.Throw<T>` only when the exception object is needed afterwards, followed by `ShouldBeOfType<T>()` when the type must be exact. Never use Shouldly's `ThrowAsync` for a cancellation test: for a cancelled task it returns a new `TaskCanceledException` in place of the original `OperationCanceledException`. `Assert.Throws` / `ThrowsAsync` / `Catch` / `CatchAsync` / `DoesNotThrow*` and Shouldly's `NotThrow*` are banned (RS0030); for "does not throw", call the code directly.
- **New test code puts one call per line in a method chain**, for LINQ queries and assertion chains alike. A single call followed by an assertion (`query.ToList().ShouldBeEmpty();`) is a chain too. Prefer the query in a variable, asserted separately. Existing tests the PR does not rewrite are left as they are.
- **A narrowed provider set is a claim.** `[DataSources]` runs every configured provider. An exclusion in `[DataSources(...)]`, or an `[IncludeDataSources(...)]` that is not a single-provider translator test, needs a `//` line above the test naming why each provider is in or out: a measured failure, or the provider-specific mechanism the test exists for. A test of a provider-independent fix (shared optimizer, evaluator or builder code) with a narrowed set and no stated reason is a finding: widen it to `[DataSources]` and let a real failure justify each exclusion. This holds in particular when a reviewer proposed the test as `[DataSources]` and the committed version narrows it.

## AI Documentation Consistency

The `Source/Skills/linq2db/docs/` directory contains machine-readable references consumed by AI agents. When reviewing a pull request, flag any of the following mismatches as a comment. Do not flag these if the PR already includes a matching update to the relevant document.

**Provider setup and capabilities**

- **`ProviderName.cs` changed** (constant added, removed, or renamed): verify `Source/Skills/linq2db/docs/provider-setup.md` `ProviderName` constants tables are up to date.

- **`DataOptionsExtensions.Provider.cs` changed** (new or modified `UseXxx` method or parameter): verify `Source/Skills/linq2db/docs/provider-setup.md` method signatures and enum tables reflect the change.

- **Any `*Version.cs` or `*Provider.cs` enum file changed** (value added or removed): verify the corresponding enum table in `Source/Skills/linq2db/docs/provider-setup.md` is up to date.

- **`SqlProviderFlags` changed**, or a provider's SQL builder gained or lost a feature (MERGE, CTE, window functions, APPLY/LATERAL, OUTPUT/RETURNING, bulk copy, upsert): verify `Source/Skills/linq2db/docs/provider-capabilities.md` matrix row for that provider is correct.

- **A translator registration changed** in `StringMemberTranslatorBase`, `MathMemberTranslatorBase`, `DateFunctionsTranslatorBase`, `ConvertMemberTranslatorDefault`, or any `*MemberTranslator*.cs` (method added, removed, or renamed): verify `Source/Skills/linq2db/docs/translatable-methods.md` reflects the change (table row added, removed, or updated).

- **`DataOptionsExtensions.cs` changed** — a `UseXxx` method added, removed, or its behavior changed (connection, tracing, retry, interceptors, member translators): verify `Source/Skills/linq2db/docs/configuration.md` reflects the change.
