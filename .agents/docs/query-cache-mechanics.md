# Query cache mechanics

Reference for translator authors who need cache-safe handling of evaluated argument values (chars arrays, enum routing flags, anything that's not a SQL parameter but varies between query invocations). Captured from the PR #5515 cache-mismatch investigation — the previous state of "knowledge in the heads of one or two maintainers" cost ~90 minutes to retrace.

## The cache compare path

Three layers run in this order when linq2db considers a previously-built query as a candidate for a new invocation (`Source/LinqToDB/Internal/Linq/Query.cs:56-108`, `Query.Compare`):

1. **Structural equality.** `CompareInfo.MainExpression.EqualsTo(expressions.MainExpression)` (`Source/LinqToDB/Internal/Expressions/ExpressionVisitors/EqualsToVisitor.cs`). Compares the LINQ expression trees node-by-node. **First gate** — fails fast if the two queries don't share the same shape after normalisation.
2. **Dynamic accessors.** Replays satellite expression accessors (e.g. nested-query expressions) and verifies they still produce the same shape against the new query.
3. **Value-compare functions.** Iterates `CompareInfo.ComparisionFunctions` — each entry is a `(mainFunc, otherFunc)` pair of compiled lambdas. `mainFunc` returns the value that was stored when the cache entry was built; `otherFunc` evaluates the same accessor expression against the new query's runtime state. The final compare is `(value1 == null && value2 == null) || (value1 != null && value1.Equals(value2))`. **The compare is type-sensitive** — `"abc".Equals(['a','b','c'])` is `false`, so if the stored type and the runtime-evaluated type disagree, the cache never hits.

`MarkAsNonParameter(expression, value)` populates the third layer's compare list: the `value` becomes `mainFunc`'s constant, the `expression` becomes `otherFunc`'s accessor body.

## Invariant 1: stored value type MUST match accessor's runtime evaluation type

The compare at `Query.cs:101` is `value1.Equals(value2)`. For reference types, `Object.Equals` is reference equality by default, and `string.Equals(char[])` / `object.Equals` across different runtime types simply returns `false`. The cache then misses on every invocation even when content is identical.

The PR #5515 bug surfaced because the original `MarkAsNonParameter(arg, new string(chars))` stored a `string` but the accessor was the bare `arg` expression (a `char[]` expression). At runtime: `mainFunc` returned the stored string `".+"`; `otherFunc` evaluated the LINQ-side `chars` field and returned `char[]`; compare → false → fresh cache entry every call.

**Fix pattern.** Wrap the accessor expression with the same transform you applied to the stored value:

```csharp
// Stored value: a sorted-string cache key derived from chars.
// Accessor:     same function applied to the runtime chars value.
translationContext.MarkAsNonParameter(
    Expression.Call(_buildCharsCacheKeyMethod, arg),
    BuildCharsCacheKey(chars));
```

Both `mainFunc` and `otherFunc` now produce `string` — `value1.Equals(value2)` is well-defined and the cache compare succeeds when content matches.

**Symmetry with `MathMemberTranslatorBase.TranslateMathRoundMethod`.** The math-round translator stores a `MidpointRounding` enum via `MarkAsNonParameter(arg, routing)` and works correctly because enums are value types — `enum.Equals(enum)` is value equality, and the accessor's runtime evaluation also returns the same enum type. Same-type-on-both-sides is what makes that pattern work; it's not unique to enums.

## Invariant 2: closure-field `MemberInfo` is NOT normalised

`ReplaceParameterizedAndClosures` (`Source/LinqToDB/Internal/Linq/ExpressionCacheManager.cs:370-402`) walks the `MainExpression` and replaces certain `ConstantExpression` nodes (closure object instances, anything `ShouldRemoveConstantFromCache` accepts) with a typed `ConstantPlaceholderExpression`. The downstream `EqualsToVisitor.EqualsTo` then matches a placeholder against any real constant of the same type (`EqualsToVisitor.cs:57`).

What this *does not* normalise: `MemberExpression` nodes that access different fields on the closure. Two distinct C# local variables captured by a lambda end up as distinct fields of the compiler-generated display class (different `FieldInfo`), and `EqualsToVisitor` compares `MemberExpression.Member` directly. The first gate (structural equality) then fails — the value-compare layer never runs.

**Practical consequence.** Two queries that look semantically identical but use *different captured locals* don't share a cache entry:

```csharp
var chars1 = new[] { '.', '+' };
var query1 = table.Select(t => t.Name.TrimEnd(chars1));  // closure field: chars1

var chars2 = new[] { '+', '.' };
var query2 = table.Select(t => t.Name.TrimEnd(chars2));  // closure field: chars2 (different MemberInfo!)
```

→ Two cache entries, even with content-equivalent values. Value-compare never gets a chance.

**To get cache HIT across invocations with the same shape**, use a local helper function with the value as a parameter:

```csharp
IQueryable<string?> BuildQuery(char[] chars) =>
    table.Select(t => t.Name.TrimEnd(chars));

BuildQuery(new[] { '.', '+' });   // closure: <DisplayClass>.chars, instance A
BuildQuery(new[] { '+', '.' });   // closure: <DisplayClass>.chars, instance B (SAME FieldInfo)
```

Both calls capture into the same display-class type with the same `chars` field, so the `MemberExpression`s structurally match. Value-compare then differentiates the values via `MarkAsNonParameter`.

## Invariant 3: `Block`-with-reused-temp survives into SQL translation ⚠️ needs re-verification

> **Stale anchor — re-verify before relying on this.** As recorded, this invariant cited an inlining shortcut in `ExposeExpressionVisitor.VisitBlock` (`…/Builder/Visitors/ExposeExpressionVisitor.cs:912-961`). That method **no longer exists**: the file is still there but contains no `VisitBlock` override and no `BlockExpression` handling at all — the only `VisitBlock` left in `Source/LinqToDB/` is in `ExpressionPrinter.cs`. Whether the shortcut was removed, relocated, or superseded is unresolved. Treat the mechanism below as historical until someone re-derives it against current code; the *practical* guidance in the last paragraph is unaffected by where the inlining lives.

As originally recorded: when a `BlockExpression` consisted of N variable assignments + a result expression, AND each variable was used **exactly once** in the result, the Block was collapsed by substituting each var's value into the result. If any var was used twice (or zero times — F# convention), inlining failed and the Block fell through to the standard visitor.

**Practical consequence.** A `Block { var s = source; s != null ? s.TrimEnd(chars) : null }` rewrite — to dodge double-evaluation of `source` — keeps `s` referenced twice in the result, so it isn't inlined; the BlockExpression survives into SQL translation, which is rarely set up to handle Block + Variable + Assign nodes. Expect a translation failure or a fallback to client-side eval.

For null-safe rewrites of obsolete static helpers (the PR #5515 case, which went through the since-removed `LegacyMemberConverterBase`), accept the double-eval in the `Expression.Condition(NotEqual(s, null), Call(s, …), null)` form — it's correct for the typical column-reference source where double-eval is harmless. Move to a Block-based rewrite only if the source can have side effects, and accept the SQL-translation regression that comes with it.

## Testing query cache behaviour

The session's testing patterns, in order of what they prove:

1. **Cache HIT on same query re-execute.** Run the same `query` variable's `ToArray()` twice; assert `GetCacheMissCount()` is unchanged across the two calls. Catches Invariant 1 (type mismatch in `MarkAsNonParameter`) cleanly.
2. **Cache HIT across invocations via local-function parameter.** Build the query inside a local function with the cache-keyed value as a parameter; invoke the function twice with semantically-equivalent values (e.g. reordered chars for a sorted-key); assert miss counter unchanged. Catches Invariant 2 + verifies the key's equivalence relation.
3. **Cache MISS on captured-var mutation.** Mutate the captured value in place between two `ToArray()` calls so the cache key changes; assert miss counter increased. Confirms cache invalidation actually happens on content change.

`GetCacheMissCount()` is a static counter on `Query<T>` — global across the AppDomain. Within one test method, sequential invocations are deterministic; across tests, cache state leaks but the delta-from-prior-capture assertion stays sound.
