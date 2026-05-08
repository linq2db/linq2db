---
area: PROV-SQLSERVER
kind: tech-debt
sources: [code, gh-issues]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# PROV-SQLSERVER — Tech debt

## Severity distribution

| Severity | Count |
|---|---|
| low | 12 |
| med | 4 |

## By category

| Category | Count |
|---|---|
| todo-fixme | 12 |
| code-smell | 4 |

## Top issues (up to 20)

| ID | Severity | Category | Title | File |
|---|---|---|---|---|
| [DI-0121](../../detected-issues/items/DI-0121.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs:109` |
| [DI-0125](../../detected-issues/items/DI-0125.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerTypes.cs:29` |
| [DI-0126](../../detected-issues/items/DI-0126.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerTypes.cs:43` |
| [DI-0127](../../detected-issues/items/DI-0127.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerTypes.cs:62` |
| [DI-0060](../../detected-issues/items/DI-0060.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/DataProvider/SqlServer/SystemDataSqlServerAttributeReader.cs:111` |
| [DI-0113](../../detected-issues/items/DI-0113.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServer2012SqlBuilder.Merge.cs:8` |
| [DI-0114](../../detected-issues/items/DI-0114.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:146` |
| [DI-0115](../../detected-issues/items/DI-0115.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs:291` |
| [DI-0116](../../detected-issues/items/DI-0116.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs:276` |
| [DI-0117](../../detected-issues/items/DI-0117.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:177` |
| [DI-0118](../../detected-issues/items/DI-0118.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:181` |
| [DI-0119](../../detected-issues/items/DI-0119.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:185` |
| [DI-0120](../../detected-issues/items/DI-0120.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs:738` |
| [DI-0122](../../detected-issues/items/DI-0122.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs:320` |
| [DI-0123](../../detected-issues/items/DI-0123.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs:397` |
| [DI-0124](../../detected-issues/items/DI-0124.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSchemaProvider.cs:491` |

## See also

- [INDEX.md](INDEX.md) — area overview
- [issues.md](issues.md) — GitHub themes
- [decisions.md](decisions.md) — recorded decisions
- [patterns.md](patterns.md) — area patterns

## Open GH bugs in this area: 4

- [#1916 Use native SQL for Sql.Concat on SQL Server](https://github.com/linq2db/linq2db/issues/1916)
- [#4195 Linq2DB & Azure SQL Edge](https://github.com/linq2db/linq2db/issues/4195)
- [#4138 string.IsNullOrEmpty does not match C# version of string.IsNullOrEmpty for space only strings, MS SQL Server (actually it matches string.IsNulOrWhitespace))](https://github.com/linq2db/linq2db/issues/4138)
- [#449 MS SQL / Schema generation (linq2db T4) / table functions ( UDF )](https://github.com/linq2db/linq2db/issues/449)
