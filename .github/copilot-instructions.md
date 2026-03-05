# Copilot Pull Request Review Rules

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
