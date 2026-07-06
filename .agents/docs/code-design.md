## Codebase design invariants

Design-level facts about what linq2db *is* as a library. These are invariants agents should preserve and reviewers should enforce — they describe the product, not the review workflow. Operational rules for how agents *act* on the codebase live in [`agent-rules.md`](agent-rules.md) → Agent Guardrails.

### Public API is a contract

Types, method signatures, and observable generated SQL in non-`Internal.*` namespaces under `Source/LinqToDB/` are a stability contract for downstream consumers. Don't modify them without a clear, explicit reason — new major-release milestone, documented breaking change, or a namespace-placement fix (see below).

The ApiCompat baseline files at `Source/**/CompatibilitySuppressions.xml` are the enforcement mechanism. Any surface change — add, modify, or remove — requires regenerating them (e.g. `dotnet pack -p:ApiCompatGenerateSuppressionFile=true`, or run the `api-baselines` skill).

### Provider-called core methods are public, not internal

When a provider's SQL builder/optimizer (`YdbSqlBuilder`, any `*SqlBuilder` / `*SqlOptimizer`) calls a *new* method on shared core — `OptimizationContext`, the SQL AST, optimizer/builder base classes — declare that method **`public`**, even when the only in-tree caller lives in the same assembly. Out-of-tree / external providers consume the same surface and need the entry point; `internal` locks them out. (A new public method then needs a `PublicAPI.Unshipped.txt` entry per the contract above.)

### Behavior is part of the public-API contract

The "stability contract" in **Public API is a contract** above covers types and signatures. It ALSO covers observable behavior of long-standing public methods — particularly the ones with a documented combination of in-memory C# and SQL behavior (`Sql.Concat`, `Sql.ConcatStrings*`, similar `Sql.*` helpers). User code has been written against the existing semantics, and a refactor that reshapes an internal pipeline must NOT shift the behavior of these methods even when the refactor's internal symmetry argues for it.

When a refactor seems to require a behavior change on such an API:

- Restore the pre-refactor behavior in the same PR. Don't accept the break with a release-notes callout — the callout doesn't save user code that already relied on the old semantics.
- The PR's design-symmetry / unification argument is not enough; the asymmetry lives inside the implementation, and the implementation can absorb it without exposing it to callers.
- If a behavior change is genuinely necessary, file it as a separate proposal with its own design review, milestone, and migration documentation.

This applies most strongly to APIs that have been public since v1.0 / older majors and have user code behind them. Newer APIs (added in the current major) have less downstream surface and the cost of a behavior change is lower.

**`[Obsolete]` does not unfreeze behavior.** A public method marked `[Obsolete]` stays callable by downstream code until it is actually removed — its observable behavior is as frozen as any other public API. When an *internal* caller (e.g. a member mapping in `Linq/Expressions.cs`) needs corrected behavior, do **not** edit the obsolete method's body: add a private copy with the fix and repoint the internal callers at it, leaving the public method untouched. (Done in #5577: public `ConvertToCaseCompareTo`'s null-returning body stayed frozen; the member mappings moved to a BCL-faithful private `ConvertToCaseCompareToImpl`.)

### Member-mapping bodies are the local-evaluation definition

A BCL method mapped via `Expressions.MapMember` / a `[Sql.Extension]` method has *two* roles: the SQL builder translates the call to an AST node (e.g. `string.Compare`/`CompareTo` → `SqlCompareToExpression`), and the mapping's **managed body / lambda is the local-evaluation fallback**. The expose pass can expand these mappings, and the test harness `AssertQuery` (plus any genuine client-side evaluation) then runs the expanded form as LINQ-to-objects. So the managed body MUST faithfully reproduce the original BCL method's semantics — including null handling — not just "something the SQL builder ignores".

A body that diverges passes for years while only the SQL path runs, then breaks the moment expose expands it into local evaluation. Concrete case (#5577): `ConvertToCaseCompareTo` returned `null` when an operand was null, which `string.Compare` never does; once the always-expand expose change made `AssertQuery` evaluate it in-memory, the `null` collapsed to `0` via null-propagation and flipped comparisons. Verify both paths: SQL correctness **and** in-memory equivalence.

### Calculated columns may also be physical columns; entity construction must not double-emit

A calculated column (`ExpressionMethodAttribute.IsColumn=true`, in `EntityDescriptor.CalculatedMembers`) can simultaneously be a mapped **physical** column. Fluent `EntityMappingBuilder.Property(x)` forces this — it calls `.IsColumn()`, registering a `ColumnAttribute` — whereas `.Member(x)` attaches attributes *without* mapping a column. So `.Property(e => e.X).HasAttribute(new ExpressionMethodAttribute(...) { IsColumn = true })` makes `X` both a physical column and a calculated member; `.Member(...)` makes it calculated-only.

Entity construction must emit each such member **once**: `BuildGenericFromMembers` (in `EntityConstructorBase`) excludes members present in `CalculatedMembers` from the physical-column assignments, because `BuildCalculatedColumns` emits the expanded substitution for them. Use `MemberInfoComparer.Instance` for any `MemberInfo` set here — `column.MemberInfo` can carry a different `ReflectedType` than the `CalculatedMembers` entry, so default equality misses the match. Skipping this dedup emits a bogus physical column read alongside the calculated expression (#5540: PostgreSQL `42703 column … does not exist`; the v6 blanket `ConvertExpressionTree(fullEntity)` pass used to mask it by rewriting the stray read).

### Cross-cutting internals are shared

The SQL AST (`Source/LinqToDB/SqlQuery/` and `Source/LinqToDB/Internal/SqlQuery/`), the `IDataProvider` interface, and the translator interfaces under `Source/LinqToDB/Linq/Translation/` are consumed by every database provider in the repo. A fix scoped to one provider or one test shouldn't reshape them — the blast radius is the whole product. When a local task seems to need a cross-cutting change, surface the question explicitly rather than making the change silently.

### Don't grow core builder API for a helper-only fix

A fix scoped to a helper / peripheral API (e.g. `ToSqlQuery`'s by-name `Query<T>` round-trip) must not expand core builder surface (`ISqlBuilder` / `BasicSqlBuilder`) to serve it — threading a new flag/parameter through the shared builder for one convenience path fails the cost/benefit test. If the fix genuinely needs new core surface, drop or narrow it instead. (User call on #5657: an export-scoping `ForGetSqlText` seam on `ISqlBuilder` was rejected — "if we need to add api changes to sqlbuilder then we will not use this fix for helper API"; the peripheral behavior was left as-is.) This is the API-surface counterpart of the general "propose minimal, let the user expand" preference.

### Companion interfaces to public contracts stay public, in the contract's namespace

An opt-in companion interface extending an existing public contract (e.g. a schema-aware extension of `IMetadataReader`) defaults to **public, in the same namespace as the contract it extends** — not `LinqToDB.Internal.*`, and not `InternalsVisibleTo` (the repo uses none). An extension seam that third parties may implement has no value hidden; it is part of the same contract surface. (User decision on #5675, overriding a proposed `Internal.Metadata` placement: "no value in hiding it".) This does not soften the SQL AST rule below — AST construction types are *not* extension seams and MUST stay in `LinqToDB.Internal.SqlQuery`.

### `IsDependsOnSources` ignore-set doesn't cover field/column refs

`QueryHelper.IsDependsOnSources(expr, onSources, sourcesToIgnore:)` applies `sourcesToIgnore` only on the **direct `ISqlTableSource`-element** match path. The `SqlField` / `SqlColumn` paths — how predicates actually reference tables — check `OnSources.Contains(field.Table)` / `Contains(column.Parent)` **without** consulting `sourcesToIgnore`. So `sourcesToIgnore` does *not* subtract a table a predicate reaches through a field, and `IsDependsOnSources(pred, [t], sourcesToIgnore: [t])` still returns true.

To ask "does this expression depend on any source *outside* a given subtree" — e.g. is a join predicate purely right-side, in `SelectQueryOptimizerVisitor.MoveJoinConditionsToWhere` — use **`QueryHelper.IsDependsOnOuterSources(expr, currentSources: <subtree sources>)`**, which collects `field.Table` / `column.Parent` and excepts `currentSources` at the field level. Reaching for `IsDependsOnSources(..., sourcesToIgnore:)` here is a dead end.

### Internal AST APIs trust NRT — validation lives in factory extensions

Constructors and `Modify` methods on types under `LinqToDB.Internal.SqlQuery.*` (and peer internal AST namespaces) do **not** carry null / empty argument guards. Validation is the job of the factory extensions (`SqlExpressionFactoryExtensions.Concat`, peers) that provide the validated entry point for broader use; bare AST ctors trust callers to respect `<Nullable>enable</Nullable>` and pass sane parameters. Adding the same guard on the ctor duplicates the check at no benefit and adds noise NRT analysis would have already surfaced.

Reviewer consequence: do **not** flag missing constructor / `Modify` null-or-empty guards on a new AST type as a defect — `SqlConcatExpression`, `SqlCoalesceExpression`, and the rest of the peers follow this convention by design. Exception: when a caller path is genuinely unverified (data off the wire with no schema validation, etc.), surface the concern at the *caller*, not the AST type itself.

### Version-aware translators: derive a subclass, don't parameterize

When provider behavior depends on the database version, the repo's convention is to **create a version-specific translator subclass** and dispatch on `Version` in the data provider's `CreateMemberTranslator()`. Do **not** parameterize the base translator constructor with a feature flag — that's not how the codebase is structured.

Canonical pattern (SqlServer; same shape used by MySql 8/MariaDB):

```csharp
// In <Provider>DataProvider.cs:
protected override IMemberTranslator CreateMemberTranslator() => Version switch
{
    >= SqlServerVersion.v2022 => new SqlServer2022MemberTranslator(),
    >= SqlServerVersion.v2017 => new SqlServer2017MemberTranslator(),
    >= SqlServerVersion.v2016 => new SqlServer2016MemberTranslator(),
    ...
    _                         => new SqlServerMemberTranslator(),
};

// Source/LinqToDB/Internal/DataProvider/SqlServer/Translation/SqlServer2022MemberTranslator.cs:
public class SqlServer2022MemberTranslator : SqlServer2017MemberTranslator
{
    protected override IMemberTranslator CreateStringMemberTranslator() => new SqlServer2022StringMemberTranslator();

    protected class SqlServer2022StringMemberTranslator : SqlServer2017StringMemberTranslator
    {
        // override the methods that gained 2022 support
    }
}
```

Each subclass inherits everything from its lower-version parent and only overrides the methods whose translation actually changed in that version. When two providers gained the same capability in equivalent versions (e.g. MySQL 8 + MariaDB 10 both got `REGEXP_REPLACE`), the data provider's switch can route both versions to the same subclass — no need to create a dedicated subclass that just inherits with no body. A `MariaDB10MemberTranslator : MySql80MemberTranslator {}` empty-body class is non-idiomatic; collapse it into `MySql80 or MariaDB10 => new MySql80MemberTranslator()` in the dispatch instead.

### Use `MemberHelper.MethodOf` for expression-tree `MethodInfo` capture

When a translator needs a `System.Reflection.MethodInfo` to construct an `Expression.Call(method, args)` node (e.g. wrapping an accessor expression, or matching a target method in a rewrite), the codebase uses `LinqToDB.Expressions.MemberHelper`:

```csharp
static readonly MethodInfo _stringTrimEndCharArrayMethodInfo =
    MemberHelper.MethodOf<string>(s => s.TrimEnd((char[])null!));

static readonly MethodInfo _toValueMethodInfo =
    MemberHelper.MethodOfGeneric<Sql.IAggregateFunction<string, string>>(f => f.ToValue());
```

`MethodOf(() => Foo(arg))` for static or instance methods reachable via expression; `MethodOf<T>(t => t.Foo())` when an instance receiver of type `T` is needed; `MethodOfGeneric<T>` strips the generic instantiation off the result. Don't roll raw `typeof(X).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!` — it's fragile, doesn't give compile-time validation that the method exists with the expected signature, and isn't the codebase pattern.

When **matching** a method-call node's identity (rather than constructing a call), register the `MethodInfo` in the shared `Methods.LinqToDB.*` registry (`Internal/Reflection/Methods.cs`, captured via the same `MemberHelper.MethodOf*` helpers) and compare with `node.IsSameGenericMethod(Methods.LinqToDB.…)` — the idiom used throughout the builders (`Methods.LinqToDB.ApplyModifierInternal`, etc.). Don't hand-roll `node.Method.DeclaringType == typeof(X) && node.Method.Name == nameof(...)`: string-by-name matching is fragile (no signature/arity check, breaks on overloads) and isn't the codebase pattern. Inside a nested `Methods.LinqToDB.*` class, fully-qualify the captured method with `global::LinqToDB.Sql.…` — a bare `LinqToDB.Sql` binds to the enclosing `Methods.LinqToDB` class, not the root namespace.

### SQL AST types live in `LinqToDB.Internal.SqlQuery`

The SQL query tree building blocks — `SqlFrameClause`, `SqlKeepClause`, `SqlExtendedFunction`, `SqlFrameBoundary`, `SqlSearchCondition`, and every other AST node used only during query construction, translation, and rendering — are library-internal. They are not part of the stable public surface. New AST types MUST go in the `LinqToDB.Internal.SqlQuery` namespace.

A handful of legacy AST classes still live under the public `LinqToDB.SqlQuery` namespace; some carry a `// TODO: v7 - move to internal namespace` marker acknowledging the debt. When a PR modifies one of those legacy AST types' signatures and ApiCompat flags it as a breaking public-API change, the correct fix is to **move the class to `LinqToDB.Internal.SqlQuery` in the same PR** — that repays the pre-existing design debt and removes the apparent public-API breakage in one step. Do **not** target a major-release milestone for what is really just internal AST evolution, and do not add the signature change to the suppression baseline as if it were an intentional public-API break.

This rule refines the `/review-pr` classification in [`api-surface-classification.md`](api-surface-classification.md): a `modified` or `removed` entry whose symbol is in `LinqToDB.SqlQuery` and targets a SQL AST type should be reviewed as a namespace-placement finding (fix: move to `Internal.SqlQuery`), not as a milestone-gated public-API-break BLK.

### Column-aligned formatting is intentional

Large blocks of the codebase use column-aligned formatting — property declarations line their `{ get; }` up at the same column, constructor parameters line their defaults up at the same column, constant declarations line their `=` up at the same column. This is deliberate house style, not accidental. Preserve it when editing; match the existing alignment of surrounding code rather than reformatting it to a narrower width.

Formatting is only worth flagging when it is clearly broken — three or more consecutive blank lines, mixed tabs and spaces that cause visible misalignment, indentation that doesn't match the enclosing scope. The positive alignment style is never the bug.

### TODO markers signal deferred cleanup, not bugs

Comments of the form `// TODO: ... v<N>` or `// FIXME: ... in next major` are an intentional project convention for flagging code that's known to need cleanup or removal in a specific future major release. They're tracked manually rather than via an issue tracker because they only need to be acted on at a major-version boundary.

Don't flag these as scope-creep, stray comments, or "uncommitted thinking-aloud edits" — even when the wording is informal (`// ??? TODO: remove Flags in v7` is a real example). When a PR introduces a new TODO marker that follows this shape, treat it as part of the deferred-cleanup ledger.

The narrower case — `// TODO: v7 - move to internal namespace` markers on legacy SQL AST types — is covered above under **SQL AST types live in `LinqToDB.Internal.SqlQuery`**.

### Exceptions carry cause + remediation; don't collapse specifics into a generic message

linq2db is a library a developer debugs at 2 a.m. through a stack trace, so a thrown exception's message is the primary diagnostic. When adding or editing a `throw`, the message should convey what failed, enough context to see why (the offending member / type / SQL fragment / provider), and — where there is a real one — the remediation. The anti-pattern to avoid is **swallowing a specific failure into a generic one**: catching a precise reason ("this member can't be translated because the subquery is correlated and provider X doesn't support it") and re-throwing it as a bare "conversion error" discards exactly the signal the user needs and makes a fixable usage error look like an engine bug. (linq2db has live instances of this — correlated-subquery reasons collapsing into a generic conversion message — currently parked as deferred work.) Preserve the inner exception / specific reason rather than flattening it.

### Prefer types that make invalid states unrepresentable

The library is `<Nullable>enable</Nullable>` and type-safe by design; lean into it. When a new API or internal structure has a "this combination is illegal" rule, prefer encoding it in the type — a non-nullable field, a discriminated shape, a required ctor parameter, an enum over loose bools — rather than a runtime guard plus a comment. Fewer reachable invalid states means fewer defensive guards, fewer "can this be null here?" review questions, and less of the defensive bloat that accretes when correctness is enforced by convention instead of by the compiler. This is the constructive flip side of **Internal AST APIs trust NRT** above: bare AST ctors can skip guards precisely because the surrounding types already make the bad states hard to construct.

### Oversized files carry an agent-comprehension cost, not just a style one

Very large source files (multi-thousand-line builders, optimizers, AST visitors) are harder for an agent to reason about correctly: comprehension and edit accuracy degrade as a single file grows, and a partial read invites the "looks done but missed a branch" failure. This is **not** a mandate to split existing files — the column-aligned, large-file house style is intentional and churn for its own sake is unwelcome (see **Column-aligned formatting is intentional**). It's a tie-breaker: when genuinely new, separable logic is added, prefer a new focused file / partial over growing an already-huge one; and when a fix inside a giant file needs the surrounding method understood, read the whole method, not a window. A heuristic, never a metric to enforce.

### Read back only the columns you consume

When reading values back from a modifying statement — `OUTPUT` / `RETURNING`, or a follow-up `SELECT` — project **only** the columns the caller will actually use, built from the target column set (`new T { col = src.col, … }`); don't select the whole row and then discard all but a few. Over-fetching is wasteful and obscures intent.
