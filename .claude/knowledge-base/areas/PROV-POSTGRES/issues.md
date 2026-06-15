---
area: PROV-POSTGRES
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-POSTGRES -- GitHub themes

## Open themes

- **Npgsql version compatibility & driver upgrades** -- Ongoing cycle of supporting new Npgsql major versions (8, 9) and PostgreSQL server releases (15–18). Recent wins: version detection via connection, BigInteger support, cache miss fixes in UNNEST/GenerateSeries (#4719, #4309, #5484, #5470).

- **Type mapping for PostgreSQL-native types** -- JSON/JSONB, enum, custom scalar types (ltree, pgvector) remain underserved. Users struggle with custom type registration and mapping interceptors (#4707, #4856, #3869, #5505, #4818).

- **Bulk copy & upsert completeness** -- BulkCopy has grown timeout support and IgnoreConflicts option, but edge cases linger: KeepIdentity sequence update, transaction handling, MERGE/WithOutput coverage (#5395, #5377, #4702, #4615, #4934).

- **Schema handling & identifier quoting** -- Case-sensitivity mismatch, quoted identifiers in scaffold and mapping (#4286, #4708, #3447). Occasional regressions in UPDATE FROM generation (#3649, #3186).

- **Performance regressions & query cache misses** -- FromSqlScalar mapper compilation per call (6x slowdown), correlations lost in subqueries over unnest/from-sql, deferred evaluation cliffs (#5480, #5285, #5169, #4812).

## Resolved themes

- **RETURNING clause support** -- Full support for OUTPUT-style RETURNING across INSERT/UPDATE/DELETE, including MergeWithOutput (#3328, #4934).
- **PostgreSQL 15, 16, 17 version support** -- Steadily tracked via scaffold & compatibility work (#3705, #4271, #4681, #5005).
- **Enum type mapping** -- Core enum support in place, though edge cases with untyped values or binary operations still surface (#3461, #4049, #4945, #5416, #5286).
- **Array & unnest support** -- UNNEST translation and array parameter handling core, with recent cache-miss optimizations (#5470, #5484).

## Active discussions

- [NpgsqlDataSource support (AutoDetect server version, passwordless connection string)](https://github.com/linq2db/linq2db/issues/5248) — [General] Future integration with Npgsql 8+ native pooling and modern connection patterns.
- [Missing InsertOrActionWithOutput methods](https://github.com/linq2db/linq2db/issues/4824) — [Q&A] Gap in merge/upsert API surface for PostgreSQL.
- [Postgresql: jsonb jsonpath querying support](https://github.com/linq2db/linq2db/issues/3869) — [Enhancement] Native @> / @@ operator translation for JSONB.

## Stats

- Open issues: 24
- Closed issues: 189
- Open PRs: 1
- Total PRs: 85
- Discussions: 30
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 328 (213 issues + 85 PRs + 30 discussions)
- Themes extracted: 5
</details>
