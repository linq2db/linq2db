---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
---

# Enum.HasFlag translated to bitwise AND in SQL

## Context

`Enum.HasFlag(Enum flag)` is a common .NET API for testing bit-flag enum values. Before PR #5503, calling it inside a LINQ query against a database column was not translated to SQL; the call would either evaluate on the client (causing a full table scan) or throw. Users with bit-flag enum columns had to write explicit bitwise expressions manually.

## Decision

Commit `9272e491d` (PR #5503, 2026-05-10) added `ProcessHasFlag` to `ProviderMemberTranslatorDefault`, the base translator that all providers inherit. The translation:

1. Strips the `Convert(x, typeof(Enum))` boxing that `Enum.HasFlag` imposes on its receiver and argument (since the method is declared on `System.Enum`).
2. Verifies the enum is stored as an integer type (bitwise AND is only valid for integer storage).
3. Emits `(columnValue & flagValue) = flagValue` in SQL.
4. Handles nullability: when the column is nullable, wraps the expression in a `CASE WHEN column IS NULL THEN NULL` guard.

Because `ProcessHasFlag` lives in `ProviderMemberTranslatorDefault`, every provider that does not override `TranslateMethodCall` inherits the behavior automatically. Providers that already override `TranslateMethodCall` call `ProcessHasFlag` explicitly (the base class calls it before returning).

## Why

The bitwise AND approach (`(x & flag) = flag`) is standard SQL and is supported by every provider linq2db targets. Placing the translation in the default base class avoids duplicating it per provider. The `Convert(x, typeof(Enum))` stripping is necessary because the C# compiler boxes both operands to `System.Enum` at the call site, and the SQL translator cannot lower that abstract-type boxing automatically.

## Consequences

- `Enum.HasFlag` inside LINQ queries now translates to SQL on all providers.
- Enum columns must be stored as integers; if the mapping produces a non-integer DB type the translation is skipped and the call falls back to client evaluation (no exception).
- Nullable enum columns get a null guard in the emitted SQL.

## Sources

- Commit `9272e491d` -- Translate Enum.HasFlag (#5503) (MaceWindu, 2026-05-10)
- PR #5503
- File anchors: `Source/LinqToDB/Internal/DataProvider/Translation/ProviderMemberTranslatorDefault.cs`
