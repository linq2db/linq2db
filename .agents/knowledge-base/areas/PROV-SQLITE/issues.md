---
area: PROV-SQLITE
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# PROV-SQLITE -- GitHub themes

## Open themes

- **DateTime/DateTimeOffset translation** -- Recurring SQL generation bugs around DateTime and DateTimeOffset in SQLite. DateTime wrapping, DateTimeOffset comparisons (!=, DateAdd), and timezone handling produce incorrect SQL. Sample open: #2432 (DateTime wrapping), #1855 (DateTimeOffset comparisons), #3766 (DateTimeOffset as PK), #2107 (UTC bulk insert).
- **Lateral/Apply join emulation** -- SQLite lacks LATERAL/APPLY syntax; several queries need cross-apply or outer-apply semantics. Requests to emulate via subqueries or alternative patterns. Sample open: #4988, #5396.
- **JSON functions** -- Support for sqlite json_each/json_tree and other JSON handling functions. #3408.
- **Connection/Runtime issues** -- Microsoft.Data.Sqlite version compatibility (ClearAllPools method not found on older TFMs or target runtimes) and LINQPad driver regression on macOS. Sample open: #4446, #5497.

## Resolved themes

- **DateTime/DateTimeOffset in SQLite** -- 15+ closed issues around DateTime/DateTimeOffset handling. Includes identity columns with DateTimeOffset, bulk insert UTC preservation, DateTime.MaxValue comparisons. Sample closed: #2099, #934, #4904, #5035.
- **Schema/Scaffolding** -- 8+ closed issues around T4 schema generation, composite keys, temp views, schema refresh after CreateTable. Sample closed: #4736, #3330, #1269, #4117, #4449.
- **Bulk operations** -- Several closed issues on bulk insert identity handling, column default values, FK constraints. Sample closed: #5282.
- **Type mapping & conversion** -- Closed issues on blob/bytea handling, GUID mapping, custom type serialization. Sample closed: #3070.

## Active discussions

- [How to use relative path for LoadSQLiteMetadata?](https://github.com/linq2db/linq2db/discussions/3126) -- [Q&A] Path resolution for LoadSQLiteMetadata relative to project directory; users want $(ProjectDir) style substitution.
- [SQLite PRAGMA functions JOIN with sqlite_master](https://github.com/linq2db/linq2db/discussions/4985) -- [Q&A] Joining PRAGMA result sets (e.g. pragma_table_info) with sqlite_master; metadata/schema introspection use case.
- [Howto limit to limit characters number in a SQLite text column](https://github.com/linq2db/linq2db/discussions/5120) -- [Q&A] Column constraints for TEXT columns; DbType annotations ignored by SQLite.

## Stats

- Open issues: 9
- Closed issues: 79
- Open PRs: 0
- Total PRs: 28
- Discussions: 12
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 128 (88 issues + 28 PRs + 12 discussions)
- Themes extracted: 8 (4 open, 4 resolved)

</details>
