---
area: MAPPING
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: df84c7784d896d15ffc626983fd202e6399daa081ca
---

# MAPPING -- GitHub themes

## Open themes

- **Type conversion and custom mapping** -- 6 open issues. Users struggle with custom type converters (generic types, open generics), MapValue behavior, EF HasConversion mapping, and provider-specific type conversions. Central blocker: limited support for generic and schema-aware converters. Sample: #1994, #2103, #3117, #363, #4662, #5675.

- **Fluent mapping API and attribute refactoring** -- 6 open issues. Requests for consistent fluent mapping API, attribute-based configuration, and advanced attribute features. Issues include QueryFilter declarative syntax, AssociationAttribute type references, ExpressionMethod behavior on materialization, and decoupling attributes from metadata. Sample: #3914, #3691, #4543, #4273, #5540, #2502.

- **Association and relationship mapping** -- 2 open issues. Problems with association key resolution, EF compatibility (owned entities), and FK-PK matching in complex scenarios. Sample: #4627, #4650.

- **Advanced mapping scenarios** -- 4 open issues. Dynamic tables, complex type properties mapped from multiple columns, explicit interface implementation, and BLOB chunked access. Emerging pattern: need for more flexible property-column mapping. Sample: #4730, #4758, #4715, #1075.

- **Infrastructure and provider data types** -- 2 open issues. Long-standing refactoring requests to unify provider-specific type systems and add naming convention support (snake_case, CamelCase). Sample: #1181, #4700.

## Resolved themes

- **Fluent mapping API enhancements** -- 17 closed issues. Historical requests for fluent variants of existing attribute-based patterns. Sample: #26, #162, #179, #202, #961, #1089, #1128, #1160.

- **Association and relationship mapping** -- 8 closed issues. Fixes to parent-child navigation, N+1 optimization, and explicit join mapping. Sample: #498, #1443, #1498, #3230, #3561, #2592, #529, #4723.

- **Column and table mapping fundamentals** -- 10 closed issues. Core attribute mapping (Column, Table, PrimaryKey, Identity), inheritance strategies. Sample: #26, #197, #1074, #2499, #3894, #3830, #1833, #4605.

- **Insert/Update statement mapping** -- 7 closed issues. Mapping of DML statements (INSERT/UPDATE/MERGE) to entity changes and bulk operations. Sample: #2504, #2692, #3342, #4055, #3131, #3891, #5289.

## Active discussions

- [How do I loop through all tables to set query filter automatically?](https://github.com/linq2db/linq2db/discussions/3062) -- [Q&A] User seeks a way to apply a global tenant-id filter across all table queries without individual filter setup.

- [Fluent mapping behavior](https://github.com/linq2db/linq2db/discussions/3118) -- [Q&A] Question about fluent mapping configuration and entity property population.

## Stats

- Open issues: 20
- Closed issues: 130
- Open PRs: 1
- Total PRs: 34
- Discussions: 16
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 150 (20 issues + 130 closed)
- Themes extracted: 5
</details>
