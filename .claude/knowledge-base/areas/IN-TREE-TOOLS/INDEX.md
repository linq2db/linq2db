---
area: IN-TREE-TOOLS
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 1/1
coverage_tier_2: 0/0
---

# IN-TREE-TOOLS

A single-file area: `Source/LinqToDB/Tools/DataExtensions.cs` (278 lines). Contains the public static class `DataExtensions` shipped inside the core `LinqToDB` assembly. Not to be confused with the `LinqToDB.Tools` *package* under `Source/LinqToDB.Tools/` (area [TOOLS](../TOOLS/INDEX.md)).

## Purpose

`DataExtensions` pre-fills identity columns on a collection of entities before a bulk insert. The caller passes a list of `T` entities; the method queries the database to determine starting identity values, then stamps them into each entity in-memory so the subsequent bulk copy can supply explicit values rather than rely on server-generated ones.

## Key types

| Type | Location | Role |
|---|---|---|
| `DataExtensions` | `DataExtensions.cs:17` | `public static class`; sole type in the area |

## Public surface

Both methods are extension methods on `IEnumerable<T>` with a `where T : notnull` constraint.

**`RetrieveIdentity<T>`** (`DataExtensions.cs:33`) — synchronous. Parameters:
- `source` — ordered list of entities.
- `context` — `IDataContext` used to execute queries.
- `useSequenceName` (default `true`) — query a named sequence for columns that carry `SequenceNameAttribute`.
- `useIdentity` (default `false`) — use `Sql.CurrentIdentity` / `Sql.IdentityStep` for SQL Server 2005+ identity columns.

**`RetrieveIdentityAsync<T>`** (`DataExtensions.cs:103`) — async equivalent; same parameters plus `CancellationToken`.

Return value for both: the original `source` reference if the entity had no identity columns; otherwise a new `List<T>` with identity values set. The collection-copy is always triggered when at least one identity column exists (`DataExtensions.cs:53`).

## Per-provider behavior matrix

| Condition | Mechanism | Providers |
|---|---|---|
| Column has `SequenceNameAttribute` and `useSequenceName=true` | `ISqlBuilder.GetReserveSequenceValuesSql` (`DataExtensions.cs:254`) | Oracle, PostgreSQL |
| `useIdentity=true` and `Sql.CurrentIdentity` returns non-null | `Sql.CurrentIdentity` + `Sql.IdentityStep` via `context.Select` (`DataExtensions.cs:70`) | SQL Server 2005+ |
| All other cases | `ISqlBuilder.GetMaxValueSql` → SELECT MAX; then increment by 1 per entity (`DataExtensions.cs:165`) | All remaining providers |

The identity-column path exits early after the first identity column when `useIdentity` succeeds (`DataExtensions.cs:75–77`), with the comment "current implementations (sql server) support single identity per table".

## Inbound / outbound dependencies

**Outbound (this area calls into):**

- [MAPPING](../MAPPING/INDEX.md) — `EntityDescriptor`, `ColumnDescriptor`, `SequenceNameAttribute`, `ColumnAttribute.IsIdentity`, `IdentityAttribute` (resolved via `context.MappingSchema.GetEntityDescriptor`; `DataExtensions.cs:47`).
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — `ISqlBuilder` (`DataExtensions.cs:163`), specifically `GetMaxValueSql`, `GetReserveSequenceValuesSql`, `BuildObjectName`.
- [CORE](../CORE/INDEX.md) — `IDataContext` (receiver of the extension methods).
- [INFRA](../INFRA/INDEX.md) — `Common.Converter.ChangeType` (`DataExtensions.cs:189`), `MemberAccessor.SetValue`.

**Inbound:** No in-tree callers found under `Source/`; this is a public API consumed by application code directly.

## Known issues / debt

- `DataExtensions.cs:16` carries a `// TODO: looks like this API needs refactoring. Why we even create collection copy here???`. The copy is triggered unconditionally on the first identity column found (`DataExtensions.cs:53`), even when `source` was already a list. No tracking issue found.
- `useIdentity` defaults to `false`, meaning the SQL Server identity path is opt-in. The parameter order (`useSequenceName` first, `useIdentity` second) is inverted from what a SQL Server user would expect as the more common fallback.
- Sync and async implementations are duplicated verbatim; there is no shared core (`DataExtensions.cs:163–223` vs `DataExtensions.cs:194–223`).

## See also

- [TOOLS](../TOOLS/INDEX.md) — `LinqToDB.Tools` package (scaffolding utilities, code generation); entirely separate from this area.
- [MAPPING](../MAPPING/INDEX.md) — `SequenceNameAttribute`, `IdentityAttribute`, `ColumnAttribute.IsIdentity`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — `ISqlBuilder.GetReserveSequenceValuesSql`, `ISqlBuilder.GetMaxValueSql`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 1 / 1 ✓
  - Source/LinqToDB/Tools/DataExtensions.cs
- Tier 2 (visited / total): 0 / 0 ✓
- Tier 3 (skipped, logged): 0
</details>
