# Provider-Specific Hints API Map

> ⚠️ **Stop. This document is incomplete by itself.**
> Before implementing anything, read [`AGENT_GUIDE.md`](../AGENT_GUIDE.md).
> It contains global rules, required namespaces, architecture constraints, and documentation navigation.
> Do not continue without reading it.

> You are here if you need to:
> - find a typed provider-specific hint API from a SQL hint keyword
> - verify whether a provider helper exists before using `QueryHint`, `TableHint`, raw SQL, or interceptors
> - choose the correct hint scope (`Table`, `TablesInScope`, `Join`, `SubQuery`, or `Query`)

---

This file is a retrieval index for concrete provider-specific hint helpers whose XML documentation names a SQL hint in `<c>...</c>` or whose implementation routes through a named provider hint constant.
In this map, "hint" is intentionally broad: it includes provider-specific table modifiers, lock
clauses, query directives, index directives, join modifiers, and optimizer hints exposed through
the LinqToDB hints API.
It is not a conceptual guide and not a substitute for XML-doc. For exact signatures, overloads, remarks, and package-version truth, inspect `lib/<TFM>/linq2db.xml`.

Use this map to go from SQL/database wording to LinqToDB API. If the required SQL hint is absent here, search XML-doc before falling back to generic APIs; absence from this map alone is not proof that the API does not exist.

Negative lookup rule: do not say "this map has no typed helper" from memory, semantic retrieval, or a partial provider summary. First perform an exact lookup in this map for both the provider heading and the SQL/database term from the request, then search `docs/api.md` / `lib/<TFM>/linq2db.xml` for the provider `*Hints` type. A negative answer about typed hint API existence is valid only after both checks fail.

Generic raw-text hint injectors such as `QueryHint(...)`, `TableHint(...)`, `TablesInScopeHint(...)`, `JoinHint(...)`, `SubQueryHint(...)`, and provider-specific low-level injectors are documented in [`docs/hints.md`](hints.md). This map focuses on typed helpers for concrete SQL hints.

Required use:

1. Search this map by provider and SQL hint text.
2. Use the API column only as a candidate.
3. When the candidate has `Hint type` = `Table`, search the same provider and SQL hint text for a
   `TablesInScope` row too. Table-local and scope-level helpers are different APIs with different
   receivers; choose based on whether the hint should apply to one table source or all table
   references in a query scope.
4. For requests that say "several tables", "all tables", "whole query", or "scope", search directly
   for `Hint type` = `TablesInScope` and API names containing `InScope` before considering
   `TablesInScopeHint("...")`. Apply the typed `TablesInScope` helper to the query/subquery that
   already contains the table references to affect; applying it to only the first table before
   adding joins does not automatically include later joined tables.
5. Treat method-name patterns as lookup hints, not as proof of an API. Common shapes are
   `<Base>Hint(...)` -> `<Base>InScopeHint(...)` and `With<Base>(...)` ->
   `With<Base>InScope(...)`. Provider aliases can exist, so do not invent names by string
   concatenation.
6. Verify the exact member in `lib/<TFM>/linq2db.xml` before writing code.
7. Prefer the typed provider-specific helper over generic raw hint APIs.
8. Use `QueryHint`, `TableHint`, `TablesInScopeHint`, `Sql.Expression`, raw SQL, or interceptors only when the map and
   XML-doc do not expose a suitable typed helper.

When answering, make the selected row visible in prose: name the typed helper and receiver you found
before showing the code. This prevents generic fallback APIs from overriding a concrete map hit.

## Summary

| Provider | Concrete helper rows | Hint types |
|---|---:|---|
| Access | 1 | SubQuery |
| ClickHouse | 38 | Join, Query, Table, TablesInScope |
| MySql | 107 | Index, Join, Query, SubQuery, Table, TablesInScope |
| Oracle | 118 | Index, Query, Table, TablesInScope |
| PostgreSQL | 12 | SubQuery |
| SqlCe | 15 | Index, Table, TablesInScope |
| SqlServer | 72 | Index, Join, Query, Table, TablesInScope |
| Ydb | 4 | Query |

## Map

### Access

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `WITH OWNERACCESS OPTION` | SubQuery | `WithOwnerAccessOption<TSource>(...)` | `IAccessSpecificQueryable&lt;TSource&gt;` |  |

### ClickHouse

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `ALL` | Join | `JoinAllHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ALL` | Join | `JoinAllHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ALL ANTI` | Join | `JoinAllAntiHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ALL ANTI` | Join | `JoinAllAntiHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ALL ANY` | Join | `JoinAllAnyHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ALL ANY` | Join | `JoinAllAnyHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ALL ASOF` | Join | `JoinAllAsOfHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ALL ASOF` | Join | `JoinAllAsOfHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ALL OUTER` | Join | `JoinAllOuterHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ALL OUTER` | Join | `JoinAllOuterHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ALL SEMI` | Join | `JoinAllSemiHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ALL SEMI` | Join | `JoinAllSemiHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ANTI` | Join | `JoinAntiHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ANTI` | Join | `JoinAntiHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ANY` | Join | `JoinAnyHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ANY` | Join | `JoinAnyHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `ASOF` | Join | `JoinAsOfHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `ASOF` | Join | `JoinAsOfHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `FINAL` | Table | `FinalHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `FINAL` | TablesInScope | `FinalInScopeHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `FINAL` | Table | `FinalInScopeHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` | Table receiver affects only that table source; for several tables use the `IClickHouseSpecificQueryable&lt;TSource&gt;` overload on the composed query scope. |
| `GLOBAL` | Join | `JoinGlobalHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `GLOBAL` | Join | `JoinGlobalHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `GLOBAL ANTI` | Join | `JoinGlobalAntiHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `GLOBAL ANTI` | Join | `JoinGlobalAntiHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `GLOBAL ANY` | Join | `JoinGlobalAnyHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `GLOBAL ANY` | Join | `JoinGlobalAnyHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `GLOBAL ASOF` | Join | `JoinGlobalAsOfHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `GLOBAL ASOF` | Join | `JoinGlobalAsOfHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `GLOBAL OUTER` | Join | `JoinGlobalOuterHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `GLOBAL OUTER` | Join | `JoinGlobalOuterHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `GLOBAL SEMI` | Join | `JoinGlobalSemiHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `GLOBAL SEMI` | Join | `JoinGlobalSemiHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `OUTER` | Join | `JoinOuterHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `OUTER` | Join | `JoinOuterHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `SEMI` | Join | `JoinSemiHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` |  |
| `SEMI` | Join | `JoinSemiHint<TSource>(...)` | `IClickHouseSpecificTable&lt;TSource&gt;` |  |
| `SETTINGS` | Query | `SettingsHint<TSource>(...)` | `IClickHouseSpecificQueryable&lt;TSource&gt;` | `string hintFormat, params object?[] hintParameters` |

### MySql

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `BKA` | Query | `BatchedKeyAccessHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `BKA` | Query | `BkaHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `BKA` | Table | `BatchedKeyAccessHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `BKA` | Table | `BkaHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `BKA` | TablesInScope | `BatchedKeyAccessInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `BKA` | TablesInScope | `BkaInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `BNL` | Query | `BlockNestedLoopHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `BNL` | Query | `BnlHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `BNL` | Table | `BlockNestedLoopHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `BNL` | Table | `BnlHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `BNL` | TablesInScope | `BlockNestedLoopInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `BNL` | TablesInScope | `BnlInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `DERIVED_CONDITION_PUSHDOWN` | Query | `DerivedConditionPushDownHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `DERIVED_CONDITION_PUSHDOWN` | Table | `DerivedConditionPushDownHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `DERIVED_CONDITION_PUSHDOWN` | TablesInScope | `DerivedConditionPushDownInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `FOR SHARE` | SubQuery | `ForShareHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR SHARE NOWAIT` | SubQuery | `ForShareNoWaitHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR SHARE SKIP LOCKED` | SubQuery | `ForShareSkipLockedHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR UPDATE` | SubQuery | `ForUpdateHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR UPDATE NOWAIT` | SubQuery | `ForUpdateNoWaitHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR UPDATE SKIP LOCKED` | SubQuery | `ForUpdateSkipLockedHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FORCE INDEX` | Index | `ForceIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE INDEX FOR GROUP BY` | Index | `ForceIndexForGroupByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE INDEX FOR JOIN` | Index | `ForceIndexForJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE INDEX FOR ORDER BY` | Index | `ForceIndexForOrderByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE KEY` | Index | `ForceKeyHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE KEY FOR GROUP BY` | Index | `ForceKeyForGroupByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE KEY FOR JOIN` | Index | `ForceKeyForJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `FORCE KEY FOR ORDER BY` | Index | `ForceKeyForOrderByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `GROUP_INDEX` | Index | `GroupIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `HASH_JOIN` | Query | `HashJoinHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `HASH_JOIN` | Table | `HashJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `HASH_JOIN` | TablesInScope | `HashJoinInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `IGNORE INDEX` | Index | `IgnoreIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE INDEX FOR GROUP BY` | Index | `IgnoreIndexForGroupByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE INDEX FOR JOIN` | Index | `IgnoreIndexForJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE INDEX FOR ORDER BY` | Index | `IgnoreIndexForOrderByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE KEY` | Index | `IgnoreKeyHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE KEY FOR GROUP BY` | Index | `IgnoreKeyForGroupByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE KEY FOR JOIN` | Index | `IgnoreKeyForJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `IGNORE KEY FOR ORDER BY` | Index | `IgnoreKeyForOrderByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX` | Index | `IndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_MERGE` | Index | `IndexMergeHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `JOIN_FIXED_ORDER` | Query | `JoinFixedOrderHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `JOIN_FIXED_ORDER` | Table | `JoinFixedOrderHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `JOIN_FIXED_ORDER` | TablesInScope | `JoinFixedOrderInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `JOIN_INDEX` | Index | `JoinIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `JOIN_ORDER` | Query | `JoinOrderHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `JOIN_ORDER` | Table | `JoinOrderHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `JOIN_ORDER` | TablesInScope | `JoinOrderInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `JOIN_PREFIX` | Query | `JoinPrefixHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `JOIN_PREFIX` | Table | `JoinPrefixHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `JOIN_PREFIX` | TablesInScope | `JoinPrefixInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `JOIN_SUFFIX` | Query | `JoinSuffixHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `JOIN_SUFFIX` | Table | `JoinSuffixHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `JOIN_SUFFIX` | TablesInScope | `JoinSuffixInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `MAX_EXECUTION_TIME(...)` | Query | `MaxExecutionTimeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `int value` |
| `MERGE` | Query | `MergeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `MERGE` | Table | `MergeHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `MERGE` | TablesInScope | `MergeInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `MRR` | Index | `MrrHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_BKA` | Query | `NoBatchedKeyAccessHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_BKA` | Query | `NoBkaHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_BKA` | Table | `NoBatchedKeyAccessHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_BKA` | Table | `NoBkaHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_BKA` | TablesInScope | `NoBatchedKeyAccessInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_BKA` | TablesInScope | `NoBkaInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_BNL` | Query | `NoBlockNestedLoopHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_BNL` | Query | `NoBnlHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_BNL` | Table | `NoBlockNestedLoopHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_BNL` | Table | `NoBnlHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_BNL` | TablesInScope | `NoBlockNestedLoopInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_BNL` | TablesInScope | `NoBnlInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_DERIVED_CONDITION_PUSHDOWN` | Query | `NoDerivedConditionPushDownHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_DERIVED_CONDITION_PUSHDOWN` | Table | `NoDerivedConditionPushDownHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_DERIVED_CONDITION_PUSHDOWN` | TablesInScope | `NoDerivedConditionPushDownInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_GROUP_INDEX` | Index | `NoGroupIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_HASH_JOIN` | Query | `NoHashJoinHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_HASH_JOIN` | Table | `NoHashJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_HASH_JOIN` | TablesInScope | `NoHashJoinInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_ICP` | Index | `NoIcpHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INDEX` | Index | `NoIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INDEX_MERGE` | Index | `NoIndexMergeHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_JOIN_INDEX` | Index | `NoJoinIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_MERGE` | Query | `NoMergeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_MERGE` | Table | `NoMergeHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` |  |
| `NO_MERGE` | TablesInScope | `NoMergeInScopeHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` |  |
| `NO_MRR` | Index | `NoMrrHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_ORDER_INDEX` | Index | `NoOrderIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_RANGE_OPTIMIZATION` | Index | `NoRangeOptimizationHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_SEMIJOIN` | Join | `NoSemiJoinHintWithQueryBlock<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params string[] values` |
| `NO_SEMIJOIN` | Query | `NoSemiJoinHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params string[] values` |
| `NO_SKIP_SCAN` | Index | `NoSkipScanHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `ORDER_INDEX` | Index | `OrderIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `RESOURCE_GROUP` | Query | `ResourceGroupHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `string value` |
| `SEMIJOIN` | Join | `SemiJoinHintWithQueryBlock<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params string[] values` |
| `SEMIJOIN` | Query | `SemiJoinHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `params string[] values` |
| `SET_VAR` | Query | `SetVarHint<TSource>(...)` | `IMySqlSpecificQueryable&lt;TSource&gt;` | `string value` |
| `SKIP_SCAN` | Index | `SkipScanHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE INDEX` | Index | `UseIndexHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE INDEX FOR GROUP BY` | Index | `UseIndexForGroupByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE INDEX FOR JOIN` | Index | `UseIndexForJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE INDEX FOR ORDER BY` | Index | `UseIndexForOrderByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE KEY` | Index | `UseKeyHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE KEY FOR GROUP BY` | Index | `UseKeyForGroupByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE KEY FOR JOIN` | Index | `UseKeyForJoinHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE KEY FOR ORDER BY` | Index | `UseKeyForOrderByHint<TSource>(...)` | `IMySqlSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |

### Oracle

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `ALL_ROWS` | Query | `AllRowsHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `APPEND` | Query | `AppendHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `APPEND_VALUES` | Query | `AppendValuesHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `CACHE` | Table | `CacheHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `CACHE` | TablesInScope | `CacheInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `CLUSTER` | Table | `ClusterHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `CLUSTER` | TablesInScope | `ClusterInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `CLUSTERING` | Query | `ClusteringHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `CURSOR_SHARING_EXACT` | Query | `CursorSharingExactHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `DISABLE_PARALLEL_DML` | Query | `DisableParallelDmlHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `DRIVING_SITE` | Table | `DrivingSiteHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `DRIVING_SITE` | TablesInScope | `DrivingSiteInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `ENABLE_PARALLEL_DML` | Query | `EnableParallelDmlHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `FACT` | Table | `FactHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `FACT` | TablesInScope | `FactInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `FIRST_ROWS(...)` | Query | `FirstRowsHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `int value` |
| `FRESH_MV` | Query | `FreshMaterializedViewHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `FRESH_MV` | Query | `FreshMVHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `FULL` | Table | `FullHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `FULL` | TablesInScope | `FullInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `GROUPING` | Query | `GroupingHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `HASH` | Table | `HashHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `HASH` | TablesInScope | `HashInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `INDEX` | Index | `IndexHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_ASC` | Index | `IndexAscHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_COMBINE` | Index | `IndexCombineHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_DESC` | Index | `IndexDescHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_FFS` | Index | `IndexFastFullScanHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_FFS` | Index | `IndexFFSHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_JOIN` | Index | `IndexJoinHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_SS` | Index | `IndexSkipScanHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_SS` | Index | `IndexSSHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_SS_ASC` | Index | `IndexSkipScanAscHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_SS_ASC` | Index | `IndexSSAscHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_SS_DESC` | Index | `IndexSkipScanDescHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INDEX_SS_DESC` | Index | `IndexSSDescHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `INMEMORY_PRUNING` | Table | `InMemoryPruningHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `INMEMORY_PRUNING` | TablesInScope | `InMemoryPruningInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `LEADING` | Query | `LeadingHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `MERGE` | Query | `MergeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `MERGE` | Table | `MergeHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `MERGE` | TablesInScope | `MergeInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `MODEL_MIN_ANALYSIS` | Query | `ModelMinAnalysisHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `MONITOR` | Query | `MonitorHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NATIVE_FULL_OUTER_JOIN` | Query | `NativeFullOuterJoinHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NMEMORY` | Table | `InMemoryHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NMEMORY` | TablesInScope | `InMemoryInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_CLUSTERING` | Query | `NoClusteringHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_EXPAND` | Query | `NoExpandHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_FACT` | Table | `NoFactHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_FACT` | TablesInScope | `NoFactInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_INDEX` | Index | `NoIndexHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INDEX_FFS` | Index | `NoIndexFastFullScanHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INDEX_FFS` | Index | `NoIndexFFSHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INDEX_SS` | Index | `NoIndexSkipScanHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INDEX_SS` | Index | `NoIndexSSHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `NO_INMEMORY` | Table | `NoInMemoryHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_INMEMORY` | TablesInScope | `NoInMemoryInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_INMEMORY_PRUNING` | Table | `NoInMemoryPruningHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_INMEMORY_PRUNING` | TablesInScope | `NoInMemoryPruningInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_MERGE` | Query | `NoMergeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `NO_MERGE` | Table | `NoMergeHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_MERGE` | TablesInScope | `NoMergeInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_MONITOR` | Query | `NoMonitorHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_NATIVE_FULL_OUTER_JOIN` | Query | `NoNativeFullOuterJoinHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_PARALLEL` | Table | `NoParallelHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_PARALLEL` | TablesInScope | `NoParallelInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_PQ_CONCURRENT_UNION` | Query | `NoPQConcurrentUnionHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_PQ_SKEW` | Table | `NoPQSkewHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_PQ_SKEW` | TablesInScope | `NoPQSkewInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_PUSH_SUBQ` | Query | `NoPushSubQueriesHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `NO_PX_JOIN_FILTER` | Table | `NoPxJoinFilterHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NO_PX_JOIN_FILTER` | TablesInScope | `NoPxJoinFilterInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_QUERY_TRANSFORMATION` | Query | `NoQueryTransformationHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_REWRITE` | Query | `NoRewriteHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `NO_STAR_TRANSFORMATION` | Query | `NoStarTransformationHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_UNNEST` | Query | `NoUnnestHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `NO_USE_BAND` | Query | `NoUseBandHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_USE_CUBE` | Query | `NoUseCubeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_USE_HASH` | Query | `NoUseHashHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_USE_MERGE` | Query | `NoUseMergeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_USE_NL` | Query | `NoUseNestedLoopHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_USE_NL` | Query | `NoUseNLHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `NO_XML_QUERY_REWRITE` | Query | `NoXmlQueryRewriteHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NO_XMLINDEX_REWRITE` | Query | `NoXmlIndexRewriteHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NOAPPEND` | Query | `NoAppendHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `NOCACHE` | Table | `NoCacheHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `NOCACHE` | TablesInScope | `NoCacheInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `ORDERED` | Query | `OrderedHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PARALLEL` | Query | `ParallelHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PQ_CONCURRENT_UNION` | Query | `PQConcurrentUnionHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PQ_FILTER(HASH)` | Query | `PQFilterHashHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PQ_FILTER(NONE)` | Query | `PQFilterNoneHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PQ_FILTER(RANDOM)` | Query | `PQFilterRandomHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PQ_FILTER(SERIAL)` | Query | `PQFilterSerialHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PQ_SKEW` | Table | `PQSkewHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `PQ_SKEW` | TablesInScope | `PQSkewInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PUSH_PRED` | Query | `NoPushPredicateHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PUSH_PRED` | Query | `PushPredicateHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PUSH_PRED` | Table | `NoPushPredicateHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `PUSH_PRED` | Table | `PushPredicateHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `PUSH_PRED` | TablesInScope | `NoPushPredicateInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PUSH_PRED` | TablesInScope | `PushPredicateInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `PUSH_SUBQ` | Query | `PushSubQueriesHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `PX_JOIN_FILTER` | Table | `PxJoinFilterHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` |  |
| `PX_JOIN_FILTER` | TablesInScope | `PxJoinFilterInScopeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `REWRITE` | Query | `RewriteHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` |  |
| `STAR_TRANSFORMATION` | Query | `StarTransformationHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `UNNEST` | Query | `UnnestHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `USE_BAND` | Query | `UseBandHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `USE_CONCAT` | Query | `UseConcatHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `string queryBlock` |
| `USE_CUBE` | Query | `UseCubeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `USE_HASH` | Query | `UseHashHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `USE_MERGE` | Query | `UseMergeHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `USE_NL` | Query | `UseNestedLoopHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `USE_NL` | Query | `UseNLHint<TSource>(...)` | `IOracleSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `USE_NL_WITH_INDEX` | Index | `UseNestedLoopWithIndexHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |
| `USE_NL_WITH_INDEX` | Index | `UseNLWithIndexHint<TSource>(...)` | `IOracleSpecificTable&lt;TSource&gt;` | `params string[] indexNames` |

### PostgreSQL

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `FOR KEY SHARE` | SubQuery | `ForKeyShareHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR KEY SHARE NOWAIT` | SubQuery | `ForKeyShareNoWaitHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR KEY SHARE SKIP LOCKED` | SubQuery | `ForKeyShareSkipLockedHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR NO KEY UPDATE` | SubQuery | `ForNoKeyUpdateHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR NO KEY UPDATE NOWAIT` | SubQuery | `ForNoKeyUpdateNoWaitHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR NO KEY UPDATE SKIP LOCKED` | SubQuery | `ForNoKeyUpdateSkipLockedHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR SHARE` | SubQuery | `ForShareHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR SHARE NOWAIT` | SubQuery | `ForShareNoWaitHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR SHARE SKIP LOCKED` | SubQuery | `ForShareSkipLockedHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR UPDATE` | SubQuery | `ForUpdateHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR UPDATE NOWAIT` | SubQuery | `ForUpdateNoWaitHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |
| `FOR UPDATE SKIP LOCKED` | SubQuery | `ForUpdateSkipLockedHint<TSource>(...)` | `IPostgreSQLSpecificQueryable&lt;TSource&gt;` | `params Sql.SqlID[] tableIDs` |

### SqlCe

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `HOLDLOCK` | Table | `WithHoldLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `HOLDLOCK` | TablesInScope | `WithHoldLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |
| `Index` | Index | `WithIndex<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` | `string indexName` |
| `NOLOCK` | Table | `WithNoLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `NOLOCK` | TablesInScope | `WithNoLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |
| `PAGLOCK` | Table | `WithPagLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `PAGLOCK` | TablesInScope | `WithPagLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |
| `ROWLOCK` | Table | `WithRowLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `ROWLOCK` | TablesInScope | `WithRowLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |
| `TABLOCK` | Table | `WithTabLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `TABLOCK` | TablesInScope | `WithTabLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |
| `UPDLOCK` | Table | `WithUpdLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `UPDLOCK` | TablesInScope | `WithUpdLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |
| `XLOCK` | Table | `WithXLock<TSource>(...)` | `ISqlCeSpecificTable&lt;TSource&gt;` |  |
| `XLOCK` | TablesInScope | `WithXLockInScope<TSource>(...)` | `ISqlCeSpecificQueryable&lt;TSource&gt;` |  |

### SqlServer

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `CONCAT UNION` | Query | `OptionConcatUnion<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `DISABLE EXTERNALPUSHDOWN` | Query | `OptionDisableExternalPushDown<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `DISABLE SCALEOUTEXECUTION` | Query | `OptionDisableScaleOutExecution<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `EXPAND VIEWS` | Query | `OptionExpandViews<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `FAST` | Query | `OptionFast<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` | `int value` |
| `FORCE EXTERNALPUSHDOWN` | Query | `OptionForceExternalPushDown<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `FORCE ORDER` | Query | `OptionForceOrder<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `FORCE SCALEOUTEXECUTION` | Query | `OptionForceScaleOutExecution<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `FORCESCAN` | Table | `WithForceScan<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `FORCESCAN` | TablesInScope | `WithForceScanInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `FORCESEEK` | Table | `WithForceSeek<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `FORCESEEK` | TablesInScope | `WithForceSeekInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `HASH` | Join | `JoinHashHint<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `HASH` | Join | `JoinHashHint<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `HASH GROUP` | Query | `OptionHashGroup<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `HASH JOIN` | Query | `OptionHashJoin<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `HASH UNION` | Query | `OptionHashUnion<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `HOLDLOCK` | Table | `WithHoldLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `HOLDLOCK` | TablesInScope | `WithHoldLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX` | Query | `OptionIgnoreNonClusteredColumnStoreIndex<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `Index` | Index | `WithIndex<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` | `string indexName` |
| `KEEP PLAN` | Query | `OptionKeepPlan<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `KEEPFIXED PLAN` | Query | `OptionKeepFixedPlan<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `LOOP` | Join | `JoinLoopHint<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `LOOP` | Join | `JoinLoopHint<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `LOOP JOIN` | Query | `OptionLoopJoin<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `MAX_GRANT_PERCENT` | Query | `OptionMaxGrantPercent<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` | `int value` |
| `MAXDOP` | Query | `OptionMaxDop<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` | `int value` |
| `MAXRECURSION` | Query | `OptionMaxRecursion<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` | `int value` |
| `MERGE` | Join | `JoinMergeHint<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `MERGE` | Join | `JoinMergeHint<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `MERGE JOIN` | Query | `OptionMergeJoin<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `MERGE UNION` | Query | `OptionMergeUnion<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `MIN_GRANT_PERCENT` | Query | `OptionMinGrantPercent<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` | `int value` |
| `NO_PERFORMANCE_SPOOL` | Query | `OptionNoPerformanceSpool<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `NOLOCK` | Table | `WithNoLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `NOLOCK` | TablesInScope | `WithNoLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `NOWAIT` | Table | `WithNoWait<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `NOWAIT` | TablesInScope | `WithNoWaitInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `OPTIMIZE FOR UNKNOWN` | Query | `OptionOptimizeForUnknown<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `ORDER GROUP` | Query | `OptionOrderGroup<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `PAGLOCK` | Table | `WithPagLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `PAGLOCK` | TablesInScope | `WithPagLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `QUERYTRACEON` | Query | `OptionQueryTraceOn<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` | `int value` |
| `READCOMMITTED` | Table | `WithReadCommitted<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `READCOMMITTED` | TablesInScope | `WithReadCommittedInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `READCOMMITTEDLOCK` | Table | `WithReadCommittedLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `READCOMMITTEDLOCK` | TablesInScope | `WithReadCommittedLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `READPAST` | Table | `WithReadPast<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `READPAST` | TablesInScope | `WithReadPastInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `READUNCOMMITTED` | Table | `WithReadUncommitted<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `READUNCOMMITTED` | TablesInScope | `WithReadUncommittedInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `RECOMPILE` | Query | `OptionRecompile<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `REMOTE` | Join | `JoinRemoteHint<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `REMOTE` | Join | `JoinRemoteHint<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `REPEATABLEREAD` | Table | `WithRepeatableRead<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `REPEATABLEREAD` | TablesInScope | `WithRepeatableReadInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `ROBUST PLAN` | Query | `OptionRobustPlan<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `ROWLOCK` | Table | `WithRowLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `ROWLOCK` | TablesInScope | `WithRowLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `SERIALIZABLE` | Table | `WithSerializable<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `SERIALIZABLE` | TablesInScope | `WithSerializableInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `SNAPSHOT` | Table | `WithSnapshot<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `SNAPSHOT` | TablesInScope | `WithSnapshotInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `TABLOCK` | Table | `WithTabLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `TABLOCK` | TablesInScope | `WithTabLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `TABLOCKX` | Table | `WithTabLockX<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `TABLOCKX` | TablesInScope | `WithTabLockXInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `UPDLOCK` | Table | `WithUpdLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `UPDLOCK` | TablesInScope | `WithUpdLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |
| `XLOCK` | Table | `WithXLock<TSource>(...)` | `ISqlServerSpecificTable&lt;TSource&gt;` |  |
| `XLOCK` | TablesInScope | `WithXLockInScope<TSource>(...)` | `ISqlServerSpecificQueryable&lt;TSource&gt;` |  |

### Ydb

| SQL hint | Hint type | API | Receiver | Extra parameters |
|---|---|---|---|---|
| `distinct` | Query | `DistinctHint<TSource>(...)` | `IQueryable&lt;TSource&gt;` | `params string[] columns` |
| `distinct` | Query | `DistinctHint<TSource>(...)` | `IYdbSpecificQueryable&lt;TSource&gt;` | `params string[] columns` |
| `unique` | Query | `UniqueHint<TSource>(...)` | `IQueryable&lt;TSource&gt;` | `params string[] columns` |
| `unique` | Query | `UniqueHint<TSource>(...)` | `IYdbSpecificQueryable&lt;TSource&gt;` | `params string[] columns` |

## Maintenance

- Keep this map aligned with XML-doc, especially `<c>...</c>` SQL hint text and `AI-Tags: Group=Hints; HintType=...`.
- For generated provider hint files, update the `.tt` template first and regenerate/check in the matching `.generated.cs` file.
- For handwritten provider hint files, update XML comments directly and then refresh this map.
- Do not add examples for only one tested hint as proof that the entire provider surface works. The map is an API discovery aid, not a behavioral test suite.
