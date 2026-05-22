---
area: PROV-ORACLE
kind: tech-debt
sources: [code, gh-issues]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# PROV-ORACLE — Tech debt

## Severity distribution

| Severity | Count |
|---|---|
| low | 9 |
| med | 2 |

## By category

| Category | Count |
|---|---|
| todo-fixme | 9 |
| code-smell | 2 |

## Top issues (up to 20)

| ID | Severity | Category | Title | File |
|---|---|---|---|---|
| [DI-0101](../../detected-issues/items/DI-0101.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleProviderDetector.cs:65` |
| [DI-0102](../../detected-issues/items/DI-0102.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSchemaProvider.cs:182` |
| [DI-0058](../../detected-issues/items/DI-0058.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/DataProvider/Oracle/OracleTools.OracleXmlTable.cs:133` |
| [DI-0097](../../detected-issues/items/DI-0097.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleBulkCopy.cs:73` |
| [DI-0098](../../detected-issues/items/DI-0098.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleDataProvider.cs:51` |
| [DI-0099](../../detected-issues/items/DI-0099.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleDataProvider.cs:195` |
| [DI-0100](../../detected-issues/items/DI-0100.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleDataProvider.cs:239` |
| [DI-0103](../../detected-issues/items/DI-0103.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilderBase.cs:168` |
| [DI-0104](../../detected-issues/items/DI-0104.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilderBase.cs:185` |
| [DI-0105](../../detected-issues/items/DI-0105.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilderBase.cs:332` |
| [DI-0106](../../detected-issues/items/DI-0106.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilderBase.Merge.cs:12` |

## See also

- [INDEX.md](INDEX.md) — area overview
- [issues.md](issues.md) — GitHub themes
- [decisions.md](decisions.md) — recorded decisions
- [patterns.md](patterns.md) — area patterns

## Open GH bugs in this area: 2

- [#2365 BulkCopy failure on CLOB fields](https://github.com/linq2db/linq2db/issues/2365)
- [#2842 Bad boolean translation in Oracle analytic functions (OVER (ORDER BY))](https://github.com/linq2db/linq2db/issues/2842)
