---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
---

# New Sql.Window fluent API replaces legacy Sql.Ext analytic pipeline

## Context

The original window-function support (2025, commit `9cc8fae`, #5168) exposed Over()/PartitionBy()/OrderBy()/Rows()/Range() as a legacy Sql.Ext-based analytic API. It lacked several standard window functions (PercentileCont/Disc, Lead/Lag null treatment, FIRST_VALUE/LAST_VALUE/NTH_VALUE frame semantics, KEEP, statistical/regression aggregates, hypothetical-set functions), had no per-provider capability model, and its FILTER/NULLS handling was inconsistent across providers.

## Decision

PR #5468 (commit `8e30fc0`, 116 files, 11096 insertions/1293 deletions) introduced a new `Sql.Window.*` fluent builder API (`Sql.Window.cs`) as the primary window-function surface, backed by `WindowFunctionsMemberTranslator` -- a per-function, per-provider capability-flag translator (`IsCumeDistSupported`, `IsPercentileContSupported`, `IsFrameGroupsSupported`, `IsFrameExclusionSupported`, `IsStdDevSupported`, `IsCovarianceSupported`, etc.) that emits a translate-time error when a provider lacks a feature instead of emitting SQL the engine would reject. New AST types (`SqlExtendedFunction`, `SqlFrameClause`, `SqlKeepClause`) moved to/landed in `LinqToDB.Internal.SqlQuery`. `LegacyMemberConverterBase` transparently rewrites old `Sql.Ext.*().Over()....ToValue()` expression trees onto the new pipeline at build time, so existing code compiled against the old API keeps working. `TranslationProviderFlags` (via `ITranslationContext.ProviderFlags`) exposes `DefaultNullsOrdering`/`IsNullsOrderingSupported` so window ORDER BY reuses the same NULLS-emulation lowering as regular ORDER BY. FILTER (WHERE ...) is emulated via CASE WHEN on providers without native support (PostgreSQL/DuckDB have it natively). The PR also added KEEP (DENSE_RANK FIRST/LAST), DISTINCT-in-window, explicit frame-boundary direction (`ValuePreceding`/`ValueFollowing`), statistical/regression aggregates (StdDev/Variance/CovarPop/CovarSamp/Corr, nine `Regr*`), RATIO_TO_REPORT, MEDIAN, and hypothetical-set Rank/DenseRank/PercentRank/CumeDist.

## Why

Capability flags per function (not one blanket `IsWindowFunctionsSupported`) were needed because provider support is uneven at function granularity (e.g. SQL Server pre-2012 supports non-ordered aggregate windows but not ranking functions; SAP HANA supports ROWS frames but rejects RANGE/GROUPS/EXCLUDE). Routing legacy expression trees through a converter rather than a breaking rename let the old `Sql.Ext` surface keep compiling while concentrating all provider logic in one translator hierarchy. A `WindowFunctions.FeatureMatrix.md` reference doc was added to track the per-provider support matrix so future gaps are documented rather than rediscovered.

## Consequences

- `Sql.Window.*` is the primary public window-function API; `Sql.Ext.*` analytic chains still compile via the legacy converter.
- `WindowFunctionBuilder` is now public.
- `Sql.Window.Rank()` returns `long` (was reconciled from `int` via a cast) to match the other ranking functions.
- Per-provider capability flags gate function availability at translate time with a descriptive `LinqToDBException` instead of a runtime SQL error.
- Known unresolved fidelity gap: ClickHouse `MEDIAN` (its `median`/`medianExact` are semantically different from the standard interpolated MEDIAN this API models) -- documented, not emulated.
- DB2 `RATIO_TO_REPORT` over a zero-sum partition yields DECFLOAT Infinity, unmappable to `decimal`; gated `[ActiveIssue] #5663`.

## Sources

- Commit `8e30fc0` -- Window Functions: new Sql.Window API (#5468) (Svyatoslav Danyliv, 2026-07-03)
- PR #5468
- File anchors: `Source/LinqToDB/Sql/Sql.Window.cs`, `Source/LinqToDB/Linq/Translation/WindowFunctionsMemberTranslator.cs`, `Source/LinqToDB/Sql/WindowFunctions.FeatureMatrix.md`
