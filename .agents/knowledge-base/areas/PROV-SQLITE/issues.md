---
area: PROV-SQLITE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: df84c7784dd1dc8db9f3c6a9c4b0c2e9f7a5b3d1
---

# PROV-SQLITE -- GitHub themes

## Open themes

- **DateTime/DateTimeOffset translation** -- SQL generation bugs in DateTime and DateTimeOffset handling. DateTimeOffset comparisons with !=, DateAdd produce incorrect SQL; bulk inserts don't preserve UTC; wrapping DateTime in DateTime() function breaks type coercion. Sample open: #1855 (comparisons), #2107 (bulk UTC), #2432 (wrapping), #3766 (DateTimeOffset as PK).
- **Lateral/Apply join emulation** -- SQLite lacks LATERAL/APPLY syntax; cross-apply and outer-apply queries need subquery-based translation or workarounds. FULL OUTER JOIN can be emulated via APPLY in some cases. Sample open: #4988 (apply translation), #5396 (lateral/apply emulation), #1224 (FULL OUTER JOIN).
- **Type mapping & conversion** -- Column affinity and CAST issues. CAST(x AS Decimal) retains numeric affinity on SQLite, causing integer division; unsigned integer (ulong) mapping loses precision with MaxValue. Sample open: #5611 (decimal division), #3899 (unsigned int mapping).
- **JSON functions** -- Support for sqlite json_each/json_tree table-valued functions. #3408.
- **Connection/Runtime issues** -- Microsoft.Data.Sqlite version compatibility (ClearAllPools method unavailable on older TFMs or Android target runtime). Sample open: #4446.
- **Interceptor callbacks** -- CompiledQuery does not invoke IEntityServiceInterceptor.EntityCreated(). Works with normal queries but not compiled. Sample open: #4365.
- **Dynamic columns with FromQuery** -- Custom SQL queries via FromSql do not preserve dynamic column mappings; dynamic column results lost when using IDataContext.FromQuery(...). Sample open: #2953.

## Resolved themes

- **DateTime/DateTimeOffset in SQLite** -- 15+ closed issues. Includes identity columns with DateTimeOffset, bulk insert UTC preservation, DateTime.MaxValue comparisons, DateTime serialization. Sample closed: #2099, #934, #4904, #5035.
- **Schema/Scaffolding** -- 8+ closed issues on T4 schema generation, composite keys, temp views, schema refresh after CreateTable. Sample closed: #4736, #3330, #1269, #4117, #4449.
- **Bulk operations** -- Several closed issues on bulk insert identity handling, column default values, FK constraints. Sample closed: #5282.
- **Type mapping & conversion** -- Closed issues on blob/bytea handling, GUID mapping, custom type serialization. Sample closed: #3070.

## Active discussions

- [How to use relative path for LoadSQLiteMetadata?](https://github.com/linq2db/linq2db/discussions/3126) -- [Q&A] Path resolution for LoadSQLiteMetadata relative to project directory; users want  style substitution.
- [SQLite PRAGMA functions JOIN with sqlite_master](https://github.com/linq2db/linq2db/discussions/4985) -- [Q&A] Joining PRAGMA result sets (e.g. pragma_table_info) with sqlite_master; metadata/schema introspection use case.
- [Howto limit to limit characters number in a SQLite text column](https://github.com/linq2db/linq2db/discussions/5120) -- [Q&A] Column constraints for TEXT columns; DbType annotations ignored by SQLite.

## Stats

- Open issues: 13
- Closed issues: 76
- Open PRs: 0
- Total PRs: 25
- Discussions: 12
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 91 (65 issues + 25 PRs + 12 discussions)
- Themes extracted: 7 (7 open, 4 resolved)

</details>
