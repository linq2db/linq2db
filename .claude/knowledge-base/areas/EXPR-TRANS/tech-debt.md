---
area: EXPR-TRANS
kind: tech-debt
sources: [code, gh-issues]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# EXPR-TRANS — Tech debt

## Severity distribution

| Severity | Count |
|---|---|
| low | 61 |

## By category

| Category | Count |
|---|---|
| todo-fixme | 61 |

## Top issues (up to 20)

| ID | Severity | Category | Title | File |
|---|---|---|---|---|
| [DI-0147](../../detected-issues/items/DI-0147.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/Visitors/ExposeExpressionVisitor.cs:914` |
| [DI-0148](../../detected-issues/items/DI-0148.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ContainsBuilder.cs:149` |
| [DI-0149](../../detected-issues/items/DI-0149.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/DefaultIfEmptyBuilder.cs:95` |
| [DI-0150](../../detected-issues/items/DI-0150.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/EntityConstructorBase.cs:498` |
| [DI-0151](../../detected-issues/items/DI-0151.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.Associations.cs:21` |
| [DI-0152](../../detected-issues/items/DI-0152.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.Generation.cs:93` |
| [DI-0153](../../detected-issues/items/DI-0153.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.SqlBuilder.cs:830` |
| [DI-0154](../../detected-issues/items/DI-0154.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuilder.SqlBuilder.cs:1861` |
| [DI-0155](../../detected-issues/items/DI-0155.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs:1091` |
| [DI-0156](../../detected-issues/items/DI-0156.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs:4120` |
| [DI-0157](../../detected-issues/items/DI-0157.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs:4129` |
| [DI-0158](../../detected-issues/items/DI-0158.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs:4597` |
| [DI-0159](../../detected-issues/items/DI-0159.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionTestGenerator.cs:637` |
| [DI-0160](../../detected-issues/items/DI-0160.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/ExpressionTestGenerator.cs:640` |
| [DI-0161](../../detected-issues/items/DI-0161.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/QueryExtensionBuilder.cs:105` |
| [DI-0162](../../detected-issues/items/DI-0162.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/SequenceHelper.cs:909` |
| [DI-0163](../../detected-issues/items/DI-0163.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/TableBuilder.RawSqlContext.cs:150` |
| [DI-0164](../../detected-issues/items/DI-0164.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/TableBuilder.RawSqlContext.cs:177` |
| [DI-0165](../../detected-issues/items/DI-0165.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/Builder/TableLikeQueryContext.cs:254` |
| [DI-0166](../../detected-issues/items/DI-0166.md) | low | todo-fixme | TODO/FIXME/HACK/XXX comment | `Source/LinqToDB/Internal/Linq/ExpressionCacheManager.cs:105` |

Plus 41 more entries — see [detected-issues index](../../detected-issues/index.json) (filter by area: `EXPR-TRANS`).

## See also

- [INDEX.md](INDEX.md) — area overview
- [issues.md](issues.md) — GitHub themes
- [decisions.md](decisions.md) — recorded decisions
- [patterns.md](patterns.md) — area patterns

## Open GH bugs in this area: 9

- [#4266 Wrong query results when using Extention (query cache problem)](https://github.com/linq2db/linq2db/issues/4266)
- [#3560 Many joins tests are wrong](https://github.com/linq2db/linq2db/issues/3560)
- [#4321 TablesInScopeHint ignores references](https://github.com/linq2db/linq2db/issues/4321)
- [#4444 Schema Build Error: System.InvalidOperationException: IsResultDynamic set for function public.dblink](https://github.com/linq2db/linq2db/issues/4444)
- [#3266 CompiledQuery  with Async Update doesn't work](https://github.com/linq2db/linq2db/issues/3266)
- [#4199 Property X is not defined for interface type Y](https://github.com/linq2db/linq2db/issues/4199)
- [#4568 Don't select all entity columns when only composite property selected](https://github.com/linq2db/linq2db/issues/4568)
- [#4365 CompiledQuery do not call IEntityServiceInterceptor.EntityCreated()?](https://github.com/linq2db/linq2db/issues/4365)
- [#3993 Problem with Calculated DateTime and Where Filter ](https://github.com/linq2db/linq2db/issues/3993)
