---
area: SQL-PROVIDER
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: df84c7784
---

# SQL-PROVIDER -- GitHub themes

## Open themes

- **Identifier quotation and escaping edge cases** -- Multiple providers lack proper identifier quotation logic, causing SQL errors when identifiers contain special characters. General issue (#1510) tracks cross-provider gaps; Access-specific issue (#3893) addresses backtick and reserved-character escaping per SQL/OLE DB standard. Both represent recurring pattern of quotation logic deficiencies affecting special characters and provider-specific identifier handling rules. Issues: #1510, #3893.

- **Operator precedence and SQL generation** -- SQL expression precedence table at `Source/LinqToDB/SqlQuery/Precedence.cs` is hardcoded and applied uniformly across all providers, but each SQL dialect defines its own operator precedence. Mismatch causes unnecessary brackets in generated SQL. TODO comment acknowledged but no prior tracking. Issues: #5527.

- **Complete INSERT and UPSERT operations coverage** -- Epic issue (#2558) tracking design and incremental implementation of various INSERT forms including UPSERT, excluding bulk operations. Aims to translate merge API calls to provider-specific UPSERT variants (e.g., `INSERT ... ON DUPLICATE KEY UPDATE` for MySQL, `INSERT ... ON CONFLICT` for PostgreSQL, native MERGE for SQL Server). Issues: #2558.

- **DDL and schema management helper extensions** -- Three related requests: table-existence detection (#506) seeks efficient check without full schema enumeration; comprehensive CreateTable type coverage (#749) needs test suite covering all supported column types per provider; convenience helpers (#1201) request ColumnExists/AddColumn fluent extensions replacing try-catch patterns. Issues: #506, #749, #1201.

- **Advanced SQL features (PIVOT/UNPIVOT, Regex)** -- Two unrelated advanced features: PIVOT/UNPIVOT support (#1475) requires per-provider implementation (SQL Server PIVOT, Oracle UNPIVOT, PostgreSQL tablefunc extension, MariaDB CONNECT, etc.); regex support (#698) needs translation of LINQ Regex.IsMatch to provider-specific regex operators (LIKE for SQLite, REGEXP for MySQL/PostgreSQL, REGEXP_LIKE for Oracle, etc.). Issues: #1475, #698.

- **Sql.Function attribute generics support** -- Feature request (#326) to allow provider-specific function overloads based on generic type parameters. Currently Sql.Function supports per-provider and per-TFM selection but not per-generic-instantiation, requiring users to create multiple concrete wrapper methods instead of one generic template. Issues: #326.

## Resolved themes

- **Provider customization barriers (mostly addressed)** -- Long-standing issue: ISqlBuilder and related builder classes were internal, blocking custom provider extensions. Fixed by making classes public (#347, #144, #156, #182, #58). Later refactoring (#4948) removed ISqlBuilder interface entirely to simplify maintainability. Fixed via #4948.

- **Type mapping and conversion edge cases** -- Historical issues with TimeSpan/DateTimeOffset literal generation (#149), anonymous type filtering (#2769), string/nullable coercion (#2746), custom type mappings (#3821). Most resolved case-by-case via converter overrides or type-specific builder fixes. Issues: #149, #2769, #2746, #3821.

- **ASP.NET Core DI registration problems** -- Multiple context registration (#3527, #4077, #4326), scoped lifetime issues (#2507, #3623), and DataOptions type selection bugs (#4326, #5024, #5041). Fixed incrementally through RC cycles and preview versions, now stabilized. Fixed via #4406.

## Active discussions

No active discussions in this area.

## Stats

- Open issues: 10
- Closed issues: 37
- Open PRs: 1 (+ 2 draft)
- Total PRs: 22
- Discussions: 0
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 47 (10 issues + 37 closed + 0 discussions)
- Themes extracted: 6
</details>
