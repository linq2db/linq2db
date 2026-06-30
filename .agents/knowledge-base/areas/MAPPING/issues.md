---
area: MAPPING
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# MAPPING -- GitHub themes

## Open themes

- **Fluent mapping API** -- 5 open issues share this pattern. Users request fluent API variants or find inconsistencies in fluent mapping configuration (MapValue, LoadWith, association configuration). Sample: #1862, #3041, #2576, #3136, #3914.
- **Association mapping and lazy loading** -- 4 open issues. Problems with parent-child navigation properties, LoadWith population, and N+1 query patterns with associations. Sample: #3041, #3806, #341, #4305.
- **Type conversion and custom mapping** -- 5 open issues. Missing or broken custom type converters (UUID, NodaTime, custom scalars), SetConverter behavior, and type inference gaps. Sample: #3554, #2502, #3691, #4671, #3119.
- **Computed/expression columns and property mapping** -- 4 open issues. Mapping of computed columns, calculated properties, and expression-based attributes to SQL. Sample: #4073, #279, #2873, #4758.
- **Property-level mapping attributes** -- 4 open issues. [MapIgnore], [Column], [PrimaryKey], and other attribute-based mapping control, particularly for edge cases and provider-specific scenarios. Sample: #4671, #3119, #4059, #3929.

## Resolved themes

- **Fluent mapping API enhancements** -- 17 closed issues. Historical requests for fluent variants of existing attribute-based patterns. Sample: #26, #162, #179, #202, #961, #1089, #1128, #1160.
- **Association and relationship mapping** -- 8 closed issues. Fixes to parent-child navigation, N+1 optimization, and explicit join mapping. Sample: #498, #1443, #1498, #3230, #3561, #2592, #529, #4723.
- **Column and table mapping fundamentals** -- 10 closed issues. Core attribute mapping (Column, Table, PrimaryKey, Identity), inheritance strategies. Sample: #26, #197, #1074, #2499, #3894, #3830, #1833, #4605.
- **Insert/Update statement mapping** -- 7 closed issues. Mapping of DML statements (INSERT/UPDATE/MERGE) to entity changes and bulk operations. Sample: #2504, #2692, #3342, #4055, #3131, #3891, #5289.

## Active discussions

- [How do I loop through all tables to set query filter automatically?](https://github.com/linq2db/linq2db/discussions/3062) -- [Q&A] User seeks a way to apply a global tenant-id filter across all table queries without individual filter setup.
- [Fluent mapping behavior](https://github.com/linq2db/linq2db/discussions/3118) -- [Q&A] Question about fluent mapping configuration and entity property population.

## Stats

- Open issues: 53
- Closed issues: 128
- Open PRs: 1
- Total PRs: 34
- Discussions: 16
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 231 (181 issues + 34 PRs + 16 discussions)
- Themes extracted: 8
</details>
