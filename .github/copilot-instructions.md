# Copilot Pull Request Review Rules

## General Guidelines
- Preserve the user's manual code changes and do not overwrite or revert them.
- When optimizing reader logic, avoid per-read dictionary lookups in hot paths. For hot-path reader logic in this repository, avoid dictionaries entirely; prefer a self-rewriting lambda/delegate where `ReadSqlServerDecimal` calls the current implementation and the implementation can replace itself after the first overflow.

## Formatting rules

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

## Indentation Rules
- Respect the repository's `.editorconfig` for indentation rules.

## Testing Guidelines

- Use Shouldly for assertions in tests instead of NUnit Assert.
