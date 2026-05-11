---
area: EXPR
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 5/5
coverage_tier_2: 10/10
---

# EXPR -- Expression utilities & public LINQ extensions

The EXPR area is the **public-surface helper layer** that sits between user code and the [EXPR-TRANS](../EXPR-TRANS/INDEX.md) translator. Two distinct responsibilities live here:

1. **`System.Linq.Expressions` plumbing** -- pooled visitors, member-info extraction, attribute-cache. Consumed by EXPR-TRANS, [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md), [MAPPING](../MAPPING/INDEX.md), and SQL-AST builders.
2. **The `LinqExtensions` partial-class group** -- every `IQueryable<T>` / `ITable<T>` extension method that adds something LINQ doesn't have (`Insert`/`Update`/`Delete`/`Merge`/`LoadWith`/CTE/hints/tagging). Each method follows a strict marshalling pattern: it builds a `MethodCallExpression` referencing itself and hands the tree off to the LINQ provider for translation.

EXPR adds no SQL semantics on its own. Methods here are *thin* -- argument validation, expression construction, dispatch into `Internal.Linq` execution. The actual translation lives in [EXPR-TRANS](../EXPR-TRANS/INDEX.md) `MethodCallBuilder` plus per-method builders.

## Key types

- `LinqExtensions` -- the `[PublicAPI]` static partial class in namespace `LinqToDB`, split across ten files. Every public extension method is here. The class is the canonical entry point for non-IQueryable LINQ-style operations.
- `ExpressionExtensions` -- `Visit`/`Find`/`Transform`/`Replace`/`GetBody` helpers. All variants come in TContext-generic and non-generic flavours; both rent a pooled visitor from `LinqToDB.Internal.Expressions.ExpressionVisitors` to avoid allocation per traversal.
- `MemberHelper` -- extracts `MemberInfo`/`MethodInfo`/`ConstructorInfo` from `Expression<Func<...>>` lambdas. The `MethodOf`/`MemberOf`/`PropertyOf`/`ConstructorOf`/`MethodOfGeneric` family are how the rest of the codebase obtains `MethodInfo` references without resorting to string-typed reflection. `MemberInfoWithType` struct bundles a `MemberInfo` with the declaring type.
- `IExpressionEvaluator` -- two-method interface (`CanBeEvaluated`/`Evaluate`) that EXPR-TRANS injects to evaluate compile-time-knowable subtrees during translation.
- `ICteBuilder` -- fluent CTE configuration handed to the `AsCte(source, cteBuilder)` callback. `HasName(string?)` is the only built-in setter; everything else (e.g. `IsMaterialized`) is an extension over this interface in `CteBuilderExtensions`.
- `IAsQueryableBuilder<TElement>` / `IAsQueryableExceptBuilder<TElement>` -- fluent parameterization-configuration interfaces introduced by PR #5495. Used only in the `AsQueryable(source, dataContext, configure)` overload. `Parameterize()` and `Inline()` set per-column policy; `Except(selectors...)` subtracts named columns from the policy.
- `AttributesExtensions` -- *the* attribute-lookup path for the entire codebase. Replaces banned native `GetCustomAttribute(s)` calls (RS0030 enforcement) with a process-wide `ConcurrentDictionary` cache keyed by `ICustomAttributeProvider`. Empty results are stored as `[]` to avoid per-lookup `Array.Empty<>` allocation noise. `GetAttributes<T>`/`GetAttribute<T>`/`HasAttribute<T>` are the public API.

## Subsystems

### Expression-tree utilities (`Source/LinqToDB/Expressions/`)

The three files form a closed surface: `IExpressionEvaluator` declares the contract, `MemberHelper` and `ExpressionExtensions` implement everything users and `Internal.*` need to *read* and *rewrite* expression trees. Two design choices stand out:

- **Pooled visitors.** Every `Visit`/`Find`/`Transform` call rents from a `Pool` and returns it via `using var`. The visitor implementations live in [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- EXPR exposes only the entrypoints.
- **`TContext`-generic overloads.** Every visitor entrypoint has both a non-generic and a generic `Func<TContext, Expression, ...>` form taking a `static` lambda.

`GetMemberInfoWithType` handles unusual cases: `Sql.Property<T>(obj, "Name")` where the second argument is a string literal, anonymous-type `new { ... }` constructors, `Array.Length`, and dynamic columns.

### Public LINQ extensions (`Source/LinqToDB/LinqExtensions/`)

All ten files are partials of one class, `LinqToDB.LinqExtensions`. The split is by feature area, not by visibility. Every extension method follows the same recipe:

1. `ArgumentNullException.ThrowIfNull` for each non-default parameter.
2. Call `source.GetLinqToDBSource()` (or `source.ProcessIQueryable()` for tunnel methods that compose into the tree).
3. Build an `Expression.Call(null, MethodHelper.GetMethodInfo(SelfMethod, args...), <quoted args>)` referencing the calling method.
4. Either `currentSource.Provider.CreateQuery<T>(expr)` or `currentSource.Execute<T>(expr)` / `ExecuteAsync<T>`.

The partials decompose as:

- `LinqExtensions.cs` -- root partial: `Select`/`Take`/`Skip`/`ElementAt`/`Having`/`ThenOrBy`/`Join`/`InnerJoin`/`LeftJoin`/`RightJoin`/`FullJoin`/`CrossJoin`/`AsCte`/`AsSubQuery`/`QueryName`/`InlineParameters`/`HasUniqueKey`/`UnionAll`/`ExceptAll`/`IntersectAll`/`IgnoreFilters`/`TagQuery`/`ToSqlQuery`/`InsertOrUpdate`/`Drop`/`Truncate`/`AggregateExecute`. Also hosts the `ProcessSourceQueryable` and `ExtensionsAdapter` static properties. The `AsQueryable<TElement>(IEnumerable<TElement>, IDataContext)` two-parameter overload lives here (`:1097`).
- `LinqExtensions.AsQueryable.cs` -- single overload: `AsQueryable<TElement>(IEnumerable<TElement>, IDataContext, Expression<Func<IAsQueryableBuilder<TElement>, IAsQueryableExceptBuilder<TElement>>>)` (`AsQueryable.cs:41`). Renders the source as a multi-row `VALUES` clause where each cell is a SQL parameter or inlined SQL literal according to the `configure` chain. The three-arg form always constructs a new `ExpressionQueryImpl<TElement>` (bypasses the `ProcessSourceQueryable` hook that the two-arg form applies). Introduced by PR #5495.
- `LinqExtensions.Insert.cs` -- the entire `Insert`/`InsertWithOutput`/`InsertWithIdentity`/`InsertWithInt32Identity`/`Decimal`/`Int64Identity` matrix; nested `ValueInsertable<T>` and `SelectInsertable<T,TT>` classes.
- `LinqExtensions.Update.cs` -- `Update`/`UpdateAsync`/`UpdateWithOutput`/`UpdateWithOutputInto`/`Set`/`AsUpdatable` family. Internal `Updatable<T>` class.
- `LinqExtensions.Delete.cs` -- `Delete`/`DeleteAsync`/`DeleteWithOutput`/`DeleteWithOutputInto` only.
- `LinqExtensions.Merge.cs` -- the merge-builder fluent API. Internal `MergeQuery<TTarget,TSource>` implements every step interface on the same wrapped `IQueryable<TTarget>` -- interface narrowing is purely compile-time.
- `LinqExtensions.LoadWith.cs` -- `LoadWithAsTable`, `LoadWith<TEntity,TProperty>` (4 overloads), `ThenLoad<TEntity,TPrev,TProperty>` (4 overloads). The `LoadWithQueryableBase<TEntity>` and `LoadWithQueryable<TEntity,TProperty>` private classes wrap an `IExpressionQuery<TEntity>`.
- `LinqExtensions.Hints.cs` -- `With`/`TableHint`/`TablesInScopeHint`/`IndexHint`/`JoinHint`/`SubQueryHint`/`QueryHint`. Each hint carries `[Sql.QueryExtension]` attributes that route per-provider extension builders.
- `LinqExtensions.TableHelpers.cs` -- `TableID`/`TableName`/`DatabaseName`/`ServerName`/`SchemaName`/`WithTableExpression`. All except `WithTableExpression` go through `((ITableMutable<T>)table).Change*Name(name)`.
- `CteBuilderExtensions.cs` -- extension methods on `ICteBuilder`. Currently only `IsMaterialized(bool)`; the doc-comment lists the supported provider matrix (PostgreSQL 12+, SQLite 3.35+, ClickHouse 26.3+).

### Attribute-cache layer (`Source/LinqToDB/Extensions/`)

`AttributesExtensions` is one file but load-bearing. The repo bans `MemberInfo.GetCustomAttribute(s)` via `Build/BannedSymbols.txt`; every attribute lookup in the codebase routes through `GetAttributes<T>(provider, inherit)`. The cache is two-tier: an outer `_inheritAttributes`/`_noInheritAttributes` keyed by `ICustomAttributeProvider`, plus per-`T` generic `InheritAttributeCache<T>`/`NoInheritAttributeCache<T>` to memoize the type-filter step. The inherit-true path has a workaround for [dotnet/runtime#30219](https://github.com/dotnet/runtime/issues/30219).

## Files (Tier 1 / Tier 2)

| Tier | File | Role |
|---|---|---|
| 1 | `Source/LinqToDB/Expressions/IExpressionEvaluator.cs` | EvaluatesExpression contract; consumed by EXPR-TRANS |
| 1 | `Source/LinqToDB/Expressions/ExpressionExtensions.cs` | Pooled-visitor entrypoints |
| 1 | `Source/LinqToDB/Expressions/MemberHelper.cs` | `MemberOf`/`MethodOf` lambda-extraction; `MemberInfoWithType` |
| 1 | `Source/LinqToDB/LinqExtensions/LinqExtensions.cs` | Root partial: scalar-select, paging, joins, CTE, set ops |
| 1 | `Source/LinqToDB/LinqExtensions/ICteBuilder.cs` | Fluent CTE-builder contract |
| 2 | `Source/LinqToDB/Extensions/AttributesExtensions.cs` | Process-wide cached `Get/HasAttribute(s)<T>` extensions |
| 2 | `Source/LinqToDB/LinqExtensions/CteBuilderExtensions.cs` | `ICteBuilder.IsMaterialized` |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.AsQueryable.cs` | Three-arg `AsQueryable` overload with per-column parameterization control; PR #5495 |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Hints.cs` | `TableHint`/`IndexHint`/`JoinHint`/`SubQueryHint`/`QueryHint` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Delete.cs` | `Delete`/`DeleteWithOutput`/`DeleteWithOutputInto` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Insert.cs` | `Insert`/`InsertWithIdentity`/`InsertWithOutput*`/`Into` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Merge.cs` | `Merge`/`MergeInto`/`Using`/`On` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Update.cs` | `Update`/`UpdateWithOutput*`/`Set`/`AsUpdatable` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.LoadWith.cs` | `LoadWith`/`ThenLoad`/`LoadWithAsTable` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.TableHelpers.cs` | `TableID`/`TableName`/`DatabaseName`/`ServerName`/`SchemaName`/`WithTableExpression` partial |

## Inbound / outbound dependencies

**Inbound** (who calls into EXPR):

- **Every user of LINQ to DB** -- `LinqExtensions` is the user-facing API.
- **[EXPR-TRANS](../EXPR-TRANS/INDEX.md)** -- every `MethodCallBuilder` in `Internal.Linq.Builder` matches calls produced by `LinqExtensions.*` via `Methods.LinqToDB.*.MakeGenericMethod` lookups.
- **[SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)** -- `LinqExtensions.Hints` decorate methods with `[Sql.QueryExtension(provider, scope, builderType)]`.
- **[MAPPING](../MAPPING/INDEX.md)** -- `AttributesExtensions.GetAttributes<T>` is *the* attribute lookup path.
- **[CORE](../CORE/INDEX.md)** -- `Internals.GetDataContext(query)` is the bridge from a public `IQueryable<T>` to the underlying `IDataContext`.

**Outbound** (what EXPR calls):

- `LinqToDB.Internal.Linq.*` (`ExpressionQueryImpl<T>`, `IExpressionQuery`, `SqlQueryRootExpression`, `Internals`) for actual query execution.
- `LinqToDB.Internal.Linq.Builder.*` (`Methods.LinqToDB.*` -- pre-resolved `MethodInfo` constants).
- `LinqToDB.Internal.Reflection.*` (`MethodHelper`, `Methods`) for self-reflection and method-info constants.
- `LinqToDB.Internal.Expressions.ExpressionVisitors.*` -- visitor implementations.
- `LinqToDB.Internal.Mapping.DynamicColumnInfo` -- dynamic-column shim.
- `LinqToDB.Internal.Linq.Builder.IAnnotatableBuilderInternal`, `CteAnnotationNames`, `CteAnnotationsContainer`, `CteBuilderImpl` for the `AsCte`-builder path.

## Known issues / debt

- **Non-static partial in `LinqExtensions.LoadWith.cs`** -- declared `public partial class LinqExtensions` (no `static`), while every other partial uses `public static partial class LinqExtensions`. The compiler treats the union as static. Cosmetic only.
- **Inconsistent indentation around `Delete<T>` returns** -- `LinqExtensions.Delete.cs:382` and `:403` close with a one-tab-shy `}`.
- **`Drop<T>(throwExceptionIfNotExists: false)` swallows every exception** -- see [issue #798](https://github.com/linq2db/linq2db/issues/798): currently catches *anything* during drop.
- **v7 cleanup -- async `WithOutput` overloads.** Every `*WithOutputAsync(..., CancellationToken)` returning `ValueTask<T[]>` is `[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7")]`.
- **v7 cleanup -- `Update<TSource,TTarget>(IQueryable, ITable, setter)`** -- obsoleted.
- **`InsertOrUpdate` cache-key drift suppressed via deferred materialization for CTE builder**, but not for `InsertOrUpdate` itself.
- **`LinqExtensions.cs:1110` -- `ProcessSourceQueryable` invocation in the two-arg `AsQueryable<TElement>`** -- the static `Func<IQueryable, IQueryable>?` hook applies *only* to the `IEnumerable->IQueryable` shortcut path when source is already `IQueryable<TElement>`; new `ExpressionQueryImpl<TElement>` instances bypass it. The three-arg parameterization overload (`LinqExtensions.AsQueryable.cs:41`) always constructs `ExpressionQueryImpl<TElement>` directly and therefore also bypasses this hook.
- **`IAsQueryableBuilder<TElement>` / `IAsQueryableExceptBuilder<TElement>` declaration location not in this area** -- the fluent configuration interfaces referenced by `LinqExtensions.AsQueryable.cs:44` are defined outside `LinqExtensions/`; the translator-side `MethodCallBuilder` for this overload (in EXPR-TRANS) is the primary consumer of the parameterization policy.

## Pointers

- **Translation entry point** -- every `LinqExtensions.*` `Expression.Call(MethodOf(...), ...)` is consumed by `Internal.Linq.Builder.MethodCallBuilder`.
- **Hint attribute infrastructure** -- `Sql.QueryExtensionAttribute`, `Sql.QueryExtensionScope`, and the builder types referenced by `LinqExtensions.Hints.cs` are declared in SQL-PROVIDER.
- **`Methods.LinqToDB.*` constants** -- every `MakeGenericMethod` call on a static `MethodInfo` goes through `LinqToDB.Internal.Reflection.Methods`.
- **`Internals.GetDataContext`** -- the only supported way to extract an `IDataContext` from an arbitrary `IQueryable`.
- **CTE annotation flow** -- `AsCte(source, Action<ICteBuilder>)` -> `CteBuilderImpl` -> `CteAnnotationsContainer` -> consumed during SQL emission.
- **`AsQueryable` parameterization flow** -- `AsQueryable(source, dc, configure)` -> `ExpressionQueryImpl<TElement>` -> EXPR-TRANS `MethodCallBuilder` for this overload -> emits `VALUES(...)` clause with per-cell parameter/inline policy. PR #5495.

## See also

- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) -- the translator that consumes every expression EXPR produces.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- owns `Sql.QueryExtension*` and per-provider hint builders.
- [MAPPING](../MAPPING/INDEX.md) -- heavy `AttributesExtensions` consumer.
- [CORE](../CORE/INDEX.md) -- `Internals`, `IDataContext`, `Methods.LinqToDB.*`.
- `code-design.md` -> **Public API contract**.

<details><summary>Coverage</summary>

**Tier 1 visited (5/5):**
- `Source/LinqToDB/Expressions/IExpressionEvaluator.cs` -- full read.
- `Source/LinqToDB/Expressions/ExpressionExtensions.cs` -- full read.
- `Source/LinqToDB/Expressions/MemberHelper.cs` -- full read.
- `Source/LinqToDB/LinqExtensions/LinqExtensions.cs` -- full read.
- `Source/LinqToDB/LinqExtensions/ICteBuilder.cs` -- full read.

**Tier 2 visited (10/10):**
- `Source/LinqToDB/Extensions/AttributesExtensions.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/CteBuilderExtensions.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Hints.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Delete.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Insert.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Update.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.LoadWith.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.TableHelpers.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Merge.cs` [prior run]
- `Source/LinqToDB/LinqExtensions/LinqExtensions.AsQueryable.cs` -- full read (62 lines) [this run, delta sha 4a478ff1; PR #5495]

**Tier 3 (counted, not read):** none in scope.

</details>
