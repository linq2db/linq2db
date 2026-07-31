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

## Indentation

- Respect the repository's `.editorconfig` for indentation rules.

## Testing

- Use Shouldly for assertions in tests instead of NUnit Assert.
