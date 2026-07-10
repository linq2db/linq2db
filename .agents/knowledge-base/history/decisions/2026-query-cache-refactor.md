---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
---

# Query cache: global bucketed dictionary with frequency-weighted eviction

## Context

Each `Query<T>` held its own small array-based plan cache. There was no cross-instance sharing, no eviction beyond that per-instance array's bound, and cache lifetime tracking was coarse.

## Decision

PR #5501 (commit `c58b446`, 10 files, 2080 insertions/229 deletions) replaced the per-`Query<T>` cache with a process-wide `QueryCache` (singleton `QueryCache.Default`), keyed by a composite of `(typeof(T), context type, ConfigurationID, QueryFlags, inline-parameters/entity-service flags, a 32-bit chain-hash of up to 8 levels of the LINQ source chain)`. Bucket-level lookup is O(1); the existing `Query.Compare` structural check remains the correctness fallback inside a bucket (hash collisions are correctness-preserving, not correctness-breaking). Each entry tracks a monotonic `ExpiresAtTicks` deadline and an EMA-smoothed hits-per-hour rate; idle timeout is frequency-weighted (1x base up to 24x for entries at >=500 hits/hour, decaying independently of sweep cadence), with pending-hit promotion between sweeps so a burst of activity extends an entry's life immediately rather than waiting for the next sweep. A per-bucket cap of 16 evicts the soonest-to-expire/coldest entry on overflow; a global cap (`DefaultMaxEntries = 10000`, overridable, 0 disables caching) triggers a sorted trim. A lazily-triggered, single-flighted 5-minute background sweep (offloaded to the thread pool) removes expired entries and empty buckets. `ClearAll` uses two-phase versioning so in-flight adds during a clear either retry or land in an orphaned, eventually-swept bucket rather than corrupting state. Separately, `CompiledTable<T>`'s translation cache moved from a shared `MemoryCache`-based `QueryRunner.Cache<T>` to a per-instance `ConcurrentDictionary`, tying its lifetime to the compiled delegate instead of a 1-hour sliding expiration.

## Why

A single global cache (vs. per-`Query<T>`) lets hot queries survive across `DataContext` instances and lets cold ones evict without every instance carrying its own bound array. The chain-hash bucket key was added because a `RootMember`-only discriminator collapsed queries that share a top-level method but operate on different sources (e.g. `db.Users.Count()` vs `db.Posts.Count()`) into one bucket, competing for its 16-entry cap. Frequency-weighted (not fixed) idle timeout keeps business-hours-hot queries alive overnight while still reclaiming one-off queries promptly. `QueryCache`/`QueryFlags`/`Query<T>(IDataContext)` were widened from `internal` to `public` because they now appear in `QueryCache`'s public surface and the eviction unit tests construct `Query<T>` directly; the class lives under `LinqToDB.Internal.Linq`, the documented use-at-your-own-risk namespace.

## Consequences

- New public surface: `QueryCache`, `QueryCache.FindResult`, `QueryFlags` (all under `LinqToDB.Internal.Linq`).
- `Query<T>.ClearCache()` / `Query.ClearCaches()` behavior is preserved; compiled-query (`CompiledTable<T>`) caches no longer participate in `ClearCaches()` since they're per-instance now.
- A `CacheActivityBenchmark` (BenchmarkDotNet, plus a manual Stopwatch-based runner for environments where the BDN child-process toolchain is blocked) was added to validate the refactor's hot-path cost against master.
- `QueryRunner.Cache<T>` (single-type-arg) is dead code post-refactor (left in place for a follow-up removal); `QueryRunner.Cache<T,TR>` remains in use by Insert/Delete/Update paths.

## Sources

- Commit `c58b446` -- Refactor query cache: global bucketed dictionary with eviction (#5501) (Svyatoslav Danyliv, 2026-06-14)
- PR #5501
- File anchors: `Source/LinqToDB/Internal/Linq/QueryCache.cs`, `Source/LinqToDB/Internal/Linq/QueryFlags.cs`, `Source/LinqToDB/Internal/Linq/Query.cs`
