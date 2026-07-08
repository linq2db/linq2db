---
area: INTERNAL-API
kind: tech-debt
sources: [code, gh-issues]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# INTERNAL-API — Tech debt

## Severity distribution

| Severity | Count |
|---|---|
| low | 23 |
| med | 9 |

## By category

| Category | Count |
|---|---|
| todo-fixme | 23 |
| perf-smell | 8 |
| code-smell | 1 |

## Top issues (up to 20)

| ID | Severity | Category | Title | File |
|---|---|---|---|---|
| [DI-0063](../../detected-issues/items/DI-0063.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:15` |
| [DI-0064](../../detected-issues/items/DI-0064.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:21` |
| [DI-0065](../../detected-issues/items/DI-0065.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:27` |
| [DI-0066](../../detected-issues/items/DI-0066.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:33` |
| [DI-0067](../../detected-issues/items/DI-0067.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:38` |
| [DI-0068](../../detected-issues/items/DI-0068.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:43` |
| [DI-0069](../../detected-issues/items/DI-0069.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Async/SafeAwaiter.cs:48` |
| [DI-0075](../../detected-issues/items/DI-0075.md) | med | perf-smell | Async-over-sync blocking call | `Source/LinqToDB/Internal/Common/StackGuard.cs:80` |
| [DI-0176](../../detected-issues/items/DI-0176.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/Remote/LinqServiceSerializer.cs:624` |
| [DI-0070](../../detected-issues/items/DI-0070.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Cache/CacheEntry.cs:295` |
| [DI-0071](../../detected-issues/items/DI-0071.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Cache/CacheItemPriority.cs:6` |
| [DI-0072](../../detected-issues/items/DI-0072.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Cache/MemoryCache.cs:214` |
| [DI-0073](../../detected-issues/items/DI-0073.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Cache/MemoryCache.cs:268` |
| [DI-0074](../../detected-issues/items/DI-0074.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Cache/MemoryCache.cs:435` |
| [DI-0129](../../detected-issues/items/DI-0129.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Translation/ConvertMemberTranslatorDefault.cs:472` |
| [DI-0130](../../detected-issues/items/DI-0130.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs:250` |
| [DI-0131](../../detected-issues/items/DI-0131.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs:267` |
| [DI-0132](../../detected-issues/items/DI-0132.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Translation/DateFunctionsTranslatorBase.cs:553` |
| [DI-0133](../../detected-issues/items/DI-0133.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Translation/SqlTypesTranslationDefault.cs:29` |
| [DI-0138](../../detected-issues/items/DI-0138.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/AssemblyResolver.cs:31` |

Plus 12 more entries — see [detected-issues index](../../detected-issues/index.json) (filter by area: `INTERNAL-API`).

## See also

- [INDEX.md](INDEX.md) — area overview
- [issues.md](issues.md) — GitHub themes
- [decisions.md](decisions.md) — recorded decisions
- [patterns.md](patterns.md) — area patterns
