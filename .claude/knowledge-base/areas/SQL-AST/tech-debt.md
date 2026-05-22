---
area: SQL-AST
kind: tech-debt
sources: [code, gh-issues]
confidence: medium
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# SQL-AST — Tech debt

## Severity distribution

| Severity | Count |
|---|---|
| low | 18 |
| med | 1 |

## By category

| Category | Count |
|---|---|
| todo-fixme | 18 |
| code-smell | 1 |

## Top issues (up to 20)

| ID | Severity | Category | Title | File |
|---|---|---|---|---|
| [DI-0201](../../detected-issues/items/DI-0201.md) | med | code-smell | Empty or no-op catch block | `Source/LinqToDB/Internal/SqlQuery/SqlTable.cs:386` |
| [DI-0191](../../detected-issues/items/DI-0191.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs:13` |
| [DI-0192](../../detected-issues/items/DI-0192.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs:106` |
| [DI-0193](../../detected-issues/items/DI-0193.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/Visitors/QueryElementVisitor.cs:2904` |
| [DI-0194](../../detected-issues/items/DI-0194.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/ISqlExpression.cs:12` |
| [DI-0195](../../detected-issues/items/DI-0195.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/ISqlTableSource.cs:5` |
| [DI-0196](../../detected-issues/items/DI-0196.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs:775` |
| [DI-0197](../../detected-issues/items/DI-0197.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/QueryHelper.cs:1929` |
| [DI-0198](../../detected-issues/items/DI-0198.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/SqlException.cs:7` |
| [DI-0199](../../detected-issues/items/DI-0199.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/SqlRawSqlTable.cs:10` |
| [DI-0200](../../detected-issues/items/DI-0200.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/SqlQuery/SqlSearchCondition.cs:131` |
| [DI-0255](../../detected-issues/items/DI-0255.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/DefaultNullable.cs:3` |
| [DI-0256](../../detected-issues/items/DI-0256.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/Precedence.cs:3` |
| [DI-0257](../../detected-issues/items/DI-0257.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/SqlDataType.cs:444` |
| [DI-0258](../../detected-issues/items/DI-0258.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/SqlExtendedFunction.cs:12` |
| [DI-0259](../../detected-issues/items/DI-0259.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/SqlFrameBoundary.cs:9` |
| [DI-0260](../../detected-issues/items/DI-0260.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/SqlFrameClause.cs:9` |
| [DI-0261](../../detected-issues/items/DI-0261.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/SqlFunctionArgument.cs:10` |
| [DI-0262](../../detected-issues/items/DI-0262.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/SqlQuery/SqlWindowOrderItem.cs:10` |

## See also

- [INDEX.md](INDEX.md) — area overview
- [issues.md](issues.md) — GitHub themes
- [decisions.md](decisions.md) — recorded decisions
- [patterns.md](patterns.md) — area patterns

## Open GH bugs in this area: 4

- [#4469 Firebird different number datatype between constant or variable usage](https://github.com/linq2db/linq2db/issues/4469)
- [#3893 Improve Access identifiers quotation](https://github.com/linq2db/linq2db/issues/3893)
- [#2549 PostgresSQL. Delete with Take (linq2db v3.1.0)](https://github.com/linq2db/linq2db/issues/2549)
- [#2451 Recursive CTE results in 'types don't match' error on (MS)SQL](https://github.com/linq2db/linq2db/issues/2451)
