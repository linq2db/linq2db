---
area: SQL-AST
kind: decision
sources: [git]
confidence: medium
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
---

# Introduce SqlConcatExpression and wire string.Concat across all providers

## Context

string.Concat translation previously lacked a dedicated SQL AST node. Each provider had ad-hoc handling, the `withoutSeparator` path in string.Concat aggregate overloads was silently ignored by all 15 provider implementations, and `string.Concat(IEnumerable<>)` / `Sql.Concat(...)` calls failed silently as a result. The problem was reported via multiple provider-specific gaps and confirmed by the skipped tests `Concat_BothArgsNonNull_SqlConcat_ReturnsNonNull` and `Concat_StringArray_FromArrayLiteral`.

## Decision

PR #5504 (commit `de4d000`) introduced `SqlConcatExpression` as a new SQL AST node and rewired all string-concatenation paths through it. `SqlExpressionConvertVisitor.ConvertConcat` lowers the node to a CAST-wrapped `SqlBinaryExpression("+", ...)` chain, so existing per-provider rewrites (`||`, `CONCAT`, native `+`) handle it without new provider-specific code. `PreserveNull=false` wraps each child in `COALESCE(x, '')`. `WrapParametersVisitor` and `SelectQueryOptimizerVisitor` received matching overrides. All 15 provider `TranslateStringJoin` overrides were corrected to branch on `withoutSeparator`: use `HasSequenceIndex(0)` instead of `(1)`, skip `TranslateArguments(0)`, and emit the native empty-string-separator aggregate (`LISTAGG`, `GROUP_CONCAT`, `STRING_AGG`, `LIST`, etc.). Oracle additionally received a missing `ConfigureConcatWsEmulation` plain-path fallback. A `StringConcatTests.cs` test file was added with a dedicated test entity. 89 files changed, 4159 insertions.

## Why

No rationale was stated explicitly in the commit body beyond fixing the broken `withoutSeparator` path and unifying the AST representation. The choice of a dedicated AST node (rather than continuing with ad-hoc per-provider hacks) matches the pattern established by other typed SQL expressions in the codebase. The `COALESCE`-wrapping on `PreserveNull=false` was the only documented behavioral option mentioned.

## Consequences

- `SqlConcatExpression` is a new node in the SQL AST that all visitors and providers must handle.
- `string.Concat(IEnumerable<>)` and `Sql.Concat(...)` now produce correct SQL on all providers instead of silently returning null on empty groups.
- The `--` separator path in aggregate string join no longer silently falls through.
- `WrapParametersVisitor` and `SelectQueryOptimizerVisitor` gained new override points for the new node type.
- Previously skipped tests were un-skipped; grouping and nullability tests were added.

## Sources

- Commit `de4d000` -- Wire string.Concat translation across all providers (Svyatoslav Danyliv, 2026-05-xx)
- PR #5504
- File anchors: `Source/LinqToDB/Internal/SqlQuery/SqlConcatExpression.cs`, `Source/LinqToDB/Internal/DataProvider/Translation/StringMemberTranslatorBase.cs`
