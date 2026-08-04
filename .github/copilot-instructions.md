# GitHub Copilot Instructions

This file is **self-contained on purpose.** The full contributor ruleset lives in `.claude/AGENTS.md`, inside the [linq2db/agents](https://github.com/linq2db/agents) submodule — which is *not* checked out for GitHub-side review, so anything a reviewer must honour is stated here rather than linked. A local agent (Copilot in an IDE, with the submodule populated) should also read `.claude/AGENTS.md` for the complete rules.

## Repository invariants worth flagging in review

- **Generated files are never hand-edited:** `Source/**/CompatibilitySuppressions.xml` (ApiCompat baselines) and `linq2db.baselines` test baselines are tool output. A hand-written change to them is a finding.
- **New or changed public API** needs the matching `PublicAPI.Unshipped.txt` entry and XML doc comments on the new public types/members (`TreatWarningsAsErrors` is on, so a dangling `<see cref="…"/>` is a build error, not doc rot).
- **Never interpolate a value into a SQL string.** linq2db generates SQL: a concatenated value is SQL injection by construction. Values go through a parameter or a `Sql.*` / AST builder.
- **Tabs for C#/VB; spaces for F#, YAML, shell, markdown.** Target frameworks include `net462` and `netstandard2.0`, so a BCL API newer than .NET Standard 2.0 needs a polyfill rather than an unguarded call.
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

- Use Shouldly for assertions in tests instead of NUnit Assert.

## AI Documentation Consistency

The `Source/Skills/linq2db/docs/` directory contains machine-readable references consumed by AI agents. When reviewing a pull request, flag any of the following mismatches as a comment. Do not flag these if the PR already includes a matching update to the relevant document.

**Provider setup and capabilities**

- **`ProviderName.cs` changed** (constant added, removed, or renamed): verify `Source/Skills/linq2db/docs/provider-setup.md` `ProviderName` constants tables are up to date.

- **`DataOptionsExtensions.Provider.cs` changed** (new or modified `UseXxx` method or parameter): verify `Source/Skills/linq2db/docs/provider-setup.md` method signatures and enum tables reflect the change.

- **Any `*Version.cs` or `*Provider.cs` enum file changed** (value added or removed): verify the corresponding enum table in `Source/Skills/linq2db/docs/provider-setup.md` is up to date.

- **`SqlProviderFlags` changed**, or a provider's SQL builder gained or lost a feature (MERGE, CTE, window functions, APPLY/LATERAL, OUTPUT/RETURNING, bulk copy, upsert): verify `Source/Skills/linq2db/docs/provider-capabilities.md` matrix row for that provider is correct.

- **A translator registration changed** in `StringMemberTranslatorBase`, `MathMemberTranslatorBase`, `DateFunctionsTranslatorBase`, `ConvertMemberTranslatorDefault`, or any `*MemberTranslator*.cs` (method added, removed, or renamed): verify `Source/Skills/linq2db/docs/translatable-methods.md` reflects the change (table row added, removed, or updated).

- **`DataOptionsExtensions.cs` changed** — a `UseXxx` method added, removed, or its behavior changed (connection, tracing, retry, interceptors, member translators): verify `Source/Skills/linq2db/docs/configuration.md` reflects the change.

**AI metadata**

- **`<ai-tags />` or `<ai-tags-defaults />` XML-doc element added or modified**: every attribute and value must match the vocabulary defined in `Source/Skills/linq2db/docs/ai-tags.md` - flag unknown attributes or values for known attributes. Multi-value fields are comma-separated (`affects="DdlStatement,Data"`), not semicolon-separated.

- **Behaviour of an already-tagged API changes**: if `execution`, `composability`, `affects`, or `pipeline` semantics change in this PR (e.g., a deferred query becomes immediate, or DDL is added or removed), flag that the corresponding `<ai-tags />` metadata needs updating.

- **New public API added** that issues SQL directly or implements `IQueryable<T>` / `ITable<T>`: flag if no appropriate `<ai-tags />` metadata is present next to the XML documentation.
