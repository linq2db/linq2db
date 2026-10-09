# AI Tags for API Documentation

> You are here if you need to:
> - add or update `[AiTags]` metadata on a new or modified public API
> - verify that a key or value in existing AI metadata is valid
> - understand the canonical vocabulary for generated `AI-Tags` keys and values

`AI-Tags` are compact generated metadata annotations for public APIs.
In source code they are authored as internal attributes (`AiTagsAttribute` /
`AiTagsDefaultsAttribute` in `LinqToDB.Internal.Metadata`), not as XML-doc elements or prose in
`<remarks>`.

They are intended for:
- LLM/agent tooling,
- semantic indexing,
- quick API behavior classification.

## Canonical format

Apply the attribute to the member, next to its XML documentation. Values are enum members, so the
compiler validates the vocabulary:

```cs
[AiTags(Groups = AiGroup.DML, Execution = AiExecution.Immediate, Composability = AiComposability.Terminal, Affects = AiAffects.DmlStatement)]
```

Optional defaults attribute for an API surface (applied to the declaring class or interface):

```cs
[AiTagsDefaults(Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
```

Generated docs render the merged attribute values in the canonical display format:

`AI metadata: Key1=Value1; Key2=Value2; ...;`

Keys are rendered in a fixed order (`Groups`, `HintType`, `Execution`, `Composability`, `Affects`,
`Pipeline`, `Provider`). Multi-value (`[Flags]`) values are rendered comma-separated in enum
declaration order, for example `Pipeline=ExpressionTree,SqlAST,SqlText`.

Example:

```cs
[AiTags(Groups = AiGroup.DML, Execution = AiExecution.Immediate, Composability = AiComposability.Terminal, Affects = AiAffects.DmlStatement, Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
```

renders as:

`AI metadata: Groups=DML; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`

Multi-group example (for aggregate API surfaces that span several categories):

```cs
[AiTags(Groups = AiGroup.QueryDirectives | AiGroup.NavigationLoading | AiGroup.DML | AiGroup.Merge | AiGroup.Helpers)]
```

Defaults example (applies to members of the attributed type unless overridden):

```cs
[AiTagsDefaults(Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText, Provider = AiProvider.ProviderDefined)]
```

## Standard keys

| Attribute property | Enum type | Generated key | Meaning |
|---|---|---|---|
| `Groups` | `AiGroup` (`[Flags]`) | `Groups` | API category, or several categories combined with `\|`. |
| `Execution` | `AiExecution` | `Execution` | When execution happens. |
| `Composability` | `AiComposability` | `Composability` | Whether API returns a composable query structure or is terminal. |
| `Affects` | `AiAffects` (`[Flags]`) | `Affects` | Main semantic artifact affected by the call. |
| `Pipeline` | `AiPipeline` (`[Flags]`) | `Pipeline` | Affected translation/execution stages. |
| `Provider` | `AiProvider` | `Provider` | Provider dependency level. |
| `HintType` | `AiHintType` | `HintType` | Hint scope/type for hint-bearing APIs. |

`[AiTagsDefaults]` uses the same properties and enum values as `[AiTags]`.
Only properties set explicitly at the attribute usage are emitted; unset properties are not
rendered as a default enum value.
Generated docs display the merged result as `AI metadata`.

## Coverage policy

Do not treat `[AiTags]` as mandatory for every public member.
Use it on API surfaces where compact machine-readable routing materially helps agents:

- DML terminal and builder APIs;
- deferred query-composition APIs;
- provider-specific hints and SQL directives;
- configuration APIs;
- connection/execution APIs;
- raw SQL/custom SQL APIs;
- APIs that execute SQL immediately, change generated SQL semantics, or alter provider setup.

For overload families, tag representative overloads or all overloads when the metadata differs by
receiver, execution timing, scope, or result shape. Avoid adding duplicate metadata mechanically when
the surrounding type-level/default metadata is already sufficient for discovery.

The accepted coverage model is selective, not exhaustive. Missing `[AiTags]` on an ordinary
public member is not a documentation defect by itself. It is a defect when the member belongs to a
routing-critical public API surface and the missing metadata makes agents more likely to confuse:

- immediate execution with deferred query composition;
- terminal APIs with composable APIs;
- provider-specific APIs with provider-agnostic APIs;
- raw SQL/custom SQL APIs with LINQ-translated APIs;
- hint scopes such as table, query, join, subquery, or tables-in-scope.

When auditing coverage, prefer adding metadata to surfaces that change agent routing decisions over
mechanically tagging every overload. If a topic guide or generated API search already gives agents a
clear route, additional duplicate metadata is optional.

The compiler validates the controlled values: they are members of the `LinqToDB.Internal.Metadata`
enums listed below. The generators reject the retired XML-doc `<ai-tags />` / `<ai-tags-defaults />`
form. If this file lists a value that the enums do not have (or the reverse), this file and the code
are out of sync.

## Controlled values (current baseline)

### `Group` / `Groups` values
- `QueryDirectives`
- `NavigationLoading`
- `Hints`
- `DML`
- `Merge`
- `Helpers`
- `Configuration`
- `Connection`
- `RawSQL` - raw SQL command execution (e.g., `SetCommand` / `CommandInfo` fluent builder pattern; no LINQ translation involved)
- `Schema` - database schema introspection (e.g., `ISchemaProvider.GetSchema`)

### `Execution`
- `Deferred`
- `Immediate`

### `Composability`
- `Composable`
- `Terminal`

### `Provider`
- `ProviderDefined`
- `ProviderAgnostic`

### `HintType`
Use this key for hint-bearing APIs, including `Groups=Hints` and MERGE hint overloads in `Groups=Merge`.
`HintType` tells agents where the hint is applied; it does not name the concrete SQL hint.
For provider-specific typed hint helpers, read the member XML summary and use the SQL hint text
inside `<c>...</c>`.

- `Table` - hint attached to a single table reference
- `TablesInScope` - table hint applied to table references inside the current query scope
- `Index` - index hint attached to a table reference
- `Join` - join hint attached to the next applicable join
- `SubQuery` - hint attached to a subquery/query block
- `Query` - hint attached to the whole query statement
- `Merge` - hint attached to a MERGE statement through the MERGE-specific API
- `TableName` - hint emitted as part of table-name syntax, e.g. temporal-table syntax

### `Affects`
The primary semantic artifact altered or produced by the call.
Compound values (flags combined with `|`, rendered comma-separated) are allowed when a single operation has
primary effects on multiple artifacts (e.g., `AiAffects.DdlStatement | AiAffects.QueryRoot`, rendered as
`DdlStatement,QueryRoot`, for a method that creates a table and returns `ITable<T>`).

- `DmlStatement` - generates a DML statement (INSERT / UPDATE / DELETE / MERGE)
- `DdlStatement` - generates a DDL statement (CREATE TABLE / DROP TABLE)
- `QueryRoot` - modifies or creates the query root (table name, CTE alias, schema/server qualifier)
- `QueryStructure` - modifies query structure (subqueries, pagination, ordering, grouping)
- `QueryCompilation` - affects query compilation or caching behavior (inlining, tagging, options)
- `JoinGraph` - modifies the join / association loading graph (`LoadWith`, `ThenLoad`)
- `SqlSemantics` - modifies SQL runtime semantics (table hints, lock types, query options)
- `CommandBuilder` - returns a fluent command builder that is not immediately executable
- `Data` - directly modifies stored data (bulk copy, non-query DML execution)
- `QueryResult` - determines the result set structure (scalar, typed sequence, raw reader)
- `ExecutionContext` - affects connection or transaction state
- `ConnectionConfiguration` - affects connection/provider configuration used to create execution contexts
- `Configuration` - affects configuration state (mapping schema, data options)
- `SchemaResult` - returns database schema information (tables, columns, procedures)
- `GeneratedSql` - returns generated SQL command text and parameters without executing the command

### `Pipeline`
The translation and execution stages involved in processing the call.
Flags combined with `|` (rendered comma-separated) when a call spans multiple stages.

- `ExpressionTree` - the LINQ Expression Tree analysis and transformation stage
- `SqlAST` - the SQL AST construction stage (internal SQL query model, before text generation)
- `SqlText` - the SQL text generation and execution stage
- `Connection` - connection/provider setup stage
- `Execution` - command execution stage
- `BulkInsert` - the native bulk insert pipeline (bypasses LINQ translation entirely)

Common combinations:
- `ExpressionTree,SqlAST,SqlText` - full LINQ translation pipeline (default for most LINQ APIs)
- `SqlAST,SqlText` - SQL AST stage only (e.g., inline hints applied after expression tree analysis)
- `SqlText` - direct SQL execution (no translation; raw SQL commands, transaction methods)

## Authoring rules

1. Keep one `[AiTags]` attribute per API member (the attribute is `AllowMultiple = false`).
2. Use a single `AiGroup` value for single-category tagging.
3. Combine several `AiGroup` values with `|` only when the API surface intentionally spans multiple categories.
4. Keep vocabulary stable; avoid introducing synonyms.
5. Extend the enum and the controlled values in this document together before using a new value in code.
6. If API semantics are multi-modal (e.g., provider-dependent execution structure), encode the dominant behavior and explain details in regular XML remarks.
7. Keep tags behavior-focused (execution/composability/semantic impact), not implementation-detail-focused.
8. Use `[AiTagsDefaults]` only for API surface-level defaults (for example on a class of extension methods), not for per-member semantics.
9. Treat `Pipeline=ExpressionTree,SqlAST,SqlText` as the default LinqToDB pipeline; prefer declaring it once in `[AiTagsDefaults]` for a surface and omit per-member repeats unless a member differs.
10. For raw SQL APIs (e.g., `SetCommand`/`CommandInfo`) use `Pipeline = AiPipeline.SqlText` - there is no Expression Tree or SQL AST stage; the caller provides SQL text directly.
11. For `BulkCopy` use `Pipeline = AiPipeline.BulkInsert` - the data transfer does not go through the LINQ translation pipeline at all.
12. `Affects` values name the primary artifact altered or produced by the API, not an internal processing phase.
13. For APIs in `Groups=Hints`, and MERGE hint overloads in `Groups=Merge`, include `HintType` so agents can distinguish table, join, query, subquery, MERGE, and scoped table hints without parsing method names.
14. For provider-specific typed hint helpers, keep the concrete SQL hint text in the member XML summary inside `<c>...</c>`; agents must inspect that summary before choosing, comparing, or rewriting hint helpers.
15. For T4-generated sources (for example `*Hints.generated.cs`), put the attribute in the `.tt` template, not in the generated file.

## Defaults merge rules

When both `[AiTagsDefaults]` on the declaring type and member-level `[AiTags]` exist:

1. Start from `[AiTagsDefaults]`.
2. Apply member-level `[AiTags]` on top.
3. For the same key, member-level value replaces the default value.
4. Keys absent in member-level `[AiTags]` are inherited from defaults.
5. If no defaults are present, member-level `[AiTags]` values are used as-is.

## Scope guidance

Prioritize tagging for:
- high-level public APIs (`DataExtensions`, `LinqExtensions`),
- APIs that switch query semantics,
- APIs where deferred vs immediate execution is easy to misinterpret,
- provider-sensitive APIs.

## Notes

`[AiTags]` complements XML documentation; it does not replace human-readable API docs.
Keep `<summary>` and `<remarks>` readable for humans. Generated `docs/api.md` exposes these
attributes as `AI metadata` for agent retrieval.
