# AI Tags for API Documentation

> You are here if you need to:
> - add or update `AI-Tags` metadata on a new or modified public API
> - verify that a key or value in an existing `AI-Tags` comment is valid
> - understand the canonical vocabulary for `AI-Tags` keys and values

`AI-Tags` are compact metadata annotations embedded in XML documentation (`<remarks>`) for public APIs.

They are intended for:
- LLM/agent tooling,
- semantic indexing,
- quick API behavior classification.

## Canonical format

Use a single-line key-value list:

`AI-Tags: Key1=Value1; Key2=Value2; ...;`

Optional defaults line for an API surface:

`AI-Tags-Defaults: Key1=Value1; Key2=Value2; ...;`

Example:

`AI-Tags: Group=DML; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`

Multi-group example (for aggregate docs like namespace/class overviews):

`AI-Tags: Groups=QueryDirectives,NavigationLoading,DML,Merge,Helpers; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`

Defaults example (applies to member tags in the same documented API surface unless overridden):

`AI-Tags-Defaults: Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`

## Standard keys

- `Group` - primary API category for a single API member.
- `Groups` - comma-separated API categories for aggregate documentation that covers multiple categories.
- `Execution` - when execution happens.
- `Composability` - whether API returns a composable query structure or is terminal.
- `Affects` - main semantic artifact affected by the call.
- `Pipeline` - affected translation/execution stages.
- `Provider` - provider dependency level.

`AI-Tags-Defaults` uses the same keys and controlled values as `AI-Tags`.

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
- `RawSQL` — raw SQL command execution (e.g., `SetCommand` / `CommandInfo` fluent builder pattern; no LINQ translation involved)
- `Schema` — database schema introspection (e.g., `ISchemaProvider.GetSchema`)

### `Execution`
- `Deferred`
- `Immediate`

### `Composability`
- `Composable`
- `Terminal`

### `Provider`
- `ProviderDefined`
- `ProviderAgnostic`

### `Affects`
The primary semantic artifact altered or produced by the call.
Compound values (comma-separated) are allowed when a single operation has primary effects on multiple artifacts
(e.g., `DdlStatement,QueryRoot` for a method that creates a table and returns `ITable<T>`).

- `DmlStatement` — generates a DML statement (INSERT / UPDATE / DELETE / MERGE)
- `DdlStatement` — generates a DDL statement (CREATE TABLE / DROP TABLE)
- `QueryRoot` — modifies or creates the query root (table name, CTE alias, schema/server qualifier)
- `QueryStructure` — modifies query structure (subqueries, pagination, ordering, grouping)
- `QueryCompilation` — affects query compilation or caching behavior (inlining, tagging, options)
- `JoinGraph` — modifies the join / association loading graph (`LoadWith`, `ThenLoadWith`)
- `SqlSemantics` — modifies SQL runtime semantics (table hints, lock types, query options)
- `CommandBuilder` — returns a fluent command builder that is not immediately executable
- `Data` — directly modifies stored data (bulk copy, non-query DML execution)
- `QueryResult` — determines the result set structure (scalar, typed sequence, raw reader)
- `ExecutionContext` — affects connection or transaction state
- `Configuration` — affects configuration state (mapping schema, data options)
- `SchemaResult` — returns database schema information (tables, columns, procedures)

### `Pipeline`
The translation and execution stages involved in processing the call.
Comma-separated when a call spans multiple stages.

- `ExpressionTree` — the LINQ Expression Tree analysis and transformation stage
- `SqlAST` — the SQL AST construction stage (internal SQL query model, before text generation)
- `SqlText` — the SQL text generation and execution stage
- `BulkInsert` — the native bulk insert pipeline (bypasses LINQ translation entirely)

Common combinations:
- `ExpressionTree,SqlAST,SqlText` — full LINQ translation pipeline (default for most LINQ APIs)
- `SqlAST,SqlText` — SQL AST stage only (e.g., inline hints applied after expression tree analysis)
- `SqlText` — direct SQL execution (no translation; raw SQL commands, transaction methods)

## Authoring rules

1. Keep one `AI-Tags` block per API member.
2. Use `Group` for single-category tagging.
3. Use `Groups` only when documentation intentionally spans multiple categories; encode values as a comma-separated list with no extra spaces.
4. Keep vocabulary stable; avoid introducing synonyms.
5. Prefer extending controlled values in this document before using new values in code.
6. If API semantics are multi-modal (e.g., provider-dependent execution structure), encode the dominant behavior and explain details in regular XML remarks.
7. Keep tags behavior-focused (execution/composability/semantic impact), not implementation-detail-focused.
8. Use `AI-Tags-Defaults` only for API surface-level defaults (for example class-level extension API docs), not for per-member semantics.
9. Treat `Pipeline=ExpressionTree,SqlAST,SqlText` as the default LinqToDB pipeline; prefer declaring it once in `AI-Tags-Defaults` for a surface and omit per-member repeats unless a member differs.
10. For raw SQL APIs (e.g., `SetCommand`/`CommandInfo`) use `Pipeline=SqlText` — there is no Expression Tree or SQL AST stage; the caller provides SQL text directly.
11. For `BulkCopy` use `Pipeline=BulkInsert` — the data transfer does not go through the LINQ translation pipeline at all.

## Defaults merge rules

When both `AI-Tags-Defaults` and member-level `AI-Tags` exist:

1. Start from `AI-Tags-Defaults`.
2. Apply member-level `AI-Tags` on top.
3. For the same key, member-level value replaces the default value.
4. Keys absent in member-level `AI-Tags` are inherited from defaults.
5. If no defaults are present, member-level `AI-Tags` are used as-is.

## Scope guidance

Prioritize tagging for:
- high-level public APIs (`DataExtensions`, `LinqExtensions`),
- APIs that switch query semantics,
- APIs where deferred vs immediate execution is easy to misinterpret,
- provider-sensitive APIs.

## Notes

`AI-Tags` complement XML documentation; they do not replace human-readable API docs.
