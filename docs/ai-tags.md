# AI Tags for API Documentation

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
- `TableConfiguration`

### `Execution`
- `Deferred`
- `Immediate`

### `Composability`
- `Composable`
- `Terminal`

### `Provider`
- `ProviderDefined`
- `ProviderAgnostic`

## Authoring rules

1. Keep one `AI-Tags` block per API member.
2. Use `Group` for single-category tagging.
3. Use `Groups` only when documentation intentionally spans multiple categories; encode values as a comma-separated list with no extra spaces.
4. Keep vocabulary stable; avoid introducing synonyms.
5. Prefer extending controlled values in this document before using new values in code.
6. If API semantics are multi-modal (e.g., provider-dependent execution structure), encode the dominant behavior and explain details in regular XML remarks.
7. Keep tags behavior-focused (execution/composability/semantic impact), not implementation-detail-focused.
8. Use `AI-Tags-Defaults` only for API surface-level defaults (for example class-level extension API docs), not for per-member semantics.

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
