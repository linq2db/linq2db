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

## Indentation Rules
- Respect the repository's `.editorconfig` for indentation rules.

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

**AI metadata**

- **`<ai-tags />` or `<ai-tags-defaults />` XML-doc element added or modified**: every attribute and value must match the vocabulary defined in `docs/ai-tags.md` - flag unknown attributes or values for known attributes. Multi-value fields are comma-separated (`affects="DdlStatement,Data"`), not semicolon-separated.

- **Behaviour of an already-tagged API changes**: if `execution`, `composability`, `affects`, or `pipeline` semantics change in this PR (e.g., a deferred query becomes immediate, or DDL is added or removed), flag that the corresponding `<ai-tags />` metadata needs updating.

- **New public API added** that issues SQL directly or implements `IQueryable<T>` / `ITable<T>`: flag if no appropriate `<ai-tags />` metadata is present next to the XML documentation.
