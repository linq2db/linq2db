---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
---

# "Now" translation split into four virtuals; new UTC and TZ public members

## Context

Before PR #5467, `DateFunctionsTranslatorBase` exposed a single `TranslateNow` virtual that providers overrode to handle `DateTime.Now`, `DateTime.UtcNow`, `Sql.CurrentTimestamp`, and `Sql.CurrentTimestamp2`. There was no dedicated translation path for UTC-wall-clock variants or for timezone-aware (`DateTimeOffset`) variants, making it impossible for providers to distinguish "local now", "UTC now", "local now with offset", and "UTC now with offset" at the translation layer.

## Decision

Commit `d779808a2` (PR #5467, 2026-05-10) split the single override into four protected virtual methods on `DateFunctionsTranslatorBase`:

- `TranslateNow` -- `DateTime.Now` and fallback for `Sql.CurrentTimestamp` / `Sql.CurrentTimestamp2` / `Sql.GetDate()`.
- `TranslateUtcNow` -- `DateTime.UtcNow` and the new `Sql.CurrentTimestampUtc` member.
- `TranslateZonedNow` -- `DateTimeOffset.Now`.
- `TranslateZonedUtcNow` -- `DateTimeOffset.UtcNow` and the new `Sql.CurrentTzTimestamp` member.

Default implementations: `TranslateNow` returns `CURRENT_TIMESTAMP`; `TranslateUtcNow` returns `null` (not translated -- providers opt in); `TranslateZonedNow` delegates to `TranslateNow`; `TranslateZonedUtcNow` delegates to `TranslateUtcNow`. Providers that cannot express UTC or TZ timestamps server-side simply inherit the defaults and the expression falls back to client-side evaluation.

Two new public members were added to `Sql`:

- `Sql.CurrentTimestampUtc` (`DateTime`) -- server-side UTC equivalent; routes to `TranslateUtcNow`.
- `Sql.CurrentTzTimestamp` (`DateTimeOffset`) -- server-side TZ-aware equivalent with per-provider `[Function]` / `[Property]` attributes for SQL Server (`SYSDATETIMEOFFSET`), PostgreSQL (`now()`), Oracle (`SYSTIMESTAMP`), ClickHouse (`now()`), and Ydb (`CurrentUtcTimestamp`).

## Why

The split was needed to let providers emit distinct SQL for UTC vs local vs TZ-aware "now" without each provider having to inspect call-site context inside a single override. The design follows the existing per-type-per-member pattern established by the `TranslateDateTimeDatePart` / `TranslateDateTimeOffsetDatePart` split.

## Consequences

- `DateFunctionsTranslatorBase` gains three new protected virtual methods; any provider subclass overriding the old single `TranslateNow` for UTC behavior must migrate its UTC logic to the new `TranslateUtcNow` override.
- `Sql.CurrentTimestampUtc` and `Sql.CurrentTzTimestamp` are new public API surface on `Sql`.
- `Sql.CurrentTzTimestamp` carries legacy `[Function]`/`[Property]` attributes for the providers listed above (pre-translator-refactor style); remaining providers go through the translator path.

## Sources

- Commit `d779808a2` -- Improve 'now' translation to support TZ and UTC variations (#5467) (MaceWindu, 2026-05-10)
- PR #5467
- File anchors: `Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs`, `Source/LinqToDB/Sql/Sql.cs`
