# GitHub Copilot Instructions

**Canonical contributor rules live in [`AGENTS.md`](../AGENTS.md) at the repository root** — read it for build, test, conventions, branching, commit/publish discipline, GitHub authoring etiquette, and security rules. The notes below are Copilot-specific additions for pull-request review.

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
