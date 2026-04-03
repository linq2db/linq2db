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

## Code Style

- In the LinqToDB project, **use tabs for indentation**, not spaces. Always use tabs when editing `.csproj` and other project files.

## Testing Guidelines

- Use Shouldly for assertions in tests instead of NUnit Assert.

## AI Documentation Consistency

The `docs/` directory contains machine-readable references consumed by AI agents. When reviewing a pull request, flag any of the following mismatches as a comment. Do not flag these if the PR already includes a matching update to the relevant document.

**Provider setup and capabilities**

- **`ProviderName.cs` changed** (constant added, removed, or renamed): verify `docs/provider-setup.md` `ProviderName` constants tables are up to date.

- **`DataOptionsExtensions.Provider.cs` changed** (new or modified `UseXxx` method or parameter): verify `docs/provider-setup.md` method signatures and enum tables reflect the change.

- **Any `*Version.cs` or `*Provider.cs` enum file changed** (value added or removed): verify the corresponding enum table in `docs/provider-setup.md` is up to date.

- **`SqlProviderFlags` changed**, or a provider's SQL builder gained or lost a feature (MERGE, CTE, window functions, APPLY/LATERAL, OUTPUT/RETURNING, bulk copy, upsert): verify `docs/provider-capabilities.md` matrix row for that provider is correct.

- **A translator registration changed** in `StringMemberTranslatorBase`, `MathMemberTranslatorBase`, `DateFunctionsTranslatorBase`, `ConvertMemberTranslatorDefault`, or any `*MemberTranslator*.cs` (method added, removed, or renamed): verify `docs/translatable-methods.md` reflects the change (table row added, removed, or updated).

- **`DataOptionsExtensions.cs` changed** — a `UseXxx` method added, removed, or its behavior changed (connection, tracing, retry, interceptors, member translators): verify `docs/configuration.md` reflects the change.

**AI-Tags**

- **`AI-Tags:` comment added or modified**: every `Key=Value` pair must match the vocabulary defined in `docs/ai-tags.md` — flag unknown keys or values for known keys. Multi-value fields are comma-separated (`Affects=DdlStatement,Data`), not semicolon-separated.

- **Behaviour of an already-tagged class changes**: if `Execution`, `Composability`, `Affects`, or `Pipeline` semantics change in this PR (e.g., a deferred query becomes immediate, or DDL is added or removed), flag that the `AI-Tags:` comment on that class needs updating.

- **New public class added** that issues SQL directly or implements `IQueryable<T>` / `ITable<T>`: flag if no `AI-Tags:` comment is present in its `<remarks>` block.
