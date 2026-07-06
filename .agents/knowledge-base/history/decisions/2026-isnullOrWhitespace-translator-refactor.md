---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd242e6399daa081ca
---

# string.IsNullOrWhiteSpace migration to StringMemberTranslator

## Context

string.IsNullOrWhiteSpace translation was implemented via the internal Sql.IsNullOrWhiteSpace helper and 11 IExtensionCallBuilder classes, one per provider variant. This was inconsistent with how string.TrimStart/TrimEnd and similar methods were handled after the #5515 StringMemberTranslatorBase refactor.

## Decision

The IExtensionCallBuilder approach was replaced with a virtual TranslateIsNullOrWhiteSpace on StringMemberTranslatorBase and per-provider overrides (commit 5999b4f, Jun 9, #5544). The 11 extension-builder classes were removed. VisitConditional was fixed to short-circuit on a known-bool test before visiting branches. Provider details: SqlCe uses nested factory.Function calls instead of a 1.5 kB literal SQL template; ClickHouse uses decomposed factory.Function/factory.Coalesce calls; Firebird/MySQL use factory.LikePredicate with SIMILAR TO / RLIKE to avoid boolean-type wrapping on Firebird 2.5 (no BOOLEAN type). A U+202F narrow no-break space was added to the whitespace character set.

## Why

Consistency with the StringMemberTranslatorBase pattern from #5515. The factory-based approach lets providers inherit DbDataType from the value column. Using factory.LikePredicate for SIMILAR TO and RLIKE avoids emitting expression = 0 against the predicate result on Firebird 2.5.

## Consequences

- 11 IExtensionCallBuilder classes removed.
- Sql.IsNullOrWhiteSpace internal helper removed.
- factory.LikePredicate isNull parameter renamed to isNot (PublicAPI.Shipped.txt updated).
- File anchors: Source/LinqToDB/Expressions/StringMemberTranslatorBase.cs

## Sources

- Commit 5999b4f -- Translate string.IsNullOrWhiteSpace via StringMemberTranslator (#5544) (Igor Tkachev, 2026-06-09)
