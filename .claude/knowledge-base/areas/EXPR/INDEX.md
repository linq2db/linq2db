---
area: EXPR
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-30
last_verified_sha: 916d3589b9c9e02e9ef87f755d0861a7a14ac2d9
coverage_tier_1: 5/5
coverage_tier_2: 9/9
---

# EXPR — Expression utilities & public LINQ extensions

The EXPR area is the **public-surface helper layer** that sits between user code and the [EXPR-TRANS](../EXPR-TRANS/INDEX.md) translator. Two distinct responsibilities live here:

1. **`System.Linq.Expressions` plumbing** — pooled visitors, member-info extraction, attribute-cache. Consumed by EXPR-TRANS, [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md), [MAPPING](../MAPPING/INDEX.md), and SQL-AST builders.
2. **The `LinqExtensions` partial-class group** — every `IQueryable<T>` / `ITable<T>` extension method that adds something LINQ doesn't have (`Insert`/`Update`/`Delete`/`Merge`/`LoadWith`/CTE/hints/tagging). Each method follows a strict marshalling pattern: it builds a `MethodCallExpression` referencing itself and hands the tree off to the LINQ provider for translation.

EXPR adds no SQL semantics on its own. Methods here are *thin* — argument validation, expression construction, dispatch into `Internal.Linq` execution. The actual translation lives in [EXPR-TRANS](../EXPR-TRANS/INDEX.md) `MethodCallBuilder` plus per-method builders.

## Key types

- `LinqExtensions` — the `[PublicAPI]` static partial class in namespace `LinqToDB`, split across nine files (`LinqExtensions.cs:28`). Every public extension method is here. The class is the canonical entry point for non-IQueryable LINQ-style operations.
- `ExpressionExtensions` (`Source/LinqToDB/Expressions/ExpressionExtensions.cs:19`) — `Visit`/`Find`/`Transform`/`Replace`/`GetBody` helpers. All variants come in TContext-generic and non-generic flavours; both rent a pooled visitor (`VisitActionVisitor`, `FindVisitor`, `TransformVisitor`, `TransformInfoVisitor`) from `LinqToDB.Internal.Expressions.ExpressionVisitors` to avoid allocation per traversal.
- `MemberHelper` (`Source/LinqToDB/Expressions/MemberHelper.cs:14`) — extracts `MemberInfo`/`MethodInfo`/`ConstructorInfo` from `Expression<Func<...>>` lambdas. The `MethodOf`/`MemberOf`/`PropertyOf`/`ConstructorOf`/`MethodOfGeneric` family are how the rest of the codebase obtains `MethodInfo` references for static `Methods.LinqToDB.*` constants without resorting to string-typed reflection. `MemberInfoWithType` struct (`MemberHelper.cs:17`) bundles a `MemberInfo` with the declaring type at the call site (relevant when the call is `Sql.Property<int>(x, "Foo")` and the member is a [dynamic column](../glossary.md#dynamic-column)).
- `IExpressionEvaluator` (`Source/LinqToDB/Expressions/IExpressionEvaluator.cs:5`) — two-method interface (`CanBeEvaluated`/`Evaluate`) that EXPR-TRANS injects to evaluate compile-time-knowable subtrees during translation. The implementation lives in `Internal.Expressions`; this area only exposes the contract.
- `ICteBuilder` (`Source/LinqToDB/LinqExtensions/ICteBuilder.cs:11`) — fluent CTE configuration handed to the `AsCte(source, cteBuilder)` callback. `HasName(string?)` is the only built-in setter; everything else (e.g. `IsMaterialized`) is an extension over this interface in `CteBuilderExtensions`.
- `AttributesExtensions` (`Source/LinqToDB/Extensions/AttributesExtensions.cs:20`) — *the* attribute-lookup path for the entire codebase. Replaces banned native `GetCustomAttribute(s)` calls (RS0030 enforcement, see `LinqExtensions/CteBuilderExtensions.cs:34` and elsewhere) with a process-wide `ConcurrentDictionary` cache keyed by `ICustomAttributeProvider`. Empty results are stored as `[]` to avoid per-lookup `Array.Empty<>` allocation noise. `GetAttributes<T>`/`GetAttribute<T>`/`HasAttribute<T>` are the public API.

## Subsystems

### Expression-tree utilities (`Source/LinqToDB/Expressions/`)

The three files form a closed surface: `IExpressionEvaluator` declares the contract, `MemberHelper` and `ExpressionExtensions` implement everything users and `Internal.*` need to *read* and *rewrite* expression trees. Two design choices stand out:

- **Pooled visitors.** Every `Visit`/`Find`/`Transform` call rents from a `Pool` (`ExpressionExtensions.cs:103`, `:178`, `:309`) and returns it via `using var`. The visitor implementations live in [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — EXPR exposes only the entrypoints. This avoids per-traversal allocation in tight translation loops.
- **`TContext`-generic overloads.** Every visitor entrypoint has both a non-generic `Func<Expression, ...>` form and a generic `Func<TContext, Expression, ...>` form taking a `static` lambda. The generic form lets callers avoid closure allocation by capturing state in `TContext` (see `Replace` at `ExpressionExtensions.cs:199` for the pattern: a tuple as `TContext`, a `static` lambda).

`GetMemberInfoWithType` (`MemberHelper.cs:97`) handles the unusual cases: `Sql.Property<T>(obj, "Name")` where the second argument is a string literal, anonymous-type `new { ... }` constructors, `Array.Length` (which is a `UnaryExpression` with `NodeType == ArrayLength`), and dynamic columns (returns `DynamicColumnInfo` when the member doesn't exist on the type). The `IsSqlPropertyMethod` check at `MemberHelper.cs:107` is how the SQL-AST translator distinguishes `Sql.Property` from a real method call.

### Public LINQ extensions (`Source/LinqToDB/LinqExtensions/`)

All ten files are partials of one class, `LinqToDB.LinqExtensions`. The split is by feature area, not by visibility. Every extension method follows the same recipe:

1. `ArgumentNullException.ThrowIfNull` for each non-default parameter.
2. Call `source.GetLinqToDBSource()` (or `source.ProcessIQueryable()` for tunnel methods that compose into the tree) to get an `IExpressionQuery`.
3. Build an `Expression.Call(null, MethodHelper.GetMethodInfo(SelfMethod, args...), <quoted args>)` referencing the calling method.
4. Either `currentSource.Provider.CreateQuery<T>(expr)` (returns `IQueryable<T>` — pure tunnel methods) or `currentSource.Execute<T>(expr)` / `ExecuteAsync<T>` (terminal methods that run the SQL).

`MethodHelper.GetMethodInfo` (in `Internal.Reflection`, used at `LinqExtensions.cs:682` etc.) reflects the calling method off a delegate for self-reference — keeps the method-info string-free and rename-safe. The few methods that bypass `MethodHelper` (`InsertOrUpdate` at `LinqExtensions.cs:107`, set-operator methods, several `Methods.LinqToDB.*.MakeGenericMethod` calls) cache a `static readonly MethodInfo` at class-load via `MemberHelper.MethodOf(() => ...).GetGenericMethodDefinition()` because they appear in hot paths or have generic signatures the `GetMethodInfo` reflector can't disambiguate.

The partials decompose as:

- `LinqExtensions.cs` — root partial: `Select`/`Take`/`Skip`/`ElementAt`/`Having`/`ThenOrBy`/`Join`/`InnerJoin`/`LeftJoin`/`RightJoin`/`FullJoin`/`CrossJoin`/`AsCte`/`AsSubQuery`/`QueryName`/`InlineParameters`/`HasUniqueKey`/`UnionAll`/`ExceptAll`/`IntersectAll`/`IgnoreFilters`/`TagQuery`/`ToSqlQuery`/`InsertOrUpdate`/`Drop`/`Truncate`/`AggregateExecute`. Also hosts the `ProcessSourceQueryable` and `ExtensionsAdapter` static properties (`LinqExtensions.cs:1491,1493`) — these are the global hooks for query-rewriting interceptors and async-EF-style adapters respectively.
- `LinqExtensions.Insert.cs` — the entire `Insert`/`InsertWithOutput`/`InsertWithIdentity`/`InsertWithInt32Identity`/`Decimal`/`Int64Identity` matrix; nested `ValueInsertable<T>` and `SelectInsertable<T,TT>` classes that wrap an `IQueryable` and expose the `IValueInsertable<T>`/`ISelectInsertable<TS,TT>` interfaces. `Into` (`LinqExtensions.Insert.cs:1368`, `:2015`) is the entry point. The `[Obsolete]/v7-removal` markers on async-`ValueTask<T[]>` overloads (e.g. `LinqExtensions.Insert.cs:529`) are the migration path to `IAsyncEnumerable`-returning equivalents.
- `LinqExtensions.Update.cs` — `Update`/`UpdateAsync`/`UpdateWithOutput`/`UpdateWithOutputInto`/`Set`/`AsUpdatable` family, including the four `Set` overloads (extract-as-expr / extract-as-value / `IUpdatable` vs. `IQueryable` source) and the string-interpolation `Set<T>(this IQueryable<T>, Expression<Func<T,string>>)` at `:2078`. Internal `Updatable<T>` class wrapping `IQueryable<T>` (`:1845`).
- `LinqExtensions.Delete.cs` — `Delete`/`DeleteAsync`/`DeleteWithOutput`/`DeleteWithOutputInto` only. Per-DB support comments on each `WithOutput` method (e.g. `:30-37`) document the upstream provider matrix.
- `LinqExtensions.Merge.cs` — the merge-builder fluent API (`Merge`/`MergeInto`/`Using`/`UsingTarget`/`On`/`OnTargetKey`/`Insert*WhenNotMatched`/`Update*WhenMatched`/`DeleteWhenMatched`/`MergeAsync`). Internal `MergeQuery<TTarget,TSource>` (`:23`) implements every step interface (`IMergeableUsing`, `IMergeableOn`, `IMergeableSource`, `IMergeable`) on the same wrapped `IQueryable<TTarget>` — interface narrowing is purely compile-time. Method-info constants live in `Internal.Reflection.Methods.LinqToDB.Merge.*`.
- `LinqExtensions.LoadWith.cs` — `LoadWithAsTable` (`:47`), `LoadWith<TEntity,TProperty>` (4 overloads), `ThenLoad<TEntity,TPrev,TProperty>` (4 overloads varying single/many cardinality on `TPrev`/`TProperty` and presence of `loadFunc`). The `LoadWithQueryableBase<TEntity>` and `LoadWithQueryable<TEntity,TProperty>` private classes (`:64`, `:80`) wrap an `IExpressionQuery<TEntity>` and expose `ILoadWithQueryable<TEntity,TProperty>` — they're the thing `ToSqlQuery` unwraps (`LinqExtensions.cs:1565`). **Note**: `class LinqExtensions` declared without `static` modifier in this partial (`LoadWith.cs:15`) — non-static partial of a static class, but the file's other partial declarations carry `static`. The compiler accepts the mix; it's an oversight rather than a behavior change.
- `LinqExtensions.Hints.cs` — `With`/`TableHint`/`TablesInScopeHint`/`IndexHint`/`JoinHint`/`SubQueryHint`/`QueryHint`. Each hint carries `[Sql.QueryExtension]` attributes that route per-provider extension builders (`TableSpecHintExtensionBuilder`, `HintExtensionBuilder`, `HintWithParameterExtensionBuilder`, `HintWithParametersExtensionBuilder`, `NoneExtensionBuilder`) — see `LinqExtensions.Hints.cs:25-28` for the canonical attribute stack pattern. The builders themselves live in [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) (`LinqToDB.Internal.SqlProvider`).
- `LinqExtensions.TableHelpers.cs` — `TableID`/`TableName`/`DatabaseName`/`ServerName`/`SchemaName`/`WithTableExpression`. All except `WithTableExpression` go through `((ITableMutable<T>)table).Change*Name(name)` rather than building expressions — these mutate the `ITable<T>` wrapper directly. `WithTableExpression` (`:127`) is the odd one out: it builds an `Expression.Call` because the SQL template has to round-trip through the translator.
- `CteBuilderExtensions.cs` — extension methods on `ICteBuilder`. Currently only `IsMaterialized(bool)` (`CteBuilderExtensions.cs:31`); the doc-comment lists the supported provider matrix (PostgreSQL 12+, SQLite 3.35+, ClickHouse 26.3+) and explicitly notes silent no-op behavior on unsupported providers. The `IAnnotatableBuilderInternal` cast at `:35` is how the public `ICteBuilder` interface stays minimal while the internal builder grows annotation slots.

### Attribute-cache layer (`Source/LinqToDB/Extensions/`)

`AttributesExtensions` is one file but it's load-bearing. The repo bans `MemberInfo.GetCustomAttribute(s)` via `Build/BannedSymbols.txt` (RS0030 errors at `Extensions/AttributesExtensions.cs:42-43`, `:53-54`, `:73`); every attribute lookup in the codebase routes through `GetAttributes<T>(provider, inherit)`. The cache is two-tier: an outer `_inheritAttributes`/`_noInheritAttributes` keyed by `ICustomAttributeProvider`, plus per-`T` generic `InheritAttributeCache<T>`/`NoInheritAttributeCache<T>` to memoize the type-filter step. The inherit-true path has a workaround for [dotnet/runtime#30219](https://github.com/dotnet/runtime/issues/30219) where the non-generic `GetCustomAttributes(inherit:true)` API mishandles property/event inheritance — for `PropertyInfo`/`EventInfo` it explicitly calls the generic `GetCustomAttributes<Attribute>(inherit:true)` instead (`AttributesExtensions.cs:60-61`).

## Files (Tier 1 / Tier 2)

| Tier | File | Role |
|---|---|---|
| 1 | `Source/LinqToDB/Expressions/IExpressionEvaluator.cs` | EvaluatesExpression contract; consumed by EXPR-TRANS |
| 1 | `Source/LinqToDB/Expressions/ExpressionExtensions.cs` | Pooled-visitor entrypoints: `Visit`/`Find`/`Transform`/`Replace`/`GetBody`; `GetMemberGetter` |
| 1 | `Source/LinqToDB/Expressions/MemberHelper.cs` | `MemberOf`/`MethodOf`/`PropertyOf`/`ConstructorOf` lambda-extraction; `MemberInfoWithType` |
| 1 | `Source/LinqToDB/LinqExtensions/LinqExtensions.cs` | Root partial: scalar-select, paging, joins, CTE, set ops, sub-query/query-name, tag, ToSqlQuery |
| 1 | `Source/LinqToDB/LinqExtensions/ICteBuilder.cs` | Fluent CTE-builder contract: `HasName(string?)` |
| 2 | `Source/LinqToDB/Extensions/AttributesExtensions.cs` | Process-wide cached `Get/HasAttribute(s)<T>` extensions; replaces banned native APIs |
| 2 | `Source/LinqToDB/LinqExtensions/CteBuilderExtensions.cs` | `ICteBuilder.IsMaterialized` — sets `CteAnnotationNames.Materialized` annotation |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Hints.cs` | `TableHint`/`IndexHint`/`JoinHint`/`SubQueryHint`/`QueryHint` partial; per-provider `[Sql.QueryExtension]` stacks |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Delete.cs` | `Delete`/`DeleteWithOutput`/`DeleteWithOutputInto` partial |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Insert.cs` | `Insert`/`InsertWithIdentity`/`InsertWithOutput*`/`Into`/`Value`/`AsValueInsertable` partial; `ValueInsertable<T>` + `SelectInsertable<T,TT>` private classes |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Merge.cs` | `Merge`/`MergeInto`/`Using`/`On`/`On(Target|Source)Key`/`Insert*`/`Update*`/`DeleteWhenMatched`/`Merge[Async]` partial; `MergeQuery<TTarget,TSource>` private class |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.Update.cs` | `Update`/`UpdateWithOutput*`/`Set`/`AsUpdatable` partial; `Updatable<T>` private class |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.LoadWith.cs` | `LoadWith`/`ThenLoad`/`LoadWithAsTable` partial; `LoadWithQueryableBase`/`LoadWithQueryable` private classes |
| 2 | `Source/LinqToDB/LinqExtensions/LinqExtensions.TableHelpers.cs` | `TableID`/`TableName`/`DatabaseName`/`ServerName`/`SchemaName`/`WithTableExpression` partial — most go via `ITableMutable<T>.Change*` |

## Inbound / outbound dependencies

**Inbound** (who calls into EXPR):

- **Every user of LINQ to DB** — `LinqExtensions` is the user-facing API. Public type per `code-design.md` → namespace `LinqToDB`. Removing or signature-changing methods here is a public API break (covered by `Source/LinqToDB/CompatibilitySuppressions.xml` / `api-baselines` skill).
- **[EXPR-TRANS](../EXPR-TRANS/INDEX.md)** — every `MethodCallBuilder` in `Internal.Linq.Builder` matches calls produced by `LinqExtensions.*` via `Methods.LinqToDB.*.MakeGenericMethod` lookups. Builder code reads `MethodHelper`/`MemberHelper` to identify which extension a `MethodCallExpression` came from.
- **[SQL-PROVIDER](../SQL-PROVIDER/INDEX.md)** — `LinqExtensions.Hints` decorate methods with `[Sql.QueryExtension(provider, scope, builderType)]`; the per-provider builders consume those attributes during SQL emission. `MemberHelper.MethodOfGeneric` is the dominant way provider code obtains `MethodInfo` for `Sql.*` static methods to register translators.
- **[MAPPING](../MAPPING/INDEX.md)** — `AttributesExtensions.GetAttributes<T>` is *the* attribute lookup path; `MAPPING/MappingSchema.cs` and the `EntityDescriptor` pipeline lean on it heavily.
- **[CORE](../CORE/INDEX.md)** — `Internals.GetDataContext(query)` (used at `LinqExtensions.cs:1478`) is the bridge from a public `IQueryable<T>` to the underlying `IDataContext`; defined in CORE.

**Outbound** (what EXPR calls):

- `LinqToDB.Internal.Linq.*` (`ExpressionQueryImpl<T>`, `IExpressionQuery`, `SqlQueryRootExpression`, `Internals`) for actual query execution.
- `LinqToDB.Internal.Linq.Builder.*` (`Methods.LinqToDB.*` — pre-resolved `MethodInfo` constants) for the static-method-info table the extension methods reference.
- `LinqToDB.Internal.Reflection.*` (`MethodHelper`, `Methods`) for self-reflection and method-info constants.
- `LinqToDB.Internal.Expressions.ExpressionVisitors.*` (`VisitActionVisitor`, `VisitFuncVisitor`, `FindVisitor`, `TransformVisitor`, `TransformInfoVisitor`) — visitor implementations.
- `LinqToDB.Internal.Mapping.DynamicColumnInfo` — dynamic-column shim used by `MemberHelper.GetMemberInfoWithType` and `ExpressionExtensions.GetMemberGetter`.
- `LinqToDB.Internal.Linq.Builder.IAnnotatableBuilderInternal`, `CteAnnotationNames`, `CteAnnotationsContainer`, `CteBuilderImpl` for the `AsCte`-builder path (`LinqExtensions.cs:1068-1086`, `CteBuilderExtensions.cs:35-40`).

## Known issues / debt

- **Non-static partial in `LinqExtensions.LoadWith.cs`** — declared `public partial class LinqExtensions` (no `static`) at `:15`, while every other partial uses `public static partial class LinqExtensions`. The compiler treats the union as static (one `static` partial declaration suffices), but the `LoadWith` file alone reads as non-static. Cosmetic only — flag for a future formatting pass.
- **Inconsistent indentation around `Delete<T>` returns** — `LinqExtensions.Delete.cs:382` and `:403` close with a one-tab-shy `}` (visible misalignment with the surrounding `}` at `:381` and `:402`). Mid-file regression, not a behavior bug.
- **`Drop<T>(throwExceptionIfNotExists: false)` swallows every exception** — see the doc-comment at `LinqExtensions.cs:271` calling out [issue #798](https://github.com/linq2db/linq2db/issues/798): currently catches *anything* during drop, not only "missing table" errors. Documented; tracked.
- **v7 cleanup — async `WithOutput` overloads.** Every `*WithOutputAsync(..., CancellationToken)` returning `ValueTask<T[]>` is `[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]` (e.g. `LinqExtensions.Insert.cs:528-538`, `Delete.cs:101-106`, `Update.cs:118-128`/`248-258`/`540-548`/`665-674`/`942-948`/`1054-1063`/`1303-1310`/`1408-1416`). Removal is a deliberate v7 break.
- **v7 cleanup — `Update<TSource,TTarget>(IQueryable, ITable, setter)`** — obsoleted (`LinqExtensions.Update.cs:1582`), replaced by the lambda-target overload at `:1795`.
- **`InsertOrUpdate` cache-key drift suppressed via deferred materialization for CTE builder**, but not for `InsertOrUpdate` itself — `LinqExtensions.cs:1067-1073` documents that placing a delegate in the expression tree collapses query-cache keys, so the `AsCte(builder)` overload runs the callback up-front and stuffs the resulting `CteAnnotationsContainer` (value-equatable) into the tree. This pattern is **specific to `AsCte`** and worth replicating elsewhere if a similar shape appears.
- **`LinqExtensions.cs:1110` — `ProcessSourceQueryable` invocation in `AsQueryable<TElement>`.** The static `Func<IQueryable, IQueryable>?` hook applies *only* to the `IEnumerable→IQueryable` shortcut path when source is already `IQueryable<TElement>`; new `ExpressionQueryImpl<TElement>` instances (the regular construction path) bypass it. Documented inline by behavior; subtle.

## Pointers

- **Translation entry point** — every `LinqExtensions.*` `Expression.Call(MethodOf(...), ...)` is consumed by `Internal.Linq.Builder.MethodCallBuilder` and per-method `BuilderInfo` registrations — see [EXPR-TRANS](../EXPR-TRANS/INDEX.md).
- **Hint attribute infrastructure** — `Sql.QueryExtensionAttribute`, `Sql.QueryExtensionScope`, and the builder types referenced by `LinqExtensions.Hints.cs` are declared in [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md).
- **`Methods.LinqToDB.*` constants** — every `MakeGenericMethod` call on a static `MethodInfo` goes through `LinqToDB.Internal.Reflection.Methods` (in CORE/Reflection); look there to learn which extension method an internal builder is matching.
- **`Internals.GetDataContext`** — the only supported way to extract an `IDataContext` from an arbitrary `IQueryable`. Lives in CORE.
- **CTE annotation flow** — `AsCte(source, Action<ICteBuilder>)` → `CteBuilderImpl` (Internal.Linq.Builder) → `CteAnnotationsContainer` (Internal.SqlQuery) → consumed during SQL emission by [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md). The `IsMaterialized` annotation flow is the smallest end-to-end example.

## See also

- [EXPR-TRANS](../EXPR-TRANS/INDEX.md) — the translator that consumes every expression EXPR produces.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — owns `Sql.QueryExtension*` and per-provider hint builders.
- [MAPPING](../MAPPING/INDEX.md) — heavy `AttributesExtensions` consumer.
- [CORE](../CORE/INDEX.md) — `Internals`, `IDataContext`, `Methods.LinqToDB.*`.
- `code-design.md` → **Public API contract** (this area is almost entirely public surface).
- `architecture/sql-ast.md`, `architecture/sql-provider.md`, `architecture/expression-translator.md`.

<details><summary>Coverage</summary>

**Tier 1 visited (5/5):**
- `Source/LinqToDB/Expressions/IExpressionEvaluator.cs` — full read (10 lines).
- `Source/LinqToDB/Expressions/ExpressionExtensions.cs` — full read (385 lines).
- `Source/LinqToDB/Expressions/MemberHelper.cs` — full read (272 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.cs` — full read (1669 lines).
- `Source/LinqToDB/LinqExtensions/ICteBuilder.cs` — full read (21 lines).

**Tier 2 visited (9/9):**
- `Source/LinqToDB/Extensions/AttributesExtensions.cs` — full read (143 lines).
- `Source/LinqToDB/LinqExtensions/CteBuilderExtensions.cs` — full read (45 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Hints.cs` — full read (498 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Delete.cs` — full read (457 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Insert.cs` — full read (2321 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Update.cs` — full read (2134 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.LoadWith.cs` — full read (765 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.TableHelpers.cs` — full read (143 lines).
- `Source/LinqToDB/LinqExtensions/LinqExtensions.Merge.cs` — read in scope (file is 1274 lines; first ~300 lines covering `MergeQuery<TTarget,TSource>` declaration and source/target/`On` configuration read in full; remainder follows the same MethodCallBuilder pattern documented elsewhere — `Insert*WhenNotMatched` / `Update*WhenMatched` / `DeleteWhenMatched` / `Merge[Async]` terminal — and the doc-comments cite the same provider matrix found in `Delete`/`Insert`).

**Tier 3 (counted, not read):** none in scope.

</details>
